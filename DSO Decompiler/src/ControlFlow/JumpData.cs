using DSODecompiler.Util;

namespace DSODecompiler.ControlFlow
{
	public class JumpData
	{
		public class Exception : System.Exception
		{
			public Exception () { }
			public Exception (string message) : base (message) { }
			public Exception (string message, System.Exception inner) : base (message, inner) { }
		}

		/// <summary>
		/// Finds jumps and loops and stores them.<br />
		/// <br />
		/// Finds loops based on back edges, which are defined as CFG nodes jumping back to one of
		/// their dominators.<br />
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		public static JumpData FindJumps (ControlFlowGraph graph)
		{
			var data = new JumpData ();

			graph.PostorderDFS ((ControlFlowGraph.Node node) =>
			{
				if (node.ImmediateDom == null && node != graph.EntryPoint)
				{
					throw new Exception ("Immediate dominator is null!");
				}

				var last = node.LastInstruction;

				if (!Opcodes.IsJump (last.Op))
				{
					return;
				}

				var jumpTarget = last.Operands[0];

				data.AddJump (last.Addr, jumpTarget);

				if (graph.GetNode (jumpTarget).Dominates (node))
				{
					if (jumpTarget > node.Addr)
					{
						// Back edge somehow jumps forward??
						throw new Exception ($"Node at {jumpTarget} dominates earlier node at {node.Addr}");
					}

					data.AddLoop (jumpTarget, last.NextAddr);
				}
			});

			return data;
		}

		// startAddr=>(endAddr1, endAddr2, ...)
		protected Multidictionary<uint, uint> loops = new Multidictionary<uint, uint> ();

		// targetAddr=>(sourceAddr1, sourceAddr2, ...)
		protected Multidictionary<uint, uint> targets = new Multidictionary<uint, uint> ();

		public int LoopCount { get; protected set; }
		public int JumpCount { get; protected set; }

		public bool AddLoop (uint startAddr, uint endAddr)
		{
			var success = loops.Add (startAddr, endAddr);

			if (success)
			{
				LoopCount++;
			}

			return success;
		}

		public bool AddJump (uint sourceAddr, uint targetAddr)
		{
			var success = targets.Add (targetAddr, sourceAddr);

			if (success)
			{
				JumpCount++;
			}

			return success;
		}

		public bool IsLoop (uint startAddr) => loops.ContainsKey (startAddr);
		public bool IsLoop (uint startAddr, uint endAddr) => loops.ContainsValue (startAddr, endAddr);

		public bool IsTarget (uint addr) => targets.ContainsKey (addr);
		public bool IsJump (uint sourceAddr, uint targetAddr) => targets.ContainsValue (targetAddr, sourceAddr);
	}
}
