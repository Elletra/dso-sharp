using System;

namespace DSODecompiler.ControlFlow
{
	public class StructureAnalyzer
	{
		protected ControlFlowGraph graph = null;

		public void Analyze (ControlFlowGraph cfg)
		{
			graph = cfg;

			while (cfg.Count > 1)
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

			// A hacky way to set the entry point to the remaining node.
			foreach (ControlFlowNode node in cfg.GetNodes())
			{
				cfg.EntryPoint = node.Addr;
				break;
			}
		}

		protected bool ReduceNode (ControlFlowNode node)
		{
			return ReduceAcyclic(node);
		}

		protected bool ReduceAcyclic (ControlFlowNode node)
		{
			switch (node.Successors.Count)
			{
				case 0:
					Console.WriteLine($"NO successors {node.Addr}");
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

			Console.WriteLine($"ReduceSequence({node.Addr}) next: {next.Addr}");

			if (next.Predecessors.Count > 1 || next.Successors.Count > 1)
			{
				Console.WriteLine("Pred or Succ Count > 1");
				return false;
			}

			Console.WriteLine("!!! Pred or Succ Count <= 1");

			var sequence = new SequenceNode(node.Addr);

			sequence.AddNodes(node.CollapsedNode, next.CollapsedNode);

			node.CollapsedNode = sequence;

			if (next.Successors.Count > 0)
			{
				node.AddEdgeTo(next.Successors[0]);
			}

			graph.RemoveNode(next);

			//PrintNode(sequence, 0);

			return true;
		}

		protected bool ReduceConditional (ControlFlowNode node)
		{
			var reduced = false;

			ControlFlowNode then = node.GetSuccessor(0);
			ControlFlowNode @else = node.GetSuccessor(1);
			ControlFlowNode thenSuccessor = then.GetSuccessor(0);
			ControlFlowNode elseSuccessor = @else.GetSuccessor(0);

			if (thenSuccessor == @else)
			{
				if (then.Predecessors.Count > 1)
				{
					Console.WriteLine($"thenSucc other preds {node.Addr}");
				}
				else
				{
					Console.WriteLine($"{node.Addr} :: If-then");

					var conditional = new ConditionalNode(node.Addr)
					{
						Then = then.CollapsedNode,
						Else = null,
					};

					if (node.CollapsedNode is InstructionNode instructionNode)
					{
						instructionNode.Instructions.ForEach(conditional.Instructions.Add);
					}
					else
					{
						throw new Exception($">>>>1 {node.CollapsedNode}");
					}

					node.CollapsedNode = conditional;

					graph.RemoveNode(then);

					reduced = true;
				}
			}
			else if (elseSuccessor != null && thenSuccessor == elseSuccessor)
			{
				if (then.Predecessors.Count > 1 || @else.Predecessors.Count > 1)
				{
					Console.WriteLine($"elseSucc other preds {node.Addr}");
				}
				else
				{
					Console.WriteLine($"{node.Addr} :: If-then-else");

					var conditional = new ConditionalNode(node.Addr)
					{
						Then = then.CollapsedNode,
						Else = @else.CollapsedNode,
					};

					if (node.CollapsedNode is InstructionNode instructionNode)
					{
						instructionNode.Instructions.ForEach(conditional.Instructions.Add);
					}
					else
					{
						throw new Exception($">>>>2 {node.CollapsedNode}");
					}

					node.CollapsedNode = conditional;

					graph.RemoveNode(then);
					graph.RemoveNode(@else);
					graph.AddEdge(node, thenSuccessor);

					reduced = true;
				}
			}
			else
			{
				Console.WriteLine($"{node.Addr} :: <FAILED>");
			}

			return reduced;
		}

		public static void PrintIndent (int indent)
		{
			for (var i = 0; i < indent; i++)
			{
				Console.Write("\t");
			}
		}

		public static void PrintNode (CollapsedNode collapsed, int indent)
		{
			if (indent == 0)
			{
				Console.WriteLine("\n================\n");
			}

			PrintIndent(indent);

			if (collapsed == null)
			{
				Console.WriteLine("<NULL>");
				return;
			}

			Console.WriteLine($"+ [{collapsed.Addr}] {collapsed}");
			PrintIndent(indent);

			switch (collapsed)
			{
				case InstructionNode node:
				{
					PrintIndent(indent - 1);

					Console.WriteLine("> Instructions:");

					foreach (var insn in node.Instructions)
					{
						PrintIndent(indent + 1);
						Console.WriteLine(insn);
					}

					Console.WriteLine("");

					if (collapsed is ConditionalNode cond)
					{
						PrintIndent(indent);
						Console.WriteLine("> Then:");
						PrintNode(cond.Then, indent + 1);

						PrintIndent(indent);
						Console.WriteLine("> Else:");
						PrintNode(cond.Else, indent + 1);
					}
					else if (collapsed is LoopNode loop)
					{
						PrintIndent(indent);
						Console.WriteLine("> Body:");

						foreach (var child in loop.Body)
						{
							PrintNode(child, indent + 1);
						}
					}

					break;
				}

				case SequenceNode node:
				{
					PrintIndent(indent - 1);

					Console.WriteLine("> Nodes:");

					foreach (var child in node.Nodes)
					{
						PrintNode(child, indent + 1);
					}

					break;
				}
			}
		}
	}
}
