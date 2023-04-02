using System;
using System.Linq;
using System.Collections.Generic;

using DSODecompiler.ControlFlow.Structure.Regions;
using DSODecompiler.Disassembler;

namespace DSODecompiler.ControlFlow.Structure
{
	/// <summary>
	/// Performs a structural analysis on a control flow graph in order to determine various structures
	/// in the code, such as loops, if-else statements, etc.<br/><br/>
	///
	/// <b>NOTE:</b> This is an extremely dumbed-down WIP version of the algorithm linked. I just want
	/// to have a working decompiler at this point and I'm burning out on this project, so I'm not
	/// going to account for any edge cases that aren't already produced by the default TorqueScript
	/// compiler. You will notice that the code is horrible. This is also a result of burnout.<br/><br/>
	///
	/// <b>Sources:</b><br/>
	/// <list type="number">
	/// <item>
	/// <see href="https://www.usenix.org/system/files/conference/usenixsecurity13/sec13-paper_schwartz.pdf">
	/// "Native x86 Decompilation Using Semantics-Preserving Structural Analysis and Iterative
	/// Control-Flow Structuring"</see> by Edward J. Schwartz, JongHyup Lee, Maverick Woo, and David Brumley.
	/// </item>
	/// <item><see href="https://github.com/uxmal/reko">Reko Decompiler</see> by John Källén, et al.</item>
	/// </list>
	/// </summary>
	public class StructureAnalyzer
	{
		public class StructuralAnalysisException : Exception
		{
			public StructuralAnalysisException () {}
			public StructuralAnalysisException (string message) : base(message) {}
			public StructuralAnalysisException (string message, Exception inner) : base(message, inner) {}
		}

		protected Disassembly disassembly;
		protected RegionGraph regionGraph;
		protected DominatorGraph<uint, RegionGraphNode> domGraph;
		protected Dictionary<uint, VirtualRegion> virtualRegions;

		public VirtualRegion Analyze (ControlFlowGraph cfg, Disassembly disasm)
		{
			disassembly = disasm;
			regionGraph = RegionGraph.From(cfg);
			domGraph = new(regionGraph);
			virtualRegions = new();

			var entryPoint = regionGraph.EntryPoint;

			/* Silly edge case where there's one graph node that isn't a loop or branch or anything. */
			if (regionGraph.Count == 1)
			{
				AddVirtualRegion(entryPoint.Addr, new InstructionRegion(entryPoint.Region));
			}

			do
			{
				var oldCount = regionGraph.Count;

				foreach (var node in regionGraph.PostorderDFS())
				{
					ReduceNode(node);
				}

				// If we couldn't reduce anything, let's try some refinement.
				if (regionGraph.Count == oldCount && regionGraph.Count > 1)
				{
					// TODO: Implement
					RefineUnreducedRegions();
				}
			}
			while (regionGraph.Count > 1);

			return GetVirtualRegion(entryPoint.Addr);
		}

		protected void ReduceNode (RegionGraphNode node)
		{
			var reduced = true;

			while (reduced)
			{
				if (node.Successors.Count > 2)
				{
					throw new NotImplementedException($"Region graph node with more than 2 successors");
				}

				reduced = !IsCycleEnd(node) && ReduceAcyclic(node);

				if (!reduced && IsCycleStart(node))
				{
					reduced = ReduceCyclic(node);
				}
			}
		}

		protected bool IsCycleStart (RegionGraphNode node)
		{
			return node.Predecessors.Any(predecessor => domGraph.Dominates(node, predecessor as RegionGraphNode, strictly: false));
		}

		protected bool IsCycleEnd (RegionGraphNode node)
		{
			return node.Successors.Any(successor => domGraph.Dominates(successor as RegionGraphNode, node, strictly: false));
		}

		// TODO: Test expression inversion and comparison.
		protected bool ReduceCyclic (RegionGraphNode node)
		{
			foreach (RegionGraphNode successor in node.Successors.ToArray())
			{
				var isSelfLoop = successor == node;

				if (isSelfLoop || (successor.FirstSuccessor == node && successor.Predecessors.Count == 1))
				{
					if (successor.Region.LastInstruction is not BranchInstruction branch)
					{
						throw new NotImplementedException($"Cyclic region at {node.Addr} does not end with branch instruction.");
					}

					var loop = new LoopRegion()
					{
						Infinite = successor.Successors.Count == 1 || branch.IsUnconditional
					};

					if (HasVirtualRegion(node.Addr))
					{
						loop.Body.Add(GetVirtualRegion(node.Addr));
					}
					else if (isSelfLoop)
					{
						loop.Body.Add(new InstructionRegion(node.Region));
					}
					else
					{
						loop.CopyInstructions(node.Region);
					}

					if (!isSelfLoop && HasVirtualRegion(successor.Addr))
					{
						loop.Body.Add(GetVirtualRegion(successor.Addr));
					}

					AddVirtualRegion(node.Addr, loop);

					regionGraph.RemoveEdge(node, successor);
					regionGraph.RemoveEdge(successor, node);

					return true;
				}
			}

			// TODO: Unreduced loops.
			// unreducedLoops.Enqueue(domGraph.FindLoopNodes(node));

			return false;
		}

		protected bool ReduceAcyclic (RegionGraphNode node)
		{
			var reduced = false;

			switch (node.Successors.Count)
			{
				case 0:
				{
					break;
				}

				case 1:
				{
					reduced = ReduceSequence(node);
					break;
				}

				case 2:
				{
					if (IsUnconditional(node))
					{
						reduced = ReduceUnconditional(node);
					}
					else
					{
						reduced = ReduceConditional(node);
					}

					break;
				}

				default:
				{
					throw new NotImplementedException($"Region graph node has {node.Successors.Count} successors");
				}
			}

			return reduced;
		}

		protected bool IsUnconditional (RegionGraphNode node)
		{
			return node.Instructions.Count > 0 && node.Instructions[^1] is BranchInstruction branch && branch.IsUnconditional;
		}

		protected bool ReduceUnconditional (RegionGraphNode node)
		{
			var successors = regionGraph.GetSuccessors(node);

			if (!IsUnconditional(node) || successors.Count != 2)
			{
				return false;
			}

			var targetAddr = successors[1].Addr;

			if (!regionGraph.Has(targetAddr))
			{
				return false;
			}

			var targetNode = regionGraph.Get(targetAddr);

			VirtualRegion region;

			if (targetNode.Predecessors.Any(predecessor => IsCycleEnd(predecessor as RegionGraphNode)))
			{
				region = new BreakRegion();
			}
			else
			{
				region = new GotoRegion(targetAddr);

				AddVirtualRegion(targetAddr, new LabelRegion(targetAddr));
			}

			AddVirtualRegion(node, region);
			regionGraph.RemoveEdge(node.Addr, targetAddr);

			return true;
		}

		protected bool ReduceConditional (RegionGraphNode node)
		{
			var successors = regionGraph.GetSuccessors(node);

			if (successors.Count != 2)
			{
				return false;
			}

			var then = successors[0];
			var target = successors[1];
			var thenSuccessor = regionGraph.FirstSuccessor(then);

			var reduced = false;

			if (thenSuccessor == target)
			{
				if (HasOtherPredecessors(then, node))
				{
					return false;
				}

				reduced = true;

				if (!HasVirtualRegion(then.Addr))
				{
					AddVirtualRegion(then.Addr, new InstructionRegion(then.Region));
				}

				// TODO: Conditional inversion based on instruction.
				AddVirtualRegion(node.Addr, new ConditionalRegion(node.Region, GetVirtualRegion(then)));

				regionGraph.RemoveEdge(node, then);
				regionGraph.RemoveEdge(then, thenSuccessor);
				regionGraph.Remove(then);
			}

			return reduced;
		}

		protected bool ReduceSequence (RegionGraphNode node)
		{
			if (node.Successors.Count != 1)
			{
				return false;
			}

			var next = regionGraph.FirstSuccessor(node);

			// Don't want to accidentally delete a jump target.
			if (next.Predecessors.Count > 1)
			{
				return false;
			}

			SequenceRegion sequence;

			if (node.Region.IsFunction)
			{
				sequence = new FunctionRegion(node.Region.FunctionHeader);
			}
			else
			{
				sequence = new SequenceRegion();
			}

			if (HasVirtualRegion(node.Addr))
			{
				sequence.Add(GetVirtualRegion(node));
			}
			else
			{
				sequence.CopyInstructions(node.Region);
			}

			if (!HasVirtualRegion(next.Addr) || GetVirtualRegion(next) is LabelRegion)
			{
				if (IsCycleEnd(next))
				{
					AddVirtualRegion(next.Addr, new LoopFooterRegion(next.Region));
				}
				else
				{
					AddVirtualRegion(next.Addr, new InstructionRegion(next.Region));
				}
			}

			sequence.Add(GetVirtualRegion(next));

			AddVirtualRegion(node.Addr, sequence);

			regionGraph.RemoveEdge(node, next);
			regionGraph.ReplaceSuccessors(next, node);
			regionGraph.Remove(next);

			return true;
		}

		protected bool HasOtherPredecessors (RegionGraphNode node, RegionGraphNode compare)
		{
			return node.Predecessors.Any(predecessor => predecessor != compare);
		}

		protected void RefineUnreducedRegions ()
		{
			throw new NotImplementedException("RefineUnreducedRegions() not implemented");
		}

		/// <summary>
		/// TODO: Implement
		/// </summary>
		/// <param name="loop"></param>
		protected void RefineLoop (Loop<RegionGraphNode> loop)
		{
			var head = EnsureSingleEntry(loop);

		}

		protected RegionGraphNode EnsureSingleEntry (Loop<RegionGraphNode> loop)
		{
			var entry = loop.Head;
			var maxEdges = CountIncomingEdges(entry, loop);

			foreach (var node in loop.Nodes)
			{
				var count = CountIncomingEdges(node, loop);

				if (count > maxEdges)
				{
					maxEdges = count;
					entry = node;
				}
			}

			/* Virtualize other incoming edges. */

			foreach (var node in loop.Nodes)
			{
				var nodes = FindIncomingNodes(node, loop);

				foreach (var incoming in nodes)
				{
					AddVirtualRegion(incoming.Addr, new GotoRegion(incoming.Addr));
					regionGraph.RemoveEdge(incoming, node);
				}
			}

			return entry;
		}

		protected int CountIncomingEdges (RegionGraphNode node, Loop<RegionGraphNode> loop) => FindIncomingNodes(node, loop).Count;

		protected HashSet<RegionGraphNode> FindIncomingNodes (RegionGraphNode node, Loop<RegionGraphNode> loop)
		{
			var edges = new HashSet<RegionGraphNode>();

			node.Predecessors.ForEach(predecessor =>
			{
				if (!loop.Nodes.Contains(predecessor))
				{
					edges.Add(predecessor as RegionGraphNode);
				}
			});

			return edges;
		}

		protected void AddVirtualRegion (uint addr, VirtualRegion vr)
		{
			var existing = GetVirtualRegion(addr);

			if (existing is LabelRegion label)
			{
				if (vr is LabelRegion vrLabel)
				{
					label.Region = vrLabel.Region;
				}
				else
				{
					label.Region = vr;
				}
			}
			else
			{
				if (vr is LabelRegion vrLabel)
				{
					if (existing == null)
					{
						vrLabel.Region = new InstructionRegion(regionGraph.Get(addr).Region);
					}
					else
					{
						vrLabel.Region = existing;
					}
				}

				virtualRegions[addr] = vr;
			}
		}

		protected void AddVirtualRegion (RegionGraphNode node, VirtualRegion vr)
		{
			vr.CopyInstructions(node.Region);

			AddVirtualRegion(node.Addr, vr);
		}

		protected bool HasVirtualRegion (uint addr) => virtualRegions.ContainsKey(addr);
		protected VirtualRegion GetVirtualRegion (uint addr) => HasVirtualRegion(addr) ? virtualRegions[addr] : null;
		protected VirtualRegion GetVirtualRegion (RegionGraphNode node) => GetVirtualRegion(node.Addr);
	}
}
