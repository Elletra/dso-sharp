﻿using DSODecompiler.AST.Nodes;
using DSODecompiler.Opcodes;

using System;
using System.Collections;
using System.Collections.Generic;

namespace DSODecompiler.CodeGeneration
{
	public class TokenStream : IEnumerable<string>
	{
		private readonly List<string> stream = new();

		public int Count => stream.Count;
		public string this[int index] => stream[index];

		public void Push (string token) => stream.Add(token);
		public void Push (params string[] tokens) => stream.AddRange(tokens);

		public string Pop ()
		{
			if (stream.Count <= 0)
			{
				return null;
			}

			var token = stream[^1];

			stream.RemoveAt(stream.Count - 1);

			return token;
		}

		public IEnumerator<string> GetEnumerator () => stream.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator () => stream.GetEnumerator();
	}

	/// <summary>
	/// Giant clusterfuck class that probably needs refactoring.
	/// </summary>
	public class TokenStreamGenerator
	{
		protected TokenStream tokens;
		protected NodeList list;
		protected int indent = 0;

		public TokenStream Generate (NodeList nodeList)
		{
			tokens = new();
			list = nodeList;

			Generate();

			return tokens;
		}

		protected void Generate () => WriteStatementList(list);

		/// <summary>
		/// Whether a node in a statement list should have a semicolon appended to it.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		protected bool ShouldAppendSemicolon (Node node) => node switch
		{
			PackageNode => true,
			FunctionStatementNode => false,
			IfNode => false,
			LoopStatementNode => false,
			BreakStatementNode => true,
			ContinueStatementNode => true,
			ContinuePointMarkerNode => false,
			ReturnStatementNode => true,
			FunctionCallNode => true,
			AssignmentNode => true,
			NewObjectNode => true,

			_ => true,
		};

		protected void WriteStatementList (NodeList list)
		{
			foreach (var node in list)
			{
				WriteIndent();
				Write(node);

				if (ShouldAppendSemicolon(node))
				{
					Write(";");
				}

				Write("\n");
			}
		}

		protected void WriteExpressionList (NodeList list, Node except = null)
		{
			foreach (var node in list)
			{
				if (except == null || node != except)
				{
					Write(node, addParens: false, isExpr: true);

					if (node != list[^1])
					{
						Write(",", " ");
					}
				}
			}
		}

		protected void Write (Node node) => Write(node, addParens: false);

		protected void Write (Node node, bool addParens, bool isExpr = false)
		{
			if (node != null)
			{
				if (addParens)
				{
					Write("(");
				}

				// I wish there was a shorthand for this, but using a switch expression just causes
				// a stack overflow because they all degrade down to the `AST.Node` base class.
				switch (node)
				{
					case BinaryExpressionNode expr:
						Write(expr);
						break;

					case UnaryExpressionNode expr:
						Write(expr);
						break;

					case VariableNode variable:
						Write(variable);
						break;

					case FieldNode field:
						Write(field);
						break;

					case VariableFieldNode field:
						Write(field);
						break;

					case ConcatNode concat:
						Write(concat);
						break;

					case CommaCatNode comma:
						Write(comma);
						break;

					case ConstantNode<uint> constant:
						Write(constant);
						break;

					case ConstantNode<double> constant:
						Write(constant);
						break;

					case StringConstantNode constant:
						Write(constant);
						break;

					case AssignmentNode assignment:
						Write(assignment);
						break;

					case FunctionCallNode call:
						Write(call);
						break;

					case NewObjectNode obj:
						Write(obj);
						break;

					case IfNode ifNode:
						Write(ifNode, isTernary: isExpr);
						break;

					case LoopStatementNode loop:
						Write(loop);
						break;

					case BreakStatementNode breakNode:
						Write(breakNode);
						break;

					case ContinueStatementNode continueNode:
						Write(continueNode);
						break;

					case ReturnStatementNode ret:
						Write(ret);
						break;

					case FunctionStatementNode function:
						Write(function);
						break;

					case PackageNode package:
						Write(package);
						break;

					case ContinuePointMarkerNode:
						break;

					default:
					{
						if (node != null)
						{
							throw new NotImplementedException();
						}

						break;
					}
				}

				if (addParens)
				{
					Write(")");
				}
			}
		}

		protected void Write (params Node[] nodes)
		{
			foreach (var node in nodes)
			{
				Write(node);
			}
		}

		protected void Write (Opcode @operator, bool padLeft = true, bool padRight = true)
		{
			if (@operator != null)
			{
				if (padLeft)
				{
					Write(" ");
				}

				Write(@operator.StringValue switch
				{
					"OP_ADD" => "+",
					"OP_SUB" => "-",
					"OP_MUL" => "*",
					"OP_DIV" => "/",
					"OP_MOD" => "%",

					"OP_CMPEQ" => "==",
					"OP_CMPGR" => ">",
					"OP_CMPGE" => ">=",
					"OP_CMPLT" => "<",
					"OP_CMPLE" => "<=",
					"OP_CMPNE" => "!=",
					"OP_COMPARE_STR" => "$=",

					"OP_XOR" => "^",
					"OP_BITAND" => "&",
					"OP_BITOR" => "|",
					"OP_SHR" => ">>",
					"OP_SHL" => "<<",

					"OP_AND" => "&&",
					"OP_OR" => "||",
					"OP_JMPIFNOT_NP" => "&&",
					"OP_JMPIF_NP" => "||",

					"OP_NOT" => "!",
					"OP_NOTF" => "!",
					"OP_ONESCOMPLEMENT" => "~",
					"OP_NEG" => "-",

					_ => throw new NotImplementedException(),
				});

				if (padRight)
				{
					Write(" ");
				}
			}
		}

		protected bool ShouldAddParens (BinaryExpressionNode parent, Node arm, bool isLeft)
		{
			if (arm.Associativity == Node.NodeAssociativity.None)
			{
				return false;
			}

			if (isLeft)
			{
				return arm.Associativity == Node.NodeAssociativity.Right || arm.Precedence > parent.Precedence;
			}

			return arm.Associativity == Node.NodeAssociativity.Left || arm.Precedence < parent.Precedence;
		}

		protected bool ShouldAddParens (UnaryExpressionNode _, Node node) => node switch
		{
			VariableFieldNode => false,
			ConstantNode<uint> => false,
			ConstantNode<double> => false,
			StringConstantNode => false,
			FunctionCallNode => false,
			_ => node.Associativity != Node.NodeAssociativity.Right,
		};

		protected bool ShouldAddParens (FieldNode _, Node objectExpr) => objectExpr switch
		{
			VariableFieldNode => false,
			ConstantNode<uint> => false,
			ConstantNode<double> => false,
			StringConstantNode => false,
			FunctionCallNode => false,
			_ => true,
		};

		protected bool ShouldAddParens (ConcatNode parent, Node arm, bool isLeft)
		{
			if (arm.Associativity == Node.NodeAssociativity.None)
			{
				return false;
			}

			if (isLeft)
			{
				return arm.Associativity == Node.NodeAssociativity.Right || arm.Precedence > parent.Precedence;
			}

			return arm.Associativity == Node.NodeAssociativity.Left || arm.Precedence < parent.Precedence;
		}

		protected bool ShouldAddParens (FunctionCallNode _, Node node) => node switch
		{
			UnaryExpressionNode => false,
			VariableFieldNode => false,
			ConstantNode<uint> => false,
			ConstantNode<double> => false,
			StringConstantNode => false,
			FunctionCallNode => false,
			_ => true,
		};

		protected bool ShouldAddParens (IfNode _, Node node, bool isThen) => isThen && node.Associativity == Node.NodeAssociativity.Right;

		protected bool ShouldAddParens (IfNode node, bool isTernary)
		{
			var testExpr = node.TestExpression;

			if (!isTernary || testExpr.Associativity == Node.NodeAssociativity.None)
			{
				return false;
			}

			return testExpr.Associativity == Node.NodeAssociativity.Right || testExpr.Precedence > node.Precedence;
		}

		protected void Write (BinaryExpressionNode node)
		{
			Write(node.Left, ShouldAddParens(node, node.Left, isLeft: true), isExpr: true);
			Write(node.Operator);
			Write(node.Right, ShouldAddParens(node, node.Right, isLeft: false), isExpr: true);
		}

		protected void Write (UnaryExpressionNode node)
		{
			Write(node.Operator, padLeft: false, padRight: false);
			Write(node.Expression, ShouldAddParens(node, node.Expression), isExpr: true);
		}

		protected void Write (VariableFieldNode node)
		{
			Write(node.Name);

			if (node.IsArray)
			{
				Write("[");
				Write(node.ArrayIndex);
				Write("]");
			}
		}

		protected void Write (VariableNode node) => Write(node as VariableFieldNode);

		protected void Write (FieldNode node)
		{
			Write(node.ObjectExpr, ShouldAddParens(node, node.ObjectExpr), isExpr: true);
			Write(".");
			Write(node as VariableFieldNode);
		}

		protected void Write (ConcatNode node)
		{
			Write(node.Left, ShouldAddParens(node, node.Left, isLeft: true), isExpr: true);

			Write(node.AppendChar switch
			{
				null => "@",
				' ' => "SPC",
				'\t' => "TAB",
				'\n' => "NL",

				_ => throw new NotImplementedException(),
			}, padLeft: true, padRight: true);

			Write(node.Right, ShouldAddParens(node, node.Right, isLeft: false), isExpr: true);
		}

		protected void Write (CommaCatNode node)
		{
			Write(node.Left, addParens: false, isExpr: true);
			Write(",");
			Write(node.Right, addParens: false, isExpr: true);
		}

		protected void Write (ConstantNode<uint> node) => Write(node.Value.ToString());
		protected void Write (ConstantNode<double> node) => Write(node.Value.ToString());

		protected void Write (StringConstantNode node)
		{
			var quote = "";

			if (node.Type == StringConstantNode.StringType.String)
			{
				quote = "\"";
			}
			else if (node.Type == StringConstantNode.StringType.TaggedString)
			{
				quote = "'";
			}

			if (node.Type == StringConstantNode.StringType.Identifier)
			{
				Write(node.Value);
			}
			else
			{
				Write($"{quote}{EscapeString(node.Value, quote[0])}{quote}");
			}
		}

		protected void Write (AssignmentNode node)
		{
			Write(node.VariableField);
			Write(" ");

			if (node.Operator != null)
			{
				Write(node.Operator, padLeft: false, padRight: false);
			}

			Write("=", " ");
			Write(node.Expression, addParens: false, isExpr: true);
		}

		protected void Write (FunctionCallNode node)
		{
			var isMethod = node.Type == FunctionCallNode.CallType.MethodCall;

			if (isMethod)
			{
				Write(node.Arguments[0], ShouldAddParens(node, node.Arguments[0]), isExpr: true);
				Write(".");
			}
			else if (node.Namespace != null)
			{
				Write(node.Namespace, "::");
			}

			Write(node.Name, "(");
			WriteExpressionList(node.Arguments, isMethod && node.Arguments.Count > 0 ? node.Arguments[0] : null);
			Write(")");
		}

		protected void Write (NewObjectNode node)
		{
			Write(node.IsDataBlock ? "datablock" : "new", " ");
			Write(node.ClassNameExpression, addParens: !node.IsDataBlock, isExpr: true);
			Write("(");
			Write(node.NameExpression, addParens: false, isExpr: true);

			if (node.HasParent)
			{
				Write(" ", ":", " ", node.ParentObject);
			}

			if (node.Arguments.Count > 0)
			{
				Write(",", " ");
				WriteExpressionList(node.Arguments);
			}

			Write(")");

			if (node.HasBody)
			{
				Write("\n");
				WriteIndent("{", "\n");

				Indent();

				WriteStatementList(node.Slots);
				Write("\n");
				WriteStatementList(node.Subobjects);

				Unindent();

				WriteIndent("}");
			}
		}

		protected void Write (IfNode node, bool isTernary)
		{
			if (!isTernary)
			{
				Write("if", " ", "(");
			}

			Write(node.TestExpression, ShouldAddParens(node, isTernary), isExpr: true);

			if (isTernary)
			{
				Write(" ", "?", " ");
				Write(node.Then[0], ShouldAddParens(node, node.Then[0], isThen: true), isExpr: true);
				Write(" ", ":", " ");
				Write(node.Else[0], ShouldAddParens(node, node.Else[0], isThen: false), isExpr: true);
			}
			else
			{
				Write(")", "\n");
				WriteIndent("{", "\n");

				Indent();
				WriteStatementList(node.Then);
				Unindent();

				WriteIndent("}", "\n");

				if (node.HasElse)
				{
					WriteIndent("else");

					if (node.Else.Count == 1 && node.Else[0] is IfNode)
					{
						Write(" ");
						Write(node.Else[0]);
					}
					else
					{
						Write("\n");
						WriteIndent("{", "\n");

						Indent();
						WriteStatementList(node.Else);
						Unindent();

						WriteIndent("}", "\n");
					}
				}
			}
		}

		protected void Write (LoopStatementNode node)
		{
			var type = node.GetLoopType();

			switch (type)
			{
				case LoopStatementNode.LoopType.DoWhile:
				{
					Write("do", "\n");
					WriteIndent("{", "\n");

					break;
				}

				case LoopStatementNode.LoopType.While:
				{
					Write("while", " ", "(");
					Write(node.TestExpression, addParens: false, isExpr: true);
					Write(")", "\n");
					WriteIndent("{", "\n");

					break;
				}

				case LoopStatementNode.LoopType.For:
				{
					Write("for", " ", "(");
					Write(node.InitExpression, addParens: false, isExpr: true);
					Write(";", " ");
					Write(node.TestExpression, addParens: false, isExpr: true);
					Write(";", " ");
					Write(node.EndExpression, addParens: false, isExpr: true);
					Write(")", "\n");
					WriteIndent("{", "\n");

					break;
				}
				
				default:
					break;
			}

			Indent();
			WriteStatementList(node.Body);
			Unindent();

			WriteIndent("}");

			if (type == LoopStatementNode.LoopType.DoWhile)
			{
				WriteIndent("while", " ", "(");
				Write(node.TestExpression, addParens: false, isExpr: true);
				Write(")");
			}
		}

		protected void Write (BreakStatementNode _) => Write("break");
		protected void Write (ContinueStatementNode _) => Write("continue");

		protected void Write (ReturnStatementNode node)
		{
			Write("return");

			if (node.ReturnsValue)
			{
				Write(" ");
				Write(node.Value, addParens: false, isExpr: true);
			}
		}

		protected void Write (FunctionStatementNode node)
		{
			Write("function", " ");

			if (node.Namespace != null)
			{
				Write(node.Namespace, "::");
			}

			Write(node.Name, "(");

			var unused = 1;

			for (var i = 0; i < node.Arguments.Count; i++)
			{
				var argument = node.Arguments[i];

				if (argument == null)
				{
					// TODO: There's a possibility for someone to name a variable this, which
					//       would mess this up... Maybe come up something better later?
					Write($"%__:unused{unused++}");
				}
				else
				{
					Write(argument);
				}

				if (i < node.Arguments.Count - 1)
				{
					Write(",", " ");
				}
			}

			Write(")", "\n");
			WriteIndent("{", "\n");

			Indent();
			WriteStatementList(node.Body);
			Unindent();

			WriteIndent("}");
		}

		protected void Write (PackageNode node)
		{
			Write("package", " ", node.Name, "\n");
			WriteIndent("{", "\n");

			Indent();
			WriteStatementList(node.Functions);
			Unindent();

			WriteIndent("}");
		}

		protected void Write (string token) => tokens.Push(token);

		protected void Write (string token, bool padLeft, bool padRight)
		{
			if (padLeft)
			{
				Write(" ");
			}

			Write(token);

			if (padRight)
			{
				Write(" ");
			}
		}

		protected void Write (params string[] tokens) => this.tokens.Push(tokens);

		protected void Indent () => indent++;
		protected void Unindent () => indent--;

		protected void WriteIndent (params string[] then)
		{
			for (var i = 0; i < indent; i++)
			{
				tokens.Push("\t");
			}

			if (then.Length > 0)
			{
				Write(then);
			}
		}

		protected void WriteNewline () => tokens.Push("\n");

		protected string EscapeString (string str, char quoteChar)
		{
			str = str.Replace("\\", "\\\\").Replace("\t", "\\\t")
				.Replace("\r", "\\\r").Replace("\n", "\\\n")
				.Replace("\x01", "\\c0").Replace("\x02", "\\c1")
				.Replace("\x03", "\\c2").Replace("\x04", "\\c3")
				.Replace("\x05", "\\c4").Replace("\x06", "\\c5")
				.Replace("\x07", "\\c6").Replace("\x08", "\\c7")
				.Replace("\x09", "\\c8").Replace("\x0A", "\\c9")
				.Replace("\x0B", "\\cr").Replace("\x0C", "\\cp")
				.Replace("\x0D", "\\co");

			if (quoteChar == '\'')
			{
				str = str.Replace("'", "\\'");
			}
			else if (quoteChar == '"')
			{
				str = str.Replace("\"", "\\\"");
			}
			else
			{
				throw new ArgumentException("Invalid quote character", nameof(quoteChar));
			}

			var escaped = "";

			foreach (var ch in str)
			{
				if (ch < 32 || ch > 126)
				{
					escaped += $"\\x{ch:X}";
				}
				else
				{
					escaped += ch;
				}
			}

			return escaped;
		}
	}
}
