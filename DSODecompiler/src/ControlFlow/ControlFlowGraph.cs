using System.Collections.Generic;

using DSODecompiler.Disassembly;
using DSODecompiler.Util;

namespace DSODecompiler.ControlFlow
{
	public class ControlFlowNode : DirectedGraph<uint>.Node
	{
		public readonly List<Instruction> Instructions = new();

		public uint Addr => Key;

		public Instruction FirstInstruction => Instructions.Count > 0 ? Instructions[0] : null;
		public Instruction LastInstruction => Instructions.Count > 0 ? Instructions[^1] : null;

		public ControlFlowNode (uint key) : base(key) { }
		public ControlFlowNode (Instruction instruction) : base(instruction.Addr) { }
	}

	public class ControlFlowGraph : DirectedGraph<uint>
	{
		public FunctionInstruction FunctionInstruction => (nodes?[0] as ControlFlowNode)?.FirstInstruction as FunctionInstruction;
		public bool IsFunction => FunctionInstruction != null;

		public ControlFlowNode AddNode (uint addr) => AddNode(new ControlFlowNode(addr)) as ControlFlowNode;

		public List<ControlFlowNode> GetNodes ()
		{
			var list = GetNodes<ControlFlowNode>();

			// TODO: Again, if I ever implement recursive descent disassembly, this will not work.
			list.Sort((node1, node2) => node1.Addr.CompareTo(node2.Addr));

			return list;
		}
	}
}
