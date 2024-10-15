using DSO.CodeGenerator;

namespace DSO.AST.Nodes
{
	public class ReturnNode(Node? value = null) : Node(NodeType.Statement)
	{
		public readonly Node? Value = value is ConstantStringNode node ? node.ConvertToUIntNode() ?? node.ConvertToDoubleNode() ?? value : value;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is ReturnNode node && Equals(node.Value, Value);
		public override int GetHashCode() => base.GetHashCode() ^ (Value?.GetHashCode() ?? 0);

		public override void Visit(TokenStream stream, bool isExpression)
		{
			stream.Write("return");

			if (Value != null)
			{
				stream.Write(" ");
				stream.Write(Value, isExpression: true);
			}

			stream.Write(";", "\n");
		}
	}
}
