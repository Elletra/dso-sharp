using DSO.CodeGenerator;

namespace DSO.AST.Nodes
{
	public class IfNode(Node? test = null) : Node(NodeType.Statement)
	{
		public Node? Test { get; set; } = test;
		public List<Node> True { get; set; } = [];
		public List<Node> False { get; set; } = [];

		public bool CanConvertToTernary => Test != null && True.Count == 1 && False.Count == 1 && True[0].IsExpression && False[0].IsExpression;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is IfNode node
			&& Equals(node.Test, Test) && node.True.SequenceEqual(True) && node.False.SequenceEqual(False);

		public override int GetHashCode() => base.GetHashCode() ^ (Test?.GetHashCode() ?? 0) ^ True.GetHashCode() ^ False.GetHashCode();

		public override void Visit(TokenStream stream, bool isExpression)
		{
			stream.Write("if", " ", "(");
			stream.Write(Test, isExpression: true);
			stream.Write(")", "\n", "{", "\n");

			True.ForEach(node => stream.Write(node, isExpression: false));

			stream.Write("}", "\n");

			if (False.Count <= 0)
			{
				return;
			}

			stream.Write("else");

			if (False.Count == 1 && False[0] is IfNode)
			{
				stream.Write(" ");
				stream.Write(False[0], isExpression: false);
			}
			else
			{
				stream.Write("\n", "{", "\n");
				False.ForEach(node => stream.Write(node, isExpression: false));
				stream.Write("}", "\n");
			}
		}

		public TernaryIfNode ConvertToTernary()
		{
			if (!CanConvertToTernary)
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
			stream.Write(Test, isExpression: true);
			stream.Write(" ", "?", " ");
			stream.Write(True, isExpression: true);
			stream.Write(" ", ":", " ");
			stream.Write(False, isExpression: true);
		}
	}
}
