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
    }

	public class BinaryStringNode(Node left, Node right, Opcode op, bool not = false) : BinaryNode(left, right, op)
	{
		public readonly bool Not = not;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is BinaryStringNode node && node.Not.Equals(Not);
		public override int GetHashCode() => base.GetHashCode() ^ Not.GetHashCode();
	}
}
