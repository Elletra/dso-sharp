using System.Collections.Generic;

using DSODecompiler.Opcodes;

namespace DSODecompiler.Disassembly
{
	/// <summary>
	/// Base instruction class.<br/><br/>
	///
	/// Used both as a base class and for instructions that don't have any operands or special properties.
	/// </summary>
	public class Instruction
	{
		public Op Op { get; }
		public uint Addr { get; }

		public Instruction (Op op, uint addr)
		{
			Op = op;
			Addr = addr;
		}

		/// <summary>
		/// Mostly a utility function for <see cref="ToString"/>.
		/// </summary>
		/// <returns>An array of relevant values to be printed.</returns>
		public virtual object[] GetValues () => new object[] { Addr, Op.Opcode };

		public override string ToString ()
		{
			/* The proper, more efficient way would probably be to use a StringBuilder, but this
			   works well enough... */

			var str = $"{GetType().Name}";
			var values = GetValues();

			if (values.Length > 0)
			{
				str += "(";

				for (var i = 0; i < values.Length; i++)
				{
					str += values[i];

					if (i < values.Length - 1)
					{
						str += ", ";
					}
				}

				str += ")";
			}

			return str;
		}
	}

	/// <summary>
	/// Function declaration instruction.
	/// </summary>
	public class FunctionInstruction : Instruction
	{
		public string Name { get; } = null;
		public string Namespace { get; } = null;
		public string Package { get; } = null;
		public bool HasBody { get; }

		/// <summary>
		/// Ignore if <see cref="HasBody"/> is <see langword="false"/>.
		/// </summary>
		public uint EndAddr { get; }

		public List<string> Arguments { get; } = new();

		public FunctionInstruction (Op op, uint addr, string name, string ns, string package,
			bool hasBody, uint endAddr) : base(op, addr)
		{
			Name = name;
			Namespace = ns;
			Package = package;
			HasBody = hasBody;
			EndAddr = endAddr;
		}

		public override object[] GetValues ()
		{
			var values = new object[7 + Arguments.Count];

			values[0] = Addr;
			values[1] = Op.Opcode;
			values[2] = Name;
			values[3] = Namespace;
			values[4] = Package;
			values[5] = HasBody;
			values[6] = EndAddr;

			var args = Arguments.Count;

			for (var i = 0; i < args; i++)
			{
				values[i + 7] = Arguments[i];
			}

			return values;
		}
	}

	/// <summary>
	/// Instruction for the first part of object creation.
	/// </summary>
	public class CreateObjectInstruction : Instruction
	{
		public string Parent { get; } = null;
		public bool IsDataBlock { get; }
		public uint FailJumpAddr { get; }

		public CreateObjectInstruction (Op op, uint addr, string parent, bool isDataBlock, uint failJumpAddr)
			: base(op, addr)
		{
			Parent = parent;
			IsDataBlock = isDataBlock;
			FailJumpAddr = failJumpAddr;
		}

		public override object[] GetValues () => new object[] { Addr, Op.Opcode, Parent, IsDataBlock, FailJumpAddr };
	}

	/// <summary>
	/// Instruction for the second part of object creation.
	/// </summary>
	public class AddObjectInstruction : Instruction
	{
		public bool PlaceAtRoot { get; }

		public AddObjectInstruction (Op op, uint addr, bool placeAtRoot) : base(op, addr)
		{
			PlaceAtRoot = placeAtRoot;
		}

		public override object[] GetValues () => new object[] { Addr, Op.Opcode, PlaceAtRoot };
	}

	/// <summary>
	/// Instruction for the third and final part of object creation.
	/// </summary>
	public class EndObjectInstruction : Instruction
	{
		/// <summary>
		/// Can either be for <c>isDataBlock</c> or <c>placeAtRoot</c>.
		/// </summary>
		public bool Value { get; }

		public EndObjectInstruction (Op op, uint addr, bool value) : base(op, addr)
		{
			Value = value;
		}

		public override object[] GetValues () => new object[] { Addr, Op.Opcode, Value };
	}

	/// <summary>
	/// Branch instruction, for loops, conditionals, breaks, continues, and logical AND/OR.
	/// </summary>
	public class BranchInstruction : Instruction
	{
		public uint TargetAddr { get; }

		public bool IsUnconditional => Op.Opcode == Opcode.OP_JMP;
		public bool IsConditional => !IsUnconditional;

		public BranchInstruction (Op op, uint addr, uint targetAddr) : base(op, addr)
		{
			TargetAddr = targetAddr;
		}

		public override object[] GetValues () => new object[] { Addr, Op.Opcode, TargetAddr };
	}

	/// <summary>
	/// Return instruction.
	/// </summary>
	public class ReturnInstruction : Instruction
	{
		public bool ReturnsValue { get; }

		public ReturnInstruction (Op op, uint addr, bool returnsValue) : base(op, addr)
		{
			ReturnsValue = returnsValue;
		}

		public override object[] GetValues () => new object[] { Addr, Op.Opcode, ReturnsValue };
	}

	/// <summary>
	/// Variable instruction for regular variables only.<br/><br/>
	///
	/// "Array"-indexing a variable is made up of multiple instructions and doesn't involve this one.
	/// </summary>
	public class VariableInstruction : Instruction
	{
		public string Name { get; }

		public VariableInstruction (Op op, uint addr, string name) : base(op, addr)
		{
			Name = name;
		}

		public override object[] GetValues () => new object[] { Addr, Op.Opcode, Name };
	}

	/// <summary>
	/// Field instruction<br/><br/>
	///
	/// Unlike variables, this instruction <em>is</em> used in "array"-indexing a field.
	/// </summary>
	public class FieldInstruction : Instruction
	{
		public string Name { get; }

		public FieldInstruction (Op op, uint addr, string name) : base(op, addr)
		{
			Name = name;
		}

		public override object[] GetValues () => new object[] { Addr, Op.Opcode, Name };
	}

	/// <summary>
	/// Type conversion instruction.<br/><br/>
	///
	/// Does not have any operands, but it still has extra information we want to know.
	/// </summary>
	public class ConvertToTypeInstruction : Instruction
	{
		public TypeReq Type => Op.TypeReq;

		public ConvertToTypeInstruction (Op op, uint addr) : base(op, addr) {}

		public override object[] GetValues () => new object[] { Addr, Op.Opcode, Type.ToString() };
	}

	/// <summary>
	/// Immediate instruction for integers, floats, strings, tagged strings, and identifiers.<br/><br/>
	///
	/// "Identifier" is a term that Torque uses to refer to strings that aren't surrounded by quotes...
	/// TorqueScript is a very odd language.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ImmediateInstruction<T> : Instruction
	{
		public T Value { get; }

		public bool IsTaggedString => Op.Opcode == Opcode.OP_TAG_TO_STR;
		public bool IsIdentifier => Op.Opcode == Opcode.OP_LOADIMMED_IDENT;

		public ImmediateInstruction (Op op, uint addr, T value) : base(op, addr)
		{
			Value = value;
		}

		public override object[] GetValues () => new object[]
		{
			Addr,
			Op.Opcode,
			typeof(T) == typeof(string) ? $"\"{Value}\"" : Value,
		};
	}

	/// <summary>
	/// Function call instruction.
	/// </summary>
	public class CallInstruction : Instruction
	{
		public string Name { get; } = null;
		public string Namespace { get; } = null;
		public uint CallType { get; }

		public CallInstruction (Op op, uint addr, string name, string ns, uint callType) : base(op, addr)
		{
			Name = name;
			Namespace = ns;
			CallType = callType;
		}

		public override object[] GetValues () => new object[] { Addr, Op.Opcode, Name, Namespace, CallType };
	}

	/// <summary>
	/// Advance-string instruction with appended character (used for SPC, TAB, and NL keywords).<br/><br/>
	///
	/// The other advance-string instructions don't get their own class because they don't have any
	/// operands or special properties.
	/// </summary>
	public class AppendStringInstruction : Instruction
	{
		public char Char { get; }

		public AppendStringInstruction (Op op, uint addr, char ch) : base(op, addr)
		{
			Char = ch;
		}

		public override object[] GetValues () => new object[] { Addr, Op.Opcode, (uint) Char };
	}
}
