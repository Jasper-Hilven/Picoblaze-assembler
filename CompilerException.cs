using System;

namespace PicoBlazeCompiler
{
	public class CompilerException:Exception
	{
		public int line;
		public string message;
		public CompilerException(int line, string message)
		{
			this.line = line;
			this.message = message;
		}
	}
}

