/**
 * ConcatNode.cs
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
	public class ConcatNode(Node left, char? ch = null) : Node(NodeType.Expression)
	{
		public readonly Node Left = left;

		private Node _right = null;

		public Node Right
		{
			get => _right;
			set => _right ??= value;
		}

		public readonly char? Char = ch;

		public override int Precedence => this is CommaConcatNode ? base.Precedence : 5;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is ConcatNode node
			&& node.Left.Equals(Left) && Equals(node._right, _right) && Equals(node.Char, Char);

		public override int GetHashCode() => base.GetHashCode() ^ Left.GetHashCode() ^ (_right?.GetHashCode() ?? 0) ^ (Char?.GetHashCode() ?? 0);

		public override bool IsAssociativeWith(Node compare) => compare is ConcatNode;

		public override void Visit(CodeWriter writer, bool isExpression)
		{
			writer.Write(Left, CheckPrecedenceAndAssociativity);

			writer.Write(" ", Char switch
			{
				' ' => "SPC",
				'\t' => "TAB",
				'\n' => "NL",
				null => "@",
			}, " ");

			writer.Write(Right, CheckPrecedenceAndAssociativity);
		}
	}

	public class CommaConcatNode : ConcatNode
	{
		public CommaConcatNode(Node left) : base(left)
		{
			Type = NodeType.CommaConcat;
		}

		public override bool Equals(object? obj) => base.Equals(obj) && obj is CommaConcatNode;
		public override int GetHashCode() => base.GetHashCode();

		public override void Visit(CodeWriter writer, bool isExpression)
		{
			writer.Write(Left, isExpression: true);
			writer.Write(",", " ");
			writer.Write(Right, isExpression: true);
		}
	}
}
