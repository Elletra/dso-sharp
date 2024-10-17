/**
 * IfNode.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using DSO.CodeGenerator;

namespace DSO.AST.Nodes
{
	public class IfNode(Node? test = null) : Node(NodeType.Statement)
	{
		public Node? Test { get; set; } = test;
		public List<Node> True { get; set; } = [];
		public List<Node> False { get; set; } = [];

		public bool CanConvertToTernary()
		{
			if (Test == null || True.Count != 1 || False.Count != 1)
			{
				return false;
			}

			var canConvert = true;

			if (True[0] is IfNode trueIf)
			{
				canConvert = canConvert && trueIf.CanConvertToTernary();
			}
			else
			{
				canConvert = canConvert && True[0].IsExpression;
			}

			if (False[0] is IfNode falseIf)
			{
				canConvert = canConvert && falseIf.CanConvertToTernary();
			}
			else
			{
				canConvert = canConvert && False[0].IsExpression;
			}

			return canConvert;
		}

		public override bool Equals(object? obj) => base.Equals(obj) && obj is IfNode node
			&& Equals(node.Test, Test) && node.True.SequenceEqual(True) && node.False.SequenceEqual(False);

		public override int GetHashCode() => base.GetHashCode() ^ (Test?.GetHashCode() ?? 0) ^ True.GetHashCode() ^ False.GetHashCode();

		public override void Visit(TokenStream stream, bool isExpression)
		{
			stream.Write("if", " ", "(");
			stream.Write(Test, isExpression: true);
			stream.Write(")", "\n", "{", "\n");

			True.ForEach(node => stream.Write(node, isExpression: false));

			stream.Write("}", "\n");

			if (False.Count <= 0)
			{
				return;
			}

			stream.Write("else");

			if (False.Count == 1 && False[0] is IfNode)
			{
				stream.Write(" ");
				stream.Write(False[0], isExpression: false);
			}
			else
			{
				stream.Write("\n", "{", "\n");
				False.ForEach(node => stream.Write(node, isExpression: false));
				stream.Write("}", "\n");
			}
		}

		public TernaryIfNode ConvertToTernary()
		{
			if (!CanConvertToTernary())
			{
				throw new InvalidCastException("Could not convert if statement to ternary expression");
			}

			return new(
				Test,
				True[0] is IfNode trueIf ? trueIf.ConvertToTernary() : True[0],
				False[0] is IfNode falseIf ? falseIf.ConvertToTernary() : False[0]
			);
		}
	}

	public class TernaryIfNode(Node test, Node @true, Node @false) : Node(NodeType.Expression)
	{
		public readonly Node Test = test;
		public readonly Node True = @true;
		public readonly Node False = @false;

		public override int Precedence => 13;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is TernaryIfNode node
			&& node.Test.Equals(Test) && node.True.Equals(True) && node.False.Equals(False);

		public override int GetHashCode() => base.GetHashCode() ^ Test.GetHashCode() ^ True.GetHashCode() ^ False.GetHashCode();

		public override void Visit(TokenStream stream, bool isExpression)
		{
			stream.Write(Test, isExpression: true);
			stream.Write(" ", "?", " ");
			stream.Write(True, isExpression: true);
			stream.Write(" ", ":", " ");
			stream.Write(False, isExpression: true);
		}
	}
}
