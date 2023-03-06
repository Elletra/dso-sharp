using System.Collections.Generic;

namespace DSODecompiler.Disassembler
{
	public class Instruction
	{
		public Opcode Opcode { get; }
		public uint Addr { get; }

		public Instruction Prev { get; set; } = null;
		public Instruction Next { get; set; } = null;

		public bool HasPrev => Prev != null;
		public bool HasNext => Next != null;

		public Instruction (Opcode op, uint addr)
		{
			Opcode = op;
			Addr = addr;
		}

		public override string ToString ()
		{
			return $"[@{Addr}, {GetType().Name}]";
		}
	}

	public class FuncDeclInsn : Instruction
	{
		public string Name { get; set; } = null;
		public string Namespace { get; set; } = null;
		public string Package { get; set; } = null;
		public bool HasBody { get; set; }
		public uint EndAddr { get; set; }

		public readonly List<string> Arguments = new();

		public FuncDeclInsn (Opcode op, uint addr) : base(op, addr) {}

		public override string ToString ()
		{
			var str = $"[@{Addr}, {GetType().Name}, \"{Name}\", \"{Namespace}\", \"{Package}\", {HasBody}, {EndAddr}";

			foreach (var arg in Arguments)
			{
				str += $", {arg}";
			}

			return $"{str}]";
		}
	}

	public class CreateObjectInsn : Instruction
	{
		public string ParentName { get; set; } = null;
		public bool IsDataBlock { get; set; } = false;
		public uint FailJumpAddr { get; set; }

		public CreateObjectInsn (Opcode op, uint addr) : base(op, addr) {}

		public override string ToString ()
		{
			return $"[@{Addr}, {GetType().Name}, \"{ParentName}\", {IsDataBlock}, {FailJumpAddr}]";
		}
	}

	public class AddObjectInsn : Instruction
	{
		public bool PlaceAtRoot { get; }

		public AddObjectInsn (Opcode op, uint addr, bool placeAtRoot) : base(op, addr)
		{
			PlaceAtRoot = placeAtRoot;
		}

		public override string ToString ()
		{
			return $"[@{Addr}, {GetType().Name}, {PlaceAtRoot}]";
		}
	}

	public class EndObjectInsn : Instruction
	{
		public bool Value { get; }

		public EndObjectInsn (Opcode op, uint addr, bool value) : base(op, addr)
		{
			Value = value;
		}

		public override string ToString ()
		{
			return $"[@{Addr}, {GetType().Name}, {Value}]";
		}
	}

	public class BranchInsn : Instruction
	{
		public uint TargetAddr { get; }
		public Opcode.BranchType Type { get; }

		public BranchInsn (Opcode op, uint addr, uint target, Opcode.BranchType type) : base(op, addr)
		{
			TargetAddr = target;
			Type = type;
		}

		public override string ToString ()
		{
			return $"[@{Addr}, {GetType().Name}, {TargetAddr}, {Type}]";
		}
	}

	public class ReturnInsn : Instruction
	{
		public bool ReturnsValue { get; }

		public ReturnInsn (Opcode op, uint addr, bool returnsValue) : base(op, addr)
		{
			ReturnsValue = returnsValue;
		}

		public override string ToString ()
		{
			return $"[@{Addr}, {GetType().Name}, {ReturnsValue}]";
		}
	}

	public class BinaryInsn : Instruction
	{
		public BinaryInsn (Opcode op, uint addr) : base(op, addr) {}
	}

	public class StringCompareInsn : Instruction
	{
		public StringCompareInsn (Opcode op, uint addr) : base(op, addr) {}
	}

	public class UnaryInsn : Instruction
	{
		public UnaryInsn (Opcode op, uint addr) : base(op, addr) {}
	}

	public class SetCurVarInsn : Instruction
	{
		public string Name { get; }

		public SetCurVarInsn (Opcode op, uint addr, string name) : base(op, addr)
		{
			Name = name;
		}

		public override string ToString ()
		{
			return $"[@{Addr}, {GetType().Name}, \"{Name}\"]";
		}
	}

	public class SetCurVarArrayInsn : Instruction
	{
		public SetCurVarArrayInsn (Opcode op, uint addr) : base(op, addr) {}
	}

	public class LoadVarInsn : Instruction
	{
		public LoadVarInsn (Opcode op, uint addr) : base(op, addr) {}
	}

	public class SaveVarInsn : Instruction
	{
		public SaveVarInsn (Opcode op, uint addr) : base(op, addr) {}
	}

	public class SetCurObjectInsn : Instruction
	{
		public bool IsNew { get; }

		public SetCurObjectInsn (Opcode op, uint addr, bool isNew) : base(op, addr)
		{
			IsNew = isNew;
		}

		public override string ToString ()
		{
			return $"[@{Addr}, {GetType().Name}, {IsNew}]";
		}
	}

	public class SetCurFieldInsn : Instruction
	{
		public string Name { get; }

		public SetCurFieldInsn (Opcode op, uint addr, string name) : base(op, addr)
		{
			Name = name;
		}

		public override string ToString ()
		{
			return $"[@{Addr}, {GetType().Name}, \"{Name}\"]";
		}
	}

	public class SetCurFieldArrayInsn : Instruction
	{
		public SetCurFieldArrayInsn (Opcode op, uint addr) : base(op, addr) {}
	}

	public class LoadFieldInsn : Instruction
	{
		public LoadFieldInsn (Opcode op, uint addr) : base(op, addr) {}
	}

	public class SaveFieldInsn : Instruction
	{
		public SaveFieldInsn (Opcode op, uint addr) : base(op, addr) {}
	}

	public class ConvertToTypeInsn : Instruction
	{
		public Opcode.ConvertToType Type { get; }

		public ConvertToTypeInsn (Opcode op, uint addr, Opcode.ConvertToType type) : base(op, addr)
		{
			Type = type;
		}

		public override string ToString ()
		{
			return $"[@{Addr}, {GetType().Name}, {Type}]";
		}
	}

	public class LoadImmedInsn : Instruction
	{
		// Strings and floats rely on knowing whether we're in a function, which we're not going
		// to do until later, so we just store the raw table index.
		public uint Value { get; }

		public LoadImmedInsn (Opcode op, uint addr, uint value) : base(op, addr)
		{
			Value = value;
		}

		public override string ToString ()
		{
			return $"[@{Addr}, {GetType().Name}, {Value}]";
		}
	}

	public class FuncCallInsn : Instruction
	{
		public string Name { get; set; } = null;
		public string Namespace { get; set; } = null;
		public uint CallType { get; set; } = 0;

		public FuncCallInsn (Opcode op, uint addr) : base(op, addr) {}

		public override string ToString ()
		{
			return $"[@{Addr}, {GetType().Name}, \"{Name}\", \"{Namespace}\", {CallType}]";
		}
	}

	public class AdvanceStringInsn : Instruction
	{
		public Opcode.AdvanceStringType Type { get; }
		public char Char { get; }
		public bool HasChar { get; }

		public AdvanceStringInsn (Opcode op, uint addr, Opcode.AdvanceStringType type, char ch)
			: base(op, addr)
		{
			Type = type;
			Char = ch;
			HasChar = true;
		}

		public AdvanceStringInsn (Opcode op, uint addr, Opcode.AdvanceStringType type = Opcode.AdvanceStringType.Default)
			: base(op, addr)
		{
			Type = type;
			Char = '\0';
			HasChar = false;
		}

		public override string ToString ()
		{
			return $"[@{Addr}, {GetType().Name}, {Type}{(HasChar ? $", {(byte) Char}" : "")}]";
		}
	}

	public class RewindInsn : Instruction
	{
		public bool Terminate { get; }

		public RewindInsn (Opcode op, uint addr, bool terminate = false) : base(op, addr)
		{
			Terminate = terminate;
		}

		public override string ToString ()
		{
			return $"[@{Addr}, {GetType().Name}, {Terminate}]";
		}
	}

	public class PushInsn : Instruction
	{
		public PushInsn (Opcode op, uint addr) : base(op, addr) {}
	}

	public class PushFrameInsn : Instruction
	{
		public PushFrameInsn (Opcode op, uint addr) : base(op, addr) {}
	}

	public class DebugBreakInsn : Instruction
	{
		public DebugBreakInsn (Opcode op, uint addr) : base(op, addr) {}
	}

	public class UnusedInsn : Instruction
	{
		public UnusedInsn (Opcode op, uint addr) : base(op, addr) {}
	}
}
