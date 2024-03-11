using System.Net.WebSockets;
using System.Text;

namespace SuperScalarASM
{
    public class Assembler
    {
        public required List<Instruction> Program;
        private List<byte> binary = new();
        private int? currentAddress = 0;

        internal void Assemble()
        {
            foreach(var inst in Program)
            {
                int hi = 0;
                int lo = 0;
                bool skip = false;

                switch(inst.OpCode)
                {

                    // Actual instructions
                    case OpCode.NOP:
                        lo = 0;
                        hi = 0;

                        break;

                    case OpCode.LDI:
                        // FIXME: this is broken
                        // 
                        lo = 0x08;
                        lo = lo | (Convert.ToInt32(inst.Operands[0].Value) + 1) << 4;
                        //hi = Convert.ToByte(inst.Operands[1].Value);
                        if (((string)inst.Operands[1].Value).Contains("-"))
                        {
                            hi = Convert.ToSByte(inst.Operands[1].Value);
                        } else
                        {
                            hi = Convert.ToByte(inst.Operands[1].Value);
                        }

                        break;

                    case OpCode.MOV:
                        lo = 0x00;
                        lo = lo | (Convert.ToInt32(inst.Operands[0].Value) + 1) << 4;
                        hi = Convert.ToByte(inst.Operands[1].Value) + 1;
                        break;

                    case OpCode.JABSR:
                        lo = 0x02;
                        hi = Convert.ToByte(inst.Operands[0].Value) + 1;
                        break;

                    case OpCode.JRELI:
                        lo = 0x09;

                        hi = Convert.ToInt32(inst.Operands[0].Value);
                        break;

                    case OpCode.JRELR:
                        lo = 0x01;
                        hi = Convert.ToByte(inst.Operands[0].Value) + 1;
                        break;

                    case OpCode.ADD:
                        lo = 0x04;
                        lo = lo | (Convert.ToInt32(inst.Operands[0].Value) + 1) << 4;
                        hi = Convert.ToByte(inst.Operands[1].Value) + 1;
                        break;

                    case OpCode.ADDI:
                        // FIXME: this is broken? (from LDI)
                        // 
                        lo = 0x0C;
                        lo = lo | (Convert.ToInt32(inst.Operands[0].Value) + 1) << 4;
                        hi = Convert.ToByte(inst.Operands[1].Value);

                        break;

                    case OpCode.SUB:
                        lo = 0x04;
                        hi = 0x10;
                        lo = lo | (Convert.ToInt32(inst.Operands[0].Value) + 1) << 4;
                        hi = hi | Convert.ToByte(inst.Operands[1].Value) + 1;
                        break;

                    case OpCode.ADDC:
                        lo = 0x04;
                        hi = 0x20;
                        lo = lo | (Convert.ToInt32(inst.Operands[0].Value) + 1) << 4;
                        hi = hi | Convert.ToByte(inst.Operands[1].Value) + 1;
                        break;

                    case OpCode.SUBC:
                        lo = 0x04;
                        hi = 0x30;
                        lo = lo | (Convert.ToInt32(inst.Operands[0].Value) + 1) << 4;
                        hi = hi | Convert.ToByte(inst.Operands[1].Value) + 1;
                        break;


                    // Pseudo instructions
                    case OpCode.HALT:
                        lo = 0x09;
                        break;

                    // Directives

                    case OpCode.D_ORG:

                        // FIXME: magic number 'currentAddress += 2' should really be 'currentAddress + instructionWidth'
                        // this will work for 2-byte instructions
                        lo = 0;
                        hi = 0;

                        while (inst.Address - 2 > currentAddress)
                        {
                            currentAddress += 2;
                            binary.Add(0);
                            binary.Add(0);
                        }
                        skip = true;
                        break;

                    case OpCode.D_LABEL: 
                        
                        break;
                }
                if (!skip)
                {
                    binary.Add((byte)lo);
                    binary.Add((byte)hi);
                }
                currentAddress = inst.Address;
            }
        }

        internal byte[] PrintBin()
        {
            return binary.ToArray();
        }

        internal string PrintHex()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("v2.0 raw");

            foreach(var @byte in binary)
            {
                sb.AppendLine(@byte.ToString("X2"));
            }

            return sb.ToString();
        }

        internal string PrintIl()
        {
            StringBuilder sb = new StringBuilder();
            int i = 0;
            bool even = true;
            foreach(var @byte in binary)
            {
                if (even)
                {
                    sb.Append(i.ToString("X4") + ": ");
                }

                sb.Append(@byte.ToString("X2"));

                if (!even)
                {
                    sb.Append("\n");
                    i += 2;
                }

                even = !even;
            }

            return sb.ToString();
        }
    }
}