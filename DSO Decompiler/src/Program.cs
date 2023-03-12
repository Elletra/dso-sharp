using System;

using DSODecompiler.Disassembly;
using DSODecompiler.Loader;

namespace DSODecompiler
{
	class Program
	{
		static void Main (string[] args)
		{
			var loader = new FileLoader();
			var fileData = loader.LoadFile("test.cs.dso", 210);
			var disassembler = new Disassembler();
			var instructions = disassembler.Disassemble(fileData);

			foreach (var instruction in instructions)
			{
				Console.WriteLine("{0,8}    {1}", instruction.Addr, instruction);
			}
		}
	}
}
