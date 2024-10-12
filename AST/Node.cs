using DSO.Opcodes;

namespace DSO.AST
{
	public enum NodeType
	{
		Statement,
		Expression,
		ExpressionStatement,
	}

	public abstract class Node(NodeType type)
	{
		public NodeType Type { get; protected set; } = type;
	}

	public class BreakNode() : Node(NodeType.Statement) { }
	public class ContinueNode() : Node(NodeType.Statement) { }

	public class ReturnNode(Node? value) : Node(NodeType.Statement)
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

	public class UnaryNode(Node node) : Node(NodeType.Expression)
	{
		public readonly Node Node = node;
	}

	public class BinaryNode(Node left, Node right, Opcode op) : Node(NodeType.Expression)
	{
		public readonly Node Left = left;
		public readonly Node Right = right;
		public readonly Opcode Op = op;
	}

	public class StringBinaryNode(Node left, Node right, Opcode op, bool not) : BinaryNode(left, right, op)
	{
		public readonly bool Not = not;
	}

	public class ConcatNode(Node left, Node right, Opcode op, char? ch) : Node(NodeType.Expression)
	{
		public readonly Node Left = left;
		public readonly Node Right = right;
		public readonly Opcode Op = op;
		public readonly char? Char = ch;
	}

	public class VariableOrFieldNode(string name, Node? index) : Node(NodeType.Expression)
	{
		public readonly string Name = name;
		public readonly Node? Index = index;
	}

	public class ConstantNode<T>(T value) : Node(NodeType.Expression)
	{
		public readonly T Value = value;
	}

	public class AssignmentNode(Node left, Node right, Opcode? op) : Node(NodeType.ExpressionStatement)
	{
		public readonly Node Left = left;
		public readonly Node Right = right;
		public readonly Opcode? Operator = op;
	}

	public class ObjectNode(bool isDataBlock, Node className, Node? objectName, string? parent) : Node(NodeType.ExpressionStatement)
	{
		public readonly bool IsDataBlock = isDataBlock;
		public readonly Node Class = className;
		public readonly Node? Name = objectName;
		public readonly string? Parent = parent;
		public readonly List<Node> Arguments = [];
		public readonly List<Node> Fields = [];
		public readonly List<Node> Children = [];
	}

	public class FunctionCallNode(string name, string? ns) : Node(NodeType.ExpressionStatement)
	{
		public readonly string Name = name;
		public readonly string? Namespace = ns;
		public readonly List<string> Arguments = [];
	}

	public class FunctionDeclarationNode(string name, string? ns) : Node(NodeType.Statement)
	{
		public readonly string Name = name;
		public readonly string? Namespace = ns;
		public readonly List<string> Arguments = [];
		public readonly List<Node> Body = [];
	}

	public class PackageNode(string name) : Node(NodeType.Statement)
	{
		public readonly string Name = name;
		public readonly List<FunctionDeclarationNode> Functions = [];
	}
}
