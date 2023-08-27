using System;
using System.Collections.Generic;

namespace DSODecompiler.AST.Nodes
{
    public class BreakStatementNode : Node
	{
		public override bool IsExpression => false;

		public override bool Equals (object obj) => obj is BreakStatementNode;
		public override int GetHashCode () => base.GetHashCode();
	}

	public class ContinueStatementNode : Node
	{
		public override bool IsExpression => false;

		public override bool Equals (object obj) => obj is ContinueStatementNode;
		public override int GetHashCode () => base.GetHashCode();
	}

	public class ReturnStatementNode : Node
	{
		public Node Value { get; set; } = null;

		public bool ReturnsValue => Value != null;

		public override bool IsExpression => false;

		public override bool Equals (object obj) => obj is ReturnStatementNode node && Equals(node.Value, Value);
		public override int GetHashCode () => Value?.GetHashCode() ?? 0;
	}

	/// <summary>
	/// For both ternaries and regular if/if-else statements.
	/// </summary>
	public class IfNode : Node
	{
		/// <summary>
		/// This is a bit trickier because ternaries and if statements are impossible to differentiate
		/// without context, so this is more just if it <em>could</em> be an expression.
		/// </summary>
		public override bool IsExpression => Then.Count == 1
			&& Then[0].IsExpression
			&& Else.Count == 1
			&& Else[0].IsExpression;

		public override NodeAssociativity Associativity => NodeAssociativity.Left;
		public override int Precedence => 12;

		public bool HasElse => Else.Count > 0;

		public Node TestExpression { get; }

		public NodeList Then { get; set; } = new();
		public NodeList Else { get; set; } = new();

		public IfNode (Node testExpression)
		{
			TestExpression = testExpression ?? throw new ArgumentNullException(nameof(testExpression));
		}

		public override bool Equals (object obj) => obj is IfNode ifNode
			&& Equals(ifNode.TestExpression, TestExpression)
			&& Equals(ifNode.Then, Then)
			&& Equals(ifNode.Else, Else);

		public override int GetHashCode () => TestExpression.GetHashCode() ^ Then.GetHashCode() ^ Else.GetHashCode();
	}

	public class LoopStatementNode : Node
	{
		public enum LoopType
		{
			DoWhile,
			While,
			For,
		};

		public Node InitExpression { get; set; } = null;
		public Node TestExpression { get; set; } = null;
		public Node EndExpression { get; set; } = null;

		public override bool IsExpression => false;

		/// <summary>
		/// Whether this loop was collapsed from an if-loop structure. This is to prevent it from
		/// being collapsed again.
		/// </summary>
		public bool WasCollapsed { get; set; } = false;

		public NodeList Body { get; set; } = new();

		public LoopStatementNode (Node testExpression = null) => TestExpression = testExpression;

		public LoopType GetLoopType ()
		{
			if (InitExpression != null && EndExpression != null)
			{
				return LoopType.For;
			}

			return WasCollapsed ? LoopType.While : LoopType.DoWhile;
		}

		public override bool Equals (object obj) => obj is LoopStatementNode loop
			&& Equals(loop.InitExpression, InitExpression)
			&& Equals(loop.TestExpression, TestExpression)
			&& Equals(loop.EndExpression, EndExpression);

		public override int GetHashCode () => (InitExpression?.GetHashCode() ?? 0)
			^ (TestExpression?.GetHashCode() ?? 0)
			^ (EndExpression?.GetHashCode() ?? 0)
			^ Body.GetHashCode();
	}

	public class FunctionStatementNode : Node
	{
		public override bool IsExpression => false;

		public string Name { get; }
		public string Namespace { get; }
		public string Package { get; }
		public NodeList Body { get; set; } = new();

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

	public class PackageNode : Node
	{
		public string Name { get; }

		public readonly NodeList Functions = new();

		public PackageNode (string name) => Name = name;

		public override bool Equals (object obj) => obj is PackageNode package
			&& Equals(package.Name, Name)
			&& Equals(package.Functions, Functions);

		public override int GetHashCode () => Name.GetHashCode() ^ Functions.GetHashCode();
	}
}
