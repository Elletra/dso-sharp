using DSO.Disassembler;

namespace DSO.AST.Nodes
{
	public class ObjectDeclarationNode(CreateObjectInstruction instruction, Node className, Node? objectName, int depth) : Node(NodeType.ExpressionStatement)
    {
        public readonly bool IsDataBlock = instruction.IsDataBlock;
        public readonly Node Class = className;
        public readonly Node? Name = objectName;
        public readonly string? Parent = instruction.Parent;
        public readonly int Depth = depth;
        public readonly List<Node> Arguments = [];
        public readonly List<AssignmentNode> Fields = [];
        public readonly List<ObjectDeclarationNode> Children = [];

        public override bool Equals(object? obj) => base.Equals(obj) && obj is ObjectDeclarationNode node
            && node.IsDataBlock.Equals(IsDataBlock) && node.Class.Equals(Class) && Equals(node.Name, Name) && Equals(node.Parent, Parent)
            && node.Depth.Equals(Depth) && node.Arguments.Equals(Arguments) && node.Fields.Equals(Fields) && node.Children.Equals(Children);

        public override int GetHashCode() => base.GetHashCode() ^ IsDataBlock.GetHashCode()
            ^ Class.GetHashCode() ^ (Name?.GetHashCode() ?? 0) ^ (Parent?.GetHashCode() ?? 0)
            ^ Depth.GetHashCode() ^ Arguments.GetHashCode() ^ Fields.GetHashCode() ^ Children.GetHashCode();
    }
}
