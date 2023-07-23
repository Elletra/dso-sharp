using DSODecompiler.AST.Nodes;
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

		protected void Generate ()
		{
			foreach (var node in list)
			{
				Write(node);

				if (ShouldAppendSemicolon(node))
				{
					Write(";");
				}

				Write("\n");
			}
		}

		/// <summary>
		/// Whether a node in a statement list should have a semicolon appended to it.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		protected bool ShouldAppendSemicolon (Node node) => node switch
		{
			FunctionStatementNode => false,
			IfNode => false,
			LoopStatementNode => false,
			BreakStatementNode => true,
			ContinueStatementNode => true,
			ReturnStatementNode => true,
			FunctionCallNode => true,
			AssignmentNode => true,
			ObjectNode => true,

			_ => true,
		};

		protected void Write (Node node) => Write(node, addParens: false);

		protected void Write (Node node, bool addParens)
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

					case ReturnStatementNode ret:
						Write(ret);
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

		protected void Write (BinaryExpressionNode node)
		{
			Write(node.Left, addParens: true);
			Write(node.Operator);
			Write(node.Right, addParens: true);
		}

		protected void Write (UnaryExpressionNode node)
		{
			Write(node.Operator, padLeft: false, padRight: false);
			Write(node.Expression, addParens: true);
		}

		protected void Write (VariableFieldNode node)
		{
			Write(node.Name, "[");
			Write(node.ArrayIndex);
			Write("]");
		}

		protected void Write (VariableNode node) => Write(node as VariableFieldNode);

		protected void Write (FieldNode node)
		{
			Write(node.ObjectExpr, addParens: true);
			Write(node as VariableFieldNode);
		}

		protected void Write (ConcatNode node)
		{
			Write(node.Left, addParens: true);

			Write(node.AppendChar switch
			{
				null => "@",
				' ' => "SPC",
				'\t' => "TAB",
				'\n' => "NL",

				_ => throw new NotImplementedException(),
			}, padLeft: true, padRight: true);

			Write(node.Right, addParens: true);
		}

		protected void Write (CommaCatNode node)
		{
			Write(node.Left);
			Write(",");
			Write(node.Right);
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

			if (node.Type == StringConstantNode.StringType.String
				&& (uint.TryParse(node.Value, out _) || double.TryParse(node.Value, out _)))
			{
				// If it can be parsed as an integer or a float, let's not add quotes around it, as
				// long as it's not a tagged string of course...
				Write(node.Value);
			}
			else
			{
				Write($"{quote}{node.Value}{quote}");
			}
		}

		protected void Write (AssignmentNode node)
		{
			Write(node.VariableField);
			Write(node.Operator, padLeft: true, padRight: false);
			Write("=");
			Write(node.Expression);
		}

		protected void Write (FunctionCallNode node)
		{

		}

		protected void Write (ReturnStatementNode node)
		{
			Write("return");

			if (node.ReturnsValue)
			{
				Write(" ");
				Write(node.Value);
			}
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

		protected void WriteIndent ()
		{
			for (var i = 0; i < indent; i++)
			{
				tokens.Push("\t");
			}
		}

		protected void WriteNewline () => tokens.Push("\n");
	}
}
