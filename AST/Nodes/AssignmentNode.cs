using DSO.Opcodes;

namespace DSO.AST.Nodes
{
	public class AssignmentNode(Node left, Node right, Opcode? op = null) : Node(NodeType.ExpressionStatement)
    {
        public readonly Node Left = left;
        public readonly Node Right = right;
        public readonly Opcode? Operator = op;

        public override bool Equals(object? obj) => base.Equals(obj) && obj is AssignmentNode node
            && node.Left.Equals(Left) && node.Right.Equals(Right) && Equals(node.Operator, Operator);

        public override int GetHashCode() => base.GetHashCode() ^ Left.GetHashCode() ^ Right.GetHashCode() ^ (Operator?.GetHashCode() ?? 0);
    }
}
