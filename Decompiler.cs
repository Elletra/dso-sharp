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
using DSO.AST.Nodes;
using DSO.ControlFlow;
using DSO.Disassembler;
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

	public class Decompiler
	{
		private CommandLineOptions _options;

		public void Decompile(CommandLineOptions options)
		{
			if (options.Paths.Count <= 0)
			{
				return;
			}

			_options = options;

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

					if (!DecompileFile(path))
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

					var result = DecompileDirectory(path);

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
			else if (failures < files)
			{
				Logger.LogWarning($"Decompiled {files - failures} of {files} file{(plural ? "s" : "")} in {totalTime} ms");
			}
			else
			{
				Logger.LogError($"Failed to decompile {(plural ? "all " : "")}{files} file{(plural ? "s" : "")}");
			}
		}

		private Tuple<int, int> DecompileDirectory(string path)
		{
			Logger.LogMessage($"Decompiling all files in directory: \"{path}\"");

			var files = Directory.GetFiles(path, $"*{EXTENSION}", SearchOption.AllDirectories);
			var failures = 0;

			foreach (var file in files)
			{
				if (!DecompileFile(file))
				{
					failures++;
				}
			}

			return new(files.Length, failures);
		}

		private bool DecompileFile(string path)
		{
			Logger.LogMessage($"Decompiling file: \"{path}\"");

			if (_options.GameIdentifier != GameIdentifier.Auto)
			{
				return DecompileFile(path, _options.GameIdentifier, silentError: false);
			}

			var version = FileLoader.ReadFileVersion(path);
			var identifiers = GameVersion.GetIdentifiersFromVersion(version);

			if (identifiers.Length <= 0)
			{
				Logger.LogError($"Could not automatically identify game from file");

				return false;
			}

			if (identifiers.Length == 1)
			{
				Logger.LogMessage($"\tGame automatically detected as {GameVersion.GetDisplayName(identifiers[0])}", ConsoleColor.DarkGray);

				return DecompileFile(path, identifiers[0], silentError: false);
			}

			Logger.LogWarning($"Multiple games use file version {version}!");

			for (var i = 0; i < identifiers.Length; i++)
			{
				var ident = identifiers[i];

				Logger.LogMessage($"\tAttempting with settings: \"{GameVersion.GetDisplayName(ident)}\"", ConsoleColor.DarkGray);

				if (DecompileFile(path, ident, silentError: i < identifiers.Length - 1))
				{
					return true;
				}
			}

			Logger.LogError("Failed to decompile after multiple attempts");

			return false;
		}

		private bool DecompileFile(string path, GameIdentifier identifier, bool silentError)
		{
			GameVersion game = new();
			FileData fileData;
			Disassembly disassembly;
			List<Node> nodes;

			try
			{
				game = GameVersion.Create(identifier);
				fileData = game.FileLoader.LoadFile(path);

				if (fileData.Version != game.Version)
				{
					Logger.LogWarning($"File version {fileData.Version} differs from expected version {game.Version}");
				}

				disassembly = new Disassembler.Disassembler().Disassemble(fileData, game.Ops);
				nodes = new Builder().Build(new ControlFlowAnalyzer().Analyze(disassembly), disassembly);
			}
			catch (Exception exception)
			{
				if (!silentError)
				{
					Logger.LogError(exception.Message);
				}

				game?.FileLoader?.Close();

				return false;
			}

			var scriptPath = $"{Directory.GetParent(path)}/{Path.GetFileNameWithoutExtension(path)}";

			Logger.LogMessage($"Writing output file: \"{scriptPath}\"");

			WriteScriptFile(scriptPath, nodes);

			if (_options.OutputDisassembly)
			{
				var disassemblyPath = $"{Directory.GetParent(path)}/{Path.GetFileNameWithoutExtension(path)}{DISASM_EXTENSION}";

				Logger.LogMessage($"Writing disassembly file: \"{disassemblyPath}\"");

				WriteDisassemblyFile(disassemblyPath, game, fileData, disassembly);
			}

			return true;
		}

		private bool WriteScriptFile(string outputPath, List<Node> nodes)
		{
			var success = false;

			try
			{
				File.WriteAllText(outputPath, string.Join("", new CodeGenerator.CodeGenerator().Generate(nodes)));
				success = true;
			}
			catch (Exception exception)
			{
				Logger.LogError(exception.Message);
			}

			return success;
		}

		private bool WriteDisassemblyFile(string outputPath, GameVersion game, FileData fileData, Disassembly disassembly)
		{
			var success = false;

			try
			{
				var writer = new DisassemblyWriter();

				writer.WriteHeader(game, fileData);
				disassembly.Visit(writer);

				File.WriteAllText(outputPath, string.Join("", writer.Stream));

				success = true;
			}
			catch (Exception exception)
			{
				Logger.LogError(exception.Message);
			}

			return success;
		}
	}
}
