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
			var traverser = new DisassemblyTraverser();

			traverser.Traverse(disassembly, PrintInstruction);
		}

		static private void PrintInstruction (Instruction instruction)
		{
			System.Console.WriteLine("{0,16}    {1}", instruction.Addr, instruction);
		}
	}
}
