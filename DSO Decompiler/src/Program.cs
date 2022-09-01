using System;
using System.Collections.Generic;

using DsoDecompiler.Loader;
using DsoDecompiler.Disassembler;

namespace DsoDecompiler
{
	class Program
	{
		static void Main (string[] args)
		{
			var loader = new FileLoader ();
			var data = loader.LoadFile ("main.cs.dso", 210);
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

			var analyzer = new BytecodeAnalyzer (data);
			var cfgAddrs = analyzer.Analyze ();

			var disassembler = new BytecodeDisassembler (data);
			var cfg = disassembler.Disassemble (cfgAddrs);

			var queue = new Queue<uint> ();
			var visited = new HashSet<uint> ();

			queue.Enqueue (0);

			while (queue.Count > 0)
			{
				var node = cfg.GetNode (queue.Dequeue ());

				if (visited.Contains (node.Addr))
				{
					continue;
				}

				visited.Add (node.Addr);

				System.Console.WriteLine ($"## CFG Node ({node.Addr}):");

				foreach (var insn in node.Instructions)
				{
					System.Console.Write ($"    * {Opcodes.OpcodeToString (insn.Op)} (");

					for (var i = 0; i < insn.Operands.Count; i++)
					{
						System.Console.Write (insn.Operands[i]);

						if (i < insn.Operands.Count - 1)
						{
							System.Console.Write (", ");
						}
					}

					System.Console.Write (")\n");
				}

				System.Console.WriteLine ("-------------------------------\n");

				foreach (var succ in node.Successors)
				{
					if (!visited.Contains (succ.Addr))
					{
						queue.Enqueue (succ.Addr);
					}
				}
			}
		}
	}
}
