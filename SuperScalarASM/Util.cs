using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperScalarASM
{
    public static class Util
    {
        public static void Error(string message, params object[] args)
        {
            Console.Error.WriteLine($"error: {message}\n");
            foreach (var arg in args)
            {
                if (arg is null)
                {
                    continue;
                }

                if (arg is Instruction)
                {

                }
                else
                {
                    Console.Write($"{arg}\n");
                }
            }

            Environment.Exit(0);
        }
    }
}
