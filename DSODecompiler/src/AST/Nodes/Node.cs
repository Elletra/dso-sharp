using System;
using System.Collections.Generic;

namespace DSODecompiler.AST.Nodes
{
    public abstract class Node
    {
        public virtual bool IsExpression => true;
        public override bool Equals(object obj) => obj is Node;
        public override int GetHashCode() => base.GetHashCode();
    }

    public class NodeList : Node
    {
        protected List<Node> nodes = new();

        public override bool IsExpression => false;
        public int Count => nodes.Count;
        public Node this[int index] => nodes[index];

        public void ForEach(Action<Node> action) => nodes.ForEach(action);

        public Node Push(Node node)
        {
            if (node is NodeList list)
            {
                list.ForEach(child => Push(child));
            }
            else
            {
                nodes.Add(node);
            }

            return node;
        }

        public Node Pop()
        {
            if (nodes.Count <= 0)
            {
                return null;
            }

            var node = nodes[^1];

            nodes.RemoveAt(nodes.Count - 1);

            return node;
        }

        public Node Peek() => nodes.Count > 0 ? nodes[^1] : null;

        public override bool Equals(object obj)
        {
            if (obj is not NodeList list || list.Count != Count)
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

        public override int GetHashCode()
        {
            var hash = base.GetHashCode();

            foreach (var node in nodes)
            {
                hash ^= node.GetHashCode();
            }

            return hash;
        }
    }
}
