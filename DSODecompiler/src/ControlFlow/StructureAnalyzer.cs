using System;
using System.Collections.Generic;
using System.Linq;

namespace DSODecompiler.ControlFlow
{
	public class StructureAnalyzer
	{
		private ControlFlowGraph graph;

		public ReducedNode Analyze(ControlFlowGraph cfg)
		{
			graph = cfg;

			new DominanceCalculator().Calculate(cfg);

			while (graph.Count > 1)
			{
				foreach (ControlFlowNode node in cfg.PostorderDFS())
				{
					// TODO: Handle functions
					// TODO: Handle breaks and continues

					var reduced = true;

					while (reduced)
					{
						reduced = Reduce(node);
					}
				}
			}

			return graph.GetEntryPoint().ReducedNode;
		}

		private bool Reduce(ControlFlowNode node)
		{
			var reduced = !IsLoopEnd(node) && ReduceAcyclic(node);

			if (!reduced && IsLoopStart(node))
			{
				reduced = ReduceCyclic(node);
			}

			return reduced;
		}

		private bool ReduceCyclic(ControlFlowNode start)
		{
			ControlFlowNode end;

			if (IsLoop(start, start))
			{
				end = start;
			}
			else if (IsLoop(start, start.GetSuccessor(0)))
			{
				end = start.GetSuccessor(0);
			}
			else
			{
				return false;
			}

			start.ReducedNode = new LoopNode()
			{
				Body = start == end
					? (start.ReducedNode ?? new InstructionsNode(start))
					: new SequenceNode(
						start.ReducedNode ?? new InstructionsNode(start),
						end.ReducedNode ?? new InstructionsNode(end)
					),
			};

			start.AddEdgeTo(end.GetSuccessor(0));
			start.RemoveEdgeTo(end);

			if (start != end)
			{
				graph.RemoveNode(end);
			}

			return true;
		}

		private bool ReduceAcyclic(ControlFlowNode node)
		{
			var reduced = false;

			switch(node.Successors.Count)
			{
				case 0:
					reduced = ReduceEnd(node);
					break;

				case 1:
					reduced = ReduceSequence(node);
					break;

				case 2:
					reduced = ReduceConditional(node);
					break;

				default:
					throw new InvalidOperationException($"Node at {node.Addr} has an invalid number of successors");
			}

			return reduced;
		}

		private bool ReduceEnd(ControlFlowNode node)
		{
			var reduced = false;

			if (node.ReducedNode == null)
			{
				node.ReducedNode = new InstructionsNode(node);
				reduced = true;
			}

			return reduced;
		}

		private bool ReduceSequence(ControlFlowNode node)
		{
			var successor = node.GetSuccessor(0);

			if (successor.Successors.Count > 1 || successor.Predecessors.Count > 1 || IsLoopNode(successor))
			{
				return false;
			}

			switch (node.ReducedNode)
			{
				case null:
					node.ReducedNode = new SequenceNode(new InstructionsNode(node), successor.ReducedNode);
					break;

				case SequenceNode sequence:
					sequence.Add(successor.ReducedNode);
					break;

				default:
					node.ReducedNode = new SequenceNode(node.ReducedNode, successor.ReducedNode);
					break;
			}

			node.AddEdgeTo(successor.GetSuccessor(0));
			graph.RemoveNode(successor);

			return true;
		}

		private bool ReduceConditional(ControlFlowNode node)
		{
			// TODO: Check for loops

			if (node.IsUnconditional)
			{
				return false;
			}

			var reduced = false;

			var then = node.GetSuccessor(0);
			var @else = node.GetSuccessor(1);
			var thenSucc = then.GetSuccessor(0);
			var elseSucc = @else.GetSuccessor(0);

			if (then.Successors.Count == 1 && (thenSucc == @else || thenSucc?.GetSuccessor(0) == @else))
			{
				/* If-Then */

				if (node.ReducedNode == null)
				{
					node.ReducedNode = new ConditionalNode()
					{
						If = new InstructionsNode(node),
						Then = then.ReducedNode ?? new InstructionsNode(then),
					};

					var target = node.BranchTarget;

					graph.RemoveNode(then);

					// We want to make sure the target stays the same... A bit of sloppy way of
					// doing this but that's okay.
					node.RemoveEdgeTo(target);
					node.AddEdgeTo(thenSucc);
					node.AddEdgeTo(target);

					reduced = true;
				}
				else if (node.ReducedNode is not ConditionalNode)
				{
					throw new InvalidOperationException($"Reduced node at {node.Addr} is not a conditional");
				}
			}
			else if (then.Successors.Count > 0 && (thenSucc == elseSucc || then.BranchTarget == elseSucc))
			{
				/* If-Then-Else */

				if (node.ReducedNode == null)
				{
					node.ReducedNode = new ConditionalNode()
					{
						If = new InstructionsNode(node),
						Then = then.ReducedNode ?? new InstructionsNode(then),
						Else = @else.ReducedNode ?? new InstructionsNode(@else),
					};
				}
				else if (node.ReducedNode is ConditionalNode conditional)
				{
					conditional.Then = conditional.Then == null
						? new InstructionsNode(then)
						: new SequenceNode(conditional.Then, then.ReducedNode);

					conditional.Else = @else.ReducedNode ?? new InstructionsNode(@else);
				}
				else
				{
					throw new InvalidOperationException($"Reduced node at {node.Addr} is not a conditional");
				}

				node.AddEdgeTo(@else.GetSuccessor(0));
				graph.RemoveNode(then);
				graph.RemoveNode(@else);

				reduced = true;
			}

			return reduced;
		}

		bool IsLoopStart(ControlFlowNode node)
		{
			return node.Predecessors.Any(predecessor => node.Dominates((ControlFlowNode) predecessor, strictly: false));
		}

		bool IsLoopEnd(ControlFlowNode node)
		{
			return node.Successors.Any(successor => (successor as ControlFlowNode).Dominates(node, strictly: false));
		}

		bool IsLoopNode(ControlFlowNode node) => IsLoopStart(node) || IsLoopEnd(node);

		bool IsLoop(ControlFlowNode start, ControlFlowNode end)
		{
			return start.Dominates(end, strictly: false) && start.Predecessors.Any(predecessor => predecessor == end);
		}
	}
}
