/**
 * String.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

namespace DSO.Util
{
	static public class String
	{
		/// <summary>
		/// For escaping special characters.
		/// </summary>
		static private readonly Dictionary<string, string> _escapeCharMap = new()
		{
			{ "\\", "\\\\" },
			{ "\x00", "\\x00" },
			{ "\x01", "\\c0" },
			{ "\x02", "\\c1" },
			{ "\x03", "\\c2" },
			{ "\x04", "\\c3" },
			{ "\x05", "\\c4" },
			{ "\x06", "\\c5" },
			{ "\x07", "\\c6" },
			{ "\x08", "\\x08" },
			{ "\t", "\\t" },
			{ "\n", "\\n" },
			{ "\x0B", "\\c7" },
			{ "\x0C", "\\c8" },
			{ "\r", "\\r" },
			{ "\x0E", "\\c9" },
			{ "\x0F", "\\cr" },
			{ "\x10", "\\cp" },
			{ "\x11", "\\co" },
			{ "\x12", "\\x12" },
			{ "\x13", "\\x13" },
			{ "\x14", "\\x14" },
			{ "\x15", "\\x15" },
			{ "\x16", "\\x16" },
			{ "\x17", "\\x17" },
			{ "\x18", "\\x18" },
			{ "\x19", "\\x19" },
			{ "\x1A", "\\x1A" },
			{ "\x1B", "\\x1B" },
			{ "\x1C", "\\x1C" },
			{ "\x1D", "\\x1D" },
			{ "\x1E", "\\x1E" },
			{ "\x1F", "\\x1F" },
			{ "\x7F", "\\x7F" },
			{ "\xA0", "\\xA0" },
		};

		static public string EscapeChar(char ch) => EscapeString($"{ch}");
		static public string EscapeString(string str)
		{
			// Since tagged strings start with the 0x01 character, and \c0 maps to 0x01, any string
			// that starts with \c0 automatically gets a 0x02 character prepended to avoid errors.
			if (str.StartsWith("\x02\x01"))
			{
				str = str[1..];
			}

			foreach (var (find, replace) in _escapeCharMap)
			{
				str = str.Replace(find, replace);
			}

			return str;
		}
	}
}
