using System;

using DSODecompiler.Loader;
using DSODecompiler.Disassembler;

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

			for (var insn = disassembly.EntryPoint; insn != null; insn = insn.Next)
			{
				System.Console.WriteLine("{0,16}    {1}", insn.Addr, insn);
			}
		}
	}
}
