using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperScalarASM
{
    
    /// <summary>
    /// The different kinds of operands we support.
    /// </summary>
    public enum OperandKind
    {
        Immediate,
        Register,
        RegisterPair
    }

    /// <summary>
    /// An instruction operand, like a register or an immediate value.
    /// </summary>
    public class Operand
    {
        public required OperandKind Kind;
        public required object Value;

        public override string ToString()
        {
            return $"{Kind.ToString()}:{Value.ToString()}";
        }

        public string ToString(string format)
        {
            var val = Convert.ToInt32(Value.ToString());
            return val.ToString("X4");
        }
    }

    /// <summary>
    /// An instruction opcode.
    /// </summary>
    public enum OpCode
    {
        // Actual instructions
        NOP,
        LDI,
        MOV,
        JABSR,
        JRELI,
        JRELR,
        ADD,
        ADDI,
        SUB,
        ADDC,
        SUBC,

        // Pseudo instructions
        HALT,

        // Assembler Directives
        D_ORG,
        D_LABEL
    }

    /// <summary>
    /// An assembly instruction, represented by its opcode and list of operands.
    /// </summary>
    public class Instruction
    {
        public required OpCode OpCode;
        public List<Operand> Operands = new();
        public int? Address = null;
        public List<byte> Bytes = new();

        public override string ToString()
        {
            var s = this.OpCode.ToString();

            foreach (var op in Operands)
            {
                s += " " + op.ToString();
            }
            return s;
        }
    }

}
