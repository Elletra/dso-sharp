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

			var analyzer = new BytecodeAnalyzer (data);
			var cfgAddrs = analyzer.Analyze ();

			var disassembler = new BytecodeDisassembler (data);
			var graph = disassembler.Disassemble (cfgAddrs);

			DominanceCalculator.CalculateDominators (graph);

			graph.PostorderDFS ((ControlFlowGraph.Node node) =>
			{
				System.Console.WriteLine ($"Node {node.Addr}");
			});

			System.Console.WriteLine ($"Num loops: {DominanceCalculator.FindLoops (graph)}");
		}
	}
}
