/**
 * BytecodeReader.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */


using DSO.Disassembler;
using DSO.Loader;
using DSO.Opcodes;

namespace DSO.Versions.Constructor
{
	public class BytecodeReader : Disassembler.BytecodeReader
	{
		public BytecodeReader(FileData data, Opcodes.Ops ops) : base(data, ops) { }

		protected override Instruction ReadInstruction(uint address, Opcode? opcode) => opcode?.Tag == OpcodeTag.OP_CREATE_OBJECT
			? new CreateObjectInstruction(opcode, address, this)
			: base.ReadInstruction(address, opcode);
	}
}
