/**
 * Program.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using DSO;
using DSO.Util;
using static DSO.Constants.Decompiler;

Console.Title = $"DSO Sharp ({VERSION})";

var (error, options) = CommandLineParser.Parse(args);
var exitImmediately = options.CommandLineMode;
var errorCode = error ? 1 : 0;

if (!error)
{
	Logger.Quiet = options.Quiet;

	if (!options.CommandLineMode)
	{
		Logger.LogHeader();
	}

	new Decompiler().Decompile(options);
}

if (!exitImmediately)
{
	// The input is "redirected" when the program is called from a terminal.
	var redirected = Console.IsInputRedirected;

	if (redirected)
	{
		Console.WriteLine("\nPress enter key to exit...\n");
	}
	else
	{
		Console.WriteLine("\nPress any key to exit...\n");
	}

	while (true)
	{
		if (redirected)
		{
			if (Console.In.Peek() >= 0)
			{
				break;
			}
		}
		else if (Console.KeyAvailable && Console.ReadKey(true).Key != ConsoleKey.None)
		{
			break;
		}
	}
}

return errorCode;
