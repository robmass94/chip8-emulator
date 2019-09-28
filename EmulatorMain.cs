using System;

namespace Emulator
{
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