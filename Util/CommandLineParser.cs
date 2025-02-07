/**
 * CommandLineParser.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using DSO.Versions;
using static DSO.Constants.Decompiler;
using static DSO.Util.CommandLineOptions;

namespace DSO.Util
{
	public class CommandLineOptions
	{
		public enum DisassemblyOutput
		{
			None,
			Disassembly,
			DisassemblyOnly,
		}

		public readonly List<string> Paths = [];

		public GameIdentifier GameIdentifier { get; set; } = GameIdentifier.Auto;
		public bool Quiet { get; set; } = false;
		public DisassemblyOutput OutputDisassembly { get; set; } = DisassemblyOutput.None;
		public bool CommandLineMode { get; set; } = false;
	}

	static public class CommandLineParser
	{
		static private readonly Dictionary<string, GameIdentifier> _gameIdentifiers = new()
		{
			{ "auto", GameIdentifier.Auto },
			{ "tge10", GameIdentifier.TGE10 },
			{ "tge14", GameIdentifier.TGE14 },
			{ "tcon", GameIdentifier.TCON },
			{ "t2", GameIdentifier.Tribes2 },
			{ "tfd", GameIdentifier.ForgettableDungeon },
			{ "blv1", GameIdentifier.BlocklandV1 },
			{ "blv20", GameIdentifier.BlocklandV20 },
			{ "blv21", GameIdentifier.BlocklandV21 },
            { "vside", GameIdentifier.VSIDE },
        };

		static public Tuple<bool, CommandLineOptions> Parse(string[] args)
		{
			var firstFlagSet = false;
			var unknownFlag = false;
			var options = new CommandLineOptions();
			var error = false;
			var paths = new List<string>();
			var gameSpecified = false;

			for (var i = 0; i < args.Length && !error; i++)
			{
				var arg = args[i];

				if (arg.StartsWith('-') && paths.Count > 0)
				{
					DisplayHelp();
					error = true;
					break;
				}

				switch (arg)
				{
					case "-h":
						DisplayHelp();
						error = true;
						break;

					case "-X":
						options.CommandLineMode = true;
						break;

					case "-q":
						options.Quiet = true;
						break;

					case "-d":
						if (options.OutputDisassembly == DisassemblyOutput.None)
						{
							options.OutputDisassembly = DisassemblyOutput.Disassembly;
						}

						break;

					case "-D":
						if (options.OutputDisassembly != DisassemblyOutput.DisassemblyOnly)
						{
							options.OutputDisassembly = DisassemblyOutput.DisassemblyOnly;
						}

						break;

					case "-g":
					{
						error = i >= args.Length - 1 || args[i + 1].StartsWith('-');

						if (error)
						{
							Logger.LogError($"Missing game after '{arg}'");
						}
						else if (gameSpecified)
						{
							Logger.LogError("Multiple games specified");
							error = true;
						}
						else
						{
							var game = args[i + 1];

							gameSpecified = true;

							if (!_gameIdentifiers.TryGetValue(game, out GameIdentifier identifier))
							{
								Logger.LogError($"Unsupported game '{game}'");
								DisplayVersions();
								error = true;
							}
							else
							{
								options.GameIdentifier = identifier;
							}

							i++;
						}

						break;
					}

					default:
					{
						if (!arg.StartsWith('-'))
						{
							if (firstFlagSet && options.Paths.Count > 0)
							{
								DisplayHelp();
								error = true;
							}
							else
							{
								options.Paths.Add(arg);
							}
						}
						else
						{
							Logger.LogError($"Unknown or unsupported flag '{arg}'\n");

							if (arg == "-H" || arg == "-Q" || arg == "-G")
							{
								Logger.LogError($"Did you mean '{arg.ToLower()}'?");
							}
							else if (arg == "-x")
							{
								Logger.LogError($"Did you mean '{arg.ToUpper()}'?");
							}

							unknownFlag = true;
							error = true;
						}

						break;
					}
				}

				if (arg.StartsWith('-') && !error && !firstFlagSet)
				{
					firstFlagSet = true;
				}
			}

			if (error)
			{
				if (unknownFlag)
				{
					DisplayHelp();
				}
			}
			else if (options.Paths.Count <= 0)
			{
				if (!options.Quiet && !options.CommandLineMode)
				{
					Logger.LogHeader();
					Logger.LogError("No file or directory path(s) specified\n");
				}

				DisplayHelp();

				error = true;
			}

			return new(error, options);
		}

		static private void DisplayHelp()
		{
			Logger.LogMessage(
				"usage: dso-sharp path1[, path2[, ...]] [-h] [-q] [-g game] [-d | -D] [-X]\n" +
				"  options:\n" +
				"    -h    Displays help.\n" +
				"    -q    Disables all messages (except command-line argument errors).\n" +
				"    -g    Specifies which game settings to use (default: 'auto').\n" +
				"    -d    Writes a `" + DISASM_EXTENSION + "` file containing the disassembly.\n" +
				"    -D    Writes only the disassembly file and nothing else.\n" +
				"    -X    Makes the program operate as a command-line interface that takes\n" +
				"          no keyboard input and closes immediately upon completion or failure.\n"
			);

			DisplayVersions();
		}

		static private void DisplayVersions()
		{
			Logger.LogMessage($"  supported games:");

			var longest = 0;

			foreach (var (arg, game) in _gameIdentifiers)
			{
				longest = Math.Max(arg.Length, longest);
			}

			foreach (var (arg, game) in _gameIdentifiers)
			{
				Logger.LogMessage($"    {{0,-{longest}}}    {{1}}", arg, GameVersion.GetDisplayName(game));
			}

			Logger.LogMessage("");
		}
	}
}
