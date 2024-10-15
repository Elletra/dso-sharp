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
		private readonly List<Node> _arguments = [];

		public readonly string Name = instruction.Name;
		public readonly string? Namespace = instruction.Namespace;
		public readonly CallType CallType = Enum.IsDefined(typeof(CallType), instruction.CallType) ? (CallType) instruction.CallType : CallType.Invalid;

		public void AddArgument(Node arg) => _arguments.Add(arg is ConstantStringNode node ? node.ConvertToUIntNode() ?? node.ConvertToDoubleNode() ?? arg : arg);

		public override bool Equals(object? obj) => base.Equals(obj) && obj is FunctionCallNode node
			&& node.Name.Equals(Name) && Equals(node.Namespace, Namespace)
			&& node.CallType.Equals(CallType) && node._arguments.SequenceEqual(_arguments);

		public override int GetHashCode() => base.GetHashCode() ^ Name.GetHashCode() ^ (Namespace?.GetHashCode() ?? 0)
			^ CallType.GetHashCode() ^ _arguments.GetHashCode();

		public override void Visit(TokenStream stream, bool isExpression)
		{
			var methodCall = CallType == CallType.MethodCall;

			if (methodCall)
			{
				stream.Write(_arguments[0], node => node.Precedence > Precedence);
				stream.Write(".");
			}
			else if (Namespace != null)
			{
				stream.Write(Namespace, "::");
			}

			stream.Write(Name, "(");

			for (var i = methodCall ? 1 : 0; i < _arguments.Count; i++)
			{
				stream.Write(_arguments[i], isExpression: true);

				if (i < _arguments.Count - 1)
				{
					stream.Write(",", " ");
				}
			}

			stream.Write(")");

			if (!isExpression)
			{
				stream.Write(";", "\n");
			}
		}
	}
}
