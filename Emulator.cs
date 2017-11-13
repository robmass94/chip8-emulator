using System;

class Emulator
{
    byte[] opcode;
    byte[] memory;
    byte[] graphics;
    byte[] V;
    byte delayTimer;
    byte soundTimer;
    byte key;
    ushort I;
    ushort PC;
    ushort[] stack;
    ushort SP;

    public Emulator()
    {
        opcode = new byte[2];
        memory = new byte[4096];
        graphics = new byte[2048];
        V = new byte[16];
        stack = new ushort[16];

        PC = 0x200;
        I = 0;
        SP = 0;
    }

    public void Start(string romName)
    {
   
    }
}

public class EmulatorMain
{ 
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("ERROR: Must specify ROM file!");
            Environment.Exit(-1);
        }

        Emulator emulator = new Emulator();
        emulator.Start(args[0]);
    }
}