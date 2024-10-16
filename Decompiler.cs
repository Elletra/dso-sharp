﻿using DSO.AST;
using DSO.ControlFlow;
using DSO.Loader;
using DSO.Util;

using static DSO.Constants.Decompiler;

namespace DSO
{
    public class Decompiler
	{
		private readonly FileLoader _loader = new();
		private readonly Disassembler.Disassembler _disassembler = new();
		private readonly ControlFlowAnalyzer _analyzer = new();
		private readonly Builder _builder = new();
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

					if (!DecompileFile(path, options.GameVersion))
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

					var result = DecompileDirectory(path, options.GameVersion);

					files += result.Item1;
					failures += result.Item2;
				}
			}

			var totalTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - startTime;

			Logger.LogMessage("");

			if (failures <= 0)
			{
				Logger.LogSuccess($"Decompiled {files} file{(files != 1 ? "s" : "")} successfully in {totalTime} ms\n");
			}
			else
			{
				if (failures < files)
				{
					Logger.LogWarning($"Decompiled {files - failures} of {files} file{(files != 1 ? "s" : "")} successfully in {totalTime} ms");
				}
				else
				{
					Logger.LogError($"Failed to decompile all {files} file{(files != 1 ? "s" : "")}");
				}
			}
		}

		private Tuple<int, int> DecompileDirectory(string path, GameVersionData game)
		{
			Logger.LogMessage($"Decompiling all files in directory: \"{path}\"");

			var files = Directory.GetFiles(path, $"*{EXTENSION}", SearchOption.AllDirectories);
			var failures = 0;

			foreach (var file in files)
			{
				if (!DecompileFile(file, game))
				{
					failures++;
				}
			}

			return new(files.Length, failures);
		}

		private bool DecompileFile(string path, GameVersionData game)
		{
			string outputPath;
			List<string> stream;

			Logger.LogMessage($"Decompiling file: \"{path}\"");

			try
			{
				var disassembly = _disassembler.Disassemble(_loader.LoadFile(path, game.Version), game.Ops);
				var data = _analyzer.Analyze(disassembly);
				var nodes = _builder.Build(data, disassembly);

				stream = _generator.Generate(nodes);
				outputPath = $"{Directory.GetParent(path)}/{Path.GetFileNameWithoutExtension(path)}";
			}
			catch (Exception exception)
			{
				Logger.LogError($"Error decompiling file: {exception.Message}");
				return false;
			}

			try
			{
				File.WriteAllText(outputPath, string.Join("", stream));
			}
			catch (Exception exception)
			{
				Logger.LogError($"Error writing output file: {exception.Message}");
				return false;
			}

			return true;
		}
	}
}