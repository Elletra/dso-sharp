namespace DSO.AST.Nodes
{
	public class FieldNode(string name) : Node(NodeType.Expression)
    {
        public readonly string Name = name;

        public Node? Object { get; set; } = null;
        public Node? Index { get; set; } = null;

        public override bool Equals(object? obj) => base.Equals(obj) && obj is FieldNode node
            && node.Name.Equals(Name) && Equals(node.Object, Object) && Equals(node.Index, Index);

        public override int GetHashCode() => base.GetHashCode() ^ Name.GetHashCode() ^ (Object?.GetHashCode() ?? 0) ^ (Index?.GetHashCode() ?? 0);
    }
}
