using DSO.Disassembler;

namespace DSO.AST.Nodes
{
	public class FunctionDeclarationNode(FunctionInstruction instruction) : Node(NodeType.Statement)
    {
        public readonly string Name = instruction.Name;
        public readonly string? Namespace = instruction.Namespace;
        public readonly List<string> Arguments = [.. instruction.Arguments];
        public List<Node> Body { get; set; } = [];

        public override bool Equals(object? obj) => base.Equals(obj) && obj is FunctionDeclarationNode node
            && node.Name.Equals(Name) && Equals(node.Namespace, Namespace)
            && node.Arguments.Equals(Arguments) && node.Body.Equals(Body);

        public override int GetHashCode() => base.GetHashCode() ^ Name.GetHashCode() ^ (Namespace?.GetHashCode() ?? 0)
            ^ Arguments.GetHashCode() ^ Body.GetHashCode();
    }
}
