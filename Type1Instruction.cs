using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PicoBlazeCompiler
{
	// instruction with 1 register and 1 constant, or 2 registers (add, sub, and, or, xor, load, input, output, ...)
	class Type1Instruction
	{
		public string name;
		public int opcode;
		public Type1Instruction(string name, int opcode)
		{
			this.name = name;
			this.opcode = opcode;
		}
	}
}
