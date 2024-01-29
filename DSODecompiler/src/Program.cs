using System;

using DSODecompiler.Disassembly;
using DSODecompiler.Loader;
using DSODecompiler.Opcodes;

namespace DSODecompiler
{
	class Program
	{
		static void Main()
		{
			var fileName = "test";
			var loader = new FileLoader();

			Console.WriteLine($"Loading file {fileName}...");

			var fileData = loader.LoadFile($"{fileName}.cs.dso", 210);
			var disassembler = new Disassembler(new OpcodeFactory(Blockland.V21.Opcodes.Strings));

			Console.WriteLine("Disassembling...");

			var disassembly = disassembler.Disassemble(fileData);
			var disasmWriter = new DisassemblyWriter();

			disasmWriter.WriteToFile(disassembly, $"./{fileName}.txt");
		}
	}
}
