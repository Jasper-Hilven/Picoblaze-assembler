using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PicoBlazeCompiler
{
	public class Compiler
	{
	    public TestForm myForm;
		private string code, form;
		private List<CodeObject> codeObjects;
		private Dictionary<string, string> defines;
		private List<int> instructionWords;
		private string output;
	    private string stupidNotation;
		public Compiler(string code, string form)
		{
			const string begintemplate = "{begin template}\r\n";
			this.code = code;
		    this.form = form.Substring(form.IndexOf(begintemplate) + begintemplate.Length);
		}
		
		public string Compile(out string stupidNot)
		{
			CreateCodeObjects();
			ReadDefines();
			Assemble();
			ConvertToHex();
		    stupidNot = stupidNotation;
			return output;
		}
		
		private void CreateCodeObjects()
		{
			
			// split into lines
			code = code.Replace("\r", "");
			var lines = code.Split('\n');
			codeObjects = new List<CodeObject>();
			for(var i = 0; i < lines.Length; ++i)
			{
				codeObjects.Add(new CodeObject(i + 1, lines[i]));
			}
			
			// remove empty lines
			for (var i = 0; i < codeObjects.Count; ++i)
			{
				if (codeObjects[i].tokens.Count != 0) continue;
				codeObjects.RemoveAt(i);
				--i;
			}
			
		}
		
		private void ReadDefines()
		{
			
			// create empty dictionary
			defines = new Dictionary<string, string>();
			
			// read defines
			foreach (var obj in codeObjects)
			{
				if (obj.tokens[0].ToLower() == "#define")
				{
					if (obj.tokens.Count != 3)
					{
						throw new CompilerException(obj.lineNumber, "#define expects 2 arguments");
					}
					if (defines.ContainsKey(obj.tokens[1]))
					{
						throw new CompilerException(obj.lineNumber, "the definition '" + obj.tokens[1] + "' has already been defined");
					}
					defines.Add(obj.tokens[1], obj.tokens[2]);
				}
			}
			
			// read labels
			int instruction_counter = 0;
			for (var i = 0; i < codeObjects.Count; ++i)
			{
				var obj = codeObjects[i];
				string first = obj.tokens[0];
				if (first.EndsWith(":"))
				{
					string label = first.Substring(0, first.Length - 1);
					if (defines.ContainsKey(label))
					{
						throw new CompilerException(obj.lineNumber, "the definition '" + label + "' has already been defined");
					}
					defines.Add(label, instruction_counter.ToString());
					obj.tokens.RemoveAt(0);
					if (obj.tokens.Count == 0)
					{
						codeObjects.RemoveAt(i);
						--i;
						continue;
					}
					first = obj.tokens[0];
				}
				if (first.StartsWith("#")) continue;
				++instruction_counter;
			}
			
		}
		
		private void Assemble()
		{
			instructionWords = new List<int>();
			foreach (var obj in codeObjects)
			{
				AssembleCodeObject(obj);
			}
		}
		
		private void AssembleCodeObject(CodeObject obj)
		{
			#region Instructions
			Type1Instruction[] TYPE1INSTRUCTIONS = {
				new Type1Instruction("add", 12),
				new Type1Instruction("addcy", 13),
				new Type1Instruction("sub", 14),
				new Type1Instruction("subcy", 15),
				new Type1Instruction("compare", 10),
				new Type1Instruction("load", 0),
				new Type1Instruction("and", 5),
				new Type1Instruction("or", 6),
				new Type1Instruction("xor", 7),
				new Type1Instruction("test", 9),
				new Type1Instruction("store", 23),
				new Type1Instruction("fetch", 3),
				new Type1Instruction("input", 2),
				new Type1Instruction("output", 22),
			};
			Type2Instruction[] TYPE2INSTRUCTIONS = {
				new Type2Instruction("sr0", 14),
				new Type2Instruction("sr1", 15),
				new Type2Instruction("srx", 10),
				new Type2Instruction("sra", 8),
				new Type2Instruction("rr", 12),
				new Type2Instruction("sl0", 6),
				new Type2Instruction("sl1", 7),
				new Type2Instruction("slx", 4),
				new Type2Instruction("sla", 0),
				new Type2Instruction("rl", 2),
			};
			#endregion
			string first = obj.tokens[0].ToLower();

			if (first == "#address")
			{
				HandleAddress(obj);
				return;
			}

			if (first.StartsWith("#")) return;
			
			// type 1 instructions ...
			foreach (var instr in TYPE1INSTRUCTIONS)
			{
				if (first != instr.name) continue;
				if (obj.tokens.Count != 3)
				{
					throw new CompilerException(obj.lineNumber, "this instruction expects 2 arguments");
				}
				string arg1 = Resolve(obj.tokens[1]);
				int reg1 = GetRegisterIndex(arg1);
				if (reg1 == -1)  throw new CompilerException(obj.lineNumber, "invalid register name '" + arg1 + "'");
				string arg2 = Resolve(obj.tokens[2]);
				if (arg2[0] >= '0' && arg2[0] <= '9')
				{
					int kk;
					try
					{
						kk = ParseNumber(arg2);
					}
					catch (System.FormatException)
					{
						throw new CompilerException(obj.lineNumber, "invalid numeric constant '" + arg2 + "'");
					}
					if (kk < 0 || kk > 255)
					{
						throw new CompilerException(obj.lineNumber, "numeric constant should be in the range 0..255");
					}
					instructionWords.Add((instr.opcode << 13) | (0 << 12) | (reg1 << 8) | kk);
				}
				else
				{
					int reg2 = GetRegisterIndex(arg2);
					if (reg2 == -1)
					{
						throw new CompilerException(obj.lineNumber, "invalid register name '" + arg2 + "'");
					}
					instructionWords.Add((instr.opcode << 13) | (1 << 12) | (reg1 << 8) | (reg2 << 4));
				}
				return;
			}
			
			// type 2 instructions ...
			foreach (var instr in TYPE2INSTRUCTIONS)
			{
				if (first == instr.name)
				{
					if (obj.tokens.Count != 2)
					{
						throw new CompilerException(obj.lineNumber, "this instruction expects 1 argument");
					}
					string arg1 = Resolve(obj.tokens[1]);
					int reg1 = GetRegisterIndex(arg1);
					if (reg1 == -1)
					{
						throw new CompilerException(obj.lineNumber, "invalid register name '" + arg1 + "'");
					}
					instructionWords.Add((32 << 12) | (reg1 << 8) | instr.opcode);
					return;
				}
			}
			
			// jump and call instructions
			if (first == "jump" || first == "call")
			{
				int opcode = (first == "jump")? 26 : 24;
				int condition;
				string address;
				if (obj.tokens.Count == 2)
				{
					condition = 0;
					address = Resolve(obj.tokens[1]);
				}
				else if (obj.tokens.Count == 3)
				{
					condition = GetConditionIndex(obj.tokens[1]);
					if (condition == -1)
					{
						throw new CompilerException(obj.lineNumber, "invalid condition code");
					}
					address = Resolve(obj.tokens[2]);
				}
				else
				{
					throw new CompilerException(obj.lineNumber, "this instruction expects 1 or 2 arguments");
				}
				int aaa;
				try
				{
					aaa = ParseNumber(address);
				}
				catch (FormatException)
				{
					throw new CompilerException(obj.lineNumber, "invalid address '" + address + "'");
				}
				if (aaa < 0 || aaa > 1023)
				{
					throw new CompilerException(obj.lineNumber, "address should be in the range 0..1023");
				}
				instructionWords.Add((opcode << 13) | (condition << 10) | aaa);
				return;
			}
			
			// return instruction
			if (first == "return")
			{
				int condition;
				switch (obj.tokens.Count)
				{
					case 1:
						condition = 0;
						break;
					case 2:
						condition = GetConditionIndex(obj.tokens[1]);
						if (condition == -1)
						{
							throw new CompilerException(obj.lineNumber, "invalid condition code");
						}
						break;
					default:
						throw new CompilerException(obj.lineNumber, "this instruction expects 0 or 1 arguments");
				}
				instructionWords.Add((21 << 13) | (condition << 10));
				return;
			}
			
			// returni instruction
			if (first == "returni")
			{
				int enable;
				if (obj.tokens.Count != 2)
				{
					throw new CompilerException(obj.lineNumber, "this instruction expects 1 argument");
				}
				string second = obj.tokens[1].ToLower();
				if (second == "disable")
				{
					enable = 0;
				}
				if (second == "enable")
				{
					enable = 1;
				}
				else
				{
					throw new CompilerException(obj.lineNumber, "argument should be 'disable' or 'enable'");
				}
				instructionWords.Add((28 << 13) | enable);
				return;
			}
			
			// disable and enable instructions
			if (first == "disable" | first == "enable")
			{
				int enable = (first == "disable") ? 0 : 1;
				if (obj.tokens.Count != 2)
				{
					throw new CompilerException(obj.lineNumber, "this instruction expects 1 argument");
				}
				string second = obj.tokens[1].ToLower();
				if (second != "interrupt")
				{
					throw new CompilerException(obj.lineNumber, "argument should be 'interrupt'");
				}
				instructionWords.Add((30 << 13) | enable);
				return;
			}
			
			// TODO interrupt, via predefined interrupt label, verplaatsen code entiteiten naar aparte code.
			// ----
			// Volgens mij werken interrupts niet zo, het is een 'vectored interrupt'. Je bent verplicht een interrupt-instructie te zetten op adres 3FFh
			// m.a.w. een jump naar de echte interrupt. Een apart interrupt label zou niet veel zin hebben, dat kan je net zo goed zelf doen en zo wordt
			// het meestal ook gedaan.
			// Voorbeeld:
			//	 interrupt:
			//	 ; ... hier komt de interruptcode ...
			//	 returni enable
			// En onderaan het bestand:
			//	 #address 3FFh
			//	 jump interrupt
			throw new CompilerException(obj.lineNumber, "unknown instruction");
			
		}

		private void HandleAddress(CodeObject obj)
		{
			if (obj.tokens.Count != 2)
			{
				throw new CompilerException(obj.lineNumber, "#address expects 1 argument");
			}
			string address = obj.tokens[1];
			int aaa;
			try
			{
				aaa = ParseNumber(address);
			}
			catch (System.FormatException)
			{
				throw new CompilerException(obj.lineNumber, "invalid address '" + address + "'");
			}
			if (aaa < 0 || aaa > 1023)
			{
				throw new CompilerException(obj.lineNumber, "address should be in the range 0..1023");
			}
			if (instructionWords.Count > aaa)
			{
				throw new CompilerException(obj.lineNumber, "address has already been used");
			}
			while (instructionWords.Count < aaa)
			{
				instructionWords.Add(0);
			}
		}

		private void ConvertToHex()
		{
		    Boolean specialSize = false;
		    int normalTextSize = 1024;
			if(myForm != null && myForm.getTextbox6().Text != null && myForm.getTextbox6().Text != "")
			{
			    normalTextSize = int.Parse(myForm.getTextbox6().Text);
			    specialSize = true;
			}

		    // make sure there are exactly 1024 instructions
			var interruptImplemented = defines.ContainsKey("interrupt");
			int maxValue = normalTextSize;
			if (interruptImplemented)
				maxValue--;
			if (instructionWords.Count > maxValue)
			{
				throw new CompilerException(0, "not enough ROM to store the program");
			}
			
			while (instructionWords.Count < maxValue)
			{
				instructionWords.Add(0);
			}
			if (interruptImplemented)
			{
				string interruptAddress;
				defines.TryGetValue("interrupt", out interruptAddress);
				instructionWords.Add((26<<13) + ParseNumber(interruptAddress));
			    maxValue++;
			}

		    stupidNotation = "[";
		    for (int i = 0; i < maxValue; i++)
		    {
		        stupidNotation += "" + instructionWords[i];
                if (i < maxValue - 1)
                    stupidNotation += ",";
		    }
		    stupidNotation += "]";
			// convert to hexadecimal and insert into form
			output = form;
			int num1 = 0, num2 = 0;
			string hex1 = "", hex2 = "";
			for (var i = 0; i < normalTextSize; i += 2)
			{
				int word1 = instructionWords[i];
				int word2 = instructionWords[i + 1];
				int wordp = (word1 >> 16) | ((word2 >> 16) << 2);
				hex1 = HexChar((word2 >> 12) & 15).ToString()
					 + HexChar((word2 >> 8) & 15).ToString()
					 + HexChar((word2 >> 4) & 15).ToString()
					 + HexChar(word2 & 15).ToString()
					 + HexChar((word1 >> 12) & 15).ToString()
					 + HexChar((word1 >> 8) & 15).ToString()
					 + HexChar((word1 >> 4) & 15).ToString()
					 + HexChar(word1 & 15).ToString()
					 + hex1;
				hex2 = HexChar((word1 >> 16) | ((word2 >> 16) << 2)).ToString()
					 + hex2;
				if (hex1.Length == 64)
				{
					output = output.Replace("{INIT_" + HexChar(num1 >> 4) + HexChar(num1 & 15) + "}", hex1);
					hex1 = "";
					++num1;
				}
				if (hex2.Length == 64)
				{
					output = output.Replace("{INITP_" + HexChar(num2 >> 4) + HexChar(num2 & 15) + "}", hex2);
					hex2 = "";
					++num2;
				}
			}
			
		}
		
		private static int ParseNumber(string number)
		{
			return number.EndsWith("h") ? Convert.ToInt32(number.Substring(0, number.Length - 1), 16) : Convert.ToInt32(number);
		}

		private static char HexChar(int value)
		{
			if (value < 0 || value > 15) throw new ArgumentOutOfRangeException();
			return (char)((value < 10)? '0' + value : 'A' + (value - 10));
		}
		
		private string Resolve(string name)
		{
			for (var i = 0; i < 100; ++i)
			{
				string newname;
				if (defines.TryGetValue(name, out newname))
				{
					name = newname;
				}
				else
				{
					return name;
				}
			}
			throw new CompilerException(0, "can't resolve '" + name + "'");
		}
		
		private static int GetRegisterIndex(string name)
		{
			name = name.ToLower();
			for (var i = 0; i < 16; ++i)
			{
				if (name == "s" + i) return i;
			}
			return -1;
		}
		
		private static int GetConditionIndex(string preCode)
		{
			preCode = preCode.ToLower();
			if (preCode == "z") return 4;
			if (preCode == "nz") return 5;
			if (preCode == "c") return 6;
			if (preCode == "nc") return 7;
			return -1;
		}
		
	}
}

