using DSODecompiler.Disassembly;
using DSODecompiler.Opcodes;

using System;
using System.Collections.Generic;

namespace DSODecompiler.AST.Nodes
{

    public class BinaryExpressionNode : Node
	{
		public Opcode Operator { get; }

		public Node Left { get; set; } = null;
		public Node Right { get; set; } = null;

		public bool IsLogicalOperator { get; }

		public BinaryExpressionNode (Instruction instruction, Node left = null, Node right = null)
		{
			Operator = instruction.Opcode;
			Left = left;
			Right = right;

			IsLogicalOperator = instruction is BranchInstruction branch && branch.IsLogicalOperator;
		}

		public override bool Equals (object obj) => obj is BinaryExpressionNode binary
			&& Equals(binary.Operator, Operator)
			&& Equals(binary.Left, Left)
			&& Equals(binary.Right, Right);

		public override int GetHashCode () => Operator.GetHashCode()
			^ (Left?.GetHashCode() ?? 0)
			^ (Right?.GetHashCode() ?? 0);
	}

	public class UnaryExpressionNode : Node
	{
		public Opcode Operator { get; }
		public Node Expression { get; set; } = null;

		public UnaryExpressionNode (Opcode opcode, Node expression)
		{
			Operator = opcode;
			Expression = expression;
		}

		public UnaryExpressionNode (Instruction instruction, Node expression)
			: this(instruction.Opcode, expression) { }

		public override bool Equals (object obj) => obj is UnaryExpressionNode unary
			&& Equals(unary.Operator, Operator)
			&& Equals(unary.Expression, Expression);

		public override int GetHashCode () => Operator.GetHashCode() ^ (Expression?.GetHashCode() ?? 0);
	}

	public abstract class VariableFieldNode : Node
	{
		public string Name { get; set; } = null;
		public Node ArrayIndex { get; set; } = null;
		public bool IsArray => ArrayIndex != null;

		public override bool Equals (object obj) => obj is VariableFieldNode node
			&& Equals(node.Name, Name)
			&& Equals(node.ArrayIndex, ArrayIndex);

		public override int GetHashCode () => (Name?.GetHashCode() ?? 0) ^ (ArrayIndex?.GetHashCode() ?? 0);
	}

	public class VariableNode : VariableFieldNode
	{
		public VariableNode (string name = null) => Name = name;

		public override bool Equals (object obj) => base.Equals(obj) && obj is VariableNode;
		public override int GetHashCode () => base.GetHashCode();
	}

	public class FieldNode : VariableFieldNode
	{
		public Node ObjectExpr { get; set; } = null;

		public FieldNode (string name = null) => Name = name;

		public override bool Equals (object obj) => base.Equals(obj) && obj is FieldNode node
			&& Equals(node.ObjectExpr, ObjectExpr);

		public override int GetHashCode () => base.GetHashCode() ^ (ObjectExpr?.GetHashCode() ?? 0);
	}

	public abstract class StringConcatNode : Node
	{
		public Node Left { get; set; } = null;
		public Node Right { get; set; } = null;

		public override bool Equals (object obj) => obj is StringConcatNode node
			&& Equals(node.Left, Left)
			&& Equals(node.Right, Right);

		public override int GetHashCode () => (Left?.GetHashCode() ?? 0) ^ (Right?.GetHashCode() ?? 0);
	}

	public class ConcatNode : StringConcatNode
	{
		public char? AppendChar { get; set; } = null;

		public ConcatNode () { }
		public ConcatNode (char appendChar) => AppendChar = appendChar;

		public override bool Equals (object obj) => obj is ConcatNode node && Equals(node.AppendChar, AppendChar);
		public override int GetHashCode () => base.GetHashCode() ^ (AppendChar?.GetHashCode() ?? 0);
	}

	public class CommaCatNode : StringConcatNode
	{
		public override bool Equals (object obj) => obj is CommaCatNode;
		public override int GetHashCode () => base.GetHashCode();
	}

	public class ConstantNode<T> : Node
	{
		public T Value { get; }

		public ConstantNode (T value) => Value = value;

		public override bool Equals (object obj) => obj is ConstantNode<T> node && Equals(node.Value, Value);
		public override int GetHashCode () => Value.GetHashCode();
	}

	public class StringConstantNode : ConstantNode<string>
	{
		public enum StringType
		{
			String,
			TaggedString,
			Identifier,
		};

		public StringType Type { get; }

		public StringConstantNode (string value, StringType type) : base(value) => Type = type;
		public override bool Equals (object obj) => obj is StringConstantNode node && Equals(node.Type, Type);
		public override int GetHashCode () => base.GetHashCode() ^ Type.GetHashCode();
	}

	public class AssignmentNode : Node
	{
		public VariableFieldNode VariableField { get; set; } = null;
		public Node Expression { get; set; } = null;
		public Opcode Operator { get; set; } = null;

		public AssignmentNode (VariableFieldNode variableField, Node expression, Instruction instruction = null)
		{
			VariableField = variableField;
			Expression = expression;
			Operator = instruction?.Opcode;
		}

		public AssignmentNode (BinaryExpressionNode binaryExpr)
		{
			if (binaryExpr.Left is not VariableFieldNode)
			{
				throw new ArgumentException($"Left expression is not VariableFieldNode", nameof(binaryExpr));
			}

			VariableField = binaryExpr.Left as VariableFieldNode;
			Expression = binaryExpr.Right;
			Operator = binaryExpr.Operator;
		}

		public override bool Equals (object obj) => obj is AssignmentNode node
			&& Equals(node.VariableField, VariableField)
			&& Equals(node.Expression, Expression)
			&& Equals(node.Operator, Operator);

		public override int GetHashCode () => (VariableField?.GetHashCode() ?? 0)
			^ (Expression?.GetHashCode() ?? 0)
			^ (Operator?.GetHashCode() ?? 0);
	}

	public class FunctionCallNode : Node
	{
		public enum CallType : uint
		{
			FunctionCall,
			MethodCall,
			ParentCall,
		}

		public string Name { get; }
		public string Namespace { get; }

		public CallType Type { get; }

		public NodeList Arguments = new();

		public FunctionCallNode (CallInstruction instruction)
		{
			Name = instruction.Name;
			Namespace = instruction.Namespace;
			Type = (CallType) instruction.CallType;
		}

		public override bool Equals (object obj) => obj is FunctionCallNode node
			&& Equals(node.Name, Name)
			&& Equals(node.Namespace, Namespace)
			&& Equals(node.Type, Type)
			&& Equals(node.Arguments, Arguments);

		public override int GetHashCode () => (Name?.GetHashCode() ?? 0)
			^ (Namespace?.GetHashCode() ?? 0)
			^ Type.GetHashCode()
			^ Arguments.GetHashCode();
	}

	public class FunctionStatementNode : Node
	{
		public override bool IsExpression => false;

		public string Name { get; }
		public string Namespace { get; }
		public string Package { get; }
		public NodeList Body { get; set; } = null;

		public readonly List<string> Arguments = new();

		public FunctionStatementNode (string name, string ns, string package)
		{
			Name = name;
			Namespace = ns;
			Package = package;
		}

		public override bool Equals (object obj)
		{
			if (obj is not FunctionStatementNode node || node.Arguments.Count != Arguments.Count)
			{
				return false;
			}

			for (var i = 0; i < node.Arguments.Count; i++)
			{
				if (!Equals(node.Arguments[i], Arguments[i]))
				{
					return false;
				}
			}

			return Equals(node.Name, Name)
				&& Equals(node.Namespace, Namespace)
				&& Equals(node.Package, Package)
				&& Equals(node.Body, Body);
		}

		public override int GetHashCode () => (Name?.GetHashCode() ?? 0)
			^ (Namespace?.GetHashCode() ?? 0)
			^ (Package?.GetHashCode() ?? 0)
			^ (Body?.GetHashCode() ?? 0)
			^ Arguments.GetHashCode();
	}

	public class ObjectNode : Node
	{
		public string ParentObject { get; }
		public bool IsDataBlock { get; }

		public Node ClassNameExpression { get; set; } = null;
		public Node NameExpression { get; set; } = null;

		/// <summary>
		/// Whether this is a standalone object declaration, or a subobject of another object.<br/><br/>
		///
		/// Example:<br/><br/>
		///
		/// <code>
		///	new ScriptObject() // Root object
		///	{
		///		new ScriptObject(); // Subobject
		///	};
		/// </code>
		/// </summary>
		public bool IsRoot { get; set; } = false;

		public NodeList Arguments = new();
		public NodeList Slots = new();
		public NodeList Subobjects = new();

		public ObjectNode (CreateObjectInstruction instruction)
		{
			ParentObject = instruction.Parent;
			IsDataBlock = instruction.IsDataBlock;
		}

		public override bool Equals (object obj) => obj is ObjectNode node
			&& Equals(node.ParentObject, ParentObject)
			&& Equals(node.IsDataBlock, IsDataBlock)
			&& Equals(node.ClassNameExpression, ClassNameExpression)
			&& Equals(node.NameExpression, NameExpression)
			&& Equals(node.IsRoot, IsRoot)
			&& Equals(node.Arguments, Arguments)
			&& Equals(node.Slots, Slots)
			&& Equals(node.Subobjects, Subobjects);

		public override int GetHashCode () => (ParentObject?.GetHashCode() ?? 0)
			^ (IsDataBlock.GetHashCode())
			^ (ClassNameExpression?.GetHashCode() ?? 0)
			^ (NameExpression?.GetHashCode() ?? 0)
			^ (IsRoot.GetHashCode())
			^ (Arguments.GetHashCode())
			^ (Slots.GetHashCode())
			^ (Subobjects.GetHashCode());
	}
}
