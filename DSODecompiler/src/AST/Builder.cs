using DSODecompiler.AST.Nodes;
using DSODecompiler.ControlFlow;
using DSODecompiler.Disassembly;

using System;
using System.Collections.Generic;

namespace DSODecompiler.AST
{
    /// <summary>
    /// Builds an AST tree from a tree of <seealso cref="CollapsedNode"/>s.<br/><br/>
    ///
    /// This is an ugly class. I'm so sorry... I've just reached my burnout point and wanted to get
    /// this decompiler <em><strong>done</strong></em>.<br/><br/>
    ///
    /// TODO: Refactor?
    /// </summary>
    public class Builder
	{
		public class Exception : System.Exception
		{
			public Exception () { }
			public Exception (string message) : base(message) { }
			public Exception (string message, Exception inner) : base(message, inner) { }
		}

		protected NodeList list;
		protected NodeList parentList;

		/// <summary>
		/// Builds an AST tree from a tree of <seealso cref="CollapsedNode"/>s.<br/><br/>
		///
		/// The <paramref name="parentNodeList"/> is a bit of a hack due to the recursive nature of
		/// this process... Please ignore it :)
		/// </summary>
		/// <param name="root"></param>
		/// <param name="parentNodeList"></param>
		/// <returns></returns>
		public NodeList Build (CollapsedNode root, NodeList parentNodeList = null)
		{
			list = new();
			parentList = parentNodeList;

			Parse(root);

			return list;
		}

		protected void Parse (CollapsedNode root)
		{
			if (root.IsContinuePoint)
			{
				PushNode(new ContinuePointMarkerNode());
			}

			switch (root)
			{
				case InstructionNode node:
				{
					ParseInstructions(node);
					break;
				}

				case SequenceNode node:
				{
					ParseSequence(node);
					break;
				}

				case ConditionalNode node:
				{
					ParseConditional(node);
					break;
				}

				case LoopNode node:
				{
					ParseLoop(node);
					break;
				}

				case BreakNode node:
				{
					ParseBreak(node);
					break;
				}

				case ContinueNode node:
				{
					ParseContinue(node);
					break;
				}

				case FunctionNode node:
				{
					ParseFunction(node);
					break;
				}

				case ElseNode:
					break;

				default:
					throw new NotImplementedException();
			}
		}

		protected void ParseInstructions (InstructionNode node)
		{
			node.Instructions.ForEach(instruction =>
			{
				switch (instruction)
				{
					case BranchInstruction insn:
					{
						if (insn.IsLogicalOperator)
						{
							PushNode(new BinaryExpressionNode(insn, PopNode()));
						}

						break;
					}

					case ImmediateInstruction<string> insn:
					{
						var type = StringConstantNode.StringType.String;

						if (insn.IsTaggedString)
						{
							type = StringConstantNode.StringType.TaggedString;
						}
						else if (insn.IsIdentifier)
						{
							type = StringConstantNode.StringType.Identifier;
						}

						PushNode(new StringConstantNode(insn.Value, type));

						break;
					}

					case ImmediateInstruction<uint> insn:
					{
						PushNode(new ConstantNode<uint>(insn.Value));
						break;
					}

					case ImmediateInstruction<double> insn:
					{
						PushNode(new ConstantNode<double>(insn.Value));
						break;
					}

					case BinaryInstruction insn:
					{
						PushNode(new BinaryExpressionNode(insn, PopNode(), PopNode()));
						break;
					}

					case BinaryStringInstruction insn:
					{
						PushNode(new BinaryExpressionNode(insn)
						{
							Right = PopNode(),
							Left = PopNode(),
						});

						break;
					}

					case UnaryInstruction insn:
					{
						PushNode(new UnaryExpressionNode(insn, PopNode()));
						break;
					}

					case ReturnInstruction insn:
					{
						PushNode(new ReturnStatementNode()
						{
							Value = insn.ReturnsValue ? PopNode() : null,
						});

						break;
					}

					case PushFrameInstruction insn:
					{
						PushNode(new PushFrameNode());
						break;
					}

					case PushInstruction insn:
					{
						var expression = PopNode();
						var popped = PopNode();

						if (popped is not PushFrameNode frame)
						{
							throw new Exception($"Expected PushFrameNode, got {popped?.GetType().Name}");
						}

						frame.Nodes.Push(expression);
						PushNode(frame);

						break;
					}

					case CreateObjectInstruction insn:
					{
						PushNode(new ObjectNode(insn));
						break;
					}

					case AddObjectInstruction insn:
					{
						var stack = new Stack<Node>();

						Node node;

						while ((node = PopNode()) is AssignmentNode && node != null)
						{
							stack.Push(node);
						}

						if (node is not ObjectNode objectNode)
						{
							throw new Exception($"Expected ObjectNode, got {node.GetType().Name}");
						}

						objectNode.IsRoot = insn.PlaceAtRoot;

						while (stack.Count > 0)
						{
							objectNode.Slots.Push(stack.Pop());
						}

						PushNode(objectNode);

						break;
					}

					case EndObjectInstruction insn:
					{
						var stack = new Stack<Node>();

						Node node;

						while ((node = PopNode()) is not PushFrameNode && node != null)
						{
							stack.Push(node);
						}

						var frame = node as PushFrameNode;

						node = stack.Pop();

						if (node is not ObjectNode objectNode)
						{
							throw new Exception($"Expected ObjectNode, got {node.GetType().Name}");
						}

						while (stack.Count > 0)
						{
							objectNode.Subobjects.Push(stack.Pop());
						}

						objectNode.ClassNameExpression = frame.Nodes[0];
						objectNode.NameExpression = frame.Nodes[1];

						for (var i = 2; i < frame.Nodes.Count; i++)
						{
							objectNode.Arguments.Push(frame.Nodes[i]);
						}

						if (objectNode.IsRoot)
						{
							// Pop extra 0 uint at the beginning of every root object.
							PopNode();
						}

						PushNode(objectNode);

						break;
					}

					case CallInstruction insn:
					{
						var frame = PopNode() as PushFrameNode;
						var call = new FunctionCallNode(insn);

						frame.Nodes.ForEach(argument => call.Arguments.Push(argument));

						PushNode(call);

						break;
					}

					case AdvanceStringInstruction:
					{
						PushNode(new ConcatNode());
						break;
					}

					case AdvanceAppendInstruction insn:
					{
						PushNode(new ConcatNode(insn.Char));
						break;
					}

					case AdvanceCommaInstruction:
					{
						PushNode(new CommaCatNode());
						break;
					}

					case RewindStringInstruction:
					{
						var right = PopNode();
						var stringNode = PopNode();
						var left = PopNode();

						if (stringNode is not StringConcatNode concat)
						{
							throw new Exception($"Expected subclass of StringConcatNode, got {stringNode.GetType().Name}");
						}

						concat.Left = left;
						concat.Right = right;

						PushNode(concat);

						break;
					}

					// Terminate rewind means we don't do anything, so just remove the concat/string
					// node we pushed earlier.
					case TerminateRewindInstruction:
					{
						var expression = PopNode();
						var stringNode = PopNode();

						if (stringNode is not StringConcatNode)
						{
							throw new Exception($"Expected subclass of StringConcatNode, got {stringNode.GetType().Name}");
						}

						PushNode(expression);

						break;
					}

					case ObjectInstruction:
					case ObjectNewInstruction:
					{
						var field = new FieldNode();

						if (instruction is not ObjectNewInstruction)
						{
							field.ObjectExpr = PopNode();
						}

						PushNode(field);

						break;
					}

					case FieldInstruction insn:
					{
						var field = PopNode() as FieldNode;

						field.Name = insn.Name;

						PushNode(field);

						break;
					}

					case FieldArrayInstruction:
					{
						var field = PopNode() as FieldNode;

						field.ArrayIndex = PopNode();

						PushNode(field);

						break;
					}

					case VariableInstruction insn:
					{
						PushNode(new VariableNode(insn.Name));
						break;
					}

					case VariableArrayInstruction:
					{
						var stringNode = PopNode();

						if (stringNode is not StringConcatNode concat)
						{
							throw new Exception($"Expected subclass of StringConcatNode, got {stringNode.GetType().Name}");
						}

						var name = (concat.Left as ConstantNode<string>).Value;

						PushNode(new VariableNode(name)
						{
							ArrayIndex = concat.Right,
						});

						break;
					}

					case SaveVariableInstruction:
					case SaveFieldInstruction:
					{
						var node = PopNode();

						if (node is VariableFieldNode variableField)
						{
							PushNode(new AssignmentNode(variableField, PopNode()));
						}
						else if (node is BinaryExpressionNode binaryExpr)
						{
							PushNode(new AssignmentNode(binaryExpr));
						}
						else
						{
							throw new Exception($"Expected VariableFieldNode or BinaryExpressionNode, got {node.GetType().Name}");
						}

						break;
					}

					case AdvanceNullInstruction:
					case ConvertToTypeInstruction:
					case LoadVariableInstruction:
					case LoadFieldInstruction:
					case DebugBreakInstruction:
					case InvalidInstruction:
					case UnusedInstruction:
						break;

					case FunctionInstruction insn:
						throw new Exception($"Unexpected function instruction at {insn.Addr}");

					default:
						throw new NotImplementedException($"Failed to parse unknown instruction {instruction.GetType().Name}");
				}
			});
		}

		protected void ParseSequence (SequenceNode node)
		{
			node.Nodes.ForEach(collapsed =>
			{
				PushNode(ParseChild(collapsed, list));
			});
		}

		protected void ParseConditional (ConditionalNode conditional)
		{
			var testExpr = PopNode();

			if (testExpr is BinaryExpressionNode binaryExpr && binaryExpr.IsLogicalOperator && binaryExpr.Right == null)
			{
				binaryExpr.Right = ParseChildExpression(conditional.Then, list);

				PushNode(binaryExpr);
			}
			else
			{
				var ifNode = new IfNode(testExpr)
				{
					Then = ParseChild(conditional.Then, list),
					Else = conditional.Else != null ? ParseChild(conditional.Else, list) : new(),
				};

				Node node = ifNode;

				// Collapse if-loop into while/for loop.
				// TODO: Maybe do this in a separate `Humanizer` class instead of doing it inline?
				if (ifNode.Then.Count == 1
					&& !ifNode.HasElse
					&& ifNode.Then[0] is LoopStatementNode loop
					&& !loop.WasCollapsed
					&& Equals(ifNode.TestExpression, loop.TestExpression))
				{
					node = loop;
					loop.WasCollapsed = true;

					if ((loop.InitExpression == null && (PeekNode()?.IsExpression ?? false))
						&& (loop.EndExpression != null || (loop.Body.Count > 0 && loop.Body[^1].IsExpression)))
					{
						loop.InitExpression ??= PopNode();
						loop.EndExpression ??= loop.Body.Pop();
					}

					if (loop.Body.Peek() is ContinuePointMarkerNode)
					{
						loop.Body.Pop();
					}
				}

				PushNode(node);
			}
		}

		protected void ParseLoop (LoopNode node)
		{
			var loop = new LoopStatementNode();

			node.Body.ForEach(child =>
			{
				loop.Body.Push(ParseChild(child, loop.Body));
			});

			loop.TestExpression = loop.Body.Pop();
			loop.EndExpression = loop.Body.Pop();

			if (loop.Body.Peek() is ContinuePointMarkerNode)
			{
				loop.Body.Pop();
			}
			else if (loop.EndExpression != null)
			{
				loop.Body.Push(loop.EndExpression);
				loop.EndExpression = null;
			}

			PushNode(loop);
		}

		protected void ParseBreak (BreakNode _) => PushNode(new BreakStatementNode());
		protected void ParseContinue (ContinueNode _) => PushNode(new ContinueStatementNode());

		protected void ParseFunction (FunctionNode node)
		{
			var instruction = node.Instruction;
			var function = new FunctionStatementNode(instruction.Name, instruction.Namespace, instruction.Package)
			{
				Body = ParseChild(node.Body, list),
			};

			// The TorqueScript compiler always tacks on an extra return at the ends of functions, so
			// we're just going to pop it off.
			if (function.Body.Count > 0 && function.Body[^1] is ReturnStatementNode ret && !ret.ReturnsValue)
			{
				function.Body.Pop();
			}

			instruction.Arguments.ForEach(function.Arguments.Add);

			PushNode(function);
		}

		/// <summary>
		/// For parsing a <seealso cref="CollapsedNode"/> that is a child of another <seealso cref="CollapsedNode"/>.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="parentNodeList"></param>
		/// <returns></returns>
		protected NodeList ParseChild (CollapsedNode node, NodeList parentNodeList)
		{
			return new Builder().Build(node, parentNodeList);
		}

		/// <summary>
		/// This is for when we expect a single expression, instead of a list of nodes.<br/><br/>
		/// </summary>
		/// <param name="node"></param>
		/// <param name="parentNodeList"></param>
		/// <exception cref="Exception">When we got a node list with more or fewer than 1 node.</exception>
		/// <returns></returns>
		protected Node ParseChildExpression (CollapsedNode node, NodeList parentNodeList)
		{
			var list = ParseChild(node, parentNodeList);
			var count = list.Count;

			if (count != 1)
			{
				throw new Exception($"Expected expression, got node list with {count} {(count == 1 ? "child" : "children")}");
			}

			return list[0];
		}

		protected Node PushNode (Node node) => list.Push(node);

		/// <summary>
		/// Pops a node from the list, and if that fails, it pops a node from the parent list.
		/// </summary>
		/// <returns></returns>
		protected Node PopNode ()
		{
			var node = list.Pop();

			if (node == null && parentList != null)
			{
				node = parentList.Pop();
			}

			return node;
		}

		protected Node PeekNode ()
		{
			var node = list.Peek();

			if (node == null && parentList != null)
			{
				node = parentList.Peek();
			}

			return node;
		}
	}
}
