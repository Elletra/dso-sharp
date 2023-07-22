using DSODecompiler.ControlFlow;
using DSODecompiler.Disassembly;
using DSODecompiler.Opcodes;

using System;
using System.Collections.Generic;

namespace DSODecompiler.AST
{
	public abstract class ASTNode { }

	public class ASTNodeList : ASTNode
	{
		protected List<ASTNode> nodes = new();

		public int Count => nodes.Count;
		public ASTNode this[int index] => nodes[index];

		public void ForEach (Action<ASTNode> action) => nodes.ForEach(action);

		public ASTNode Push (ASTNode node)
		{
			if (node is ASTNodeList list)
			{
				list.ForEach(child => Push(child));
			}
			else
			{
				nodes.Add(node);
			}

			return node;
		}

		public ASTNode Pop ()
		{
			if (nodes.Count <= 0)
			{
				return null;
			}

			var node = nodes[^1];

			nodes.RemoveAt(nodes.Count - 1);

			return node;
		}

		public ASTNode Peek () => nodes.Count > 0 ? nodes[^1] : null;
	}

	public class BreakStatementNode : ASTNode { }
	public class ContinueStatementNode : ASTNode { }

	public class ReturnStatementNode : ASTNode
	{
		public ASTNode Value { get; set; } = null;
	}

	/// <summary>
	/// For both ternaries and regular if/if-else statements.
	/// </summary>
	public class IfNode : ASTNode
	{
		public ASTNode TestExpression { get; }

		public ASTNodeList Then = new();
		public ASTNodeList Else = new();

		public IfNode (ASTNode testExpression)
		{
			TestExpression = testExpression ?? throw new ArgumentNullException(nameof(testExpression));
		}
	}

	public class LoopStatementNode : ASTNode
	{
		public ASTNode InitExpression { get; set; } = null;
		public ASTNode TestExpression { get; set; } = null;
		public ASTNode EndExpression { get; set; } = null;

		public ASTNodeList Body = new();

		public LoopStatementNode (ASTNode testExpression = null) => TestExpression = testExpression;
	}

	public class BinaryExpressionNode : ASTNode
	{
		public Opcode Opcode { get; }

		public ASTNode Left { get; set; } = null;
		public ASTNode Right { get; set; } = null;

		public bool IsLogicalOperator { get; }

		public BinaryExpressionNode (Instruction instruction, ASTNode left = null, ASTNode right = null)
		{
			Opcode = instruction.Opcode;
			Left = left;
			Right = right;

			IsLogicalOperator = instruction is BranchInstruction branch && branch.IsLogicalOperator;
		}
	}

	public class UnaryExpressionNode : ASTNode
	{
		public Opcode Opcode { get; }
		public ASTNode Expression { get; set; } = null;

		public UnaryExpressionNode (Opcode opcode, ASTNode expression)
		{
			Opcode = opcode;
			Expression = expression;
		}

		public UnaryExpressionNode (Instruction instruction, ASTNode expression)
			: this(instruction.Opcode, expression) { }
	}

	public abstract class VariableFieldNode : ASTNode
	{
		public string Name { get; set; } = null;
		public ASTNode ArrayIndex { get; set; } = null;
		public bool IsArray => ArrayIndex != null;
	}

	public class VariableNode : VariableFieldNode
	{
		public VariableNode (string name = null) => Name = name;
	}

	public class FieldNode : VariableFieldNode
	{
		public ASTNode ObjectExpr { get; set; } = null;

		public FieldNode (string name = null) => Name = name;
	}

	public abstract class StringConcatNode : ASTNode
	{
		public ASTNode Left { get; set; } = null;
		public ASTNode Right { get; set; } = null;
	}

	public class ConcatNode : StringConcatNode
	{
		public char? AppendChar { get; set; } = null;

		public ConcatNode () { }
		public ConcatNode (char appendChar) => AppendChar = appendChar;
	}

	public class CommaCatNode : StringConcatNode { }

	public class ConstantNode<T> : ASTNode
	{
		public T Value { get; }

		public ConstantNode (T value) => Value = value;
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
	}

	public class AssignmentNode : ASTNode
	{
		public VariableFieldNode VariableField { get; set; } = null;
		public ASTNode Expression { get; set; } = null;
		public Opcode Opcode { get; set; } = null;

		public AssignmentNode (VariableFieldNode variableField, ASTNode expression, Instruction instruction = null)
		{
			VariableField = variableField;
			Expression = expression;
			Opcode = instruction?.Opcode;
		}

		public AssignmentNode (BinaryExpressionNode binaryExpr)
		{
			if (binaryExpr.Left is not VariableFieldNode)
			{
				throw new ArgumentException($"Left expression is not VariableFieldNode", nameof(binaryExpr));
			}

			VariableField = binaryExpr.Left as VariableFieldNode;
			Expression = binaryExpr.Right;
			Opcode = binaryExpr.Opcode;
		}
	}

	public class FunctionCallNode : ASTNode
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

		public ASTNodeList Arguments = new();

		public FunctionCallNode (CallInstruction instruction)
		{
			Name = instruction.Name;
			Namespace = instruction.Namespace;
			Type = (CallType) instruction.CallType;
		}
	}

	public class FunctionStatementNode : ASTNode
	{
		public string Name { get; }
		public string Namespace { get; }
		public string Package { get; }
		public ASTNodeList Body { get; set; } = null;

		public readonly List<string> Arguments = new();

		public FunctionStatementNode (string name, string ns, string package)
		{
			Name = name;
			Namespace = ns;
			Package = package;
		}
	}

	public class ObjectNode : ASTNode
	{
		public string ParentObject { get; }
		public bool IsDataBlock { get; }

		public ASTNode ClassNameExpression { get; set; } = null;
		public ASTNode NameExpression { get; set; } = null;

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

		public ASTNodeList Arguments = new();
		public ASTNodeList Slots = new();
		public ASTNodeList Subobjects = new();

		public ObjectNode (CreateObjectInstruction instruction)
		{
			ParentObject = instruction.Parent;
			IsDataBlock = instruction.IsDataBlock;
		}
	}

	/// <summary>
	/// Not a valid node -- It just acts as a marker to stop popping nodes from the stack.
	/// </summary>
	public class PushFrameNode : ASTNode
	{
		public ASTNodeList Nodes = new();
	}
}
