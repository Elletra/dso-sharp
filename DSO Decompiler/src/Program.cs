using DSODecompiler.Loader;
using DSODecompiler.Opcodes;
using DSODecompiler.Disassembly;

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

			foreach (var instruction in disassembly.GetInstructions())
			{
				System.Console.WriteLine($"{{0, -8}} {instruction}", instruction.Addr);
			}
		}
	}
}
