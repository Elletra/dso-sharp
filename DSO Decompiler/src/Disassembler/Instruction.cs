using System.Collections.Generic;

using DSODecompiler.Opcodes;

namespace DSODecompiler.Disassembler
{
	public abstract class Instruction
	{
		public Opcode Opcode { get; }
		public uint Addr { get; }

		public Instruction (Opcode opcode, uint addr)
		{
			Opcode = opcode;
			Addr = addr;
		}

		public override string ToString () => $"{GetType().Name}({Addr}, {Opcode})";
	}

	/// <summary>
	/// An instruction that does not have any arguments.
	/// </summary>
	public abstract class SimpleInstruction : Instruction
	{
		public SimpleInstruction (Opcode opcode, uint addr) : base (opcode, addr) {}
	}

	public class FunctionInstruction : Instruction
	{
		public string Name { get; } = null;
		public string Namespace { get; } = null;
		public string Package { get; } = null;
		public bool HasBody { get; }
		public uint EndAddr { get; }

		public List<string> Arguments { get; } = new();

		public FunctionInstruction (Opcode opcode, uint addr, string name, string ns, string package, bool hasBody, uint endAddr)
			: base (opcode, addr)
		{
			Name = name;
			Namespace = ns;
			Package = package;
			HasBody = hasBody;
			EndAddr = endAddr;
		}

		public override string ToString ()
		{
			/* The proper, more efficient way would probably be to use a StringBuilder, but this
			   works well enough... */
			var str = $"{GetType().Name}({Addr}, {Opcode}, {Name}, {Namespace}, {Package}, {HasBody}, {EndAddr}";

			foreach (var arg in Arguments)
			{
				str += $", {arg}";
			}

			return $"{str})";
		}
	}

	public class CreateObjectInstruction : Instruction
	{
		public string ParentName { get; } = null;
		public bool IsDataBlock { get; }
		public uint FailJumpAddr { get; }

		public CreateObjectInstruction (Opcode opcode, uint addr, string parent, bool isDataBlock, uint failJumpAddr)
			: base (opcode, addr)
		{
			ParentName = parent;
			IsDataBlock = isDataBlock;
			FailJumpAddr = failJumpAddr;
		}

		public override string ToString () => $"{GetType().Name}({Addr}, {Opcode}, {ParentName}, {IsDataBlock}, {FailJumpAddr})";
	}

	public class AddObjectInstruction : Instruction
	{
		public bool PlaceAtRoot { get; }

		public AddObjectInstruction (Opcode opcode, uint addr, bool placeAtRoot) : base (opcode, addr)
		{
			PlaceAtRoot = placeAtRoot;
		}

		public override string ToString () => $"{GetType().Name}({Addr}, {Opcode}, {PlaceAtRoot})";
	}

	public class EndObjectInstruction : Instruction
	{
		// Can either be isDataBlock or placeAtRoot.
		public bool Value { get; }

		public EndObjectInstruction (Opcode opcode, uint addr, bool value) : base (opcode, addr)
		{
			Value = value;
		}

		public override string ToString () => $"{GetType().Name}({Addr}, {Opcode}, {Value})";
	}

	public class BranchInstruction : Instruction
	{
		public uint TargetAddr { get; }

		public bool IsUnconditional => Opcode.Op == Opcode.Value.OP_JMP;
		public bool IsConditional => !IsUnconditional;

		public BranchInstruction (Opcode opcode, uint addr, uint targetAddr) : base (opcode, addr)
		{
			TargetAddr = targetAddr;
		}

		public override string ToString () => $"{GetType().Name}({Addr}, {Opcode}, {TargetAddr})";
	}

	public class ReturnInstruction : SimpleInstruction
	{
		public bool ReturnsValue { get; }

		public ReturnInstruction (Opcode opcode, uint addr, bool returnsValue) : base (opcode, addr)
		{
			ReturnsValue = returnsValue;
		}

		public override string ToString () => $"{GetType().Name}({Addr}, {Opcode}, {ReturnsValue})";
	}

	public class BinaryInstruction : SimpleInstruction
	{
		public BinaryInstruction (Opcode opcode, uint addr) : base(opcode, addr) {}
	}

	public class BinaryStringInstruction : SimpleInstruction
	{
		public BinaryStringInstruction (Opcode opcode, uint addr) : base(opcode, addr) {}
	}

	public class UnaryInstruction : SimpleInstruction
	{
		public UnaryInstruction (Opcode opcode, uint addr) : base(opcode, addr) {}
	}

	public class VariableInstruction : Instruction
	{
		public string Name { get; }

		public VariableInstruction (Opcode opcode, uint addr, string name) : base (opcode, addr)
		{
			Name = name;
		}

		public override string ToString () => $"{GetType().Name}({Addr}, {Opcode}, {Name})";
	}

	public class VariableArrayInstruction : SimpleInstruction
	{
		public VariableArrayInstruction (Opcode opcode, uint addr) : base(opcode, addr) {}
	}

	public class LoadVariableInstruction : SimpleInstruction
	{
		public LoadVariableInstruction (Opcode opcode, uint addr) : base (opcode, addr) {}
	}

	public class SaveVariableInstruction : SimpleInstruction
	{
		public SaveVariableInstruction (Opcode opcode, uint addr) : base(opcode, addr) {}
	}

	public class ObjectInstruction : SimpleInstruction
	{
		public ObjectInstruction (Opcode opcode, uint addr) : base(opcode, addr) {}
	}

	public class ObjectNewInstruction : SimpleInstruction
	{
		public ObjectNewInstruction (Opcode opcode, uint addr) : base(opcode, addr) {}
	}

	public class FieldInstruction : Instruction
	{
		public string Name { get; }

		public FieldInstruction (Opcode opcode, uint addr, string name) : base (opcode, addr)
		{
			Name = name;
		}

		public override string ToString () => $"{GetType().Name}({Addr}, {Opcode}, {Name})";
	}

	public class FieldArrayInstruction : SimpleInstruction
	{
		public FieldArrayInstruction (Opcode opcode, uint addr) : base(opcode, addr) {}
	}

	public class LoadFieldInstruction : SimpleInstruction
	{
		public LoadFieldInstruction (Opcode opcode, uint addr) : base(opcode, addr) {}
	}

	public class SaveFieldInstruction : SimpleInstruction
	{
		public SaveFieldInstruction (Opcode opcode, uint addr) : base(opcode, addr) {}
	}

	public class ConvertToTypeInstruction : SimpleInstruction
	{
		public TypeReq Type => Opcode.TypeReq;

		public ConvertToTypeInstruction (Opcode opcode, uint addr) : base (opcode, addr) { }

		public override string ToString () => $"{GetType().Name}({Addr}, {Opcode}, {Type})";
	}

	public class ImmediateInstruction<T> : Instruction
	{
		public T Value { get; }

		public ImmediateInstruction (Opcode opcode, uint addr, T value) : base (opcode, addr)
		{
			Value = value;
		}

		public override string ToString ()
		{
			var str = $"{GetType().Name}({Addr}, {Opcode}, ";

			if (typeof(T) == typeof(string))
			{
				str += $"\"{Value}\"";
			}
			else
			{
				str += Value;
			}

			return $"{str})";
		}
	}

	public class CallInstruction : Instruction
	{
		public enum Type : byte
		{
			FunctionCall,
			MethodCall,
			ParentCall,
		}

		public string Name { get; } = null;
		public string Namespace { get; } = null;
		public Type CallType { get; }

		public CallInstruction (Opcode opcode, uint addr, string name, string ns, uint callType)
			: base (opcode, addr)
		{
			Name = name;
			Namespace = ns;
			CallType = (Type) callType;
		}

		public override string ToString () => $"{GetType().Name}({Addr}, {Opcode}, {Name}, {Namespace}, {CallType})";
	}

	public class StringInstruction : SimpleInstruction
	{
		public StringInstruction (Opcode opcode, uint addr) : base(opcode, addr) {}
	}

	public class AppendStringInstruction : Instruction
	{
		public char Char { get; }

		public AppendStringInstruction (Opcode opcode, uint addr, char ch) : base(opcode, addr)
		{
			Char = ch;
		}

		public override string ToString () => $"{GetType().Name}({Addr}, {Opcode}, {(uint) Char})";
	}

	public class NullStringInstruction : SimpleInstruction
	{
		public NullStringInstruction (Opcode opcode, uint addr) : base(opcode, addr) {}
	}

	public class CommaStringInstruction : SimpleInstruction
	{
		public CommaStringInstruction (Opcode opcode, uint addr) : base(opcode, addr) {}
	}

	public class RewindInstruction : SimpleInstruction
	{
		public RewindInstruction (Opcode opcode, uint addr) : base(opcode, addr) {}
	}

	public class TerminateRewindInstruction : SimpleInstruction
	{
		public TerminateRewindInstruction (Opcode opcode, uint addr) : base(opcode, addr) {}
	}

	public class PushInstruction : SimpleInstruction
	{
		public PushInstruction (Opcode opcode, uint addr) : base(opcode, addr) {}
	}

	public class PushFrameInstruction : SimpleInstruction
	{
		public PushFrameInstruction (Opcode opcode, uint addr) : base(opcode, addr) {}
	}

	public class DebugBreakInstruction : SimpleInstruction
	{
		public DebugBreakInstruction (Opcode opcode, uint addr) : base(opcode, addr) {}
	}

	/// <summary>
	/// Some games (e.g. Blockland) have weird, unused "filler" instructions that don't do anything.
	/// </summary>
	public class UnusedInstruction : SimpleInstruction
	{
		public UnusedInstruction (Opcode opcode, uint addr) : base(opcode, addr) {}
	}
}
