using System;
using System.Linq;
using System.Collections.Generic;

using DSODecompiler.ControlFlow.Structure.Regions;
using DSODecompiler.Disassembler;

namespace DSODecompiler.ControlFlow.Structure
{
	/// <summary>
	/// Performs a structural analysis on a control flow graph in order to determine various structures
	/// in the code, such as loops, if-else statements, etc.<br /><br />
	///
	/// <b>Sources:</b><br />
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
		protected Disassembly disassembly = null;
		protected RegionGraph regionGraph = null;
		protected DominatorGraph<uint, RegionGraphNode> domGraph = null;
		protected Dictionary<uint, VirtualRegion> virtualRegions = null;

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
				foreach (var node in regionGraph.PostorderDFS())
				{
					ReduceNode(node);
				}
			}
			while (regionGraph.Count > 1);

			return GetVirtualRegion(entryPoint.Addr);
		}

		/* TODO: Gotos, breaks, tail regions, etc. */
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
			return node.Predecessors.Any(predecessor => IsBackEdge(predecessor as RegionGraphNode, node));
		}

		protected bool IsCycleEnd (RegionGraphNode node)
		{
			return node.Successors.Any(successor => IsBackEdge(node, successor as RegionGraphNode));
		}

		protected bool IsBackEdge (RegionGraphNode node1, RegionGraphNode node2)
		{
			return domGraph.Dominates(node2, node1, strictly: false);
		}

		// TODO: Test expression inversion and comparison.
		protected bool ReduceCyclic (RegionGraphNode node)
		{
			foreach (RegionGraphNode successor in node.Successors.ToArray())
			{
				if (successor == node)
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
					else
					{
						loop.Body.Add(new InstructionRegion(node.Region));
					}

					AddVirtualRegion(node.Addr, loop);

					regionGraph.RemoveEdge(node, successor);
					regionGraph.RemoveEdge(successor, node);

					return true;
				}
			}

			foreach (RegionGraphNode successor in node.Successors.ToArray())
			{
				if (successor.FirstSuccessor == node && successor.Predecessors.Count == 1)
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
					else
					{
						loop.CopyInstructions(node.Region);
					}

					if (HasVirtualRegion(successor.Addr))
					{
						loop.Body.Add(GetVirtualRegion(successor.Addr));
					}

					AddVirtualRegion(node.Addr, loop);

					regionGraph.RemoveEdge(node, successor);
					regionGraph.RemoveEdge(successor, node);

					return true;
				}
			}

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
					reduced = ReduceConditional(node);
					break;
				}

				default:
				{
					throw new NotImplementedException($"Region graph node has {node.Successors.Count} successors");
				}
			}

			return reduced;
		}

		// TODO: Test expression inversion and comparison.
		protected bool ReduceConditional (RegionGraphNode node)
		{
			var region = node.Region;
			var successors = regionGraph.GetSuccessors(node);

			if (successors.Count != 2)
			{
				return false;
			}

			var then = successors[0];
			var @else = successors[1];
			var thenSucc = regionGraph.FirstSuccessor(then);
			var elseSucc = regionGraph.FirstSuccessor(@else);

			var reduced = false;

			if (thenSucc == @else)
			{
				reduced = true;

				Console.WriteLine($"{node.Addr} :: if-then");

				if (!HasVirtualRegion(then.Addr))
				{
					AddVirtualRegion(then.Addr, new InstructionRegion(then.Region));
				}

				AddVirtualRegion(node.Addr, new ConditionalRegion(node.Region, GetVirtualRegion(then)));

				regionGraph.RemoveEdge(node, then);
				regionGraph.RemoveEdge(then, thenSucc);
				regionGraph.Remove(then);
			}
			else if (elseSucc != null && thenSucc == elseSucc)
			{
				reduced = true;

				Console.WriteLine($"{node.Addr} :: if-then-else");

				if (!HasVirtualRegion(then.Addr))
				{
					AddVirtualRegion(then.Addr, new InstructionRegion(then.Region));
				}

				if (!HasVirtualRegion(@else.Addr))
				{
					AddVirtualRegion(@else.Addr, new InstructionRegion(@else.Region));
				}

				AddVirtualRegion(
					node.Addr,
					new ConditionalRegion(
						node.Region,
						GetVirtualRegion(then),
						GetVirtualRegion(@else)
					)
				);

				regionGraph.RemoveEdge(then, thenSucc);
				regionGraph.RemoveEdge(@else, elseSucc);
				regionGraph.RemoveEdge(node, then);
				regionGraph.RemoveEdge(node, @else);

				regionGraph.AddEdge(node, elseSucc);

				regionGraph.Remove(then);
				regionGraph.Remove(@else);
			}
			else
			{
				Console.WriteLine($"{node.Addr} :: <failed>    {node.Instructions[^1]}");
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

			var sequence = new SequenceRegion();

			if (HasVirtualRegion(node.Addr))
			{
				sequence.Add(GetVirtualRegion(node));
			}
			else
			{
				sequence.CopyInstructions(node.Region);
			}

			if (!HasVirtualRegion(next.Addr))
			{
				AddVirtualRegion(next.Addr, new InstructionRegion(next.Region));
			}

			sequence.Add(GetVirtualRegion(next));

			AddVirtualRegion(node.Addr, sequence);

			regionGraph.RemoveEdge(node, next);
			regionGraph.ReplaceSuccessors(next, node);
			regionGraph.Remove(next);

			return true;
		}

		protected VirtualRegion AddVirtualRegion (uint addr, VirtualRegion vr) => virtualRegions[addr] = vr;
		protected bool HasVirtualRegion (uint addr) => virtualRegions.ContainsKey(addr);
		protected VirtualRegion GetVirtualRegion (uint addr) => HasVirtualRegion(addr) ? virtualRegions[addr] : null;
		protected VirtualRegion GetVirtualRegion (RegionGraphNode node) => GetVirtualRegion(node.Addr);
	}
}
