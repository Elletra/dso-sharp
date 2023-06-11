using System.Collections.Generic;
using DSODecompiler.Disassembly;

namespace DSODecompiler.ControlFlow
{
	/// <summary>
	/// Builds a <see cref="ControlFlowGraph"/> from disassembly.
	/// </summary>
	public class ControlFlowGraphBuilder
	{
		public class Exception : System.Exception
		{
			public Exception () {}
			public Exception (string message) : base(message) {}
			public Exception (string message, Exception inner) : base(message, inner) {}
		}

		protected Dictionary<uint, ControlFlowGraph> graphs;

		public Dictionary<uint, ControlFlowGraph> Build (Disassembly.Disassembly disassembly)
		{
			graphs = new();

			foreach (var block in disassembly.GetSplitInstructions())
			{
				BuildInitialGraph(disassembly, block);
			}

			ConnectBranches();

			return graphs;
		}

		/// <summary>
		/// Builds the initial <see cref="ControlFlowGraph"/> (CFG) from the disassembly.<br/><br/>
		///
		/// CFG nodes start at branch targets and end at branch instructions. Usually, returns also
		/// end CFG nodes, but for our purposes, they don't. We want that juicy unreachable code!!
		/// </summary>
		/// <param name="disassembly"></param>
		/// <param name="block"></param>
		protected void BuildInitialGraph (Disassembly.Disassembly disassembly, InstructionBlock block)
		{
			var graph = CreateGraph(block.First.Addr);

			ControlFlowNode currNode = null;

			foreach (var instruction in block)
			{
				if (currNode == null)
				{
					/* Start of the code block. */

					currNode = graph.AddNode(instruction.Addr);
				}
				else if (disassembly.HasBranchTarget(instruction.Addr))
				{
					/* Branch targets start CFG nodes. */

					var newNode = graph.AddNode(instruction.Addr);

					currNode.AddEdgeTo(newNode);
					currNode = newNode;
				}
				else if (currNode.LastInstruction is BranchInstruction branch)
				{
					/* Branch instructions end CFG nodes. */

					var newNode = graph.AddNode(instruction.Addr);

					// If the branch is unconditional, then there's no way to get to the next
					// sequential instruction from it.
					if (branch.IsConditional)
					{
						currNode.AddEdgeTo(newNode);
					}

					currNode = newNode;
				}

				currNode.Instructions.Add(instruction);
			}
		}

		protected ControlFlowGraph CreateGraph (uint addr)
		{
			var graph = new ControlFlowGraph();

			graphs[addr] = graph;

			return graph;
		}

		/// <summary>
		/// Connect branch nodes to their targets.<br/><br/>
		///
		/// It's much easier to do this in a second pass.
		/// </summary>
		protected void ConnectBranches ()
		{
			foreach (var (_, graph) in graphs)
			{
				foreach (var node in graph.GetNodes())
				{
					if (node.LastInstruction is BranchInstruction branch && !graph.AddEdge(node.Addr, branch.TargetAddr))
					{
						throw new Exception($"Tried to add edge to nonexistent node ({node.Addr}=>{branch.TargetAddr})");
					}
				}
			}
		}
	}
}
