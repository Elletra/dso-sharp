using DSO.CodeGenerator;
using DSO.Opcodes;

namespace DSO.AST.Nodes
{
	public class BinaryNode(Node left, Node right, Opcode op) : Node(NodeType.Expression)
	{
		public readonly Node Left = left;
		public readonly Node Right = right;
		public readonly Opcode Op = op;

		public bool IsOpAssociative => Op.Value switch
		{
			Ops.OP_ADD or Ops.OP_MUL or
			Ops.OP_BITAND or Ops.OP_BITOR or Ops.OP_XOR or
			Ops.OP_JMPIFNOT_NP or Ops.OP_JMPIF_NP => true,
			_ => false,
		};

		public override int Precedence => Op.Value switch
		{
			Ops.OP_MUL or Ops.OP_DIV or Ops.OP_MOD => 2,
			Ops.OP_ADD or Ops.OP_SUB => 3,
			Ops.OP_SHL or Ops.OP_SHR => 4,
			Ops.OP_CMPLT or Ops.OP_CMPGR or Ops.OP_CMPLE or Ops.OP_CMPGE => 6,
			Ops.OP_CMPEQ or Ops.OP_CMPNE => 7,
			Ops.OP_BITAND => 8,
			Ops.OP_XOR => 9,
			Ops.OP_BITOR => 10,
			Ops.OP_JMPIFNOT_NP => 11,
			Ops.OP_JMPIF_NP => 12,
		};

		public override bool IsAssociativeWith(Node compare) => compare is BinaryNode binary && binary.Op.Equals(Op) && IsOpAssociative;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is BinaryNode node
			&& node.Left.Equals(Left) && node.Right.Equals(Right) && node.Op.Equals(Op);

		public override int GetHashCode() => base.GetHashCode() ^ Left.GetHashCode() ^ Right.GetHashCode() ^ Op.GetHashCode();

		public override void Visit(TokenStream stream, bool isExpression)
		{
			stream.Write(Left, CheckPrecedenceAndAssociativity);

			stream.Write(" ", Op.Value switch
			{
				Ops.OP_ADD => "+",
				Ops.OP_SUB => "-",
				Ops.OP_MUL => "*",
				Ops.OP_DIV => "/",
				Ops.OP_CMPLT => "<",
				Ops.OP_CMPGR => ">",
				Ops.OP_MOD => "%",
				Ops.OP_BITOR => "|",
				Ops.OP_BITAND => "&",
				Ops.OP_XOR => "^",
				Ops.OP_CMPEQ => "==",
				Ops.OP_CMPNE => "!=",
				Ops.OP_CMPLE => "<=",
				Ops.OP_CMPGE => ">=",
				Ops.OP_SHL => "<<",
				Ops.OP_SHR => ">>",
				Ops.OP_JMPIF_NP => "||",
				Ops.OP_JMPIFNOT_NP => "&&",
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
