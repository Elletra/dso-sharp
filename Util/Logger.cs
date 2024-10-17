/**
 * Logger.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using static DSO.Constants.Decompiler;

namespace DSO.Util
{
    static public class Logger
	{
		static public bool Quiet = false;

		static public void LogError(string message, bool indented = false) => LogMessage($"{(indented ? "\t" : "")}[ERROR] {message}", ConsoleColor.DarkRed);
		static public void LogWarning(string message) => LogMessage($"[WARNING] {message}", ConsoleColor.Yellow);
		static public void LogSuccess(string message) => LogMessage($"[SUCCESS] {message}", ConsoleColor.Green);
		static public void LogMessage(string message, ConsoleColor textColor)
		{
			var prev = Console.ForegroundColor;

			Console.ForegroundColor = textColor;
			LogMessage(message);
			Console.ForegroundColor = prev;
		}

		static public void LogMessage(string message)
		{
			if (!Quiet)
			{
				Console.WriteLine(message);
			}
		}

		static public void LogMessage(string format, params object[] args)
		{
			if (!Quiet)
			{
				Console.WriteLine(format, args);
			}
		}

		static public void LogHeader()
		{
			LogMessage($"### DSO Sharp ({VERSION}) by {AUTHOR} ###\n", ConsoleColor.White);
		}
	}
}
