﻿/**
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

namespace DSO.Util
{
	public class CommandLineOptions
	{
		public readonly List<string> Paths = [];

		public GameIdentifier GameIdentifier { get; set; } = GameIdentifier.Auto;
		public bool Quiet { get; set; } = false;
		public bool OutputDisassembly { get; set; } = false;
		public bool CommandLineMode { get; set; } = false;
	}

	static public class CommandLineParser
	{
		static private readonly Dictionary<string, GameIdentifier> _gameIdentifiers = new()
		{
			{ "auto", GameIdentifier.Auto },
			{ "tge14", GameIdentifier.TorqueGameEngine14 },
			{ "tfd", GameIdentifier.ForgettableDungeon },
			{ "blbeta", GameIdentifier.BlocklandBeta },
			{ "blv1", GameIdentifier.BlocklandV1 },
			{ "blv20", GameIdentifier.BlocklandV20 },
			{ "blv21", GameIdentifier.BlocklandV21 },
		};

		static public Tuple<bool, CommandLineOptions> Parse(string[] args)
		{
			var firstFlagSet = false;
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
					case "--help" or "-h":
						DisplayHelp();
						error = true;
						break;

					case "--cli" or "-X":
						options.CommandLineMode = true;
						break;

					case "--quiet" or "-q":
						options.Quiet = true;
						break;

					case "--disassembly" or "-d":
						options.OutputDisassembly = true;
						break;

					case "--game" or "-g":
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
							Logger.LogError($"Unknown or unsupported flag '{arg}'");

							if (arg == "-H" || arg == "-Q" || arg == "-G")
							{
								Logger.LogError($"Did you mean '{arg.ToLower()}'?");
							}
							else if (arg == "-x")
							{
								Logger.LogError($"Did you mean '{arg.ToUpper()}'?");
							}

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

			if (!error && options.Paths.Count <= 0)
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
				"usage: dso-sharp path1[, path2[, ...]] [-h] [-q] [-g game_version] [-d] [-X]\n" +
				"  options:\n" +
				"    -h, --help           Displays help.\n" +
				"    -q, --quiet          Disables all messages (except command-line argument errors).\n" +
				"    -g, --game           Specifies which game settings to use (default: 'auto').\n" +
				"    -d, --disassembly    Writes a `" + DISASM_EXTENSION + "` file containg the disassembly.\n" +
				"    -X, --cli            Makes the program operate as a command-line interface\n" +
				"                         that takes no keyboard input and closes immediately\n" +
				"                         upon completion or failure.\n"
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
