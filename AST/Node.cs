using DSO.Disassembler;
using DSO.Opcodes;

namespace DSO.AST
{
	public enum NodeType
	{
		Statement,
		Expression,
		ExpressionStatement,
		CommaConcat,
	}

	public abstract class Node(NodeType type)
	{
		public NodeType Type { get; protected set; } = type;

		public override bool Equals(object? obj) => obj is Node node && node.Type.Equals(Type);
		public override int GetHashCode() => Type.GetHashCode();
	}

	public class BreakNode() : Node(NodeType.Statement)
	{
		public override bool Equals(object? obj) => base.Equals(obj) && obj is BreakNode;
		public override int GetHashCode() => base.GetHashCode();
	}

	public class ContinueNode() : Node(NodeType.Statement)
	{
		public override bool Equals(object? obj) => base.Equals(obj) && obj is ContinueNode;
		public override int GetHashCode() => base.GetHashCode();
	}

	public class ReturnNode(Node? value = null) : Node(NodeType.Statement)
	{
		public Node? Value = value;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is ReturnNode node && Equals(node.Value, Value);
		public override int GetHashCode() => base.GetHashCode() ^ (Value?.GetHashCode() ?? 0);
	}

	public class IfNode(Node? test = null) : Node(NodeType.Statement)
	{
		public Node? Test { get; set; } = test;
		public List<Node> True { get; set; } = [];
		public List<Node> False { get; set; } = [];

		public override bool Equals(object? obj) => base.Equals(obj) && obj is IfNode node && Equals(node.Test, Test)
			&& node.True.Equals(True) && node.False.Equals(False);

		public override int GetHashCode() => base.GetHashCode() ^ True.GetHashCode() ^ False.GetHashCode() ^ (Test?.GetHashCode() ?? 0);
	}

	public class LoopNode(Node test) : Node(NodeType.Statement)
	{
		public readonly Node Test = test;
		public List<Node> Body { get; set; } = [];

		public override bool Equals(object? obj) => base.Equals(obj) && obj is LoopNode node && node.Test.Equals(Test) && node.Body.Equals(Body);
		public override int GetHashCode() => base.GetHashCode() ^ Test.GetHashCode() ^ Body.GetHashCode();
	}

	public class WhileLoopNode(Node test) : LoopNode(test)
	{
		public override bool Equals(object? obj) => base.Equals(obj) && obj is WhileLoopNode;
		public override int GetHashCode() => base.GetHashCode() ^ 41;
	}

	public class ForLoopNode(Node init, Node test, Node end) : LoopNode(test)
	{
		public readonly Node Init = init;
		public readonly Node End = end;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is ForLoopNode node
			&& node.Init.Equals(Init) && node.End.Equals(End);

		public override int GetHashCode() => base.GetHashCode() ^ Init.GetHashCode() ^ End.GetHashCode();
	}

	public class UnaryNode(Node node, Opcode op) : Node(NodeType.Expression)
	{
		public readonly Node Node = node;
		public readonly Opcode Op = op;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is UnaryNode node && node.Node.Equals(Node) && node.Op.Equals(Op);
		public override int GetHashCode() => base.GetHashCode() ^ Node.GetHashCode() ^ Op.GetHashCode();
	}

	public class BinaryNode(Node left, Node right, Opcode op) : Node(NodeType.Expression)
	{
		public readonly Node Left = left;
		public readonly Node Right = right;
		public readonly Opcode Op = op;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is BinaryNode node
			&& node.Left.Equals(Left) && node.Right.Equals(Right) && node.Op.Equals(Op);

		public override int GetHashCode() => base.GetHashCode() ^ Left.GetHashCode() ^ Right.GetHashCode() ^ Op.GetHashCode();
	}

	public class BinaryStringNode(Node left, Node right, Opcode op, bool not = false) : BinaryNode(left, right, op)
	{
		public readonly bool Not = not;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is BinaryStringNode node && node.Not.Equals(Not);
		public override int GetHashCode() => base.GetHashCode() ^ Not.GetHashCode();
	}

	public class ConcatNode(Node left, char? ch = null) : Node(NodeType.Expression)
	{
		public readonly Node Left = left;

		private Node _right = null;

		public Node Right
		{
			get => _right;
			set => _right ??= value;
		}

		public readonly char? Char = ch;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is ConcatNode node
			&& node.Left.Equals(Left) && Equals(node._right, _right) && Equals(node.Char, Char);

		public override int GetHashCode() => base.GetHashCode() ^ Left.GetHashCode() ^ (_right?.GetHashCode() ?? 0) ^ (Char?.GetHashCode() ?? 0);
	}

	public class CommaConcatNode : ConcatNode
	{
		public CommaConcatNode(Node left) : base(left)
		{
			Type = NodeType.CommaConcat;
		}

		public override bool Equals(object? obj) => base.Equals(obj) && obj is CommaConcatNode;
		public override int GetHashCode() => base.GetHashCode();
	}

	public class VariableNode(string name, Node? index = null) : Node(NodeType.Expression)
	{
		public readonly string Name = name;
		public Node? Index { get; set; } = index;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is VariableNode node && node.Name.Equals(Name) && Equals(node.Index, Index);
		public override int GetHashCode() => base.GetHashCode() ^ Name.GetHashCode() ^ (Index?.GetHashCode() ?? 0);
	}

	public class FieldNode(string name) : Node(NodeType.Expression)
	{
		public readonly string Name = name;

		public Node? Object { get; set; } = null;
		public Node? Index { get; set; } = null;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is FieldNode node
			&& node.Name.Equals(Name) && Equals(node.Object, Object) && Equals(node.Index, Index);

		public override int GetHashCode() => base.GetHashCode() ^ Name.GetHashCode() ^ (Object?.GetHashCode() ?? 0) ^ (Index?.GetHashCode() ?? 0);
	}

	public class ConstantNode<T>(T value) : Node(NodeType.Expression)
	{
		public readonly T Value = value;
		public ConstantNode(ImmediateInstruction<T> instruction) : this(instruction.Value) { }

		public override bool Equals(object? obj) => base.Equals(obj) && obj is ConstantNode<T> node && Equals(node.Value, Value);
		public override int GetHashCode() => base.GetHashCode() ^ (Value?.GetHashCode() ?? 0);
	}

	public class AssignmentNode(Node left, Node right, Opcode? op = null) : Node(NodeType.ExpressionStatement)
	{
		public readonly Node Left = left;
		public readonly Node Right = right;
		public readonly Opcode? Operator = op;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is AssignmentNode node
			&& node.Left.Equals(Left) && node.Right.Equals(Right) && Equals(node.Operator, Operator);

		public override int GetHashCode() => base.GetHashCode() ^ Left.GetHashCode() ^ Right.GetHashCode() ^ (Operator?.GetHashCode() ?? 0);
	}

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
	}

	public class FunctionDeclarationNode(FunctionInstruction instruction) : Node(NodeType.Statement)
	{
		public readonly string Name = instruction.Name;
		public readonly string? Namespace = instruction.Namespace;
		public readonly List<string> Arguments = [..instruction.Arguments];
		public List<Node> Body { get; set; } = [];

		public override bool Equals(object? obj) => base.Equals(obj) && obj is FunctionDeclarationNode node
			&& node.Name.Equals(Name) && Equals(node.Namespace, Namespace)
			&& node.Arguments.Equals(Arguments) && node.Body.Equals(Body);

		public override int GetHashCode() => base.GetHashCode() ^ Name.GetHashCode() ^ (Namespace?.GetHashCode() ?? 0)
			^ Arguments.GetHashCode() ^ Body.GetHashCode();
	}

	public class PackageNode(string name) : Node(NodeType.Statement)
	{
		public readonly string Name = name;
		public readonly List<FunctionDeclarationNode> Functions = [];

		public override bool Equals(object? obj) => base.Equals(obj) && obj is PackageNode node
			&& node.Name.Equals(Name) && node.Functions.Equals(Functions);

		public override int GetHashCode() => base.GetHashCode() ^ Name.GetHashCode() ^ Functions.GetHashCode();
	}
}
