using DSO.CodeGenerator;

namespace DSO.AST.Nodes
{
	public class ReturnNode(Node? value = null) : Node(NodeType.Statement)
	{
		public Node? Value = value;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is ReturnNode node && Equals(node.Value, Value);
		public override int GetHashCode() => base.GetHashCode() ^ (Value?.GetHashCode() ?? 0);

		public override void Visit(TokenStream stream, bool isExpression)
		{
			stream.Write("return");
			Value?.Visit(stream, isExpression: true);
			stream.Write(";", "\n");
		}
	}
}
