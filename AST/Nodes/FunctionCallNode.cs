using DSO.Disassembler;

namespace DSO.AST.Nodes
{
	public enum CallType : uint
	{
		FunctionCall,
		MethodCall,
		ParentCall,
		Invalid,
	}

	public class FunctionCallNode(CallInstruction instruction) : Node(NodeType.ExpressionStatement)
    {
        public readonly string Name = instruction.Name;
        public readonly string? Namespace = instruction.Namespace;
        public readonly CallType CallType = Enum.IsDefined(typeof(CallType), instruction.CallType) ? (CallType)instruction.CallType : CallType.Invalid;
        public readonly List<Node> Arguments = [];

        public override bool Equals(object? obj) => base.Equals(obj) && obj is FunctionCallNode node
            && node.Name.Equals(Name) && Equals(node.Namespace, Namespace)
            && node.CallType.Equals(CallType) && node.Arguments.Equals(Arguments);

        public override int GetHashCode() => base.GetHashCode() ^ Name.GetHashCode() ^ (Namespace?.GetHashCode() ?? 0)
            ^ CallType.GetHashCode() ^ Arguments.GetHashCode();
    }
}
