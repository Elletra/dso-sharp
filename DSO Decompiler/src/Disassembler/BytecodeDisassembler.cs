using System.Collections.Generic;

using DSODecompiler.Loader;
using DSODecompiler.ControlFlow;

namespace DSODecompiler.Disassembler
{
	public class BytecodeDisassembler : BytecodeReader
	{
		protected HashSet<uint> cfgAddrs;

		protected ControlFlowGraph graph;
		protected ControlFlowGraph.Node currNode;

		public BytecodeDisassembler (FileData fileData) : base (fileData) {}

		public ControlFlowGraph Disassemble (HashSet<uint> cfgAddrs)
		{
			Reset ();

			this.cfgAddrs = cfgAddrs;
			graph = new ControlFlowGraph ();

			ReadCode ();

			return graph;
		}

		protected override void Reset ()
		{
			base.Reset ();
			currNode = null;
		}

		protected override void ReadOp (uint op)
		{
			base.ReadOp (op);

			var addr = Pos;
			var size = GetOpcodeSize (op, addr);
			var instruction = new Instruction (Read (), addr);

			for (uint i = 1; i < size; i++)
			{
				instruction.Operands.Add (Read ());
			}

			if (currNode == null || cfgAddrs.Contains (addr))
			{
				currNode = CreateNode (addr);
			}

			currNode.Instructions.Add (instruction);
		}

		protected ControlFlowGraph.Node CreateNode (uint addr)
		{
			var node = graph.HasNode (addr) ? graph.GetNode (addr) : graph.AddNode (addr);

			if (currNode != null)
			{
				currNode.AddEdgeTo (node);
			}

			return node;
		}
	}
}
