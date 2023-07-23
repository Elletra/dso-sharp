using System;

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
		public override bool IsExpression => false;

		public Node Value { get; set; } = null;

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

		public bool HasElse => Else.Count > 0;

		public Node TestExpression { get; }

		public NodeList Then = new();
		public NodeList Else = new();

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
		public override bool IsExpression => false;

		public Node InitExpression { get; set; } = null;
		public Node TestExpression { get; set; } = null;
		public Node EndExpression { get; set; } = null;

		/// <summary>
		/// Whether this loop was collapsed from an if-loop structure. This is to prevent it from
		/// being collapsed again.
		/// </summary>
		public bool WasCollapsed { get; set; } = false;

		public NodeList Body = new();

		public LoopStatementNode (Node testExpression = null) => TestExpression = testExpression;

		public override bool Equals (object obj) => obj is LoopStatementNode loop
			&& Equals(loop.InitExpression, InitExpression)
			&& Equals(loop.TestExpression, TestExpression)
			&& Equals(loop.EndExpression, EndExpression);

		public override int GetHashCode () => (InitExpression?.GetHashCode() ?? 0)
			^ (TestExpression?.GetHashCode() ?? 0)
			^ (EndExpression?.GetHashCode() ?? 0)
			^ Body.GetHashCode();
	}
}
