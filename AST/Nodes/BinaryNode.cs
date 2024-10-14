using DSO.CodeGenerator;
using DSO.Opcodes;

namespace DSO.AST.Nodes
{
	public class BinaryNode(Node left, Node right, Opcode op) : Node(NodeType.Expression)
	{
		public readonly Node Left = left;
		public readonly Node Right = right;
		public readonly Opcode Op = op;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is BinaryNode node
			&& node.Left.Equals(Left) && node.Right.Equals(Right) && node.Op.Equals(Op);

		public override int GetHashCode() => base.GetHashCode() ^ Left.GetHashCode() ^ Right.GetHashCode() ^ Op.GetHashCode();

		public override void Visit(TokenStream stream, bool isExpression)
		{
			stream.Write(Left, isExpression: true);

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

			stream.Write(Right, isExpression: true);
		}
	}

	public class BinaryStringNode(Node left, Node right, Opcode op, bool not = false) : BinaryNode(left, right, op)
	{
		public readonly bool Not = not;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is BinaryStringNode node && node.Not.Equals(Not);
		public override int GetHashCode() => base.GetHashCode() ^ Not.GetHashCode();

		public override void Visit(TokenStream stream, bool isExpression)
		{
			stream.Write(Left, isExpression: true);
			stream.Write(" ", Not ? "!$=" : "$=", " ");
			stream.Write(Right, isExpression: true);
		}
	}
}
