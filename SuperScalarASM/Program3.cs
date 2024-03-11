using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace SuperScalarASM
{
    class Program3
    {
        double version = 0.3;

        // The different kinds of operand we support
        enum OperandKind
        {
            Immediate,
            Register,
            RegisterPair
        }

        // An instruction operand, like a register or an immediate value
        class Operand
        {
            public OperandKind Kind;
            public object? Value;

            public override string ToString()
            {
                return $"{this.Kind.ToString()}:{this.Value.ToString()}";
            }
        }

        // An instruction opcode
        enum OpCode
        {
            // Actual instructions
            NOP,
            LDI,
            MOV,
            JABSR,
            JRELI,
            JRELR,

            // Pseudo instructions
            HALT,

            // Assembler Directives
            D_ORG
        }

        // An assembly instruction, represented by its opcode and list of operands
        class Instruction
        {
            public OpCode OpCode;
            public List<Operand> Operands;

            public override string ToString()
            {
                var s = this.OpCode.ToString();
                if (Operands != null)
                {
                    foreach (var op in Operands)
                    {
                        s += " " + op.ToString();
                    }
                }
                return s;
            }
        }

        // A parser that converts human-readable assembly text into a list of 'Instruction' objects
        class AssemblyParser
        {
            public List<Instruction> program = new List<Instruction>();
            private string? _current_contents;
            private string? _current_input;
            private string? _current_file;


            // Abort with an error message

            public void Error(string message)
            {
                var consumed = this._current_contents.Length - this._current_input.Length;
                var consumed_lines = this._current_contents[..consumed].Split("\n");
                var line_num = consumed_lines.Length;
                var col_num = consumed_lines.Last().Length;
                var remaining_line = this._current_input.Split("\n")[0];

                Console.WriteLine($"error: {message}");
                Console.WriteLine($"{this._current_file}:{line_num}:{col_num + 1}");
                Console.WriteLine($"  {consumed_lines.Last()}{remaining_line}");
                Console.WriteLine($"{'-' * col_num}^");
                // exit here
            }

            public void ParseFile(string file)
            {
                this._current_file = file;
                this._current_input = File.ReadAllText(file);
                this._current_contents = this._current_input;
                this.ParseProgram();
                this._current_file = null;
                this._current_input = null;
                this._current_contents = null;
            }

            public void ParseProgram()
            {
                this.Skip();
                while (this._current_input.Length > 0)
                {
                    var inst = this.ParseInstruction();
                    this.program.Append(inst);
                    Console.WriteLine(inst.ToString());
                }
            }

            public Instruction? ParseInstruction()
            {
                // Actual instructions

                if (this.ConsumeIdentifier("nop"))
                {
                    return new Instruction { OpCode = OpCode.NOP };
                }

                if (this.ConsumeIdentifier("ldi"))
                {
                    var rd = this.ParseRegister();
                    this.ParseRegex(",");
                    var imm = this.ParseImmediate();
                    return new Instruction { OpCode = OpCode.LDI, Operands = new List<Operand> { rd, imm } };
                }

                if (this.ConsumeIdentifier("mov"))
                {
                    var rd = this.ParseRegister();
                    this.ParseRegex(",");
                    var rs = this.ParseRegister();
                    return new Instruction { OpCode = OpCode.MOV, Operands = new List<Operand> { rd, rs } };
                }

                if (this.ConsumeIdentifier("jabsr"))
                {
                    var rs16 = this.ParseRegisterPair();
                    return new Instruction { OpCode = OpCode.JABSR, Operands = new List<Operand> { rs16 } };
                }

                if (this.ConsumeIdentifier("jreli"))
                {
                    var imm = this.ParseImmediate();
                    return new Instruction { OpCode = OpCode.JRELI, Operands = new List<Operand> { imm } };
                }

                if (this.ConsumeIdentifier("jrelr"))
                {
                    var rs = this.ParseRegister();
                    return new Instruction { OpCode = OpCode.JRELR, Operands = new List<Operand> { rs } };
                }

                // Pseudo instructions

                if (this.ConsumeIdentifier("halt"))
                {
                    return new Instruction { OpCode = OpCode.HALT };
                }

                // Directives
                if (this.ConsumeIdentifier(@"\.org"))
                {
                    var imm = this.ParseImmediate();
                    return new Instruction { OpCode = OpCode.D_ORG, Operands = new List<Operand> { imm } };
                }


                this.Error("unknown instruction");
                return null;
            }

            public Operand ParseRegister()
            {
                var m = this.ParseRegex(@"r([0-6])\b", "expected a register");
                var idx = Convert.ToInt32(m.Captures[0].Value[1].ToString());
                return new Operand { Kind = OperandKind.Register, Value = idx };
            }

            public Operand ParseRegisterPair()
            {
                var m = this.ParseRegex(@"r([0-6])r([0-6])\b", "expected a 16 bit register pair");
                var temp = m.Value.Replace("r", "");
                var lo = Convert.ToInt32(temp[0].ToString());
                var hi = Convert.ToInt32(temp[1].ToString());
                if (hi != lo + 1)
                {
                    this.Error($"registers in a 16 bit register pair must be consecutive; got {m.Captures[0]}");
                }
                return new Operand { Kind = OperandKind.RegisterPair, Value = lo };
            }

            public Operand? ParseImmediate()
            {
                var negative = false;
                int nbase = 10;
                string digits = @"[0-9]+\b";

                var m = this.ConsumeRegex(@"[+-]", false);
                if (m != null)
                {
                    if (m.Captures[0].Value == "-") { negative = true; }
                }

                m = this.ConsumeRegex(@"0[xob]", false);
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

                var val = this.ParseRegex(digits, $"expected base-{nbase} integer");
                var num = Convert.ToInt32(val.Captures[0].Value.Replace("_", ""), nbase);

                if (negative)
                {
                    num = -num;
                }

                return new Operand { Kind = OperandKind.Immediate, Value = num };
            }

            public void Skip()
            {
                while (true)
                {
                    // Skip whitespace
                    if (this.ConsumeRegex(@"\s+", false) != null) { continue; }

                    // Skip single-line comments, such as '#' and '//'
                    if (this.ConsumeRegex(@"(#|//).*[\n$]", false) != null) { continue; }

                    // Skip multi-line comments, appearing between '/*' and '*/'
                    if (this.ConsumeRegex(@"(?s)/\*.*\*/", false) != null) { continue; };

                    break;
                }
            }

            public Match? ConsumeRegex(string regex, bool skip = true)
            {
                //var r = new Regex(regex);
                var m = Regex.Match(this._current_input, regex);
                var success = m.Success;
                //var matches = r.Matches(this._current_input);

                if (m.Success && m.Captures[0].Index == 0)
                {
                    this._current_input = this._current_input[m.Captures[0].Length..];
                    if (skip)
                    {
                        this.Skip();
                    }
                    return m;
                }
                return null;
            }

            public bool ConsumeIdentifier(string identifier)
            {
                if (this.ConsumeRegex($@"({identifier})\b") != null)
                {
                    return true;
                }
                return false;
            }

            public Match? ParseRegex(string regex, string? error_message = null)
            {
                var m = this.ConsumeRegex(regex);
                if (m != null)
                {
                    return m;
                }

                if (error_message != null)
                {
                    this.Error(error_message);
                }
                else
                {
                    this.Error($"expected {regex}");
                }
                return null;
            }

        }



        static void Main(string[] args)
        {

            double version = 0.3;
            // Read input file into list of strings

            Console.WriteLine($"SuperScalar Assember version {version}");
            if (args.Length == 0)
            {
                Console.WriteLine("Error: No source file specified.");
                //    return;
            }
            //Console.WriteLine($"Sourcefile: {args[0]}");
            //string inputFile = args[0];
            //string name = inputFile.Split(".")[0];
            //List<string> inputLines = File.ReadAllLines(inputFile).ToList();
            var parser = new AssemblyParser();
            parser.ParseFile("second.s");
        }
    }
}
