namespace Emulator
{
    class OpcodeData
    {
        public ushort FullOpcode { get; set; } // full opcode
        public ushort NNN { get; set; } // last three nibbles
        public byte X { get; set; } // second nibble
        public byte Y { get; set; } // third nibble
        public byte NN { get; set; } // last byte
        public byte N { get; set; } // last nibble
    }
}