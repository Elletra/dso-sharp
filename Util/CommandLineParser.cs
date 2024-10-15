using static DSO.Util.Constants.Decompiler.GameVersions;

namespace DSO.Util
{
	public enum GameIdentifier : uint
	{
		Unknown,
		ForgettableDungeon,
		TorqueGameEngine14,
		BlocklandV20,
		BlocklandV21,
	};

	public class GameVersionData(string displayName, GameIdentifier identifier, uint version)
	{
		public readonly string DisplayName = displayName;
		public readonly GameIdentifier Identifier = identifier;
		public readonly uint Version = version;

		public GameVersionData() : this("", GameIdentifier.Unknown, 0) { }
		public GameVersionData(GameVersionData data) : this(data.DisplayName, data.Identifier, data.Version) { }
	}

	public class CommandLineOptions
	{
		public readonly List<string> Paths = [];

		public GameVersionData? GameVersion { get; set; } = null;
		public bool Quiet { get; set; } = false;
		public bool CommandLineMode { get; set; } = false;
	}

	static public class CommandLineParser
	{
		static private readonly Dictionary<string, GameVersionData> _gameVersions = new()
		{
			{ "blv20", new("Blockland v20", GameIdentifier.BlocklandV20, BLV20) },
			{ "blv21", new("Blockland v21", GameIdentifier.BlocklandV21, BLV21) },
			{ "tfd", new("  The Forgettable Dungeon", GameIdentifier.ForgettableDungeon, TFD) },
			{ "tge14", new("Torque Game Engine 1.4", GameIdentifier.TorqueGameEngine14, TGE14) },
		};

		static public Tuple<bool, CommandLineOptions> Parse(string[] args)
		{
			var firstFlagSet = false;
			var options = new CommandLineOptions();
			var error = false;
			var paths = new List<string>();

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

					case "--game" or "-g":
					{
						error = i >= args.Length - 1 || args[i + 1].StartsWith('-');

						if (error)
						{
							Logger.LogError($"Missing game version after '{arg}'");
						}
						else if (options.GameVersion != null)
						{
							Logger.LogError("Multiple game versions specified");
							error = true;
						}
						else
						{
							var version = args[i + 1];

							if (!_gameVersions.TryGetValue(version, out GameVersionData? data))
							{
								Logger.LogError($"Unsupported game version '{version}'");
								DisplayVersions();
								error = true;
							}
							else
							{
								options.GameVersion = data;
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
				Logger.LogHeader();
				Logger.LogError("No file or directory path(s) specified");

				error = true;
			}

			return new(error, options);
		}

		static private void DisplayHelp()
		{
			Logger.LogMessage(
				"usage: dso-sharp path1[, path2[, ...]] [-h] [-q] [-g game_version] [-X]\n" +
				"  options:\n" +
				"    -h, --help     Displays help.\n" +
				"    -q, --quiet    Disables all messages (except command-line argument errors).\n" +
				"    -g, --game     Specifies which game's scripts we are decompiling.\n" +
				"    -X, --cli      Makes the program operate as a command-line interface\n" +
				"                   that takes no keyboard input and closes immediately\n" +
				"                   upon completion or failure.\n"
			);

			DisplayVersions();
		}

		static private void DisplayVersions()
		{
			Logger.LogMessage($"  game versions:");

			foreach (var (arg, game) in _gameVersions)
			{
				Logger.LogMessage($"    {arg}    {game.DisplayName}");
			}

			Logger.LogMessage("");
		}
	}
}
