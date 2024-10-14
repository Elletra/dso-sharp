using DSO.CodeGenerator;

namespace DSO.AST.Nodes
{
	public class FieldNode(string name) : Node(NodeType.Expression)
	{
		public readonly string Name = name;

		public Node? Object { get; set; } = null;
		public Node? Index { get; set; } = null;

		public override bool IsAssociativeWith(Node compare) => compare is FieldNode;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is FieldNode node
			&& node.Name.Equals(Name) && Equals(node.Object, Object) && Equals(node.Index, Index);

		public override int GetHashCode() => base.GetHashCode() ^ Name.GetHashCode() ^ (Object?.GetHashCode() ?? 0) ^ (Index?.GetHashCode() ?? 0);

		public override void Visit(TokenStream stream, bool isExpression)
		{
			if (Object != null)
			{
				Node? obj = null;

				if (Object is ConstantStringNode constant)
				{
					obj ??= constant.ConvertToDoubleNode();
					obj ??= constant.ConvertToUIntNode();
				}

				obj ??= Object;

				stream.Write(obj, node => node.Precedence > Precedence);
				stream.Write(".");
			}

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
				stream.Write(index, isExpression: true);
				stream.Write("]");
			}
		}
	}
}
