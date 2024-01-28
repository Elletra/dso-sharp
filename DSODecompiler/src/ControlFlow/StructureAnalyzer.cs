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
	/// TODO: Refactor?<br/><br/>
	///
	/// <strong>Sources:</strong><br/><br/>
	///
	/// <list type="number">
	/// <item>
	/// <see href="https://www.usenix.org/system/files/conference/usenixsecurity13/sec13-paper_schwartz.pdf">
	/// "Native x86 Decompilation Using Semantics-Preserving Structural Analysis and Iterative
	/// Control-Flow Structuring"</see> by Edward J. Schwartz, JongHyup Lee, Maverick Woo, and David Brumley.
	/// </item>
	/// </list>
	/// </summary>
	public class StructureAnalyzer
	{
		public class Exception : System.Exception
		{
			public Exception () { }
			public Exception (string message) : base(message) { }
			public Exception (string message, Exception inner) : base(message, inner) { }
		}

		private ControlFlowGraph graph;
		private Dictionary<uint, CollapsedNode> collapsedNodes;
		private LoopFinder loopFinder;
		private Queue<ControlFlowNode> unreducedJumps;
		private HashSet<uint> continuePoints;

		public CollapsedNode Analyze (ControlFlowGraph cfg)
		{
			graph = cfg;
			collapsedNodes = new();
			loopFinder = new();
			unreducedJumps = new();
			continuePoints = new();

			// Edge case where there's only one node, but it still needs to be reduced!
			if (graph.Count == 1)
			{
				AddCollapsed(graph.EntryPoint, new InstructionNode(graph.GetEntryPoint()));
			}

			while (graph.Count > 1)
			{
				var noProgress = true;

				foreach (ControlFlowNode node in graph.PostorderDFS(graph.EntryPoint))
				{
					var reduced = true;

					while (reduced)
					{
						reduced = ReduceNode(node);

						if (reduced)
						{
							noProgress = false;
						}
					}
				}

				if (noProgress)
				{
					ReduceUnreducedJumps();
				}
			}

			var entryPoint = graph.GetNodes()[0].Addr;

			graph.EntryPoint = entryPoint;

			if (graph.IsFunction)
			{
				AddCollapsed(entryPoint, new FunctionNode(graph.FunctionInstruction, ExtractCollapsed(entryPoint)));
			}

			return collapsedNodes[graph.EntryPoint];
		}

		private bool ReduceNode (ControlFlowNode node)
		{
			var reduced = ReduceAcyclic(node);

			if (!reduced && loopFinder.IsLoopStart(node))
			{
				reduced = ReduceCyclic(node);
			}

			return reduced;
		}

		private bool ReduceAcyclic (ControlFlowNode node)
		{
			if (node.LastInstruction is BranchInstruction branch && branch.IsUnconditional && !collapsedNodes.ContainsKey(node.Addr))
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

		private bool ReduceSequence (ControlFlowNode node)
		{
			var next = node.GetSuccessor(0);

			if (!next.IsSequential)
			{
				return false;
			}

			var sequence = ExtractSequence(node);

			node.AddEdgeTo(next.GetSuccessor(0));
			sequence.AddNode(ExtractCollapsed(next));

			AddCollapsed(node.Addr, sequence);

			return true;
		}

		private bool ReduceConditional (ControlFlowNode node)
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

			// Try to collapse an else node if we can.
			if (@else != null)
			{
				var elsePredecessor = @else.GetPredecessor(0);

				if (!collapsedNodes.ContainsKey(elsePredecessor.Addr)
					&& elsePredecessor?.GetBranchTarget() == elseSuccessor
					&& elseSuccessor.Predecessors.Count < 3
					&& @else.Predecessors.Count < 3)
				{
					CollapseUnconditional(elsePredecessor, new ElseNode(elsePredecessor));
					elsePredecessor.RemoveEdgeTo(@else);

					return true;
				}
			}

			if (thenSuccessor == @else)
			{
				/* if-then */

				if (then.IsSequential && !loopFinder.IsLoopEnd(then))
				{
					var sequence = new SequenceNode();

					sequence.AddNode(ExtractCollapsed(node, remove: false));
					sequence.AddNode(new ConditionalNode()
					{
						Then = ExtractCollapsed(then),
					});

					AddCollapsed(node.Addr, sequence);

					reduced = true;
				}
			}
			else if (thenSuccessor == elseSuccessor)
			{
				/* if-then-else */

				if (then.IsSequential && @else.IsSequential && !loopFinder.IsLoopEnd(then) && !loopFinder.IsLoopEnd(@else))
				{
					var sequence = new SequenceNode();

					sequence.AddNode(ExtractCollapsed(node, remove: false));
					sequence.AddNode(new ConditionalNode()
					{
						Then = ExtractCollapsed(then),
						Else = ExtractCollapsed(@else),
					});

					AddCollapsed(node.Addr, sequence);

					node.AddEdgeTo(thenSuccessor);

					reduced = true;
				}
			}

			return reduced;
		}

		private bool ReduceUnconditional (ControlFlowNode node)
		{
			if (!IsUnconditional(node) || loopFinder.IsLoopEnd(node))
			{
				return false;
			}

			var target = node.GetBranchTarget();
			var targetPred = target?.GetPredecessor(0);

			if (target == null)
			{
				return false;
			}

			if (!loopFinder.IsLoopEnd(targetPred))
			{
				if (!unreducedJumps.Contains(node))
				{
					unreducedJumps.Enqueue(node);
				}
			}
			else
			{
				/* Reduce breaks */

				var loopStart = targetPred.GetBranchTarget();
				var loops = loopFinder.Find(loopStart);

				// Make sure that we're actually in the loop we're jumping to the end of.
				foreach (var loop in loops)
				{
					if (loop.HasNode(node))
					{
						CollapseUnconditional(node, new BreakNode(node));
						node.RemoveEdgeTo(target);

						return true;
					}
				}
			}

			return false;
		}

		private bool ReduceCyclic (ControlFlowNode node)
		{
			var isSelfLoop = node.GetBranchTarget() == node;

			if (node.Successors.Count != 1 && !isSelfLoop)
			{
				return false;
			}

			LoopNode loop;

			if (isSelfLoop)
			{
				loop = new();

				node.RemoveEdgeTo(node);
				loop.AddNode(ExtractCollapsed(node, remove: false));
			}
			else
			{
				var next = node.GetSuccessor(0);

				if (!loopFinder.IsLoop(node, next) || next.Predecessors.Count > 1)
				{
					return false;
				}

				loop = new();

				node.AddEdgeTo(next.GetSuccessor(0));
				loop.AddNode(ExtractCollapsed(node, remove: false));
				loop.AddNode(ExtractCollapsed(next));
			}

			AddCollapsed(node.Addr, loop);

			return true;
		}

		private void ReduceUnreducedJumps ()
		{
			while (unreducedJumps.Count > 0)
			{
				var node = unreducedJumps.Dequeue();

				if (!collapsedNodes.ContainsKey(node.Addr) && node.Instructions.Count > 0 && node.IsUnconditional)
				{
					// TODO: This is bad. As far as I know, there aren't any TorqueScript compilers that
					//       produce gotos, but it is entirely possible to write one that does. It is bad
					//       to assume that anything that's not an else or a break is a continue, but for
					//       the first version of this decompiler, it'll do.
					//
					//       I just really hope it doesn't accidentally mark the wrong thing as a continue...
					//       Please let me know if it does, but only if it's not functionally equivalent.
					//
					//       What I mean by that is this:
					//
					//       while (true)
					//       {
					//           if (...)
					//           {
					//               echo("if statement is true");
					//               continue;
					//           }
					//           echo("if statement is false");
					//       }
					//
					//      ...is functionally equivalent to this:
					//
					//       while (true)
					//       {
					//           if (...)
					//               echo("if statement is true");
					//           else
					//               echo("if statement is false");
					//       }
					//
					//       and will produce the exact same TorqueScript bytecode.
					//
					//       So if it's something like that, please don't contact me about it. But if
					//       you DO find something that is marked incorrectly as a continue and is NOT
					//       functionally equivalent to something else, please let me know.
					CollapseUnconditional(node, new ContinueNode(node));

					if (node.GetBranchTarget() != node.GetSuccessor(0))
					{
						node.RemoveEdgeTo(node.GetBranchTarget());
					}

					break;
				}
			}
		}

		private CollapsedNode ExtractCollapsed (ControlFlowNode node, bool remove = true)
		{
			var collapsed = ExtractCollapsed(node.Addr);

			if (collapsed == null)
			{
				if (node.IsUnconditional)
				{
					// TODO: This runs into the same issue as above -- will need to fix at some point!
					var continueNode = new ContinueNode(node);

					CollapseUnconditional(node, continueNode);

					collapsed = continueNode;
				}
				else
				{
					collapsed = new InstructionNode(node);
				}
			}

			if (remove)
			{
				graph.RemoveNode(node);
			}

			// TODO: Have I mentioned this is a hack yet?
			collapsed.IsContinuePoint = continuePoints.Contains(node.Addr);

			return collapsed;
		}

		private CollapsedNode ExtractCollapsed (uint key)
		{
			CollapsedNode node = null;

			if (collapsedNodes.ContainsKey(key))
			{
				node = collapsedNodes[key];
				collapsedNodes.Remove(key);
			}

			return node;
		}

		private SequenceNode ExtractSequence (ControlFlowNode node)
		{
			var sequence = new SequenceNode();

			sequence.AddNode(ExtractCollapsed(node, remove: false));

			return sequence;
		}

		private void CollapseUnconditional<T> (ControlFlowNode node, T unconditional) where T : UnconditionalNode
		{
			if (unconditional is ContinueNode)
			{
				continuePoints.Add(node.GetBranchTarget().Addr);
			}

			if (node.Instructions.Count <= 0)
			{
				AddCollapsed(node.Addr, unconditional);
			}
			else
			{
				var sequence = new SequenceNode();

				sequence.AddNode(ExtractCollapsed(node, remove: false));
				sequence.AddNode(unconditional);

				AddCollapsed(node.Addr, sequence);
			}
		}

		private void AddCollapsed (uint addr, CollapsedNode collapsed)
		{
			collapsedNodes[addr] = collapsed;
			collapsed.IsContinuePoint = continuePoints.Contains(addr);
		}

		private CollapsedNode GetCollapsed (uint key) => collapsedNodes.ContainsKey(key)
			? collapsedNodes[key]
			: null;

		private bool IsUnconditional (ControlFlowNode node) => node?.IsUnconditional ?? false;
	}
}
