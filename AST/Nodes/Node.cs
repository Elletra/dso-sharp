﻿/**
 * Node.cs
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
	public enum NodeType
	{
		Statement,
		Expression,
		ExpressionStatement,
		CommaConcat, // Special case because the CommaConcatNode is only viable under one circumstance.
	}

	public abstract class Node(NodeType type)
	{
		public NodeType Type { get; protected set; } = type;

		public virtual int Precedence => 0;

		public bool IsExpression => Type == NodeType.Expression || Type == NodeType.ExpressionStatement;
		public bool IsExpressionOnly => Type == NodeType.Expression;
		public bool IsStatement => Type == NodeType.Statement || Type == NodeType.ExpressionStatement;
		public bool IsStatementOnly => Type == NodeType.Statement;

		protected bool CheckPrecedenceAndAssociativity(Node node) => !IsAssociativeWith(node)
			&& (node.Precedence >= Precedence || (node is AssignmentNode assign && !assign.IsIncrementDecrement));

		public virtual bool IsAssociativeWith(Node compare) => false;
		public override bool Equals(object? obj) => obj is Node node && node.Type.Equals(Type);
		public override int GetHashCode() => Type.GetHashCode();
		public virtual void Visit(CodeWriter writer, bool isExpression) { }
	}
}
