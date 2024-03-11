using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperScalarASM
{
    public class Layouter
    {
        int current_address = 0;

        public void LayoutProgram(List<Instruction> instructions)
        {
            foreach(var instruction in instructions) 
            {
                LayoutInstruction(instruction);
            }
        }

        public void LayoutInstruction(Instruction instruction)
        {
            if(instruction.OpCode == OpCode.D_ORG)
            {
                var org_address = Convert.ToInt32(instruction.Operands[0].Value);
                if (current_address > org_address)
                {
                    Util.Error($"org directive address 0x{org_address.ToString("X4")} is behind current address 0x{current_address.ToString("X4")}", instruction);
                }
                else {
                    current_address = org_address;
                    instruction.Address = org_address;
                    return;
                }
            }

            instruction.Address = current_address;
            current_address += 2;
        }
    }
}
