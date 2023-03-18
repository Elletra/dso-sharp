using System;
using System.Collections.Generic;

using DSODecompiler.Disassembler;
using DSODecompiler.Util;

namespace DSODecompiler.ControlFlow.Structure.Regions
{
	public class RegionGraphNode : GraphNode
	{
		public Region Region { get; }

		public uint Addr => Region.Addr;
		public List<Instruction> Instructions => Region.Instructions;

		public RegionGraphNode (Region region) => Region = region;
		public RegionGraphNode (ControlFlowNode node) : this(new Region(node)) {}
	}

	public class RegionGraph : DirectedGraph<uint, RegionGraphNode>
	{
		public static RegionGraph From (ControlFlowGraph cfg)
		{
			var regionGraph = new RegionGraph();

			foreach (var node in cfg)
			{
				var regionNode = regionGraph.AddOrGet(node);

				foreach (ControlFlowNode successor in node.Successors)
				{
					regionGraph.AddEdge(regionNode, regionGraph.AddOrGet(successor));
				}
			}

			regionGraph.EntryPoint = regionGraph.Get(cfg.EntryPoint.Addr);

			return regionGraph;
		}

		public RegionGraphNode AddOrGet (ControlFlowNode node)
		{
			if (Has(node.Addr))
			{
				return Get(node.Addr);
			}

			return Add(node.Addr, new RegionGraphNode(node));
		}

		public RegionGraphNode Get (Region region) => Get(region.Addr);

		public bool Remove (Region region) => Remove(region.Addr);
		public bool Remove (RegionGraphNode node) => Remove(node.Addr);
	}
}
