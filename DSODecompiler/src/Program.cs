using System;

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
			var loader = new FileLoader();
			var fileData = loader.LoadFile("init.cs.dso", 210);
			var disassembler = new Disassembler(new OpFactory());
			var disassembly = disassembler.Disassemble(fileData);
			var graphs = new ControlFlowGraphBuilder().Build(disassembly);

			foreach (var (_, graph) in graphs)
			{
				foreach (var node in graph.GetNodes())
				{
					Console.Write(node.Addr);

					if (node.Successors.Count > 0)
					{
						Console.Write("=>{");

						foreach (var succ in node.Successors)
						{
							Console.Write($" {succ.Key}");
						}

						Console.Write(" }");
					}

					Console.Write($"\n    First: {node.FirstInstruction}");
					Console.Write($"\n    Last: {node.LastInstruction}\n");
					Console.Write("\n");
				}

				Console.WriteLine("========\n");
			}
		}
	}
}
