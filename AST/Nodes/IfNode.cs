using DSO.CodeGenerator;

namespace DSO.AST.Nodes
{
	public class IfNode(Node? test = null) : Node(NodeType.Statement)
	{
		public Node? Test { get; set; } = test;
		public List<Node> True { get; set; } = [];
		public List<Node> False { get; set; } = [];

		public override bool Equals(object? obj) => base.Equals(obj) && obj is IfNode node
			&& Equals(node.Test, Test) && node.True.Equals(True) && node.False.Equals(False);

		public override int GetHashCode() => base.GetHashCode() ^ (Test?.GetHashCode() ?? 0) ^ True.GetHashCode() ^ False.GetHashCode();

		public TernaryIfNode ConvertToTernary()
		{
			if (Test == null || True.Count != 1 || False.Count != 1 || !True[0].IsExpression || !False[0].IsExpression)
			{
				throw new InvalidCastException("Could not convert if statement to ternary expression");
			}

			return new(Test, True[0], False[0]);
		}
	}

	public class TernaryIfNode(Node test, Node @true, Node @false) : Node(NodeType.Expression)
	{
		public readonly Node Test = test;
		public readonly Node True = @true;
		public readonly Node False = @false;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is TernaryIfNode node
			&& node.Test.Equals(Test) && node.True.Equals(True) && node.False.Equals(False);

		public override int GetHashCode() => base.GetHashCode() ^ Test.GetHashCode() ^ True.GetHashCode() ^ False.GetHashCode();

		public override void Visit(TokenStream stream, bool isExpression)
		{
			stream.Write(Test, this);
			stream.Write("?");
			stream.Write(True, this);
			stream.Write(":");
			stream.Write(False, this);
		}
	}
}
