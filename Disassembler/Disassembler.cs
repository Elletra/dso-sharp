/**
 * Disassembler.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using DSO.Loader;
using DSO.Opcodes;

namespace DSO.Disassembler
{
	public class DisassemblerException : Exception
	{
		public DisassemblerException() { }
		public DisassemblerException(string message) : base(message) { }
		public DisassemblerException(string message, Exception inner) : base(message, inner) { }
	}

	public class Disassembler
	{
		private BytecodeReader _reader = new();

		public Disassembly Disassemble(FileData data, Ops ops)
		{
			_reader = new(data, ops);

			return Disassemble();
		}

		private Disassembly Disassemble()
		{
			var disassembly = new Disassembly();

			while (!_reader.IsAtEnd)
			{
				var instruction = _reader.ReadInstruction();

				ValidateInstruction(instruction);
				disassembly.AddInstruction(instruction);
			}

			return disassembly;
		}

		private void ValidateInstruction(Instruction instruction)
		{
			switch (instruction)
			{
				case FunctionInstruction function:
					if (function.HasBody && function.EndAddress >= _reader.CodeSize)
					{
						throw new DisassemblerException($"Function at {function.Address} has invalid end address {function.EndAddress}");
					}

					break;

				case BranchInstruction branch:
					if (_reader.InFunction)
					{
						var address = branch.Address;
						var target = branch.TargetAddress;

						if (target <= _reader.Function?.Address || target >= _reader.Function?.EndAddress)
						{
							throw new DisassemblerException($"Branch at {address} jumps out of function to {target}");
						}
					}

					break;

				default:
					break;
			}
		}
	}
}
