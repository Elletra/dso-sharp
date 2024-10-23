/**
 * DisassemblyWriter.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using DSO.Loader;
using DSO.Versions;
using static DSO.Constants.Decompiler;
using static DSO.Constants.Disassembler;

namespace DSO.Disassembler
{
	public class DisassemblyWriter
	{
		public List<string> Stream { get; set; } = [];
		public uint Address { get; set; } = 0;
		public FunctionInstruction? Function { get; set; } = null;

		public void WriteHeader(GameVersion game, FileData data)
		{
			WriteCommentLine("========================================================================");
			WriteCommentLine("");
			WriteCommentLine($" This file was automatically generated with DSO Sharp ({VERSION})");
			WriteCommentLine("");
			WriteCommentLine("========================================================================");

			game.Visit(this);
			data.Visit(this);

			WriteCommentLine("========================================================================");
			Write("\n");
		}

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
				WriteLine(Address, $"; End of `{(Function.Namespace == null ? "" : $"{Function.Namespace}::")}{Function.Name}()`\n");
			}

			instruction.Visit(this);
		}

		public void WriteValue(object token, string comment = "", bool indent = true)
		{
			if (token is StringTableEntry entry)
			{
				var str = Util.String.EscapeString(entry.Value);

				if (str.Length > VALUE_TRUNCATE_LENGTH)
				{
					str = $"\"{str[..VALUE_TRUNCATE_LENGTH]}\" <...>";
					comment += " (truncated)";
				}
				else
				{
					str = $"\"{str}\"";
				}

				comment += $" ({(entry.Global ? "global" : "function")} table index: {entry.Index})";
				token = str;
			}

			var stringToken = token switch
			{
				string str => str,
				char ch => $"'{Util.String.EscapeChar(ch)}'",
				bool => $"({token.ToString().ToLower()})",
				null => "(null)",
				_ => token.ToString(),
			};

			WriteLine(Address++, stringToken, comment, indent);
		}

		public void WriteBranchLabel(uint target)
		{
			WriteLine(Address, "", "", indent: false);
			WriteLine(Address, $" addr_{target:x8}:", "", indent: false);
		}

		public void WriteBranchTarget(uint target, string comment = "") => WriteLine(Address++, $"[addr_{target:x8}]", comment);
		public void WriteAddressValue(uint address, string comment = "") => WriteLine(Address++, $"0x{address:X8}", comment);
		public void WriteCommentLine(string comment, bool writeAddress = false, bool indent = false)
		{
			if (writeAddress)
			{
				Write(string.Format("{0,-8:X8}", Address));
			}

			Write(string.Format("{0}; {1}\n", indent ? "        " : (writeAddress ? " " : ""), comment));
		}

		public void WriteLine(uint address, string token = "", string comment = "", bool indent = true)
		{
			if (token == "")
			{
				Write(string.Format($"{{0,-8:X8}}{{1}}", address, indent ? "        " : ""));
			}
			else
			{
				Write(string.Format($"{{0,-8:X8}}{{1}}{{2,-{VALUE_COLUMN_LENGTH}}}", address, indent ? "        " : "", token));
			}

			if (comment != "")
			{
				Write($"    ; {comment}");
			}

			Write("\n");
		}
	}
}
