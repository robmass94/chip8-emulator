using System;
using System.IO;

namespace Emulator 
{
    class OpcodeData
    {
        public ushort Opcode { get; set; } // full opcode
        public ushort NNN { get; set; } // last three nibbles
        public byte X { get; set; } // second nibble
        public byte Y { get; set; } // third nibble
        public byte NN { get; set; } // last byte
        public byte N { get; set; } // last nibble
    }

    class Emulator
    {
        private byte[] memory;
        private bool[,] graphics;
        private const int SCREEN_WIDTH = 64;
        private const int SCREEN_HEIGHT = 32;
        private byte[] V; // general purpose registers V) through VE, carry register VF
        private byte delayTimer;
        private byte soundTimer;
        private byte key;
        private ushort I; // index register
        private ushort PC; // program counter
        private ushort[] stack;
        private ushort SP; // stack pointer

        private byte[] fontSet = 
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
            graphics = new bool[SCREEN_WIDTH, SCREEN_HEIGHT];
            V = new byte[16];
            stack = new ushort[16];

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
                ushort fetchedOpcode = (ushort)(memory[PC++] << 8 | memory[PC++]);
                OpcodeData opcode = new OpcodeData
                {
                    Opcode = fetchedOpcode,
                    NNN = (ushort)(fetchedOpcode & 0x0FFF),
                    X = (byte)((fetchedOpcode & 0x0F00) >> 8),
                    Y = (byte)((fetchedOpcode & 0x00F0) >> 4),
                    NN = (byte)(fetchedOpcode & 0x00FF),
                    N = (byte)(fetchedOpcode & 0x000F)
                };

                Console.WriteLine($"{opcode.Opcode:X}");
                DecodeAndExecute(opcode);
            }
        }

        private void DecodeAndExecute(OpcodeData opcode)
        {
            // evaluate first nibble (four bits)
            switch (opcode.Opcode & 0xF000)
            {
                case 0x0:
                    // further evaluate second byte to determine instruction
                    switch (opcode.NN)
                    {
                        case 0x0E0:
                            // 00EO - clear the screen
                            for (int x = 0; x < SCREEN_WIDTH; ++x)
                            {
                                for (int y = 0; y < SCREEN_HEIGHT; ++y)
                                {
                                    graphics[x, y] = false;
                                }
                            }
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
                case 0x1000:
                    // 1NNN - jump to address NNN
                    PC = opcode.NNN;
                    break;
                case 0x2000:
                    // 2NNN - call subroutine at address NNN
                    stack[SP++] = PC;
                    PC = opcode.NNN;
                    break;
                case 0x3000:
                    // 3XNN - skip next instruction if VX equals NN
                    if (V[opcode.X] == opcode.NN)
                    {
                        PC += 2;
                    }
                    break;
                case 0x4000:
                    // 4XNN - skip next instruction if VX does not equal NN
                    if (V[opcode.X] != opcode.NN)
                    {
                        PC += 2;
                    }
                    break;
                case 0x5000:
                    // 5XY0 - skip next instruction if VX equals VY
                    if (V[opcode.X] == V[opcode.Y])
                    {
                        PC += 2;
                    }
                    break;
                case 0x6000:
                    // 6XNN - set VX to NN
                    V[opcode.X] = opcode.NN;
                    break;
                case 0x7000:
                    // 7XNN - add NN to VX
                    V[opcode.X] += opcode.NN;
                    break;
                case 0x8000:
                    // further evaluate last nibble to determine instruction
                    switch (opcode.N)
                    {
                        case 0x0:
                            // 8XY0 - set VX to VY
                            V[opcode.X] = V[opcode.Y];
                            break;
                        case 0x1:
                            // 8XY1 - set VX to VX OR VY
                            V[opcode.X] |= V[opcode.Y];
                            break;
                        case 0x2:
                            // 8XY2 - set VX to VX AND VY
                            V[opcode.X] &= V[opcode.Y];
                            break;
                        case 0x3:
                            // 8XY3 - set VX to VX XOR VY
                            V[opcode.X] ^= V[opcode.Y];
                            break;
                        case 0x4:
                            // 8XY4 - add VY to VX;
                            // Set VF to 1 if there's a carry, 0 if not
                            V[0xF] = (byte)((V[opcode.X] > 255 - V[opcode.Y]) ? 1 : 0);
                            V[opcode.X] += V[opcode.Y];
                            break;
                        case 0x5:
                            // 8XY5 - subtract VY from VX;
                            // set VF to 0 if there's a borrow, 1 if not
                            V[0xF] = (byte)((V[opcode.Y] > V[opcode.X]) ? 0 : 1);
                            V[opcode.X] -= V[opcode.Y];
                            break;
                        case 0x6:
                            // 8XY6 - shift VY right by one and copy result into VX;
                            // set VF to least significant bit of VY before shift
                            V[0xF] = (byte)(V[opcode.Y] & 1);
                            V[opcode.X] = V[opcode.Y] = (byte)(V[opcode.Y] >> 1);
                            break;
                        case 0x7:
                            // 8XY7 - set VX to VY - VX; set VF to 0 if there's a borrow, 1 if not
                            V[0xF] = (byte)((V[opcode.X] > V[opcode.Y]) ? 0 : 1);
                            V[opcode.X] = (byte)(V[opcode.Y] - V[opcode.X]);
                            break;
                        case 0xE:
                            // 8XYE - shift VY left by one and copy result into VX;
                            // set VF to most significant bit of VY before shift
                            V[0xF] = (byte)(V[opcode.Y] & 0x80);
                            V[opcode.X] = V[opcode.Y] = (byte)(V[opcode.Y] << 1);
                            break;
                        default:
                            throw new Exception("Invalid instruction!");
                    }
                    break;
                case 0x9000:
                    // 0x9XY0 - skip next instruction if VX != VY
                    if (V[opcode.X] != V[opcode.Y])
                    {
                        PC += 2;
                    }
                    break;
                case 0xA000:
                    // ANNN - set index register to address NNN
                    I = opcode.NNN;
                    break;
                case 0xB000:
                    // BNNN - jump to address NNN + V0
                    PC = (ushort)(opcode.NNN + V[0]);
                    break;
                case 0xC000:
                    // CXNN - set VX to result of NN AND (random number from 0 to 255)
                    V[opcode.X] = (byte)(opcode.NN & new Random().Next(256));
                    break;
                case 0xD000:
                    // DXYN - draws a sprite at coordinate (VX, VY) that has a width of 8 pixels and height of N pixels;
                    // each row of 8 pixels is read as bit-coded starting from memory location I;
                    // VF is set to 1 if any screen pixels are flipped from set to unset when sprite is drawn, 0 if not
                    break;
                case 0xE000:
                    // further evaluate last byte to determine instruction
                    switch (opcode.NN)
                    {
                        case 0x9E:
                            // EX9E - skip next instruction if key stored in VX is pressed
                            break;
                        case 0xA1:
                            // EXA1 - skip next instruction if key stored in VX isn't pressed
                            break;
                        default:
                            throw new Exception("Invalid instruction!");
                    }
                    break;
                case 0xF000:
                    // further evaluate last byte to determine instruction
                    switch (opcode.NN)
                    {
                        case 0x07:
                            // FX07 - set VX to value of delay timer
                            V[opcode.X] = delayTimer;
                            break;
                        case 0x0A:
                            // FX0A - await key press, then store in VX
                            break;
                        case 0x15:
                            // FX15 - set delay timer to VX
                            delayTimer = V[opcode.X];
                            break;
                        case 0x18:
                            // FX18 - set sound timer to VX
                            soundTimer = V[opcode.X];
                            break;
                        case 0x1E:
                            // FX1E - add VX to I
                            I += V[opcode.X];
                            break;
                        case 0x29:
                            // FX29 - set I to the location of the sprite for the character in VX
                            break;
                        case 0x33:
                            // FX33 - store the binary-coded decimal representation of VX, with the most signficant of three digits at the address in I,
                            // the middle digit at I + 1, and the least significant digit at I + 2
                            memory[I] = (byte)(V[opcode.X] / 100);
                            memory[I + 1] = (byte)((V[opcode.X] % 100) / 10);
                            memory[I + 2] = (byte)(V[opcode.X] % 10);
                            break;
                        case 0x55:
                            // FX55 - store V0 to VX (inclusive) in memory starting at address I
                            for (int i = 0; i <= (opcode.X); ++i)
                            {
                                memory[I + i] = V[i];
                            }
                            break;
                        case 0x65:
                            // FX65 - fill V0 to VX (inclusive) with values from memory starting at address I
                            for (int i = 0; i <= (opcode.X); ++i)
                            {
                                V[i] = memory[I + i];
                            }
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

    public class EmulatorMain
    { 
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("ERROR: Must specify ROM file!");
                Environment.Exit(-1);
            }

            new Emulator(args[0]).Start();
        }
    }
}