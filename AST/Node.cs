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
	}

	public class BreakNode() : Node(NodeType.Statement) { }
	public class ContinueNode() : Node(NodeType.Statement) { }

	public class ReturnNode(Node? value = null) : Node(NodeType.Statement)
	{
		public Node? Value = value;
	}

	public class IfNode(Node test) : Node(NodeType.Statement)
	{
		public readonly Node Test = test;
		public readonly List<Node> Body = [];
		public readonly List<Node> Else = [];
	}

	public class WhileNode(Node test) : Node(NodeType.Statement)
	{
		public readonly Node Test = test;
		public readonly List<Node> Body = [];
	}

	public class ForLoopNode(Node init, Node test, Node end) : Node(NodeType.Statement)
	{
		public readonly Node Init = init;
		public readonly Node Test = test;
		public readonly Node End = end;
		public readonly List<Node> Body = [];
	}

	public class UnaryNode(Node node, Opcode op) : Node(NodeType.Expression)
	{
		public readonly Node Node = node;
		public readonly Opcode Op = op;
	}

	public class BinaryNode(Node left, Node right, Opcode op) : Node(NodeType.Expression)
	{
		public readonly Node Left = left;
		public readonly Node Right = right;
		public readonly Opcode Op = op;
	}

	public class BinaryStringNode(Node left, Node right, Opcode op, bool not = false) : BinaryNode(left, right, op)
	{
		public readonly bool Not = not;
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
	}

	public class CommaConcatNode : ConcatNode
	{
		public CommaConcatNode(Node left) : base(left)
		{
			Type = NodeType.CommaConcat;
		}
	}

	public class VariableNode(string name, Node? index = null) : Node(NodeType.Expression)
	{
		public readonly string Name = name;
		public Node? Index { get; set; } = index;
	}

	public class FieldNode(string name) : Node(NodeType.Expression)
	{
		public readonly string Name = name;

		public Node? Object { get; set; } = null;
		public Node? Index { get; set; } = null;
	}

	public class ConstantNode<T>(T value) : Node(NodeType.Expression)
	{
		public readonly T Value = value;
		public ConstantNode(ImmediateInstruction<T> instruction) : this(instruction.Value) { }
	}

	public class AssignmentNode(Node left, Node right, Opcode? op = null) : Node(NodeType.ExpressionStatement)
	{
		public readonly Node Left = left;
		public readonly Node Right = right;
		public readonly Opcode? Operator = op;
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
	}

	public class FunctionCallNode(CallInstruction instruction) : Node(NodeType.ExpressionStatement)
	{
		public readonly string Name = instruction.Name;
		public readonly string? Namespace = instruction.Namespace;
		public readonly List<Node> Arguments = [];
	}

	public class FunctionDeclarationNode(FunctionInstruction instruction) : Node(NodeType.Statement)
	{
		public readonly string Name = instruction.Name;
		public readonly string? Namespace = instruction.Namespace;
		public readonly uint EndAddress = instruction.EndAddress;
		public readonly List<string> Arguments = [..instruction.Arguments];
		public readonly List<Node> Body = [];
	}

	public class PackageNode(string name) : Node(NodeType.Statement)
	{
		public readonly string Name = name;
		public readonly List<FunctionDeclarationNode> Functions = [];
	}
}
