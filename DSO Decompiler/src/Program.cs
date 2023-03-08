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

			var cfg = new CFGBuilder().Build(disassembly);

			DominanceCalculator.CalculateDominators(cfg);

			Console.WriteLine($"\nFound {DominanceCalculator.FindLoops(cfg)} loop(s).");

			foreach (var insn in disassembly.GetInstructions())
			{
				if (insn.IsLoopStart)
				{
					Console.Write($">> {insn.NumLoopsTo}  ");
				}

				if (insn is BranchInsn branch)
				{
					Console.Write("#{0,8}    {1}    {2}\n", insn.Addr, branch.IsLoopEnd, insn);
				}
				else
				{
					Console.Write("#{0,8}    {1}\n", insn.Addr, insn);
				}
			}
		}
	}
}
