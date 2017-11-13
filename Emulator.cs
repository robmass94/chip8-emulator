using System;
using System.IO;

class Emulator
{
    private byte[] memory;
    private byte[] graphics;
    private byte[] V; // general purpose registers V) through VE, carry register VF
    private byte delayTimer;
    private byte soundTimer;
    private byte key;

    private ushort opcode;
    private ushort I; // index register
    private ushort PC; // program counter
    private ushort[] stack;
    private ushort SP; // stack pointer

    private static byte[] fontSet = 
    {
        0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
        0x20, 0x60, 0x20, 0x20, 0x70, // 1
        0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
        0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
        0x90, 0x90, 0xF0, 0x10, 0x10, // 4
        0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
        0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
        0xF0, 0x10, 0x20, 0x40, 0x40, // 7
        0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
        0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
        0xF0, 0x90, 0xF0, 0x90, 0x90, // A
        0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
        0xF0, 0x80, 0x80, 0x80, 0xF0, // C
        0xE0, 0x90, 0x90, 0x90, 0xE0, // D
        0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
        0xF0, 0x80, 0xF0, 0x80, 0x80  // F
    };

    public Emulator(string pathToROM)
    {
        memory = new byte[4096];
        graphics = new byte[2048];
        V = new byte[16];
        stack = new ushort[16];

        opcode = 0;
        I = 0;
        PC = 0x200;
        SP = 0;

        fontSet.CopyTo(memory, 0);
        File.ReadAllBytes(pathToROM).CopyTo(memory, 512);
    }

    public void Start()
    {
        while (true)
        {
            // fetch opcode
            opcode = (ushort)(memory[PC++] << 8);
            opcode |= memory[PC++];

            // decode/execute opcode
            switch (opcode & 0xF000)
            {
                case 0x0:
                    switch (opcode & 0x0FFF)
                    {
                        case 0x0E0:
                            // 00EO - clear the screen
                            break;
                        case 0x0EE:
                            // 00EE - return from subroutine
                            PC = stack[--SP];
                            break;
                        default:
                            // 0NNN - not necessary for most ROMs
                            break;
                    }
                    break;
                case 0x1:
                    // 1NNN - jump to address NNN
                    PC = (ushort)(opcode & 0x0FFF);
                    break;
                case 0x2:
                    // 2NNN - call subroutine at address NNN
                    stack[SP++] = PC;
                    PC = (ushort)(opcode & 0x0FFF);
                    break;
                case 0x3:
                    // 3XNN - skip next instruction if VX equals NN
                    if (V[opcode & 0x0F00] == (opcode & 0x00FF))
                    {
                        PC += 2;
                    }
                    break;
                case 0x4:
                    // 4XNN - skip next instruction if VX does not equal NN
                    if (V[opcode & 0x0F00] != (opcode & 0x00FF))
                    {
                        PC += 2;
                    }
                    break;
                case 0x5:
                    // 5XY0 - skip next instruction if VX equals VY
                    if (V[opcode & 0x0F00] == V[opcode & 0x00F0])
                    {
                        PC += 2;
                    }
                    break;
                case 0x6:
                    // 6XNN - set VX to NN
                    V[opcode & 0x0F00] = (byte)(opcode & 0x00FF);
                    break;
                case 0x7:
                    // 7XNN - add NN to VX
                    V[opcode & 0x0F00] += (byte)(opcode & 0x00FF);
                    break;
                case 0x8:
                    switch (opcode & 0x000F)
                    {
                        case 0x0:
                            // 8XY0 - set VX to VY
                            V[opcode & 0x0F00] = V[opcode & 0x00F0];
                            break;
                        case 0x1:
                            // 8XY1 - set VX to VX OR VY
                            V[opcode & 0x0F00] |= V[opcode & 0x00F0];
                            break;
                        case 0x2:
                            // 8XY2 - set VX to VX AND VY
                            V[opcode & 0x0F00] &= V[opcode & 0x00F0];
                            break;
                        case 0x3:
                            // 8XY3 - set VX to VX XOR VY
                            V[opcode & 0x0F00] ^= V[opcode & 0x00F0];
                            break;
                        case 0x4:
                            // 8XY4 - add VY to VX;
                            // Set VF to 1 if there's a carry, 0 if not
                            V[0xF] = (V[opcode & 0x0F00] > 255 - V[opcode & 0x00F0]) ? 1 : 0;
                            V[opcode & 0x0F00] += V[opcode & 0x00F0];
                            break;
                        case 0x5:
                            // 8XY5 - subtract VY from VX;
                            // set VF to 0 if there's a borrow, 1 if not
                            V[0xF] = (V[opcode & 0x00F0] > V[opcode & 0x0F00]) ? 0 : 1;
                            V[opcode & 0x0F00] -= V[opcode & 0x00F0];
                            break;
                        case 0x6:
                            // 8XY6 - shift VY right by one and copy result into VX;
                            // set VF to least significant bit of VY before shift
                            V[0xF] = V[opcode & 0x00F0] & 1;
                            V[opcode & 0x0F00] = V[opcode & 0x00F0] = V[0x00F0] >> 1;
                            break;
                        case 0x7:
                            // 8XY7 - set VX to VY - VX; set VF to 0 if there's a borrow, 1 if not
                            V[0xF] = (V[opcode & 0x0F00] > V[opcode & 0x00F0]) ? 0 : 1;
                            V[opcode & 0x0F00] = V[opcode & 0x00F0] - V[opcode & 0x0F00];
                            break;
                        case 0xE:
                            // 8XYE - shift VY left by one and copy result into VX;
                            // set VF to most significant bit of VY before shift
                            V[0xF] = V[opcode & 0x00F0] & 0x80;
                            V[opcode & 0x0F00] = V[opcode & 0x00F0] = V[opcode & 0x00F0] << 1;
                            break;
                        default:
                            throw new Exception("Invalid instruction!");
                    }
                    break;
                case 0x9:
                    // 0x9XY0 - skip next instruction if VX != VY
                    if (V[opcode & 0x0F00] != V[opcode & 0x00F0])
                    {
                        PC += 2;
                    }
                    break;
                case 0xA:
                    // ANNN - set index register to address NNN
                    I = opcode & 0x0FFF;
                    break;
                case 0xB:
                    // BNNN - jump to address NNN + V0
                    PC = (opcode & 0x0FFF) + V[0];
                    break;
                case 0xC:
                    // CXNN - set VX to result of NN AND (random number from 0 to 255)
                    V[opcode & 0x0F00] = (opcode & 0x00FF) & new Random().Next(256);
                    break;
                case 0xD:
                    break;
                case 0xE:
                    switch (opcode & 0x00F0)
                    {
                        case 0x9:
                            // EX9E - skip next instruction if key stored in VX is pressed
                            Console.
                            break;
                        case 0xA:
                            // EXA1 - skip next instruction if key stored in VX isn't pressed
                            break;
                        default:
                            throw new Exception("Invalid instruction!");
                    }
                    break;
                case 0xF:
                    switch (opcode & 0x00FF)
                    {
                        case 0x07:
                            break;
                        case 0x0A:
                            break;
                        case 0x15:
                            break;
                        case 0x18:
                            break;
                        case 0x1E:
                            break;
                        case 0x29:
                            break;
                        case 0x33:
                            break;
                        case 0x55:
                            break;
                        case 0x65:
                            break;
                        default:
                            throw new Exception("Invalid instruction!");
                    }
                    break;
                default:
                    throw new Exception("Invalid instruction!");
            }
        }
    }
}

public class EmulatorMain
{ 
    public static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("ERROR: Must specify ROM file!");
            Environment.Exit(-1);
        }

        Emulator emulator = new Emulator(args[0]);
        emulator.Start();
    }
}