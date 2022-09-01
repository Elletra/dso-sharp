using System.Collections.Generic;

using DsoDecompiler;
using DsoDecompiler.Loader;

namespace DsoDecompiler.Disassembler
{
	public abstract class BytecodeReader
	{
		protected FileData data = null;

		protected HashSet<uint> visited = new HashSet<uint> ();
		protected Queue<uint> queue = new Queue<uint> ();

		protected uint Pos { get; set; } = 0;

		public BytecodeReader (FileData fileData)
		{
			data = fileData;
		}

		protected virtual void ReadCode ()
		{
			// Enqueue entry point
			AddToQueue (0);

			/* The queue should typically only have 1 item in it. The reason we use a queue at all
			   is to disassemble jumps that jump in the middle of an instruction to try to avoid
			   disassembly.

			   I don't think there are any DSO files that actually do that, but it doesn't hurt to
			   be thorough... */
			while (queue.Count > 0)
			{
				ReadFrom (queue.Dequeue ());
			}
		}

		protected virtual void ReadFrom (uint startAddr)
		{
			Pos = startAddr;

			while (!IsAtEnd () && !visited.Contains (Pos))
			{
				ReadOp (Peek ());
			}
		}

		protected virtual void ReadOp (uint op)
		{
			visited.Add (Pos);

			switch ((Opcodes.Ops) op)
			{
				case Opcodes.Ops.OP_JMP:
				case Opcodes.Ops.OP_JMPIF:
				case Opcodes.Ops.OP_JMPIFF:
				case Opcodes.Ops.OP_JMPIFNOT:
				case Opcodes.Ops.OP_JMPIFFNOT:
				case Opcodes.Ops.OP_JMPIF_NP:
				case Opcodes.Ops.OP_JMPIFNOT_NP:
					ReadJump (op);
					break;

				case Opcodes.Ops.OP_FUNC_DECL:
					ReadFuncDecl (op);
					break;

				case Opcodes.Ops.OP_RETURN:
					ReadReturn (op);
					break;
			}
		}

		protected virtual void ReadJump (uint op) => AddToQueue (Peek (Pos + 1));
		protected virtual void ReadFuncDecl (uint op) => AddToQueue (Peek (Pos + 5));
		protected virtual void ReadReturn (uint op) {}

		protected virtual void Reset ()
		{
			Pos = 0;
			queue.Clear ();
			visited.Clear ();
		}

		protected uint GetOpcodeSize (uint op, uint addr)
		{
			switch ((Opcodes.Ops) op)
			{
				case Opcodes.Ops.OP_FUNC_DECL:
					return 7 + Peek (addr + 6);

				case Opcodes.Ops.OP_CALLFUNC:
				case Opcodes.Ops.OP_CALLFUNC_RESOLVE:
				case Opcodes.Ops.OP_CREATE_OBJECT:
					return 4;

				case Opcodes.Ops.OP_LOADIMMED_UINT:
				case Opcodes.Ops.OP_LOADIMMED_FLT:
				case Opcodes.Ops.OP_LOADIMMED_STR:
				case Opcodes.Ops.OP_LOADIMMED_IDENT:
				case Opcodes.Ops.OP_TAG_TO_STR:
				case Opcodes.Ops.OP_ADVANCE_STR_APPENDCHAR:
				case Opcodes.Ops.OP_JMP:
				case Opcodes.Ops.OP_JMPIF:
				case Opcodes.Ops.OP_JMPIFF:
				case Opcodes.Ops.OP_JMPIFNOT:
				case Opcodes.Ops.OP_JMPIFFNOT:
				case Opcodes.Ops.OP_JMPIF_NP:
				case Opcodes.Ops.OP_JMPIFNOT_NP:
				case Opcodes.Ops.OP_SETCURVAR:
				case Opcodes.Ops.OP_SETCURVAR_CREATE:
				case Opcodes.Ops.OP_SETCURFIELD:
				case Opcodes.Ops.OP_ADD_OBJECT:
				case Opcodes.Ops.OP_END_OBJECT:
					return 2;

				default:
					return 1;
			}
		}

		protected void AddToQueue (uint addr)
		{
			if (!visited.Contains (addr))
			{
				queue.Enqueue (addr);
			}
		}

		protected bool IsValidAddr (uint addr) => addr < data.CodeSize;

		protected uint Read () => data.Op (Pos++);
		protected uint Peek () => Peek (Pos);
		protected uint Peek (uint addr) => data.Op (addr);
		protected void Skip (uint amount = 1) => Pos += amount;

		protected bool IsAtEnd () => Pos >= data.CodeSize;
	}
}
