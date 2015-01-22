using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PicoBlazeCompiler
{
	// instruction with 1 register (shift, rotate)
	class Type2Instruction
	{
		public string name;
		public int opcode;
		public Type2Instruction(string name, int opcode)
		{
			this.name = name;
			this.opcode = opcode;
		}
	}
}
