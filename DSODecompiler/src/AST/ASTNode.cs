using DSODecompiler.Disassembly;
using DSODecompiler.Opcodes;

using System;
using System.Collections.Generic;

namespace DSODecompiler.AST
{
	public abstract class ASTNode
	{
		public virtual bool IsExpression => true;
		public override bool Equals (object obj) => obj is ASTNode;
		public override int GetHashCode () => base.GetHashCode();
	}

	public class ASTNodeList : ASTNode
	{
		protected List<ASTNode> nodes = new();

		public override bool IsExpression => false;
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

		public override bool Equals (object obj)
		{
			if (obj is not ASTNodeList list || list.Count != Count)
			{
				return false;
			}

			for (var i = 0; i < list.Count; i++)
			{
				if (!list[i].Equals(this[i]))
				{
					return false;
				}
			}

			return true;
		}

		public override int GetHashCode ()
		{
			var hash = base.GetHashCode();

			foreach (var node in nodes)
			{
				hash ^= node.GetHashCode();
			}

			return hash;
		}
	}

	public class BreakStatementNode : ASTNode 
	{
		public override bool IsExpression => false;

		public override bool Equals (object obj) => obj is BreakStatementNode;
		public override int GetHashCode () => base.GetHashCode();
	}

	public class ContinueStatementNode : ASTNode
	{
		public override bool IsExpression => false;

		public override bool Equals (object obj) => obj is ContinueStatementNode;
		public override int GetHashCode () => base.GetHashCode();
	}

	/// <summary>
	/// Hacks on hacks on hacks... This is just to get for loops working.<br/><br/>
	///
	/// TODO: Again, I will come back and fix the code so I don't need to do this.
	/// </summary>
	public class ContinuePointMarkerNode : ASTNode
	{
		public override bool IsExpression => false;

		public override bool Equals (object obj) => obj is ContinuePointMarkerNode;
		public override int GetHashCode () => base.GetHashCode();
	}

	public class ReturnStatementNode : ASTNode
	{
		public override bool IsExpression => false;

		public ASTNode Value { get; set; } = null;

		public override bool Equals (object obj) => obj is ReturnStatementNode node && Equals(node.Value, Value);
		public override int GetHashCode () => Value?.GetHashCode() ?? 0;
	}

	/// <summary>
	/// For both ternaries and regular if/if-else statements.
	/// </summary>
	public class IfNode : ASTNode
	{
		/// <summary>
		/// This is a bit trickier because ternaries and if statements are impossible to differentiate
		/// without context, so this is more just if it <em>could</em> be an expression.
		/// </summary>
		public override bool IsExpression => Then.Count == 1
			&& Then[0].IsExpression
			&& HasElse
			&& Else.Count == 1
			&& Else[0].IsExpression;

		public bool HasElse => Else != null && Else.Count > 0;

		public ASTNode TestExpression { get; }

		public ASTNodeList Then = new();
		public ASTNodeList Else = new();

		public IfNode (ASTNode testExpression)
		{
			TestExpression = testExpression ?? throw new ArgumentNullException(nameof(testExpression));
		}

		public override bool Equals (object obj) => obj is IfNode ifNode
			&& Equals(ifNode.TestExpression, TestExpression)
			&& Equals(ifNode.Then, Then)
			&& Equals(ifNode.Else, Else);

		public override int GetHashCode () => TestExpression.GetHashCode() ^ Then.GetHashCode() ^ Else.GetHashCode();
	}

	public class LoopStatementNode : ASTNode
	{
		public override bool IsExpression => false;

		public ASTNode InitExpression { get; set; } = null;
		public ASTNode TestExpression { get; set; } = null;
		public ASTNode EndExpression { get; set; } = null;

		/// <summary>
		/// Whether this loop was collapsed from an if-loop structure. This is to prevent it from
		/// being collapsed again.
		/// </summary>
		public bool WasCollapsed { get; set; } = false;

		public ASTNodeList Body = new();

		public LoopStatementNode (ASTNode testExpression = null) => TestExpression = testExpression;

		public override bool Equals (object obj) => obj is LoopStatementNode loop
			&& Equals(loop.InitExpression, InitExpression)
			&& Equals(loop.TestExpression, TestExpression)
			&& Equals(loop.EndExpression, EndExpression);

		public override int GetHashCode () => (InitExpression?.GetHashCode() ?? 0)
			^ (TestExpression?.GetHashCode() ?? 0)
			^ (EndExpression?.GetHashCode() ?? 0)
			^ Body.GetHashCode();
	}

	public class BinaryExpressionNode : ASTNode
	{
		public Opcode Operator { get; }

		public ASTNode Left { get; set; } = null;
		public ASTNode Right { get; set; } = null;

		public bool IsLogicalOperator { get; }

		public BinaryExpressionNode (Instruction instruction, ASTNode left = null, ASTNode right = null)
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

	public class UnaryExpressionNode : ASTNode
	{
		public Opcode Operator { get; }
		public ASTNode Expression { get; set; } = null;

		public UnaryExpressionNode (Opcode opcode, ASTNode expression)
		{
			Operator = opcode;
			Expression = expression;
		}

		public UnaryExpressionNode (Instruction instruction, ASTNode expression)
			: this(instruction.Opcode, expression) { }

		public override bool Equals (object obj) => obj is UnaryExpressionNode unary
			&& Equals(unary.Operator, Operator)
			&& Equals(unary.Expression, Expression);

		public override int GetHashCode () => Operator.GetHashCode() ^ (Expression?.GetHashCode() ?? 0);
	}

	public abstract class VariableFieldNode : ASTNode
	{
		public string Name { get; set; } = null;
		public ASTNode ArrayIndex { get; set; } = null;
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
		public ASTNode ObjectExpr { get; set; } = null;

		public FieldNode (string name = null) => Name = name;

		public override bool Equals (object obj) => base.Equals(obj) && obj is FieldNode node
			&& Equals(node.ObjectExpr, ObjectExpr);

		public override int GetHashCode () => base.GetHashCode() ^ (ObjectExpr?.GetHashCode() ?? 0);
	}

	public abstract class StringConcatNode : ASTNode
	{
		public ASTNode Left { get; set; } = null;
		public ASTNode Right { get; set; } = null;

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

	public class ConstantNode<T> : ASTNode
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

	public class AssignmentNode : ASTNode
	{
		public VariableFieldNode VariableField { get; set; } = null;
		public ASTNode Expression { get; set; } = null;
		public Opcode Operator { get; set; } = null;

		public AssignmentNode (VariableFieldNode variableField, ASTNode expression, Instruction instruction = null)
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

	public class FunctionStatementNode : ASTNode
	{
		public override bool IsExpression => false;

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

	/// <summary>
	/// Not a valid node -- It just acts as a marker to stop popping nodes from the stack.
	/// </summary>
	public class PushFrameNode : ASTNode
	{
		public override bool IsExpression => false;

		public ASTNodeList Nodes = new();

		public override bool Equals (object obj) => obj is PushFrameNode node && Equals(node.Nodes, Nodes);
		public override int GetHashCode () => Nodes.GetHashCode();
	}
}
