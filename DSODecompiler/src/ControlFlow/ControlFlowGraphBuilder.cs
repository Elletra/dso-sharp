﻿using System.Collections.Generic;
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
		/// Builds the initial <see cref="ControlFlowGraph"/> (CFG) from disassembly.<br/><br/>
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
					graph.EntryPoint = currNode.Addr;
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

					currNode.AddEdgeTo(newNode);

					currNode = newNode;
				}

				if (instruction is FunctionInstruction function)
				{
					if (graph.FunctionInstruction != null)
					{
						// TODO: Maybe support nested functions someday??
						throw new Exception($"Nested function detected at {function.Addr}");
					}

					graph.FunctionInstruction = function;
				}
				else
				{
					currNode.AddInstruction(instruction);
				}
			}
		}

		/// <summary>
		/// A hack for an edge case where a function declaration follows a conditional. We insert a
		/// dummy node for the next address so they connect properly.
		/// </summary>
		protected void CreateDummyNode (ControlFlowGraph graph, uint addr)
		{
			graph.AddNode(addr).IsDummyNode = true;
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
				foreach (ControlFlowNode node in graph.GetNodes())
				{
					if (node.LastInstruction is not BranchInstruction branch)
					{
						continue;
					}

					// Gross hack
					if (!graph.HasNode(branch.TargetAddr))
					{
						CreateDummyNode(graph, branch.TargetAddr);
					}

					graph.AddEdge(node.Addr, branch.TargetAddr);
				}
			}
		}
	}
}
