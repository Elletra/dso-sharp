using System.Collections.Generic;

using DSODecompiler.Opcodes;

namespace DSODecompiler.Disassembler
{
	public abstract class Instruction
	{
		public Opcode Opcode { get; }
		public uint Addr { get; }
		public uint NumBranchesTo { get; set; } = 0;
		public uint NumLoopsTo { get; set; } = 0;

		public bool IsBranchTarget => NumBranchesTo > 0;
		public bool IsLoopStart => NumLoopsTo > 0;

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
		public bool IsLoopEnd { get; set; } = false;

		public BranchInsn (Opcode op, uint addr, uint target) : base(op, addr)
		{
			TargetAddr = target;
		}

		public override string ToString ()
		{
			return $"[@{Addr}, {GetType().Name}, {TargetAddr}, {Opcode.BranchType}]";
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
		public SetCurObjectInsn (Opcode op, uint addr) : base(op, addr) {}
	}

	public class SetCurObjectNewInsn : Instruction
	{
		public SetCurObjectNewInsn (Opcode op, uint addr) : base(op, addr) { }
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
		public ConvertToTypeInsn (Opcode op, uint addr) : base(op, addr) {}

		public override string ToString ()
		{
			return $"[@{Addr}, {GetType().Name}, {Opcode.ConvertToType}]";
		}
	}

	public class LoadImmedInsn<T> : Instruction
	{
		public T Value { get; }

		public LoadImmedInsn (Opcode op, uint addr, T value) : base(op, addr)
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
		public char Char { get; }
		public bool HasChar { get; }

		public AdvanceStringInsn (Opcode op, uint addr, char ch)
			: base(op, addr)
		{
			Char = ch;
			HasChar = true;
		}

		public AdvanceStringInsn (Opcode op, uint addr)
			: base(op, addr)
		{
			Char = '\0';
			HasChar = false;
		}

		public override string ToString ()
		{
			return $"[@{Addr}, {GetType().Name}, {Opcode.AdvanceStringType}{(HasChar ? $", {(byte) Char}" : "")}]";
		}
	}

	public class RewindInsn : Instruction
	{
		public RewindInsn (Opcode op, uint addr) : base(op, addr) {}
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
