using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperScalarASM
{
    public class AssemblyPrinter
    {
        public required List<Instruction> Program;
        private string address = "????";
        private bool emit_address = false;
        private string? output = "";

        public string Print()
        {
            foreach(var instruction in Program)
            {
                if(instruction.Address != null)
                {
                    emit_address = true;
                    break;
                }
            }

            foreach(var instruction in Program)
            {
                PrintInstruction(instruction);
                Emit("\n");
            }

            string s = output;
            output = null;
            return s;
        }

        public void PrintInstruction(Instruction instruction)
        {
            if(emit_address && instruction.Address != null)
            {
                address = $"{instruction.Address.Value.ToString("X4")}";
            }

            Emit($"{address}:  ");

            // Actual instructions

            switch (instruction.OpCode)
            {
                case OpCode.NOP:
                    {
                        PrintOpcode("nop");
                        return;
                    }

                case OpCode.LDI:
                    {
                        PrintOpcode("ldi ");
                        PrintOperand(instruction.Operands[0]);
                        Emit(", ");
                        PrintOperand(instruction.Operands[1]);
                        return;
                    }

                case OpCode.MOV:
                    {
                        PrintOpcode("mov ");
                        PrintOperand(instruction.Operands[0]);
                        Emit(", ");
                        PrintOperand(instruction.Operands[1]);
                        return;
                    }

                case OpCode.JABSR:
                    {
                        PrintOpcode("jabsr ");
                        PrintOperand(instruction.Operands[0]);
                        return;
                    }

                case OpCode.JRELI:
                    {
                        PrintOpcode("jreli ");
                        PrintOperand(instruction.Operands[0], true);
                        return;
                    }

                case OpCode.JRELR:
                    {
                        PrintOpcode("jrelr ");
                        PrintOperand(instruction.Operands[0]);
                        return;
                    }

                case OpCode.ADD:
                    {
                        PrintOpcode("add ");
                        PrintOperand(instruction.Operands[0]);
                        Emit(", ");
                        PrintOperand(instruction.Operands[1]);
                        return;
                    }

                case OpCode.ADDI:
                    {
                        PrintOpcode("addi ");
                        PrintOperand(instruction.Operands[0]);
                        Emit(", ");
                        PrintOperand(instruction.Operands[1]);
                        return;
                    }

                case OpCode.SUB:
                    {
                        PrintOpcode("sub ");
                        PrintOperand(instruction.Operands[0]);
                        Emit(", ");
                        PrintOperand(instruction.Operands[1]);
                        return;
                    }

                case OpCode.ADDC:
                    {
                        PrintOpcode("addc ");
                        PrintOperand(instruction.Operands[0]);
                        Emit(", ");
                        PrintOperand(instruction.Operands[1]);
                        return;
                    }

                case OpCode.SUBC:
                    {
                        PrintOpcode("subc ");
                        PrintOperand(instruction.Operands[0]);
                        Emit(", ");
                        PrintOperand(instruction.Operands[1]);
                        return;
                    }



                // Pseudo-instructions
                case OpCode.HALT:
                    {
                        PrintOpcode("halt");
                        return;
                    }

                // Directives
                case OpCode.D_ORG:
                    {
                        PrintOpcode(".org ");
                        PrintOperand(instruction.Operands[0], false, true);
                        return;
                    }
            }

            Emit($"<{instruction}>");
        }

        public void PrintOpcode(string opcode)
        {
            Emit($"{opcode, -7}");
            
        }

        public void PrintOperand(Operand operand, bool hint_relative = false, bool hint_addr = false)
        {

            switch (operand.Kind)
            {
                case OperandKind.Immediate:
                    {
                        int val = Convert.ToInt32(operand.Value);
                        if (hint_addr)
                        {
                            Emit($"0x{val.ToString("X4")}");
                        }
                        else
                        {
                            if (val > 0 && hint_relative)
                            {
                                Emit($"+{val.ToString()}");
                            }
                            else
                            {
                                Emit($"{val.ToString()}");
                            }
                        }
                        break;
                    }

                case OperandKind.Register:
                    {
                        Emit($"r{operand.Value.ToString()}");
                        break;
                    }

                case OperandKind.RegisterPair:
                    {
                        var val = Convert.ToInt32(operand.Value);
                        Emit($"r{val.ToString()}r{(val + 1).ToString()}");
                        break;
                    }
            }
        }

        public void Emit(string text)
        {
            output += text;
        }
    }
}
