namespace DSO.AST.Nodes
{
	public class LoopNode(Node test) : Node(NodeType.Statement)
	{
		public readonly Node Test = test;
		public List<Node> Body { get; set; } = [];

		public override bool Equals(object? obj) => base.Equals(obj) && obj is LoopNode node && node.Test.Equals(Test) && node.Body.Equals(Body);
		public override int GetHashCode() => base.GetHashCode() ^ Test.GetHashCode() ^ Body.GetHashCode();
	}

	public class WhileLoopNode(Node test) : LoopNode(test)
	{
		public override bool Equals(object? obj) => base.Equals(obj) && obj is WhileLoopNode;
		public override int GetHashCode() => base.GetHashCode() ^ 41;
	}

	public class ForLoopNode(Node init, Node test, Node end) : LoopNode(test)
	{
		public readonly Node Init = init;
		public readonly Node End = end;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is ForLoopNode node
			&& node.Init.Equals(Init) && node.End.Equals(End);

		public override int GetHashCode() => base.GetHashCode() ^ Init.GetHashCode() ^ End.GetHashCode();
	}
}
