using System.Collections.Generic;

using DSODecompiler;
using DSODecompiler.Loader;

namespace DSODecompiler.Disassembler
{
	public class BytecodeDisassembler
	{
		public class Exception : System.Exception
		{
			public Exception () {}
			public Exception (string message) : base (message) {}
			public Exception (string message, System.Exception inner) : base (message, inner) {}
		}

		protected FileData data = null;
		protected InstructionGraph graph = null;
		protected Instruction prevInsn = null;

		protected HashSet<uint> visited = new HashSet<uint> ();
		protected Queue<uint> queue = new Queue<uint> ();

		protected uint Pos { get; set; } = 0;

		protected bool IsAtEnd => Pos >= data.CodeSize;

		public InstructionGraph Disassemble (FileData fileData)
		{
			Reset ();

			data = fileData;
			graph = new InstructionGraph ();

			ReadCode ();
			ConnectJumps ();

			return graph;
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
			prevInsn = null;

			while (!IsAtEnd && !visited.Contains (Pos))
			{
				var addr = Pos;

				ReadOp ((Opcodes.Ops) Read (), addr);
			}
		}

		protected virtual void ReadOp (Opcodes.Ops op, uint addr)
		{
			visited.Add (addr);

			var instruction = DisassembleInstruction (op, addr);

			ProcessInstruction (instruction, op, addr);

			if (prevInsn != null)
			{
				prevInsn.AddEdgeTo (instruction);
			}

			graph.Add (instruction);
			prevInsn = instruction;
		}

		protected Instruction DisassembleInstruction (Opcodes.Ops op, uint addr)
		{
			switch (op)
			{
				case Opcodes.Ops.OP_FUNC_DECL:
				{
					var instruction = new FuncDeclInsn (op, addr)
					{
						Name = ReadIdent (),
						Namespace = ReadIdent (),
						Package = ReadIdent (),
						HasBody = ReadBool (),
						EndAddr = Read (),
					};

					var numArgs = Read ();

					for (uint i = 0; i < numArgs; i++)
					{
						instruction.Arguments.Add (ReadIdent ());
					}

					return instruction;
				}

				case Opcodes.Ops.OP_CREATE_OBJECT:
				{
					var instruction = new CreateObjectInsn (op, addr)
					{
						ParentName = ReadIdent (),
						IsDataBlock = ReadBool (),
						FailJumpAddr = Read (),
					};

					return instruction;
				}

				case Opcodes.Ops.OP_ADD_OBJECT:
					return new AddObjectInsn (op, addr, ReadBool ());

				case Opcodes.Ops.OP_END_OBJECT:
					return new EndObjectInsn (op, addr, ReadBool ());

				case Opcodes.Ops.OP_JMPIFFNOT:
				case Opcodes.Ops.OP_JMPIFNOT:
				case Opcodes.Ops.OP_JMPIFF:
				case Opcodes.Ops.OP_JMPIF:
					return new JumpInsn (op, addr, Read (), JumpInsn.InsnType.Branch);

				case Opcodes.Ops.OP_JMPIFNOT_NP:
				case Opcodes.Ops.OP_JMPIF_NP:
					return new JumpInsn (op, addr, Read (), JumpInsn.InsnType.TernaryBranch);

				case Opcodes.Ops.OP_JMP:
					return new JumpInsn (op, addr, Read (), JumpInsn.InsnType.Unconditional);

				case Opcodes.Ops.OP_RETURN:
					return new ReturnInsn (op, addr);

				case Opcodes.Ops.OP_CMPEQ:
				case Opcodes.Ops.OP_CMPGR:
				case Opcodes.Ops.OP_CMPGE:
				case Opcodes.Ops.OP_CMPLT:
				case Opcodes.Ops.OP_CMPLE:
				case Opcodes.Ops.OP_CMPNE:
				case Opcodes.Ops.OP_XOR:
				case Opcodes.Ops.OP_MOD:
				case Opcodes.Ops.OP_BITAND:
				case Opcodes.Ops.OP_BITOR:
				case Opcodes.Ops.OP_SHR:
				case Opcodes.Ops.OP_SHL:
				case Opcodes.Ops.OP_AND:
				case Opcodes.Ops.OP_OR:
				case Opcodes.Ops.OP_ADD:
				case Opcodes.Ops.OP_SUB:
				case Opcodes.Ops.OP_MUL:
				case Opcodes.Ops.OP_DIV:
					return new BinaryInsn (op, addr);

				case Opcodes.Ops.OP_COMPARE_STR:
					return new StringCompareInsn (op, addr);

				case Opcodes.Ops.OP_NEG:
				case Opcodes.Ops.OP_NOT:
				case Opcodes.Ops.OP_NOTF:
				case Opcodes.Ops.OP_ONESCOMPLEMENT:
					return new UnaryInsn (op, addr);

				case Opcodes.Ops.OP_SETCURVAR:
				case Opcodes.Ops.OP_SETCURVAR_CREATE:
					return new SetCurVarInsn (op, addr, ReadIdent ());

				case Opcodes.Ops.OP_SETCURVAR_ARRAY:
				case Opcodes.Ops.OP_SETCURVAR_ARRAY_CREATE:
					return new SetCurVarArrayInsn (op, addr);

				case Opcodes.Ops.OP_LOADVAR_UINT:
				case Opcodes.Ops.OP_LOADVAR_FLT:
				case Opcodes.Ops.OP_LOADVAR_STR:
					return new LoadVarInsn (op, addr);

				case Opcodes.Ops.OP_SAVEVAR_UINT:
				case Opcodes.Ops.OP_SAVEVAR_FLT:
				case Opcodes.Ops.OP_SAVEVAR_STR:
					return new SaveVarInsn (op, addr);

				case Opcodes.Ops.OP_SETCUROBJECT:
					return new SetCurObjectInsn (op, addr, isNew: false);

				case Opcodes.Ops.OP_SETCUROBJECT_NEW:
					return new SetCurObjectInsn (op, addr, isNew: true);

				case Opcodes.Ops.OP_SETCURFIELD:
					return new SetCurFieldInsn (op, addr, ReadIdent ());

				case Opcodes.Ops.OP_SETCURFIELD_ARRAY:
					return new SetCurFieldArrayInsn (op, addr);

				case Opcodes.Ops.OP_LOADFIELD_UINT:
				case Opcodes.Ops.OP_LOADFIELD_FLT:
				case Opcodes.Ops.OP_LOADFIELD_STR:
					return new LoadFieldInsn (op, addr);

				case Opcodes.Ops.OP_SAVEFIELD_UINT:
				case Opcodes.Ops.OP_SAVEFIELD_FLT:
				case Opcodes.Ops.OP_SAVEFIELD_STR:
					return new SaveFieldInsn (op, addr);

				case Opcodes.Ops.OP_STR_TO_UINT:
				case Opcodes.Ops.OP_FLT_TO_UINT:
					return new ConvertToTypeInsn (op, addr, ConvertToTypeInsn.InsnType.UInt);

				case Opcodes.Ops.OP_STR_TO_FLT:
				case Opcodes.Ops.OP_UINT_TO_FLT:
					return new ConvertToTypeInsn (op, addr, ConvertToTypeInsn.InsnType.Float);

				case Opcodes.Ops.OP_FLT_TO_STR:
				case Opcodes.Ops.OP_UINT_TO_STR:
					return new ConvertToTypeInsn (op, addr, ConvertToTypeInsn.InsnType.String);

				case Opcodes.Ops.OP_STR_TO_NONE:
				case Opcodes.Ops.OP_FLT_TO_NONE:
				case Opcodes.Ops.OP_UINT_TO_NONE:
					return new ConvertToTypeInsn (op, addr, ConvertToTypeInsn.InsnType.None);

				case Opcodes.Ops.OP_LOADIMMED_UINT:
				case Opcodes.Ops.OP_LOADIMMED_FLT:
				case Opcodes.Ops.OP_TAG_TO_STR:
				case Opcodes.Ops.OP_LOADIMMED_STR:
				case Opcodes.Ops.OP_LOADIMMED_IDENT:
					// Strings and floats rely on knowing whether we're in a function, so we're
					// just going to read in the raw table index for now.
					return new LoadImmedInsn (op, addr, Read ());

				case Opcodes.Ops.OP_CALLFUNC:
				case Opcodes.Ops.OP_CALLFUNC_RESOLVE:
				{
					var instruction = new FuncCallInsn (op, addr)
					{
						Name = ReadIdent (),
						Namespace = ReadIdent (),
						CallType = Read (),
					};

					return instruction;
				}

				case Opcodes.Ops.OP_ADVANCE_STR:
					return new AdvanceStringInsn (op, addr);

				case Opcodes.Ops.OP_ADVANCE_STR_APPENDCHAR:
					return new AdvanceStringInsn (op, addr, AdvanceStringInsn.InsnType.Append, ReadChar ());

				case Opcodes.Ops.OP_ADVANCE_STR_COMMA:
					return new AdvanceStringInsn (op, addr, AdvanceStringInsn.InsnType.Comma);

				case Opcodes.Ops.OP_ADVANCE_STR_NUL:
					return new AdvanceStringInsn (op, addr, AdvanceStringInsn.InsnType.Null);

				case Opcodes.Ops.OP_REWIND_STR:
					return new RewindInsn (op, addr);

				case Opcodes.Ops.OP_TERMINATE_REWIND_STR:
					return new RewindInsn (op, addr, terminate: true);

				case Opcodes.Ops.OP_PUSH:
					return new PushInsn (op, addr, pushFrame: false);

				case Opcodes.Ops.OP_PUSH_FRAME:
					return new PushInsn (op, addr, pushFrame: true);

				case Opcodes.Ops.OP_BREAK:
					return new DebugBreakInsn (op, addr);

				case Opcodes.Ops.UNUSED1:
				case Opcodes.Ops.UNUSED2:
					return new UnusedInsn (op, addr);

				default:
					return null;
			}
		}

		protected void ProcessInstruction (Instruction instruction, Opcodes.Ops op, uint addr)
		{
			if (instruction == null)
			{
				throw new Exception ($"Invalid opcode {op} at {addr}");
			}

			if (instruction is JumpInsn)
			{
				ProcessJump ((JumpInsn) instruction);
			}
		}

		protected void ProcessJump (JumpInsn instruction)
		{
			if (!IsValidAddr (instruction.TargetAddr))
			{
				throw new Exception ($"Jump at {instruction.Addr} jumps to invalid address {instruction.TargetAddr}");
			}

			AddToQueue (instruction.TargetAddr);
		}

		protected void ConnectJumps ()
		{
			graph.PreorderDFS ((Instruction instruction, InstructionGraph graph) =>
			{
				if (instruction is JumpInsn)
				{
					graph.AddEdge (instruction.Addr, (instruction as JumpInsn).TargetAddr);
				}
			});
		}

		protected virtual void Reset ()
		{
			Pos = 0;
			queue.Clear ();
			visited.Clear ();
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

		protected string ReadIdent () => data.Identifer (Pos, Read ());
		protected bool ReadBool () => Read () != 0;
		protected char ReadChar () => (char) Read ();

		protected uint Peek () => Peek (Pos);
		protected uint Peek (uint addr) => data.Op (addr);
	}
}
