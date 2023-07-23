namespace DSODecompiler.AST.Nodes
{
    /// <summary>
    /// Hacks on hacks on hacks... This is just to get for loops working.<br/><br/>
    ///
    /// TODO: Again, I will come back and fix the code so I don't need to do this.
    /// </summary>
    public class ContinuePointMarkerNode : Node
	{
		public override bool IsExpression => false;

		public override bool Equals (object obj) => obj is ContinuePointMarkerNode;
		public override int GetHashCode () => base.GetHashCode();
	}

	/// <summary>
	/// Not a valid node -- It just acts as a marker to stop popping nodes from the stack.
	/// </summary>
	public class PushFrameNode : Node
	{
		public override bool IsExpression => false;

		public NodeList Nodes = new();

		public override bool Equals (object obj) => obj is PushFrameNode node && Equals(node.Nodes, Nodes);
		public override int GetHashCode () => Nodes.GetHashCode();
	}
}
