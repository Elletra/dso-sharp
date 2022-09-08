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
			ConnectJumps ();

			return graph;
		}

		protected override void Reset ()
		{
			base.Reset ();

			currNode = null;
		}

		protected override void ReadFrom (uint startAddr)
		{
			currNode = null;

			base.ReadFrom (startAddr);
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
				currNode = CreateAndConnectNode (addr);
			}

			currNode.Instructions.Add (instruction);
		}

		/// <summary>
		/// Because logistical issues would make the code messy, we have to go back and connect
		/// all CFG nodes that end with jumps to their jump target CFG nodes.
		/// </summary>
		protected void ConnectJumps ()
		{
			graph.PreorderDFS ((ControlFlowGraph.Node node) =>
			{
				var last = node.LastInstruction;

				if (Opcodes.IsJump (last.Op))
				{
					graph.AddEdge (node.Addr, last.Operands[0]);
				}
			});
		}

		protected ControlFlowGraph.Node CreateAndConnectNode (uint addr)
		{
			var node = CreateOrGetNode (addr);

			if (currNode != null)
			{
				currNode.AddEdgeTo (node);
			}

			return node;
		}

		protected ControlFlowGraph.Node CreateOrGetNode (uint addr)
		{
			return graph.HasNode (addr) ? graph.GetNode (addr) : graph.AddNode (addr);
		}
	}
}
