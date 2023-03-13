using System;

using DSODecompiler.ControlFlow;
using DSODecompiler.Disassembler;
using DSODecompiler.Loader;

namespace DSODecompiler
{
	class Program
	{
		static void Main (string[] args)
		{
			var loader = new FileLoader();
			var fileData = loader.LoadFile("test.cs.dso", 210);
			var disassembler = new Disassembler.Disassembler();
			var disassembly = disassembler.Disassemble(fileData);

			foreach (var instruction in disassembly)
			{
				Console.WriteLine("{0,8}    {1}", instruction.Addr, instruction);
			}

			Console.WriteLine($"\n==== Branches: {disassembly.NumBranches} ====\n");

			foreach (var branch in disassembly.GetBranches())
			{
				Console.Write(branch);

				if (disassembly.HasInstruction(branch.Source))
				{
					var insn = disassembly.GetInstruction(branch.Source) as BranchInstruction;

					Console.WriteLine($" {insn.IsUnconditional}");
				}
			}

			Console.WriteLine("\n==== Control Flow Graph ====\n");

			var cfg = new ControlFlowGraphBuilder().Build(disassembly);

			foreach (var node in cfg.PreorderDFS())
			{
				Console.Write($"{node.Addr}    {node}=>{{");

				var count = node.Successors.Count;

				for (var i = 0; i < count; i++)
				{
					var successor = node.Successors[i];

					Console.Write(successor.Addr);

					if (i < count - 1)
					{
						Console.Write(", ");
					}
				}

				Console.Write($"}}    <{node.LastInstruction}>\n");
			}
		}
	}
}
