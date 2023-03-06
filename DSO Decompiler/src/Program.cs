using System;

using DSODecompiler.Loader;
using DSODecompiler.Disassembler;
using DSODecompiler.ControlFlow;

namespace DSODecompiler
{
	class Program
	{
		static void Main (string[] args)
		{
			var loader = new FileLoader();
			var fileData = loader.LoadFile("init.cs.dso", 210);
			var disassembly = new Disassembler.Disassembler().Disassemble(fileData);

			for (var insn = disassembly.EntryPoint; insn != null; insn = insn.Next)
			{
				Console.WriteLine("{0,8}    {1}", insn.Addr, insn);
			}

			var cfgBuilder = new CFGBuilder();
			var cfg = cfgBuilder.Build(disassembly);

			DominanceCalculator.CalculateDominators(cfg);

			Console.WriteLine($"\nFound {DominanceCalculator.FindLoops(cfg)} loop(s).");
		}

		static private void PrintCFG (ControlFlowNode node)
		{
			Console.Write("{0,8}    {1,-100}", node.Addr, node.LastInstruction);

			if (node.Successors.Count > 0)
			{
				Console.Write("=>{");

				foreach (var succ in node.Successors)
				{
					Console.Write($"{succ.Addr},");
				}

				Console.Write("}");
			}

			Console.Write("\n");
		}
	}
}
