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
    class Program
    {
        static void Main(string[] args)
        {

            double version = 0.31;
            // Read input file into list of strings

            // the following line is for visual studio debugging, comment out for running command line exe
            args = new string[] { "alu.s" };

            Console.WriteLine($"SuperScalar Assember version {version}");
            
            if (args.Length == 0)
            {
                Console.WriteLine("Error: No source file specified.");
                return;
            }

            Console.WriteLine($"Sourcefile: {args[0]}");
            string inputFile = args[0];
            inputFile = inputFile.Split(".")[0];
            //List<string> inputLines = File.ReadAllLines(inputFile).ToList();

            // Parse the input file.
            var parser = new AssemblyParser();
            parser.ParseFile(args[0]);

            // Compute the addresses of each instruction.
            var layouter = new Layouter();
            layouter.LayoutProgram(parser.Program);

            // Print the assembly.
            var printer = new AssemblyPrinter { Program = parser.Program };
            Console.Write(printer.Print());

            // Experimental assembler
            var asm = new Assembler { Program = parser.Program };
            asm.Assemble();
            var p = asm.PrintIl();
            File.WriteAllText($"{inputFile}.il", p);
            var h = asm.PrintHex();
            File.WriteAllText($"{inputFile}.hex", h);
            var b = asm.PrintBin();
            File.WriteAllBytes($"{inputFile}.bin", b);


        }
    }
}