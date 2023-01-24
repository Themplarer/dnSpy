/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.Decompiler.IL;

sealed class ModifiedInstructionBytesReader : IInstructionBytesReader
{
    private readonly ITokenResolver _resolver;
    private readonly List<short> _instrBytes = new(10);
    private readonly IList<Instruction> _instructions;
    private int _instrIndex;
    private int _byteIndex;

    public bool IsOriginalBytes => false;

    public ModifiedInstructionBytesReader(MethodDef method)
    {
        _resolver = method.Module;
        _instructions = method.Body.Instructions;
    }

    public int ReadByte()
    {
        if (_byteIndex >= _instrBytes.Count)
            InitializeNextInstruction();

        return _instrBytes[_byteIndex++];
    }

    void InitializeNextInstruction()
    {
        if (_instrIndex >= _instructions.Count)
            throw new InvalidOperationException();

        var instr = _instructions[_instrIndex++];
        _byteIndex = 0;
        _instrBytes.Clear();

        InstructionUtils.AddOpCode(_instrBytes, instr.OpCode.Code);
        InstructionUtils.AddOperand(_instrBytes, _resolver, instr.Offset + (uint)instr.OpCode.Size, instr.OpCode, instr.Operand);
    }

    public void SetInstruction(int index, uint offset)
    {
        _instrIndex = index;
        InitializeNextInstruction();
    }
}