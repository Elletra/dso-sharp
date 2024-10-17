/**
 * BinaryNode.cs
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
	public class BinaryNode(Node left, Node right, Opcode op) : Node(NodeType.Expression)
	{
		public readonly Node Left = left;
		public readonly Node Right = right;
		public readonly Opcode Op = op;

		public bool IsOpAssociative => Op.Tag switch
		{
			OpcodeTag.OP_ADD or OpcodeTag.OP_MUL or
			OpcodeTag.OP_BITAND or OpcodeTag.OP_BITOR or OpcodeTag.OP_XOR or
			OpcodeTag.OP_JMPIFNOT_NP or OpcodeTag.OP_JMPIF_NP => true,
			_ => false,
		};

		public override int Precedence => Op.Tag switch
		{
			OpcodeTag.OP_MUL or OpcodeTag.OP_DIV or OpcodeTag.OP_MOD => 2,
			OpcodeTag.OP_ADD or OpcodeTag.OP_SUB => 3,
			OpcodeTag.OP_SHL or OpcodeTag.OP_SHR => 4,
			OpcodeTag.OP_CMPLT or OpcodeTag.OP_CMPGR or OpcodeTag.OP_CMPLE or OpcodeTag.OP_CMPGE => 6,
			OpcodeTag.OP_CMPEQ or OpcodeTag.OP_CMPNE => 7,
			OpcodeTag.OP_BITAND => 8,
			OpcodeTag.OP_XOR => 9,
			OpcodeTag.OP_BITOR => 10,
			OpcodeTag.OP_JMPIFNOT_NP => 11,
			OpcodeTag.OP_JMPIF_NP => 12,
		};

		public override bool IsAssociativeWith(Node compare) => compare is BinaryNode binary && binary.Op.Equals(Op) && IsOpAssociative;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is BinaryNode node
			&& node.Left.Equals(Left) && node.Right.Equals(Right) && node.Op.Equals(Op);

		public override int GetHashCode() => base.GetHashCode() ^ Left.GetHashCode() ^ Right.GetHashCode() ^ Op.GetHashCode();

		public override void Visit(TokenStream stream, bool isExpression)
		{
			stream.Write(Left, CheckPrecedenceAndAssociativity);

			stream.Write(" ", Op.Tag switch
			{
				OpcodeTag.OP_ADD => "+",
				OpcodeTag.OP_SUB => "-",
				OpcodeTag.OP_MUL => "*",
				OpcodeTag.OP_DIV => "/",
				OpcodeTag.OP_CMPLT => "<",
				OpcodeTag.OP_CMPGR => ">",
				OpcodeTag.OP_MOD => "%",
				OpcodeTag.OP_BITOR => "|",
				OpcodeTag.OP_BITAND => "&",
				OpcodeTag.OP_XOR => "^",
				OpcodeTag.OP_CMPEQ => "==",
				OpcodeTag.OP_CMPNE => "!=",
				OpcodeTag.OP_CMPLE => "<=",
				OpcodeTag.OP_CMPGE => ">=",
				OpcodeTag.OP_SHL => "<<",
				OpcodeTag.OP_SHR => ">>",
				OpcodeTag.OP_JMPIF_NP => "||",
				OpcodeTag.OP_JMPIFNOT_NP => "&&",
			}, " ");

			stream.Write(Right, CheckPrecedenceAndAssociativity);
		}
	}

	public class BinaryStringNode(Node left, Node right, Opcode op, bool not = false) : BinaryNode(left, right, op)
	{
		public readonly bool Not = not;

		public override int Precedence => 5;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is BinaryStringNode node && node.Not.Equals(Not);
		public override int GetHashCode() => base.GetHashCode() ^ Not.GetHashCode();

		public override void Visit(TokenStream stream, bool isExpression)
		{
			stream.Write(Left, CheckPrecedenceAndAssociativity);
			stream.Write(" ", Not ? "!$=" : "$=", " ");
			stream.Write(Right, CheckPrecedenceAndAssociativity);
		}
	}
}
