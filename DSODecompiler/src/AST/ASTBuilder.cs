﻿using DSODecompiler.ControlFlow;
using DSODecompiler.Disassembly;
using DSODecompiler.Util;

using System;
using System.Collections.Generic;

namespace DSODecompiler.AST
{
	public class ASTBuilder
	{
		public class Exception : System.Exception
		{
			public Exception () { }
			public Exception (string message) : base(message) { }
			public Exception (string message, Exception inner) : base(message, inner) { }
		}

		protected ASTNodeList list;
		protected ASTNodeList parentList;

		/// <summary>
		/// Builds an AST tree from a tree of <seealso cref="CollapsedNode"/>s.<br/><br/>
		///
		/// The <paramref name="parentNodeList"/> is a bit of a hack due to the recursive nature of
		/// this process... Please ignore it :)
		/// </summary>
		/// <param name="root"></param>
		/// <param name="parentNodeList"></param>
		/// <returns></returns>
		public ASTNodeList Build (CollapsedNode root, ASTNodeList parentNodeList = null)
		{
			list = new();
			parentList = parentNodeList;

			Parse(root);

			return list;
		}

		protected void Parse (CollapsedNode root)
		{
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
							PushNode(new BinaryExpressionNode(insn));
						}

						break;
					}

					case ImmediateInstruction<string> insn:
					{
						PushNode(new ConstantNode<string>(insn.Value));
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
						var expression = PopNode(parentFallback: true);
						var popped = PopNode(parentFallback: true);

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
						var stack = new Stack<ASTNode>();

						ASTNode node;

						while ((node = PopNode(parentFallback: true)) is AssignmentNode && node != null)
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
						var stack = new Stack<ASTNode>();

						ASTNode node;

						while ((node = PopNode(parentFallback: true)) is not PushFrameNode && node != null)
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
							PopNode(parentFallback: true);
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
						break;

					default:
						throw new NotImplementedException();
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

		protected void ParseConditional (ConditionalNode node)
		{
			var testExpr = PopNode(parentFallback: true);

			if (testExpr is BinaryExpressionNode binaryExpr && binaryExpr.IsLogicalOperator)
			{
				binaryExpr.Left = PopNode(parentFallback: true);
				binaryExpr.Right = ParseChildExpression(node.Then);

				PushNode(binaryExpr);
			}
			else
			{
				PushNode(new IfNode(testExpr)
				{
					Then = ParseChild(node.Then),
					Else = node.Else != null ? ParseChild(node.Else) : null,
				});
			}
		}

		protected void ParseLoop (LoopNode node)
		{
			var body = new ASTNodeList();

			node.Body.ForEach(child =>
			{
				body.Push(ParseChild(child));
			});

			PushNode(new LoopStatementNode(body.Pop())
			{
				Body = body,
			});
		}

		protected void ParseBreak (BreakNode _) => PushNode(new BreakStatementNode());
		protected void ParseContinue (ContinueNode _) => PushNode(new ContinueStatementNode());

		protected void ParseFunction (FunctionNode node)
		{
			var instruction = node.Instruction;
			var function = new FunctionStatementNode(instruction.Name, instruction.Name, instruction.Package)
			{
				Body = ParseChild(node.Body),
			};

			instruction.Arguments.ForEach(function.Arguments.Add);

			PushNode(function);
		}

		/// <summary>
		/// For parsing a <seealso cref="CollapsedNode"/> that is a child of another <seealso cref="CollapsedNode"/>.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="parentNodeList"></param>
		/// <returns></returns>
		protected ASTNodeList ParseChild (CollapsedNode node, ASTNodeList parentNodeList = null)
		{
			return new ASTBuilder().Build(node, parentNodeList);
		}

		/// <summary>
		/// This is for when we expect a single expression, instead of a list of nodes.<br/><br/>
		/// </summary>
		/// <param name="node"></param>
		/// <param name="parentNodeList"></param>
		/// <exception cref="Exception">When we got a node list with more or fewer than 1 node.</exception>
		/// <returns></returns>
		protected ASTNode ParseChildExpression (CollapsedNode node, ASTNodeList parentNodeList = null)
		{
			var list = ParseChild(node, parentNodeList);
			var count = list.Count;

			if (count != 1)
			{
				throw new Exception($"Expected expression, got node list with {count} {(count == 1 ? "child" : "children")}");
			}

			return list[0];
		}

		protected ASTNode PushNode (ASTNode node) => list.Push(node);
		protected ASTNode PopNode (bool parentFallback = false)
		{
			var node = list.Pop();

			if (node == null && parentFallback && parentList != null)
			{
				node = parentList.Pop();
			}

			return node;
		}

		protected ASTNode PeekNode (bool parentFallback = false)
		{
			var node = list.Peek();

			if (node == null && parentFallback && parentList != null)
			{
				node = parentList.Peek();
			}

			return node;
		}
	}
}