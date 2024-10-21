/**
 * DisassemblyWriter.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using static DSO.Constants.Disassembly;

namespace DSO.Disassembler
{
	public class DisassemblyWriter
	{
		public readonly List<string> Stream = [];
		public uint Address { get; set; } = 0;
		public FunctionInstruction? Function { get; set; } = null;

		public void Write(params string[] tokens)
		{
			foreach (var token in tokens)
			{
				Stream.Add(token);
			}
		}

		public void Write(Instruction instruction)
		{
			Address = instruction.Address;

			if (instruction.Address >= Function?.EndAddress)
			{
				WriteLine(Address, $"; End of {(Function.Namespace == null ? "" : $"{Function.Namespace}::")}{Function.Name}()\n\n");
				Function = null;
			}

			if (instruction is FunctionInstruction function)
			{
				Function = function;
				WriteLine(Address, $"; Start of {(Function.Namespace == null ? "" : $"{Function.Namespace}::")}{Function.Name}()");
			}

			instruction.Visit(this);
		}

		public void WriteValue(object token, string comment = "")
		{
			if (token is string str)
			{
				str = Util.String.EscapeString(str);

				if (str.Length > VALUE_TRUNCATE_LENGTH)
				{
					str = $"\"{str[..VALUE_TRUNCATE_LENGTH]}\" <...>";
					comment += " (truncated)";
				}
				else
				{
					str = $"\"{str}\"";
				}

				token = str;
			}

			var stringToken = token switch
			{
				string => $"{token}",
				char ch => $"'{Util.String.EscapeChar(ch)}'",
				bool => $"({token.ToString().ToLower()})",
				null => "(null)",
				_ => token.ToString(),
			};

			WriteLine(Address++, stringToken, comment);
		}

		public void WriteBranchLabel(uint target) => WriteLine(Address, $"\naddr_{target:x8}:");
		public void WriteBranchTarget(uint target, string comment = "") => WriteLine(Address++, $"[addr_{target:x8}]", comment);
		public void WriteAddressValue(uint address, string comment = "") => WriteLine(Address++, $"0x{address:X8}", comment);

		public void WriteLine(uint address, string token, string comment = "")
		{
			Write(string.Format($"        {{0,-8:X8}}    {{1,-{VALUE_COLUMN_LENGTH}}}", address, token));

			if (comment != "")
			{
				Write($"    ; {comment}");
			}

			Write("\n");
		}
	}
}
