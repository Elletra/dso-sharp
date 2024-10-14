using DSO.CodeGenerator;

namespace DSO.AST.Nodes
{
	public class VariableNode(string name, Node? index = null) : Node(NodeType.Expression)
	{
		public readonly string Name = name;
		public Node? Index { get; set; } = index;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is VariableNode node && node.Name.Equals(Name) && Equals(node.Index, Index);
		public override int GetHashCode() => base.GetHashCode() ^ Name.GetHashCode() ^ (Index?.GetHashCode() ?? 0);

		public override void Visit(TokenStream stream, bool isExpression)
		{
			stream.Write(Name);

			if (Index != null)
			{
				Node? index = null;

				if (Index is ConstantStringNode constant)
				{
					index ??= constant.ConvertToDoubleNode();
					index ??= constant.ConvertToUIntNode();
				}

				index ??= Index;

				stream.Write("[");
				stream.Write(index, this);
				stream.Write("]");
			}
		}

		public override bool ShouldAddParentheses(Node parent) => parent is ObjectDeclarationNode;
	}
}
