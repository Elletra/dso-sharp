using System.Collections.Generic;

using DSODecompiler.Disassembler;

namespace DSODecompiler.ControlFlow
{
	public class ControlFlowData
	{
		public List<ControlFlowGraph> ControlFlowGraphs { get; } = null;
		public Dictionary<ControlFlowGraph, DominatorGraph> DominatorGraphs { get; } = new();

		public ControlFlowData (Disassembly disassembly)
		{
			ControlFlowGraphs = new ControlFlowGraphBuilder().Build(disassembly);

			BuildDominatorGraphs();
		}

		protected void BuildDominatorGraphs ()
		{
			foreach (var cfg in ControlFlowGraphs)
			{
				DominatorGraphs[cfg] = new DominatorGraph(cfg);
			}
		}
	}
}
