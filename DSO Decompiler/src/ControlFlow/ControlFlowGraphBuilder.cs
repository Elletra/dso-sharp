using System.Collections.Generic;

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
					graphs.Add(new(func));

					currGraph = null;
					currNode = null;

					continue;
				}

				var isFuncStart = instruction is FunctionInstruction;

				if (currGraph == null || isFuncStart || IsFunctionEnd(instruction))
				{
					currGraph = isFuncStart ? new(instruction as FunctionInstruction) : new();
					currNode = null;

					graphs.Add(currGraph);
				}

				if (!isFuncStart)
				{
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
		}

		protected void CreateAndConnect (Instruction instruction)
		{
			var node = currGraph.AddOrGet(instruction.Addr);

			if (currNode != null && ShouldConnectToNext(currNode.LastInstruction))
			{
				currGraph.AddEdge(currNode, node);
			}

			currNode = node;
		}

		protected bool IsFunctionEnd (Instruction instruction)
		{
			return currGraph.IsFunction && instruction.Addr >= currGraph.FunctionHeader.EndAddr;
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

		protected bool ShouldConnectToNext (Instruction instruction)
		{
			return instruction is not BranchInstruction branch || !branch.IsUnconditional;
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
						graph.AddEdge(node, graph.AddOrGet(branch.TargetAddr));
					}
				}
			}
		}
	}
}
