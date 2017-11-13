using System;
using System.IO;

class Emulator
{
    private byte[] opcode;
    private byte[] memory;
    private byte[] graphics;
    private byte[] V;
    private byte delayTimer;
    private byte soundTimer;
    private byte key;
    private ushort I;
    private ushort PC;
    private ushort[] stack;
    private ushort SP;

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
        opcode = new byte[2];
        memory = new byte[4096];
        graphics = new byte[2048];
        V = new byte[16];
        stack = new ushort[16];

        PC = 0x200;
        I = 0;
        SP = 0;

        Array.Copy(fontSet, memory, fontSet.Length);
        byte[] program = File.ReadAllBytes(pathToROM);
        for (int i = 512; i < program.Length; ++i)
        {
            memory[i] = program[i];
        }
    }

    public void Start(string pathToROM)
    {

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
        emulator.Start(args[0]);
    }
}