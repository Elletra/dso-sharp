/**
 * Decompiler.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using DSO.AST;
using DSO.ControlFlow;
using DSO.Loader;
using DSO.Util;
using DSO.Versions;
using static DSO.Constants.Decompiler;

namespace DSO
{
	public class DecompilerException : Exception
	{
		public DecompilerException() { }
		public DecompilerException(string message) : base(message) { }
		public DecompilerException(string message, Exception inner) : base(message, inner) { }
	}

	/// <summary>
	/// This class is an abomination but I do not care anymore.
	/// </summary>
	public class Decompiler
	{
		private readonly CodeGenerator.CodeGenerator _generator = new();

		public void Decompile(CommandLineOptions options)
		{
			if (options.Paths.Count <= 0)
			{
				return;
			}

			var startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

			foreach (var path in options.Paths)
			{
				if (Path.HasExtension(path))
				{
					if (Path.GetExtension(path) != EXTENSION)
					{
						Logger.LogError($"File \"{path}\" does not have a `{EXTENSION}` extension");
						return;
					}

					if (!File.Exists(path))
					{
						Logger.LogError($"File \"{path}\" does not exist at the path specified");
						return;
					}
				}
				else if (!Directory.Exists(path))
				{
					Logger.LogError($"Directory \"{path}\" does not exist at the path specified");
					return;
				}
			}

			var files = 0;
			var failures = 0;

			foreach (var path in options.Paths)
			{
				if (Path.HasExtension(path))
				{
					files++;

					if (!DecompileFile(path, options.GameIdentifier))
					{
						failures++;
					}
				}
				else
				{
					if (files > 0)
					{
						Logger.LogMessage("");
					}

					var result = DecompileDirectory(path, options.GameIdentifier);

					files += result.Item1;
					failures += result.Item2;
				}
			}

			var totalTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - startTime;
			var plural = files != 1;

			Logger.LogMessage("");

			if (failures <= 0)
			{
				Logger.LogSuccess($"Decompiled {files} file{(plural ? "s" : "")} successfully in {totalTime} ms\n");
			}
			else
			{
				if (failures < files)
				{
					Logger.LogWarning($"Decompiled {files - failures} of {files} file{(plural ? "s" : "")} in {totalTime} ms");
				}
				else
				{
					Logger.LogError($"Failed to decompile {(plural ? "all " : "")}{files} file{(plural ? "s" : "")}");
				}
			}
		}

		private Tuple<int, int> DecompileDirectory(string path, GameIdentifier identifier)
		{
			Logger.LogMessage($"Decompiling all files in directory: \"{path}\"");

			var files = Directory.GetFiles(path, $"*{EXTENSION}", SearchOption.AllDirectories);
			var failures = 0;

			foreach (var file in files)
			{
				if (!DecompileFile(file, identifier))
				{
					failures++;
				}
			}

			return new(files.Length, failures);
		}

		private bool DecompileFile(string path, GameIdentifier identifier)
		{
			List<string> stream = [];

			Exception? decompileException = null;

			Logger.LogMessage($"Decompiling file: \"{path}\"");

			if (identifier != GameIdentifier.Auto)
			{
				decompileException = DisassembleAndParseFile(path, identifier, out stream);
			}
			else
			{
				var version = FileLoader.ReadFileVersion(path);
				var identifiers = GameVersion.GetIdentifiersFromVersion(version);

				if (identifiers.Length <= 0)
				{
					decompileException = new DecompilerException("Could not automatically identify game version");
				}

				if (identifiers.Length == 1)
				{
					Logger.LogMessage($"\tGame version automatically detected as {GameVersion.GetDisplayName(identifiers[0])}", ConsoleColor.DarkGray);

					decompileException = DisassembleAndParseFile(path, identifiers[0], out stream);
				}
				else
				{
					Logger.LogWarning($"Multiple games use file version {version}!");

					foreach (var ident in identifiers)
					{
						Logger.LogMessage($"\tAttempting with settings: \"{GameVersion.GetDisplayName(ident)}\"", ConsoleColor.DarkGray);

						decompileException = DisassembleAndParseFile(path, ident, out stream);

						if (decompileException == null)
						{
							break;
						}
					}
				}
			}

			if (decompileException != null)
			{
				Logger.LogError($"Error decompiling file: {decompileException.Message}");
				return false;
			}

			try
			{
				File.WriteAllText($"{Directory.GetParent(path)}/{Path.GetFileNameWithoutExtension(path)}", string.Join("", stream));
			}
			catch (Exception writeException)
			{
				Logger.LogError($"Error writing output file: {writeException.Message}");
				return false;
			}

			return true;
		}

		/// <summary>
		/// Returning exceptions from functions is really bad, but I have gone past the point of caring.
		/// </summary>
		private Exception? DisassembleAndParseFile(string path, GameIdentifier identifier, out List<string> tokens)
		{
			tokens = [];

			try
			{
				var game = GameVersion.Create(identifier);
				var disassembly = new Disassembler.Disassembler().Disassemble(game.FileLoader.LoadFile(path, game.Version), game.Ops);
				var data = new ControlFlowAnalyzer().Analyze(disassembly);
				var nodes = new Builder().Build(data, disassembly);

				tokens = _generator.Generate(nodes);

				return null;
			}
			catch (Exception exception)
			{
				return exception;
			}
		}
	}
}
