using System.Collections.Generic;

using DSODecompiler.Loader;
using DSODecompiler.ControlFlow;

namespace DSODecompiler.Disassembler
{
	public class BytecodeDisassembler : BytecodeReader
	{
		protected HashSet<uint> cfgAddrs;

		protected ControlFlowGraph graph;
		protected ControlFlowNode currNode;

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
			var instruction = new Instruction ((Opcodes.Ops) Read (), addr);

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
		/// We have to go back and connect all CFG nodes that end with jumps to their jump target
		/// CFG nodes, because doing it during the initial disassembly would make the code messier.
		/// </summary>
		protected void ConnectJumps ()
		{
			graph.PreorderDFS ((ControlFlowNode node) =>
			{
				var last = node.LastInstruction;

				if (Opcodes.IsJump (last.Op))
				{
					graph.AddEdge (node.Addr, last.Operands[0]);
				}
			});
		}

		protected ControlFlowNode CreateAndConnectNode (uint addr)
		{
			var node = graph.CreateOrGet (addr);

			if (currNode != null)
			{
				currNode.AddEdgeTo (node);
			}

			return node;
		}
	}
}
