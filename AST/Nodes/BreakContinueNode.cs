namespace DSO.AST.Nodes
{
	public class BreakNode() : Node(NodeType.Statement)
	{
		public override bool Equals(object? obj) => base.Equals(obj) && obj is BreakNode;
		public override int GetHashCode() => base.GetHashCode();
	}

	public class ContinueNode() : Node(NodeType.Statement)
	{
		public override bool Equals(object? obj) => base.Equals(obj) && obj is ContinueNode;
		public override int GetHashCode() => base.GetHashCode();
	}
}
