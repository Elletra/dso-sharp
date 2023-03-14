using System;

using DSODecompiler.ControlFlow;
using DSODecompiler.ControlFlow.Structure;
using DSODecompiler.Disassembler;
using DSODecompiler.Loader;

namespace DSODecompiler
{
	class Program
	{
		static void Main (string[] args)
		{
			var loader = new FileLoader();
			var fileData = loader.LoadFile("allGameScripts.cs.dso", 210);
			var disassembler = new Disassembler.Disassembler();
			var disassembly = disassembler.Disassemble(fileData);

			/*foreach (var instruction in disassembly)
			{
				Console.WriteLine("{0,8}    {1}", instruction.Addr, instruction);
			}*/

			/*Console.WriteLine($"\n==== Branches: {disassembly.NumBranches} ====\n");

			foreach (var branch in disassembly.GetBranches())
			{
				Console.Write(branch);

				if (disassembly.HasInstruction(branch.Source))
				{
					var insn = disassembly.GetInstruction(branch.Source) as BranchInstruction;

					Console.WriteLine($" {insn.IsUnconditional}");
				}
			}*/

			var controlFlowData = new ControlFlowData(disassembly);
			var totalLoops = 0;

			foreach (var graph in controlFlowData.ControlFlowGraphs)
			{
				if (graph.IsFunction)
				{
					Console.WriteLine(graph.FunctionHeader);
				}
				else
				{
					Console.WriteLine($"\nBLOCK at {graph.EntryPoint.Addr}:");

					/*foreach (var node in graph.PreorderDFS())
					{
						foreach (var instruction in node.Instructions)
						{
							Console.WriteLine($"    {instruction}");
							break;
						}

						break;
					}*/

					Console.Write("\n");
				}

				Console.Write("Num loops: ");

				var count = controlFlowData.DominatorGraphs[graph].FindLoops().Count;
				totalLoops += count;

				Console.Write(count);
				Console.Write("\n\n");
			}

			Console.WriteLine($"\n## Total Loops: {totalLoops}");
		}
	}
}
