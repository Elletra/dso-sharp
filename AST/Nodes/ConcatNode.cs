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

		public override bool Equals(object? obj) => base.Equals(obj) && obj is ConcatNode node
			&& node.Left.Equals(Left) && Equals(node._right, _right) && Equals(node.Char, Char);

		public override int GetHashCode() => base.GetHashCode() ^ Left.GetHashCode() ^ (_right?.GetHashCode() ?? 0) ^ (Char?.GetHashCode() ?? 0);

		public override void Visit(TokenStream stream, bool isExpression)
		{
			stream.Write(Left, this);

			stream.Write(Char switch
			{
				' ' => "SPC",
				'\t' => "TAB",
				'\n' => "NL",
				null => "@",
			});

			stream.Write(Right, this);
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

		public override void Visit(TokenStream stream, bool isExpression)
		{
			stream.Write(Left, this);
			stream.Write(",");
			stream.Write(Right, this);
		}
	}
}
