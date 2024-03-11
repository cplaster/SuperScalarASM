using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SuperScalarASM
{
    /// <summary>
    /// A parser that converts human-readable assembly text into a list of 'Instruction' objects.
    /// </summary>
    public class AssemblyParser
    {
        public List<Instruction> Program = new List<Instruction>();
        private string? _current_contents;
        private string? _current_input;
        private string? _current_file;
        private Match? _current_match;

        /// <summary>
        /// Abort with an error message.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public void Error(string message)
        {
            var tab_width = 8;
            var consumed = _current_contents.Length - _current_input.Length;
            var consumed_lines = _current_contents[..consumed].Split('\n');
            var line_num = consumed_lines.Length;
            var col_num = consumed_lines.Last().Length;
            var remaining_line = _current_input.Split("\n")[0];
            Console.WriteLine($"error: {message}");
            Console.WriteLine($"{_current_file}:{line_num}:{col_num + 1}");
            Console.WriteLine($"  {consumed_lines.Last()}{remaining_line}");
            int tab_count = consumed_lines.Last().Split("\t").Length - 1;
            var dashes = new String('-', (col_num - 1) + (tab_width * tab_count));
            Console.WriteLine($"{dashes}^");
            Environment.Exit(0);
            // exit here
        }

        /// <summary>
        /// Reads a file and parses it.
        /// </summary>
        /// <param name="file"></param>
        public void ParseFile(string file)
        {
            _current_file = file;
            _current_input = File.ReadAllText(file);
            _current_contents = _current_input;
            ParseProgram();
            _current_file = null;
            _current_input = null;
            _current_contents = null;
        }

        /// <summary>
        /// Parses an entire program
        /// </summary>
        public void ParseProgram()
        {
            Skip();
            while (_current_input.Length > 0)
            {
                var inst = ParseInstruction();
                Program.Add(inst);
                //Program.Append(inst);
                //Console.WriteLine(inst.ToString());
            }
        }

        /// <summary>
        /// Parse an instruction
        /// </summary>
        /// <returns>Newly created Instruction object</returns>
        public Instruction? ParseInstruction()
        {
            // Actual instructions

            if (ConsumeIdentifier("nop"))
            {
                return new Instruction { OpCode = OpCode.NOP };
            }

            if (ConsumeIdentifier("ldi"))
            {
                var rd = ParseRegister();
                ParseRegex(",");
                var imm = ParseImmediate();
                return new Instruction { OpCode = OpCode.LDI, Operands = new List<Operand> { rd, imm } };
            }

            if (ConsumeIdentifier("mov"))
            {
                var rd = ParseRegister();
                ParseRegex(",");
                var rs = ParseRegister();
                return new Instruction { OpCode = OpCode.MOV, Operands = new List<Operand> { rd, rs } };
            }

            if (ConsumeIdentifier("jabsr"))
            {
                var rs16 = ParseRegisterPair();
                return new Instruction { OpCode = OpCode.JABSR, Operands = new List<Operand> { rs16 } };
            }

            if (ConsumeIdentifier("jreli"))
            {
                var imm = ParseImmediate();
                return new Instruction { OpCode = OpCode.JRELI, Operands = new List<Operand> { imm } };
            }

            if (ConsumeIdentifier("jrelr"))
            {
                var rs = ParseRegister();
                return new Instruction { OpCode = OpCode.JRELR, Operands = new List<Operand> { rs } };
            }

            if (ConsumeIdentifier("add"))
            {
                var rd = ParseRegister();
                ParseRegex(",");
                var rs = ParseRegister();
                return new Instruction { OpCode = OpCode.ADD, Operands = new List<Operand> { rd, rs } };
            }

            if (ConsumeIdentifier("sub"))
            {
                var rd = ParseRegister();
                ParseRegex(",");
                var rs = ParseRegister();
                return new Instruction { OpCode = OpCode.SUB, Operands = new List<Operand> { rd, rs } };
            }

            if (ConsumeIdentifier("addc"))
            {
                var rd = ParseRegister();
                ParseRegex(",");
                var rs = ParseRegister();
                return new Instruction { OpCode = OpCode.ADDC, Operands = new List<Operand> { rd, rs } };
            }

            if (ConsumeIdentifier("subc"))
            {
                var rd = ParseRegister();
                ParseRegex(",");
                var rs = ParseRegister();
                return new Instruction { OpCode = OpCode.SUBC, Operands = new List<Operand> { rd, rs } };
            }

            /*
            if (ConsumeIdentifier("addi"))
            {
                var rd = ParseRegister();
                ParseRegex(",");
                var imm = ParseImmediate();
                return new Instruction { OpCode = OpCode.ADDI, Operands = new List<Operand> { rd, imm } };
            }
            */

            // Pseudo instructions

            if (ConsumeIdentifier("halt"))
            {
                return new Instruction { OpCode = OpCode.HALT };
            }

            // Directives
            if (ConsumeIdentifier(@"\.org"))
            {
                var imm = ParseImmediate();
                return new Instruction { OpCode = OpCode.D_ORG, Operands = new List<Operand> { imm } };
            }

            if (ConsumeIdentifier(@"\w+:", false))
            {
                var name = _current_match.Value.Replace(":", "");
                var op = new Operand { Kind = OperandKind.Immediate, Value = name };
                return new Instruction { OpCode = OpCode.D_LABEL, Operands = new List<Operand> { op } };
            }


            Error("unknown instruction");
            return null;
        }

        public Operand ParseRegister()
        {
            var m = ParseRegex(@"r([0-6])\b", "expected a register");
            var idx = Convert.ToInt32(m.Captures[0].Value[1].ToString());
            return new Operand { Kind = OperandKind.Register, Value = idx };
        }

        public Operand ParseRegisterPair()
        {
            var m = ParseRegex(@"r([0-6])r([0-6])\b", "expected a 16 bit register pair");
            var temp = m.Value.Replace("r", "");
            var lo = Convert.ToInt32(temp[0].ToString());
            var hi = Convert.ToInt32(temp[1].ToString());
            if (hi != lo + 1)
            {
                Error($"registers in a 16 bit register pair must be consecutive; got {m.Captures[0]}");
            }
            return new Operand { Kind = OperandKind.RegisterPair, Value = lo };
        }

        public Operand? ParseImmediate()
        {
            var negative = false;
            int nbase = 10;
            string digits = @"[0-9]+\b";
            bool is_label = false;

            var m = ConsumeRegex(@"[+-]", false);
            if (m != null)
            {
                if (m.Captures[0].Value == "-") { negative = true; }
            }

            m = ConsumeRegex(@"0[xob]", false);
            if (m != null)
            {
                if (m.Captures[0].Value == "0x")
                {
                    nbase = 16;
                    digits = @"[0-9a-fA-F_]+\b";
                }
                if (m.Captures[0].Value == "0o")
                {
                    nbase = 8;
                    digits = @"[0-7_]+\b";
                }
                if (m.Captures[0].Value == "0b")
                {
                    nbase = 2;
                    digits = @"[01_]+\b";
                }
            }
            else
            {
                var i_dec = _current_input;
                var m_dec = ConsumeRegex(digits, false, false);
                var ii_dec = _current_input;
                if (m_dec == null)
                {
                    var reg = @"\w+";
                    var m_label = ConsumeRegex(reg, false, false);
                    if(m_label != null)
                    {
                        digits = reg;
                        is_label = true;
                    }
                }
            }

            var value = "";

            if (is_label)
            {
                var val = ParseRegex(digits, "expected a label");
                value = val.Value;
            }
            else
            {

                var val = ParseRegex(digits, $"expected base-{nbase} integer");
                var num = Convert.ToInt32(val.Captures[0].Value.Replace("_", ""), nbase);

                if (negative)
                {
                    num = -num;
                }

                value = num.ToString();
            }
            return new Operand { Kind = OperandKind.Immediate, Value = value };
        }

        public void Skip()
        {
            while (true)
            {
                // Skip whitespace
                if (ConsumeRegex(@"\s+", false) != null) { continue; }

                // Skip single-line comments, such as '#' and '//'
                if (ConsumeRegex(@"(#|//).*[\n$]", false) != null) { continue; }

                // Skip multi-line comments, appearing between '/*' and '*/'
                if (ConsumeRegex(@"(?s)/\*.*?\*/", false) != null) { continue; };

                break;
            }
        }

        public Match? ConsumeRegex(string regex, bool skip = true, bool consume = true)
        {
            //var r = new Regex(regex);
            var m = Regex.Match(_current_input, regex);
            var success = m.Success;
            //var matches = r.Matches(_current_input);

            if (m.Success && m.Captures[0].Index == 0)
            {
                if (consume)
                {
                    _current_input = _current_input[m.Captures[0].Length..];
                }
                if (skip)
                {
                    Skip();
                }
                return m;
            }
            return null;
        }

        public bool ConsumeIdentifier(string identifier, bool boundary = true)
        {
            var bound = @"";

            if(boundary)
            {
                bound = @"\b";
            }

            var m = ConsumeRegex($@"({identifier}{bound})");

            if (m != null)
            {
                _current_match = m;
                return true;
            }
            return false;
        }

        public Match? ParseRegex(string regex, string? error_message = null)
        {
            var m = ConsumeRegex(regex);
            if (m != null)
            {
                return m;
            }

            if (error_message != null)
            {
                Error(error_message);
            }
            else
            {
                Error($"expected {regex}");
            }
            return null;
        }

    }
}
