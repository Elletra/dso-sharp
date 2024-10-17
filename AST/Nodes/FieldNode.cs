/**
 * FieldNode.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using DSO.CodeGenerator;

namespace DSO.AST.Nodes
{
	public class FieldNode(string name) : Node(NodeType.Expression)
	{
		public readonly string Name = name;

		private Node? _object = null;
		private Node? _index = null;

		/* I realize these are abominations and I deeply apologize. */
		public Node? Object { get => _object; set => _object = value is ConstantStringNode node ? node.ConvertToUIntNode() ?? node.ConvertToDoubleNode() ?? value : value; }
		public Node? Index { get => _index; set => _index = value is ConstantStringNode node ? node.ConvertToUIntNode() ?? node.ConvertToDoubleNode() ?? value : value; }

		public override bool IsAssociativeWith(Node compare) => compare is FieldNode;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is FieldNode node
			&& node.Name.Equals(Name) && Equals(node.Object, Object) && Equals(node.Index, Index);

		public override int GetHashCode() => base.GetHashCode() ^ Name.GetHashCode() ^ (Object?.GetHashCode() ?? 0) ^ (Index?.GetHashCode() ?? 0);

		public override void Visit(TokenStream stream, bool isExpression)
		{
			if (Object != null)
			{
				stream.Write(Object, node => node.Precedence > Precedence);
				stream.Write(".");
			}

			stream.Write(Name);

			if (Index != null)
			{
				stream.Write("[");
				stream.Write(Index, isExpression: true);
				stream.Write("]");
			}
		}
	}
}
