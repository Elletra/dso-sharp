namespace DSO.AST.Nodes
{
	public class IfNode(Node? test = null) : Node(NodeType.Statement)
	{
		public Node? Test { get; set; } = test;
		public List<Node> True { get; set; } = [];
		public List<Node> False { get; set; } = [];

		public override bool Equals(object? obj) => base.Equals(obj) && obj is IfNode node && Equals(node.Test, Test)
			&& node.True.Equals(True) && node.False.Equals(False);

		public override int GetHashCode() => base.GetHashCode() ^ True.GetHashCode() ^ False.GetHashCode() ^ (Test?.GetHashCode() ?? 0);
	}
}
