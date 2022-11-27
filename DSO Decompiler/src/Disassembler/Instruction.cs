using System.Collections.Generic;

using DSODecompiler.Util;

namespace DSODecompiler.Disassembler
{
	public class Instruction : GraphNode<uint>
	{
		public Opcodes.Ops Op { get; }
		public uint Addr => Key;
		public int Label { get; set; } = -1;
		public bool HasLabel => Label >= 0;

		public Instruction (Opcodes.Ops op, uint addr) : base (addr)
		{
			Op = op;
		}
	}

	public class FuncDeclInsn : Instruction
	{
		public string Name { get; set; } = null;
		public string Namespace { get; set; } = null;
		public string Package { get; set; } = null;
		public bool HasBody { get; set; }
		public uint EndAddr { get; set; }

		public readonly List<string> Arguments = new List<string> ();

		public FuncDeclInsn (Opcodes.Ops op, uint addr) : base (op, addr) {}
	}

	public class CreateObjectInsn : Instruction
	{
		public string ParentName { get; set; } = null;
		public bool IsDataBlock { get; set; } = false;
		public uint FailJumpAddr { get; set; }

		public CreateObjectInsn (Opcodes.Ops op, uint addr) : base (op, addr) {}
	}

	public class AddObjectInsn : Instruction
	{
		public bool PlaceAtRoot { get; }

		public AddObjectInsn (Opcodes.Ops op, uint addr, bool placeAtRoot) : base (op, addr)
		{
			PlaceAtRoot = placeAtRoot;
		}
	}

	public class EndObjectInsn : Instruction
	{
		public bool Value { get; }

		public EndObjectInsn (Opcodes.Ops op, uint addr, bool value) : base (op, addr)
		{
			Value = value;
		}
	}

	public class JumpInsn : Instruction
	{
		public enum InsnType
		{
			Unconditional,
			Branch,
			TernaryBranch,
		}

		public uint TargetAddr { get; }

		public InsnType Type { get; }

		public JumpInsn (Opcodes.Ops op, uint addr, uint target, InsnType type) : base (op, addr)
		{
			TargetAddr = target;
			Type = type;
		}
	}

	public class ReturnInsn : Instruction
	{
		// FIXME: Uncomment
		//public bool ReturnsValue { get; }

		public ReturnInsn (Opcodes.Ops op, uint addr/*, bool returnsValue*/) : base (op, addr)
		{
			//ReturnsValue = returnsValue;
		}
	}

	public class BinaryInsn : Instruction
	{
		public BinaryInsn (Opcodes.Ops op, uint addr) : base (op, addr) {}
	}

	public class StringCompareInsn : Instruction
	{
		public StringCompareInsn (Opcodes.Ops op, uint addr) : base (op, addr) {}
	}

	public class UnaryInsn : Instruction
	{
		public UnaryInsn (Opcodes.Ops op, uint addr) : base (op, addr) {}
	}

	public class SetCurVarInsn : Instruction
	{
		public string Name { get; }

		public SetCurVarInsn (Opcodes.Ops op, uint addr, string name) : base (op, addr)
		{
			Name = name;
		}
	}

	public class SetCurVarArrayInsn : Instruction
	{
		public SetCurVarArrayInsn (Opcodes.Ops op, uint addr) : base (op, addr) {}
	}

	public class LoadVarInsn : Instruction
	{
		public LoadVarInsn (Opcodes.Ops op, uint addr) : base (op, addr) {}
	}

	public class SaveVarInsn : Instruction
	{
		public SaveVarInsn (Opcodes.Ops op, uint addr) : base (op, addr) {}
	}

	public class SetCurObjectInsn : Instruction
	{
		public bool IsNew { get; }

		public SetCurObjectInsn (Opcodes.Ops op, uint addr, bool isNew) : base (op, addr)
		{
			IsNew = isNew;
		}
	}

	public class SetCurFieldInsn : Instruction
	{
		public string Name { get; }

		public SetCurFieldInsn (Opcodes.Ops op, uint addr, string name) : base (op, addr)
		{
			Name = name;
		}
	}

	public class SetCurFieldArrayInsn : Instruction
	{
		public SetCurFieldArrayInsn (Opcodes.Ops op, uint addr) : base (op, addr) {}
	}

	public class LoadFieldInsn : Instruction
	{
		public LoadFieldInsn (Opcodes.Ops op, uint addr) : base (op, addr) {}
	}

	public class SaveFieldInsn : Instruction
	{
		public SaveFieldInsn (Opcodes.Ops op, uint addr) : base (op, addr) {}
	}

	public class ConvertToTypeInsn : Instruction
	{
		public enum InsnType
		{
			UInt,
			Float,
			String,
			None,
		}

		public InsnType Type { get; }

		public ConvertToTypeInsn (Opcodes.Ops op, uint addr, InsnType type) : base (op, addr)
		{
			Type = type;
		}
	}

	public class LoadImmedInsn : Instruction
	{
		// Strings and floats rely on knowing whether we're in a function, which we're not going
		// to do until later, so we just store the raw table index.
		public uint Value { get; }

		public LoadImmedInsn (Opcodes.Ops op, uint addr, uint value) : base (op, addr)
		{
			Value = value;
		}
	}

	public class FuncCallInsn : Instruction
	{
		public string Name { get; set; } = null;
		public string Namespace { get; set; } = null;
		public uint CallType { get; set; } = 0;

		public FuncCallInsn (Opcodes.Ops op, uint addr) : base (op, addr) {}
	}

	public class AdvanceStringInsn : Instruction
	{
		public enum InsnType
		{
			Default,
			Append,
			Comma,
			Null,
		}

		public InsnType Type { get; }
		public char Char { get; }

		public AdvanceStringInsn (Opcodes.Ops op, uint addr, InsnType type = InsnType.Default, char ch = '\0')
			: base (op, addr)
		{
			Type = type;
			Char = ch;
		}
	}

	public class RewindInsn : Instruction
	{
		public bool Terminate { get; }

		public RewindInsn (Opcodes.Ops op, uint addr, bool terminate = false) : base (op, addr)
		{
			Terminate = terminate;
		}
	}

	public class PushInsn : Instruction
	{
		public PushInsn (Opcodes.Ops op, uint addr) : base (op, addr) {}
	}

	public class PushFrameInsn : Instruction
	{
		public PushFrameInsn (Opcodes.Ops op, uint addr) : base (op, addr) {}
	}

	public class DebugBreakInsn : Instruction
	{
		public DebugBreakInsn (Opcodes.Ops op, uint addr) : base (op, addr) {}
	}

	public class UnusedInsn : Instruction
	{
		public UnusedInsn (Opcodes.Ops op, uint addr) : base (op, addr) {}
	}
}
