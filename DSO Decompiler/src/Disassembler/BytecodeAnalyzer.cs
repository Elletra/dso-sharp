using System.Collections.Generic;
using DsoDecompiler.Loader;

namespace DsoDecompiler.Disassembler
{
	public class BytecodeAnalyzer : BytecodeReader
	{
		protected HashSet<uint> cfgAddrs = new HashSet<uint> ();

		public BytecodeAnalyzer (FileData fileData) : base (fileData) { }

		public HashSet<uint> Analyze ()
		{
			Reset ();
			ReadCode ();
			return cfgAddrs;
		}

		protected override void Reset ()
		{
			base.Reset ();
			cfgAddrs.Clear ();
		}

		protected override void ReadOp (uint op)
		{
			base.ReadOp (op);
			Skip (GetOpcodeSize (op, Pos));
		}

		protected override void ReadJump (uint op)
		{
			/* First rule of control flow graphs: jump targets start blocks, jumps end them. */

			// Add jump target.
			AddCfgAddr (Peek (Pos + 1));

			// Add address of next instruction.
			AddCfgAddr (Pos + GetOpcodeSize (op, Pos));
		}

		protected override void ReadFuncDecl (uint op)
		{
			/* We're counting function bounds as jumps here. */

			// Add function end
			AddCfgAddr (Peek (Pos + 5));

			// Add address of next instruction
			AddCfgAddr (Pos + GetOpcodeSize (op, Pos));
		}

		protected override void ReadReturn (uint op)
		{
			/* We're also counting returns as jumps. */

			// Add address of next instruction
			AddCfgAddr (Pos + GetOpcodeSize (op, Pos));
		}

		protected void AddCfgAddr (uint addr)
		{
			if (IsValidAddr (addr))
			{
				cfgAddrs.Add (addr);
			}
		}
	}
}
