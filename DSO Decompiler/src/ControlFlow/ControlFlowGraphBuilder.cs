﻿using System.Collections.Generic;

using DSODecompiler.Disassembler;

namespace DSODecompiler.ControlFlow
{
	public class ControlFlowGraphBuilder
	{
		protected List<ControlFlowGraph> graphs = null;
		protected ControlFlowGraph currGraph = null;
		protected ControlFlowNode currNode = null;
		protected Disassembly disassembly = null;

		public List<ControlFlowGraph> Build (Disassembly disasm)
		{
			disassembly = disasm;
			graphs = new();
			currGraph = null;
			currNode = null;

			BuildInitialGraph();
			ConnectBranches();

			return graphs;
		}

		protected void BuildInitialGraph ()
		{
			foreach (var instruction in disassembly)
			{
				/* Special handling for empty functions. */
				if (instruction is FunctionInstruction func && !func.HasBody)
				{
					var graph = new ControlFlowGraph();
					var node = new ControlFlowNode(func.Addr);

					node.Instructions.Add(func);

					graphs.Add(graph);
					graph.Add(func.Addr, node);

					graph.EntryPoint = graph.Get(func.Addr);

					currGraph = null;
					currNode = null;

					continue;
				}

				var isFuncStart = instruction is FunctionInstruction;

				if (currGraph == null || isFuncStart || IsFunctionEnd(instruction))
				{
					currGraph = new();
					currNode = null;

					graphs.Add(currGraph);
				}

				if (currNode == null || IsBlockStart(instruction) || IsBlockEnd(currNode.LastInstruction))
				{
					CreateAndConnect(instruction);
				}

				if (currGraph.EntryPoint == null)
				{
					currGraph.EntryPoint = currNode;
				}

				currNode.Instructions.Add(instruction);
			}
		}

		protected void CreateAndConnect (Instruction instruction)
		{
			var node = currGraph.AddOrGet(instruction.Addr);

			if (currNode != null)
			{
				currGraph.AddEdge(currNode, node);
			}

			currNode = node;
		}

		protected bool IsFunctionEnd (Instruction instruction)
		{
			var first = currGraph.EntryPoint.FirstInstruction;

			if (first is FunctionInstruction func)
			{
				return instruction.Addr >= func.EndAddr;
			}

			return false;
		}

		protected bool IsBlockStart (Instruction instruction)
		{
			return disassembly.HasBranchTarget(instruction.Addr) || disassembly.HasFunctionEnd(instruction.Addr);
		}

		protected bool IsBlockEnd (Instruction instruction)
		{
			/* For most CFG implementations, return statements also end blocks, but for our purposes they don't... */
			return instruction is BranchInstruction;
		}

		/**
		 * We do this in a separate function/pass because we want branch targets to come after the
		 * adjacent nodes, and it's much simpler to just do it in a second pass.
		 */
		protected void ConnectBranches ()
		{
			foreach (var graph in graphs)
			{
				foreach (var node in graph)
				{
					if (node.LastInstruction is BranchInstruction branch)
					{
						if (!graph.Has(branch.TargetAddr))
						{
							throw new KeyNotFoundException($"Branch to non-existent CFG node at {branch.TargetAddr}");
						}

						graph.AddEdge(node, graph.Get(branch.TargetAddr));
					}
				}
			}
		}
	}
}
