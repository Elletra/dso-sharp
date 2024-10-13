namespace DSO.AST.Nodes
{
	public class ReturnNode(Node? value = null) : Node(NodeType.Statement)
	{
		public Node? Value = value;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is ReturnNode node && Equals(node.Value, Value);
		public override int GetHashCode() => base.GetHashCode() ^ (Value?.GetHashCode() ?? 0);
	}
}
