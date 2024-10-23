/**
 * AssignmentNode.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using DSO.CodeGenerator;
using DSO.Opcodes;

namespace DSO.AST.Nodes
{
	public class AssignmentNode(Node left, Node right, Opcode? op = null) : Node(NodeType.ExpressionStatement)
	{
		public readonly Node Left = left;
		public readonly Node Right = right is ConstantStringNode node ? node.ConvertToUIntNode() ?? node.ConvertToDoubleNode() ?? right : right;
		public readonly Opcode? Operator = op;

		public override int Precedence => IsIncrementDecrement ? 1 : 14;

		public bool IsIncrementDecrement => Right is ConstantDoubleNode constant && constant.Value == 1.0f
			&& (Operator?.Tag == OpcodeTag.OP_ADD || Operator?.Tag == OpcodeTag.OP_SUB);

		public override bool Equals(object? obj) => base.Equals(obj) && obj is AssignmentNode node
			&& node.Left.Equals(Left) && node.Right.Equals(Right) && Equals(node.Operator, Operator);

		public override int GetHashCode() => base.GetHashCode() ^ Left.GetHashCode() ^ Right.GetHashCode() ^ (Operator?.GetHashCode() ?? 0);

		public override void Visit(CodeWriter writer, bool isExpression)
		{
			writer.Write(Left, isExpression: true);

			if (Operator == null)
			{
				writer.Write(" ", "=", " ");
				writer.Write(Right, isExpression: true);
			}
			else
			{
				var tag = Operator.Tag;
				var incDec = IsIncrementDecrement;

				if (incDec)
				{
					if (tag == OpcodeTag.OP_ADD)
					{
						writer.Write("++");
					}
					else if (tag == OpcodeTag.OP_SUB)
					{
						writer.Write("--");
					}
				}

				if (!incDec)
				{
					writer.Write(" ", $"{tag switch
					{
						OpcodeTag.OP_ADD => "+",
						OpcodeTag.OP_SUB => "-",
						OpcodeTag.OP_MUL => "*",
						OpcodeTag.OP_DIV => "/",
						OpcodeTag.OP_MOD => "%",
						OpcodeTag.OP_BITOR => "|",
						OpcodeTag.OP_BITAND => "&",
						OpcodeTag.OP_XOR => "^",
						OpcodeTag.OP_SHL => "<<",
						OpcodeTag.OP_SHR => ">>",
					}}=", " ");

					writer.Write(Right, isExpression: true);
				}
			}

			if (!isExpression)
			{
				writer.Write(";", "\n");
			}
		}
	}
}
