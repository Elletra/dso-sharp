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
		public Opcode Opcode { get; }
		public uint Addr { get; }

		public Instruction (Opcode opcode, uint addr)
		{
			Opcode = opcode;
			Addr = addr;
		}

		/// <summary>
		/// Mostly a utility function for <see cref="ToString"/>.
		/// </summary>
		/// <returns>An array of relevant values to be printed.</returns>
		public virtual object[] GetValues () => new object[] { Addr, Opcode.Value };

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

		public FunctionInstruction (Opcode opcode, uint addr, string name, string ns, string package,
			bool hasBody, uint endAddr) : base(opcode, addr)
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
			values[1] = Opcode.Value;
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

		public CreateObjectInstruction (Opcode opcode, uint addr, string parent, bool isDataBlock, uint failJumpAddr)
			: base(opcode, addr)
		{
			Parent = parent;
			IsDataBlock = isDataBlock;
			FailJumpAddr = failJumpAddr;
		}

		public override object[] GetValues () => new object[] { Addr, Opcode.Value, Parent, IsDataBlock, FailJumpAddr };
	}

	/// <summary>
	/// Instruction for the second part of object creation.
	/// </summary>
	public class AddObjectInstruction : Instruction
	{
		public bool PlaceAtRoot { get; }

		public AddObjectInstruction (Opcode opcode, uint addr, bool placeAtRoot) : base(opcode, addr)
		{
			PlaceAtRoot = placeAtRoot;
		}

		public override object[] GetValues () => new object[] { Addr, Opcode.Value, PlaceAtRoot };
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

		public EndObjectInstruction (Opcode opcode, uint addr, bool value) : base(opcode, addr)
		{
			Value = value;
		}

		public override object[] GetValues () => new object[] { Addr, Opcode.Value, Value };
	}

	/// <summary>
	/// Branch instruction, for loops, conditionals, breaks, continues, and logical AND/OR.
	/// </summary>
	public class BranchInstruction : Instruction
	{
		public uint TargetAddr { get; }

		public bool IsUnconditional => Opcode.StringValue == "OP_JMP";
		public bool IsConditional => !IsUnconditional;

		public BranchInstruction (Opcode opcode, uint addr, uint targetAddr) : base(opcode, addr)
		{
			TargetAddr = targetAddr;
		}

		public override object[] GetValues () => new object[] { Addr, Opcode.Value, TargetAddr };
	}

	/// <summary>
	/// Return instruction.
	/// </summary>
	public class ReturnInstruction : Instruction
	{
		public bool ReturnsValue { get; }

		public ReturnInstruction (Opcode opcode, uint addr, bool returnsValue) : base(opcode, addr)
		{
			ReturnsValue = returnsValue;
		}

		public override object[] GetValues () => new object[] { Addr, Opcode.Value, ReturnsValue };
	}

	/// <summary>
	/// Variable instruction for regular variables only.<br/><br/>
	///
	/// "Array"-indexing a variable is made up of multiple instructions and doesn't involve this one.
	/// </summary>
	public class VariableInstruction : Instruction
	{
		public string Name { get; }

		public VariableInstruction (Opcode opcode, uint addr, string name) : base(opcode, addr)
		{
			Name = name;
		}

		public override object[] GetValues () => new object[] { Addr, Opcode.Value, Name };
	}

	/// <summary>
	/// Field instruction<br/><br/>
	///
	/// Unlike variables, this instruction <em>is</em> used in "array"-indexing a field.
	/// </summary>
	public class FieldInstruction : Instruction
	{
		public string Name { get; }

		public FieldInstruction (Opcode opcode, uint addr, string name) : base(opcode, addr)
		{
			Name = name;
		}

		public override object[] GetValues () => new object[] { Addr, Opcode.Value, Name };
	}

	/// <summary>
	/// Type conversion instruction.<br/><br/>
	///
	/// Does not have any operands, but it still has extra information we want to know.
	/// </summary>
	public class ConvertToTypeInstruction : Instruction
	{
		public TypeReq Type => Opcode.TypeReq;

		public ConvertToTypeInstruction (Opcode opcode, uint addr) : base(opcode, addr) {}

		public override object[] GetValues () => new object[] { Addr, Opcode.Value, Type.ToString() };
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

		public bool IsTaggedString => Opcode.StringValue == "OP_TAG_TO_STR";
		public bool IsIdentifier => Opcode.StringValue == "OP_LOADIMMED_IDENT";

		public ImmediateInstruction (Opcode opcode, uint addr, T value) : base(opcode, addr)
		{
			Value = value;
		}

		public override object[] GetValues () => new object[]
		{
			Addr,
			Opcode.Value,
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

		public CallInstruction (Opcode opcode, uint addr, string name, string ns, uint callType) : base(opcode, addr)
		{
			Name = name;
			Namespace = ns;
			CallType = callType;
		}

		public override object[] GetValues () => new object[] { Addr, Opcode.Value, Name, Namespace, CallType };
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

		public AppendStringInstruction (Opcode opcode, uint addr, char ch) : base(opcode, addr)
		{
			Char = ch;
		}

		public override object[] GetValues () => new object[] { Addr, Opcode.Value, (uint) Char };
	}
}
