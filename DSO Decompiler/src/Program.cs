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
			var loader = new FileLoader ();
			var data = loader.LoadFile ("test.cs.dso", 210);
			var size = data.StringTableSize (true);

			Console.WriteLine ("\n### Global String Table:");

			for (uint i = 0; i < size; i++)
			{
				if (data.HasString (i, true))
				{
					Console.WriteLine (data.StringTableValue (i, true));
				}
			}

			size = data.StringTableSize (false);

			Console.WriteLine ("\n### Function String Table:");

			for (uint i = 0; i < size; i++)
			{
				if (data.HasString (i, false))
				{
					Console.WriteLine (data.StringTableValue (i, false));
				}
			}

			size = data.FloatTableSize (true);

			Console.WriteLine ("\n### Global Float Table:");

			for (uint i = 0; i < size; i++)
			{
				Console.WriteLine (data.FloatTableValue (i, true));
			}

			size = data.FloatTableSize (false);

			Console.WriteLine ("\n### Function Float Table:");

			for (uint i = 0; i < size; i++)
			{
				Console.WriteLine (data.FloatTableValue (i, false));
			}

			Console.WriteLine ($"\nCode size: {data.CodeSize}");

			var disassembler = new BytecodeDisassembler ();
			var graph = disassembler.Disassemble (data);

			graph.PreorderDFS ((Instruction instruction, InstructionGraph graph) =>
			{
				if (instruction is JumpInsn)
				{
					Console.WriteLine ($"({instruction.Addr}=>{(instruction as JumpInsn).TargetAddr}) {instruction}");
				}
				else
				{
					Console.WriteLine ($"({instruction.Addr}) {instruction}");
				}
			});
		}
	}
}
