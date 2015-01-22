using System;
using System.Collections.Generic;

namespace PicoBlazeCompiler
{
	public class CodeObject
	{
		
		public List<string> tokens;
		public int lineNumber;
		
		public CodeObject(int lineNumber, string line)
		{
			
			char[] WHITESPACE = {' ', '\t'};
			
			// remove comments
			if (line.Contains(";"))
			{
				line = line.Substring(0, line.IndexOf(';'));
			}
			
			// split line
			this.tokens = new List<string>(line.Split(WHITESPACE));
			this.lineNumber = lineNumber;
			
			// remove empty tokens
			for (var i = 0; i < tokens.Count; ++i)
			{
				if (tokens[i] != "") continue;
				tokens.RemoveAt(i);
				--i;
			}
			
		}
		
	}
}

