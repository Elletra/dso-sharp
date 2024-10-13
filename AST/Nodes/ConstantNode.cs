using DSO.Disassembler;

namespace DSO.AST.Nodes
{
	public class ConstantNode<T>(T value) : Node(NodeType.Expression)
    {
        public readonly T Value = value;
        public ConstantNode(ImmediateInstruction<T> instruction) : this(instruction.Value) { }

        public override bool Equals(object? obj) => base.Equals(obj) && obj is ConstantNode<T> node && Equals(node.Value, Value);
        public override int GetHashCode() => base.GetHashCode() ^ (Value?.GetHashCode() ?? 0);
    }
}
