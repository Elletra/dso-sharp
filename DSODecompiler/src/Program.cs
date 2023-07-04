﻿using System;
using System.IO;

using DSODecompiler.Loader;
using DSODecompiler.Opcodes;
using DSODecompiler.Disassembly;
using DSODecompiler.ControlFlow;

namespace DSODecompiler
{
	class Program
	{
		static void Main (string[] args)
		{
			var fileName = "test";
			var loader = new FileLoader();
			var fileData = loader.LoadFile($"{fileName}.cs.dso", 210);
			var disassembler = new Disassembler(new OpFactory());
			var disassembly = disassembler.Disassemble(fileData);
			var graphs = new ControlFlowGraphBuilder().Build(disassembly);
			var analyzer = new StructureAnalyzer();

			StreamWriter writer = null;
			var writeGraph = true;
			var loopFinder = new LoopFinder();

			if (writeGraph)
			{
				writer = new($"{fileName}.dot");
				writer.WriteLine("digraph {");
			}

			foreach (var (_, graph) in graphs)
			{
				new DominanceCalculator().Calculate(graph);

				if (writeGraph)
				{
					writer.WriteLine($"\tsubgraph cluster_{graph.EntryPoint} {{");
				}

				foreach (ControlFlowNode node in graph.PreorderDFS())
				{
					if (writeGraph)
					{
						var label = "";

						if (loopFinder.IsLoopStart(node))
						{
							label += "(LOOP START)\n";
						}

						if (loopFinder.IsLoopEnd(node))
						{
							label += "(LOOP END)\n";
						}

						label += $"{node.Addr}\n{node.FirstInstruction}";

						if (node.Instructions.Count > 2)
						{
							label += "\n...";
						}

						if (node.Instructions.Count > 1)
						{
							label += $"\n{node.LastInstruction}";
						}

						label = label.Replace("\"", "\\\"");

						writer.WriteLine($"\t\tnode_{node.Addr} [label=\"{label}\"];\n");
					}
				}

				if (writeGraph)
				{
					writer.Flush();

					foreach (ControlFlowNode node in graph.GetNodes())
					{
						foreach (ControlFlowNode successor in node.Successors)
						{
							writer.WriteLine($"\t\tnode_{node.Addr} -> node_{successor.Addr};");
						}
					}
				}

				if (writeGraph)
				{
					writer.WriteLine("\t}");
					writer.Flush();
				}

				Console.WriteLine("========\n");

				var collapsed = analyzer.Analyze(graph);

				{ } // Only for debug breakpoint
			}

			if (writeGraph)
			{
				writer.WriteLine("}");
				writer.Flush();
				writer.Close();

				System.Diagnostics.Process process = new();

				process.StartInfo.FileName = "createGraph.bat";
				process.StartInfo.Arguments = $"{fileName}.dot";
				process.Start();
			}
		}
	}
}
