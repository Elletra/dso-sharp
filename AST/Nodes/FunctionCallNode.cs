using DSO.CodeGenerator;
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
		public readonly CallType CallType = Enum.IsDefined(typeof(CallType), instruction.CallType) ? (CallType) instruction.CallType : CallType.Invalid;
		public readonly List<Node> Arguments = [];

		public override bool Equals(object? obj) => base.Equals(obj) && obj is FunctionCallNode node
			&& node.Name.Equals(Name) && Equals(node.Namespace, Namespace)
			&& node.CallType.Equals(CallType) && node.Arguments.Equals(Arguments);

		public override int GetHashCode() => base.GetHashCode() ^ Name.GetHashCode() ^ (Namespace?.GetHashCode() ?? 0)
			^ CallType.GetHashCode() ^ Arguments.GetHashCode();

		public override void Visit(TokenStream stream, bool isExpression)
		{
			var methodCall = CallType == CallType.MethodCall;

			if (methodCall)
			{
				stream.Write(Arguments[0], this);
				stream.Write(".");
			}
			else if (Namespace != null)
			{
				stream.Write(Namespace, "::");
			}

			stream.Write(Name, "(");

			for (var i = methodCall ? 1 : 0; i < Arguments.Count; i++)
			{
				stream.Write(Arguments[i], this);

				if (i < Arguments.Count - 1)
				{
					stream.Write(",");
				}
			}

			stream.Write(")");
		}
	}
}
