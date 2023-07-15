using DSODecompiler.Disassembly;

using System.Collections.Generic;

namespace DSODecompiler.ControlFlow
{
	/// <summary>
	/// Performs structural analysis on a control flow graph to recover control flow structures (e.g.
	/// if statements, loops, etc.) from it.<br/><br/>
	///
	/// This doesn't implement everything described in the Schwartz paper, but it works for our current
	/// target, which is normal DSO files produced by the Torque Game Engine.<br/><br/>
	///
	/// <strong>Sources:</strong><br/><br/>
	///
	/// <list type="number">
	/// <item>
	/// <see href="https://www.usenix.org/system/files/conference/usenixsecurity13/sec13-paper_schwartz.pdf">
	/// "Native x86 Decompilation Using Semantics-Preserving Structural Analysis and Iterative
	/// Control-Flow Structuring"</see> by Edward J. Schwartz, JongHyup Lee, Maverick Woo, and David Brumley.
	/// </item>
	///
	/// <item>
	/// <see href="https://www.ndss-symposium.org/wp-content/uploads/2017/09/11_4_2.pdf">
	/// "No More Gotos: Decompilation Using Pattern-Independent Control-Flow Structuring and
	/// Semantics-Preserving Transformations"</see> by Khaled Yakdan, Sebastian Eschweiler,
	/// Elmar Gerhards-Padilla, Matthew Smith.
	/// </item>
	/// </list>
	/// </summary>
	public class StructureAnalyzer
	{
		public class Exception : System.Exception
		{
			public Exception () {}
			public Exception (string message) : base(message) {}
			public Exception (string message, Exception inner) : base(message, inner) {}
		}

		protected ControlFlowGraph graph;
		protected Dictionary<uint, CollapsedNode> collapsedNodes;
		protected LoopFinder loopFinder;

		public CollapsedNode Analyze (ControlFlowGraph cfg)
		{
			graph = cfg;
			collapsedNodes = new();
			loopFinder = new();

			// Edge case where there's only one node, but it still needs to be reduced!
			if (graph.Count == 1)
			{
				collapsedNodes[graph.EntryPoint] = new InstructionNode(graph.GetEntryPoint());
			}

			while (graph.Count > 1)
			{
				foreach (ControlFlowNode node in graph.PostorderDFS(graph.EntryPoint))
				{
					var reduced = true;

					while (reduced)
					{
						reduced = ReduceNode(node);
					}
				}
			}

			graph.EntryPoint = graph.GetNodes()[0].Addr;

			return collapsedNodes[graph.EntryPoint];
		}

		protected bool ReduceNode (ControlFlowNode node)
		{
			var reduced = ReduceAcyclic(node);

			if (!reduced && loopFinder.IsLoopStart(node))
			{
				reduced = ReduceCyclic(node);
			}

			return reduced;
		}

		protected bool ReduceAcyclic (ControlFlowNode node)
		{
			if (node.LastInstruction is BranchInstruction branch && branch.IsUnconditional)
			{
				return ReduceUnconditional(node);
			}

			switch (node.Successors.Count)
			{
				case 0:
					return false;

				case 1:
					return ReduceSequence(node);

				case 2:
					return ReduceConditional(node);

				default:
					throw new Exception($"Node {node.Addr} has more than 2 successors");
			}
		}

		protected bool ReduceSequence (ControlFlowNode node)
		{
			var next = node.GetSuccessor(0);

			if (!next.IsSequential)
			{
				return false;
			}

			var sequence = ExtractSequence(node);

			node.AddEdgeTo(next.GetSuccessor(0));
			sequence.AddNode(ExtractCollapsed(next));

			collapsedNodes[node.Addr] = sequence;

			return true;
		}

		protected bool ReduceConditional (ControlFlowNode node)
		{
			// We don't accidentally want to mistake the ends of loops as if statements.
			if (loopFinder.IsLoopEnd(node))
			{
				return false;
			}

			var reduced = false;
			var then = node.GetSuccessor(0);
			var @else = node.GetSuccessor(1);
			var thenSuccessor = then.GetSuccessor(0);
			var elseSuccessor = @else.GetSuccessor(0);

			if (thenSuccessor == @else)
			{
				/* if-then */

				if (then.IsSequential && !loopFinder.IsLoopEnd(then))
				{
					collapsedNodes[node.Addr] = new ConditionalNode(node)
					{
						Then = ExtractCollapsed(then),
					};

					reduced = true;
				}
			}
			else if (thenSuccessor == elseSuccessor)
			{
				/* if-then-else */

				if (then.IsSequential && @else.IsSequential && !loopFinder.IsLoopEnd(then) && !loopFinder.IsLoopEnd(@else))
				{
					collapsedNodes[node.Addr] = new ConditionalNode(node)
					{
						Then = ExtractCollapsed(then),
						Else = ExtractCollapsed(@else),
					};

					node.AddEdgeTo(thenSuccessor);

					reduced = true;
				}
			}

			return reduced;
		}

		protected bool ReduceUnconditional (ControlFlowNode node)
		{
			if (!IsUnconditional(node) || loopFinder.IsLoopEnd(node) || collapsedNodes.ContainsKey(node.Addr))
			{
				return false;
			}

			var successor = node.GetSuccessor(0);
			var target = node.GetBranchTarget();
			var targetPred = target?.GetPredecessor(0);

			if (target == null)
			{
				return false;
			}

			// Reduce breaks
			if (loopFinder.IsLoopEnd(targetPred))
			{
				var loopStart = targetPred.GetBranchTarget();
				var loops = loopFinder.Find(loopStart);

				// Make sure that we're actually in the loop we're jumping to the end of.
				foreach (var loop in loops)
				{
					if (loop.HasNode(node))
					{
						collapsedNodes[node.Addr] = new BreakNode(node);

						node.RemoveEdgeTo(target);

						return true;
					}
				}
			}
			else if (successor != null && successor.GetSuccessor(0) == target && successor.Predecessors.Count < 3)
			{
				collapsedNodes[node.Addr] = new ElseNode(node);

				node.RemoveEdgeTo(successor);

				return true;
			}
			else
			{
				// TODO: This is bad. As far as I know, there aren't any TorqueScript compilers that
				//       produce gotos, but it is entirely possible to write one that does. It is bad
				//       to assume that anything that's not an else or a break is a continue, but for
				//       the first version of this decompiler, it'll do.
				//
				//       I just really hope it doesn't accidentally mark the wrong thing as a continue...
				//       Please let me know if it does, but only if it's not FUNCTIONALLY EQUIVALENT.
				//
				//       What I mean by that is this:
				//
				//       while (true)
				//       {
				//           if ( ... )
				//           {
				//               echo("if statement is true");
				//               continue;
				//           }
				//           echo("if statement is false");
				//       }
				//
				//      ...is FUNCTIONALLY EQUIVALENT to this:
				//
				//       while (true)
				//       {
				//           if ( ... )
				//               echo("if statement is true");
				//           else
				//               echo("if statement is false");
				//       }
				//
				//       and will look IDENTICAL in the TorqueScript bytecode.
				//
				//       So if it's something like that, please don't contact me about it. But if you DO
				//       find something that is marked incorrectly as a continue and is NOT functionally
				//       equivalent to something else, please let me know.
				collapsedNodes[node.Addr] = new ContinueNode(node);

				if (target != successor)
				{
					node.RemoveEdgeTo(target);
				}

				return true;
			}

			return false;
		}

		protected bool ReduceCyclic (ControlFlowNode node)
		{
			var isSelfLoop = node.GetBranchTarget() == node;

			if (node.Successors.Count != 1 && !isSelfLoop)
			{
				return false;
			}

			LoopNode loop;

			if (isSelfLoop)
			{
				loop = new LoopNode(node);

				node.RemoveEdgeTo(node);
				loop.AddNode(collapsedNodes.GetValueOrDefault(node.Addr));

				collapsedNodes[node.Addr] = loop;

				return true;
			}

			var next = node.GetSuccessor(0);

			if (!loopFinder.IsLoop(node, next) || next.Predecessors.Count > 1)
			{
				return false;
			}

			loop = new LoopNode(node);

			node.AddEdgeTo(next.GetSuccessor(0));
			loop.AddNode(collapsedNodes.GetValueOrDefault(node.Addr));
			loop.AddNode(ExtractCollapsed(next));

			collapsedNodes[node.Addr] = loop;

			return true;
		}

		protected CollapsedNode ExtractCollapsed (ControlFlowNode node)
		{
			graph.RemoveNode(node);

			return ExtractCollapsed(node.Addr) ?? new InstructionNode(node);
		}

		protected CollapsedNode ExtractCollapsed (uint key)
		{
			CollapsedNode node = null;

			if (collapsedNodes.ContainsKey(key))
			{
				node = collapsedNodes[key];
				collapsedNodes.Remove(key);
			}

			return node;
		}

		protected SequenceNode ExtractSequence (ControlFlowNode node)
		{
			var sequence = new SequenceNode(node.Addr);

			sequence.AddNode(GetCollapsed(node.Addr) ?? new InstructionNode(node));

			return sequence;
		}

		protected CollapsedNode GetCollapsed (uint key) => collapsedNodes.ContainsKey(key)
			? collapsedNodes[key]
			: null;

		protected bool IsUnconditional (ControlFlowNode node) => node?.LastInstruction is BranchInstruction branch
			&& branch.IsUnconditional;
	}
}
