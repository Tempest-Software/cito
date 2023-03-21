// GenSwift.cs - Swift code generator
//
// Copyright (C) 2020-2023  Piotr Fusik
//
// This file is part of CiTo, see https://github.com/pfusik/cito
//
// CiTo is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// CiTo is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with CiTo.  If not, see http://www.gnu.org/licenses/

using System;
using System.Collections.Generic;
using System.Linq;

namespace Foxoft.Ci
{

public class GenSwift : GenPySwift
{
	CiSystem System;
	bool Throw;
	bool ArrayRef;
	bool StringCharAt;
	bool StringIndexOf;
	bool StringSubstring;
	readonly List<HashSet<string>> VarsAtIndent = new List<HashSet<string>>();
	readonly List<bool> VarBytesAtIndent = new List<bool>();

	protected override string GetTargetName() => "Swift";

	protected override void StartDocLine()
	{
		Write("/// ");
	}

	protected override string GetDocBullet() => "/// * ";

	protected override void WriteDoc(CiCodeDoc doc)
	{
		if (doc != null)
			WriteContent(doc);
	}

	void WriteCamelCaseNotKeyword(string name)
	{
		switch (name) {
		case "this":
			Write("self");
			break;
		case "As":
		case "Associatedtype":
		case "Await":
		case "Break":
		case "Case":
		case "Catch":
		case "Class":
		case "Continue":
		case "Default":
		case "Defer":
		case "Deinit":
		case "Do":
		case "Else":
		case "Enum":
		case "Extension":
		case "Fallthrough":
		case "False":
		case "Fileprivate":
		case "For":
		case "Foreach":
		case "Func":
		case "Guard":
		case "If":
		case "Import":
		case "In":
		case "Init":
		case "Inout":
		case "Int":
		case "Internal":
		case "Is":
		case "Let":
		case "Nil":
		case "Operator":
		case "Private":
		case "Protocol":
		case "Public":
		case "Repeat":
		case "Rethrows":
		case "Return":
		case "Self":
		case "Static":
		case "Struct":
		case "Switch":
		case "Subscript":
		case "Super":
		case "Throw":
		case "Throws":
		case "True":
		case "Try":
		case "Typealias":
		case "Var":
		case "Void":
		case "Where":
		case "While":
		case "as":
		case "associatedtype":
		case "await":
		case "catch":
		case "defer":
		case "deinit":
		case "extension":
		case "fallthrough":
		case "fileprivate":
		case "func":
		case "guard":
		case "import":
		case "init":
		case "inout":
		case "is":
		case "let":
		case "nil":
		case "operator":
		case "private":
		case "protocol":
		case "repeat":
		case "rethrows":
		case "self":
		case "struct":
		case "subscript":
		case "super":
		case "try":
		case "typealias":
		case "var":
		case "where":
			WriteCamelCase(name);
			WriteChar('_');
			break;
		default:
			WriteCamelCase(name);
			break;
		}
	}

	protected override void WriteName(CiSymbol symbol)
	{
		switch (symbol) {
		case CiContainerType _:
			Write(symbol.Name);
			break;
		case CiConst konst when konst.InMethod != null:
			WriteCamelCase(konst.InMethod.Name);
			WritePascalCase(symbol.Name);
			break;
		case CiVar _:
		case CiMember _:
			WriteCamelCaseNotKeyword(symbol.Name);
			break;
		default:
			throw new NotImplementedException(symbol.GetType().Name);
		}
	}

	protected override void WriteLocalName(CiSymbol symbol, CiPriority parent)
	{
		if (symbol.Parent is CiForeach forEach && forEach.Collection.Type is CiStringType) {
			Write("Int(");
			WriteCamelCaseNotKeyword(symbol.Name);
			Write(".value)");
		}
		else
			base.WriteLocalName(symbol, parent);
	}

	protected override void WriteMemberOp(CiExpr left, CiSymbolReference symbol)
	{
		if (left.Type != null && left.Type.Nullable)
			WriteChar('!');
		WriteChar('.');
	}

	void OpenIndexing(CiExpr collection)
	{
		collection.Accept(this, CiPriority.Primary);
		if (collection.Type.Nullable)
			WriteChar('!');
		WriteChar('[');
	}

	static bool IsArrayRef(CiArrayStorageType array) => array.PtrTaken || array.GetElementType() is CiStorageType;

	void WriteClassName(CiClassType klass)
	{
		switch (klass.Class.Id) {
		case CiId.StringClass:
			Write("String");
			break;
		case CiId.ArrayPtrClass:
			this.ArrayRef = true;
			Write("ArrayRef<");
			WriteType(klass.GetElementType());
			WriteChar('>');
			break;
		case CiId.ListClass:
		case CiId.QueueClass:
		case CiId.StackClass:
			WriteChar('[');
			WriteType(klass.GetElementType());
			WriteChar(']');
			break;
		case CiId.HashSetClass:
		case CiId.SortedSetClass:
			Write("Set<");
			WriteType(klass.GetElementType());
			WriteChar('>');
			break;
		case CiId.DictionaryClass:
		case CiId.SortedDictionaryClass:
			WriteChar('[');
			WriteType(klass.GetKeyType());
			Write(": ");
			WriteType(klass.GetValueType());
			WriteChar(']');
			break;
		case CiId.OrderedDictionaryClass:
			NotSupported(klass, "OrderedDictionary");
			break;
		case CiId.LockClass:
			Include("Foundation");
			Write("NSRecursiveLock");
			break;
		default:
			Write(klass.Class.Name);
			break;
		}
	}

	void WriteType(CiType type)
	{
		switch (type) {
		case CiNumericType _:
			switch (type.Id) {
			case CiId.SByteRange:
				Write("Int8");
				break;
			case CiId.ByteRange:
				Write("UInt8");
				break;
			case CiId.ShortRange:
				Write("Int16");
				break;
			case CiId.UShortRange:
				Write("UInt16");
				break;
			case CiId.IntType:
				Write("Int");
				break;
			case CiId.LongType:
				Write("Int64");
				break;
			case CiId.FloatType:
				Write("Float");
				break;
			case CiId.DoubleType:
				Write("Double");
				break;
			default:
				throw new NotImplementedException(type.ToString());
			}
			break;
		case CiEnum _:
			Write(type.Id == CiId.BoolType ? "Bool" : type.Name);
			break;
		case CiArrayStorageType arrayStg:
			if (IsArrayRef(arrayStg)) {
				this.ArrayRef = true;
				Write("ArrayRef<");
				WriteType(arrayStg.GetElementType());
				WriteChar('>');
			}
			else {
				WriteChar('[');
				WriteType(arrayStg.GetElementType());
				WriteChar(']');
			}
			break;
		case CiClassType klass:
			WriteClassName(klass);
			if (klass.Nullable)
				WriteChar('?');
			break;
		default:
			Write(type.Name);
			break;
		}
	}

	protected override void WriteTypeAndName(CiNamedValue value)
	{
		WriteName(value);
		if (!value.Type.IsFinal() || value.IsAssignableStorage()) {
			Write(" : ");
			WriteType(value.Type);
		}
	}

	public override void VisitLiteralNull() => Write("nil");

	void WriteUnwrapped(CiExpr expr, CiPriority parent, bool substringOk)
	{
		if (expr.Type.Nullable) {
			expr.Accept(this, CiPriority.Primary);
			WriteChar('!');
		}
		else if (!substringOk && expr is CiCallExpr call && call.Method.Symbol.Id == CiId.StringSubstring)
			WriteCall("String", expr);
		else
			expr.Accept(this, parent);
	}

	public override void VisitInterpolatedString(CiInterpolatedString expr, CiPriority parent)
	{
		if (expr.Parts.Any(part => part.WidthExpr != null || part.Format != ' ' || part.Precision >= 0)) {
			Include("Foundation");
			Write("String(format: ");
			WritePrintf(expr, false);
		}
		else {
			WriteChar('"');
			foreach (CiInterpolatedPart part in expr.Parts) {
				Write(part.Prefix);
				Write("\\(");
				WriteUnwrapped(part.Argument, CiPriority.Argument, true);
				WriteChar(')');
			}
			Write(expr.Suffix);
			WriteChar('"');
		}
	}

	protected override void WriteCoercedInternal(CiType type, CiExpr expr, CiPriority parent)
	{
		if (type is CiNumericType && !(expr is CiLiteral)
		 && GetTypeId(type, false) != GetTypeId(expr.Type, expr is CiBinaryExpr binary && binary.Op != CiToken.LeftBracket)) {
			WriteType(type);
			WriteChar('(');
			if (type is CiIntegerType && expr is CiCallExpr call && call.Method.Symbol.Id == CiId.MathTruncate)
				call.Arguments[0].Accept(this, CiPriority.Argument);
			else
				expr.Accept(this, CiPriority.Argument);
			WriteChar(')');
		}
		else if (!type.Nullable)
			WriteUnwrapped(expr, parent, false);
		else
			expr.Accept(this, parent);
	}

	protected override void WriteStringLength(CiExpr expr)
	{
		WriteUnwrapped(expr, CiPriority.Primary, true);
		Write(".count");
	}

	protected override void WriteCharAt(CiBinaryExpr expr)
	{
		this.StringCharAt = true;
		Write("ciStringCharAt(");
		WriteUnwrapped(expr.Left, CiPriority.Argument, false);
		Write(", ");
		expr.Right.Accept(this, CiPriority.Argument);
		WriteChar(')');
	}

	public override void VisitSymbolReference(CiSymbolReference expr, CiPriority parent)
	{
		switch (expr.Symbol.Id) {
		case CiId.MathNaN:
			Write("Float.nan");
			break;
		case CiId.MathNegativeInfinity:
			Write("-Float.infinity");
			break;
		case CiId.MathPositiveInfinity:
			Write("Float.infinity");
			break;
		default:
			base.VisitSymbolReference(expr, parent);
			break;
		}
	}

	protected override string GetReferenceEqOp(bool not) => not ? " !== " : " === ";

	void WriteStringContains(CiExpr obj, string name, List<CiExpr> args)
	{
		WriteUnwrapped(obj, CiPriority.Primary, true);
		WriteChar('.');
		Write(name);
		WriteChar('(');
		WriteUnwrapped(args[0], CiPriority.Argument, true);
		WriteChar(')');
	}

	void WriteRange(CiExpr startIndex, CiExpr length)
	{
		WriteCoerced(this.System.IntType, startIndex, CiPriority.Shift);
		Write("..<");
		WriteAdd(startIndex, length); // TODO: side effect
	}

	protected override void WriteCallExpr(CiExpr obj, CiMethod method, List<CiExpr> args, CiPriority parent)
	{
		switch (method.Id) {
		case CiId.None:
		case CiId.ListContains:
		case CiId.ListSortAll:
		case CiId.HashSetContains:
		case CiId.HashSetRemove:
		case CiId.SortedSetContains:
		case CiId.SortedSetRemove:
			if (obj == null) {
				if (method.IsStatic()) {
					WriteName(this.CurrentMethod.Parent);
					WriteChar('.');
				}
			}
			else if (IsReferenceTo(obj, CiId.BasePtr))
				Write("super.");
			else {
				obj.Accept(this, CiPriority.Primary);
				WriteMemberOp(obj, null);
			}
			WriteName(method);
			WriteArgsInParentheses(method, args);
			break;
		case CiId.ClassToString:
			obj.Accept(this, CiPriority.Primary);
			WriteMemberOp(obj, null);
			Write("description");
			break;
		case CiId.StringContains:
			WriteStringContains(obj, "contains", args);
			break;
		case CiId.StringEndsWith:
			WriteStringContains(obj, "hasSuffix", args);
			break;
		case CiId.StringIndexOf:
			Include("Foundation");
			this.StringIndexOf = true;
			Write("ciStringIndexOf(");
			WriteUnwrapped(obj, CiPriority.Argument, true);
			Write(", ");
			WriteUnwrapped(args[0], CiPriority.Argument, true);
			WriteChar(')');
			break;
		case CiId.StringLastIndexOf:
			Include("Foundation");
			this.StringIndexOf = true;
			Write("ciStringIndexOf(");
			WriteUnwrapped(obj, CiPriority.Argument, true);
			Write(", ");
			WriteUnwrapped(args[0], CiPriority.Argument, true);
			Write(", .backwards)");
			break;
		case CiId.StringReplace:
			WriteUnwrapped(obj, CiPriority.Primary, true);
			Write(".replacingOccurrences(of: ");
			WriteUnwrapped(args[0], CiPriority.Argument, true);
			Write(", with: ");
			WriteUnwrapped(args[1], CiPriority.Argument, true);
			WriteChar(')');
			break;
		case CiId.StringStartsWith:
			WriteStringContains(obj, "hasPrefix", args);
			break;
		case CiId.StringSubstring:
			if (args[0].IsLiteralZero())
				WriteUnwrapped(obj, CiPriority.Primary, true);
			else {
				this.StringSubstring = true;
				Write("ciStringSubstring(");
				WriteUnwrapped(obj, CiPriority.Argument, false);
				Write(", ");
				WriteCoerced(this.System.IntType, args[0], CiPriority.Argument);
				WriteChar(')');
			}
			if (args.Count == 2) {
				Write(".prefix(");
				WriteCoerced(this.System.IntType, args[1], CiPriority.Argument);
				WriteChar(')');
			}
			break;
		case CiId.ArrayCopyTo:
		case CiId.ListCopyTo:
			OpenIndexing(args[1]);
			WriteRange(args[2], args[3]);
			Write("] = ");
			OpenIndexing(obj);
			WriteRange(args[0], args[3]);
			WriteChar(']');
			break;
		case CiId.ArrayFillAll:
			obj.Accept(this, CiPriority.Assign);
			if (obj.Type is CiArrayStorageType array && !IsArrayRef(array)) {
				Write(" = [");
				WriteType(array.GetElementType());
				Write("](repeating: ");
				WriteCoerced(array.GetElementType(), args[0], CiPriority.Argument);
				Write(", count: ");
				VisitLiteralLong(array.Length);
				WriteChar(')');
			}
			else {
				Write(".fill");
				WriteArgsInParentheses(method, args);
			}
			break;
		case CiId.ArrayFillPart:
			if (obj.Type is CiArrayStorageType array2 && !IsArrayRef(array2)) {
				OpenIndexing(obj);
				WriteRange(args[1], args[2]);
				Write("] = ArraySlice(repeating: ");
				WriteCoerced(array2.GetElementType(), args[0], CiPriority.Argument);
				Write(", count: ");
				WriteCoerced(this.System.IntType, args[2], CiPriority.Argument); // FIXME: side effect
				WriteChar(')');
			}
			else {
				obj.Accept(this, CiPriority.Primary);
				WriteMemberOp(obj, null);
				Write("fill");
				WriteArgsInParentheses(method, args);
			}
			break;
		case CiId.ArraySortAll:
			WritePostfix(obj, "[0..<");
			VisitLiteralLong(((CiArrayStorageType) obj.Type).Length);
			Write("].sort()");
			break;
		case CiId.ArraySortPart:
		case CiId.ListSortPart:
			OpenIndexing(obj);
			WriteRange(args[0], args[1]);
			Write("].sort()");
			break;
		case CiId.ListAdd:
		case CiId.QueueEnqueue:
		case CiId.StackPush:
			WriteListAppend(obj, args);
			break;
		case CiId.ListAddRange:
			obj.Accept(this, CiPriority.Assign);
			Write(" += ");
			args[0].Accept(this, CiPriority.Argument);
			break;
		case CiId.ListAll:
			WritePostfix(obj, ".allSatisfy ");
			args[0].Accept(this, CiPriority.Argument);
			break;
		case CiId.ListAny:
			WritePostfix(obj, ".contains ");
			args[0].Accept(this, CiPriority.Argument);
			break;
		case CiId.ListClear:
		case CiId.QueueClear:
		case CiId.StackClear:
		case CiId.HashSetClear:
		case CiId.SortedSetClear:
		case CiId.DictionaryClear:
		case CiId.SortedDictionaryClear:
			WritePostfix(obj, ".removeAll()");
			break;
		case CiId.ListIndexOf:
			if (parent > CiPriority.Rel)
				WriteChar('(');
			WritePostfix(obj, ".firstIndex(of: ");
			args[0].Accept(this, CiPriority.Argument);
			Write(") ?? -1");
			if (parent > CiPriority.Rel)
				WriteChar(')');
			break;
		case CiId.ListInsert:
			WritePostfix(obj, ".insert(");
			CiType elementType = ((CiClassType) obj.Type).GetElementType();
			if (args.Count == 1)
				WriteNewStorage(elementType);
			else
				WriteCoerced(elementType, args[1], CiPriority.Argument);
			Write(", at: ");
			WriteCoerced(this.System.IntType, args[0], CiPriority.Argument);
			WriteChar(')');
			break;
		case CiId.ListLast:
		case CiId.StackPeek:
			WritePostfix(obj, ".last");
			break;
		case CiId.ListRemoveAt:
			WritePostfix(obj, ".remove(at: ");
			WriteCoerced(this.System.IntType, args[0], CiPriority.Argument);
			WriteChar(')');
			break;
		case CiId.ListRemoveRange:
			WritePostfix(obj, ".removeSubrange(");
			WriteRange(args[0], args[1]);
			WriteChar(')');
			break;
		case CiId.QueueDequeue:
			WritePostfix(obj, ".removeFirst()");
			break;
		case CiId.QueuePeek:
			WritePostfix(obj, ".first");
			break;
		case CiId.StackPop:
			WritePostfix(obj, ".removeLast()");
			break;
		case CiId.HashSetAdd:
		case CiId.SortedSetAdd:
			WritePostfix(obj, ".insert(");
			WriteCoerced(((CiClassType) obj.Type).GetElementType(), args[0], CiPriority.Argument);
			WriteChar(')');
			break;
		case CiId.DictionaryAdd:
			WriteDictionaryAdd(obj, args);
			break;
		case CiId.DictionaryContainsKey:
		case CiId.SortedDictionaryContainsKey:
			if (parent > CiPriority.Equality)
				WriteChar('(');
			WriteIndexing(obj, args[0]);
			Write(" != nil");
			if (parent > CiPriority.Equality)
				WriteChar(')');
			break;
		case CiId.DictionaryRemove:
		case CiId.SortedDictionaryRemove:
			WritePostfix(obj, ".removeValue(forKey: ");
			args[0].Accept(this, CiPriority.Argument);
			WriteChar(')');
			break;
		case CiId.ConsoleWrite:
			// TODO: stderr
			Write("print(");
			WriteUnwrapped(args[0], CiPriority.Argument, true);
			Write(", terminator: \"\")");
			break;
		case CiId.ConsoleWriteLine:
			// TODO: stderr
			Write("print(");
			if (args.Count == 1)
				WriteUnwrapped(args[0], CiPriority.Argument, true);
			WriteChar(')');
			break;
		case CiId.UTF8GetByteCount:
			WriteUnwrapped(args[0], CiPriority.Primary, true);
			Write(".utf8.count");
			break;
		case CiId.UTF8GetBytes:
			if (AddVar("cibytes"))
				Write(this.VarBytesAtIndent[this.Indent] ? "var " : "let ");
			Write("cibytes = [UInt8](");
			WriteUnwrapped(args[0], CiPriority.Primary, true);
			WriteLine(".utf8)");
			OpenIndexing(args[1]);
			WriteCoerced(this.System.IntType, args[2], CiPriority.Shift);
			if (args[2].IsLiteralZero())
				Write("..<");
			else {
				Write(" ..< ");
				WriteCoerced(this.System.IntType, args[2], CiPriority.Add); // TODO: side effect
				Write(" + ");
			}
			WriteLine("cibytes.count] = cibytes[...]");
			break;
		case CiId.UTF8GetString:
			Write("String(decoding: ");
			OpenIndexing(args[0]);
			WriteRange(args[1], args[2]);
			Write("], as: UTF8.self)");
			break;
		case CiId.EnvironmentGetEnvironmentVariable:
			Include("Foundation");
			Write("ProcessInfo.processInfo.environment[");
			WriteUnwrapped(args[0], CiPriority.Argument, false);
			WriteChar(']');
			break;
		case CiId.MathMethod:
		case CiId.MathLog2:
			Include("Foundation");
			WriteCamelCase(method.Name);
			WriteArgsInParentheses(method, args);
			break;
		case CiId.MathAbs:
		case CiId.MathMaxInt:
		case CiId.MathMaxDouble:
		case CiId.MathMinInt:
		case CiId.MathMinDouble:
			WriteCamelCase(method.Name);
			WriteArgsInParentheses(method, args);
			break;
		case CiId.MathCeiling:
			Include("Foundation");
			WriteCall("ceil", args[0]);
			break;
		case CiId.MathClamp:
			Write("min(max(");
			WriteClampAsMinMax(args);
			break;
		case CiId.MathFusedMultiplyAdd:
			Include("Foundation");
			WriteCall("fma", args[0], args[1], args[2]);
			break;
		case CiId.MathIsFinite:
			WritePostfix(args[0], ".isFinite");
			break;
		case CiId.MathIsInfinity:
			WritePostfix(args[0], ".isInfinite");
			break;
		case CiId.MathIsNaN:
			WritePostfix(args[0], ".isNaN");
			break;
		case CiId.MathRound:
			WritePostfix(args[0], ".rounded()");
			break;
		case CiId.MathTruncate:
			Include("Foundation");
			WriteCall("trunc", args[0]);
			break;
		default:
			NotSupported(obj, method.Name);
			break;
		}
	}

	protected override void WriteNewArrayStorage(CiArrayStorageType array)
	{
		if (IsArrayRef(array))
			base.WriteNewArrayStorage(array);
		else {
			WriteChar('[');
			WriteType(array.GetElementType());
			Write("](repeating: ");
			WriteDefaultValue(array.GetElementType());
			Write(", count: ");
			VisitLiteralLong(array.Length);
			WriteChar(')');
		}
	}

	protected override void WriteNew(CiReadWriteClassType klass, CiPriority parent)
	{
		WriteClassName(klass);
		Write("()");
	}

	void WriteDefaultValue(CiType type)
	{
		if (type is CiNumericType)
			WriteChar('0');
		else if (type is CiEnum) {
			if (type.Id == CiId.BoolType)
				Write("false");
			else {
				WriteName(type);
				WriteChar('.');
				WriteName(type.First);
			}
		}
		else if (type is CiStringType && !type.Nullable)
			Write("\"\"");
		else if (type is CiArrayStorageType array)
			WriteNewArrayStorage(array);
		else
			Write("nil");
	}

	protected override void WriteNewArray(CiType elementType, CiExpr lengthExpr, CiPriority parent)
	{
		this.ArrayRef = true;
		Write("ArrayRef<");
		WriteType(elementType);
		Write(">(");
		switch (elementType) {
		case CiArrayStorageType _:
			Write("factory: { ");
			WriteNewStorage(elementType);
			Write(" }");
			break;
		case CiStorageType klass:
			Write("factory: ");
			WriteName(klass.Class);
			Write(".init");
			break;
		default:
			Write("repeating: ");
			WriteDefaultValue(elementType);
			break;
		}
		Write(", count: ");
		lengthExpr.Accept(this, CiPriority.Argument);
		WriteChar(')');
	}

	public override void VisitPrefixExpr(CiPrefixExpr expr, CiPriority parent)
	{
		if (expr.Op == CiToken.Tilde && expr.Type is CiEnumFlags) {
			Write(expr.Type.Name);
			Write("(rawValue: ~");
			WritePostfix(expr.Inner, ".rawValue)");
		}
		else
			base.VisitPrefixExpr(expr, parent);
	}

	protected override void WriteIndexingExpr(CiBinaryExpr expr, CiPriority parent)
	{
		OpenIndexing(expr.Left);
		CiClassType klass = (CiClassType) expr.Left.Type;
		CiType indexType;
		switch (klass.Class.Id) {
		case CiId.ArrayPtrClass:
		case CiId.ArrayStorageClass:
		case CiId.ListClass:
			indexType = this.System.IntType;
			break;
		default:
			indexType = klass.GetKeyType();
			break;
		}
		WriteCoerced(indexType, expr.Right, CiPriority.Argument);
		WriteChar(']');
		if (parent != CiPriority.Assign && expr.Left.Type is CiClassType dict && dict.Class.TypeParameterCount == 2)
			WriteChar('!');
	}

	protected override void WriteBinaryOperand(CiExpr expr, CiPriority parent, CiBinaryExpr binary)
	{
		if (expr.Type.Id != CiId.BoolType) {
			if (binary.Op == CiToken.Plus && binary.Type.Id == CiId.StringStorageType) {
				WriteUnwrapped(expr, parent, true);
				return;
			}
			CiType type;
			switch (binary.Op) {
			case CiToken.Plus:
			case CiToken.Minus:
			case CiToken.Asterisk:
			case CiToken.Slash:
			case CiToken.Mod:
			case CiToken.And:
			case CiToken.Or:
			case CiToken.Xor:
			case CiToken.ShiftLeft when expr == binary.Left:
			case CiToken.ShiftRight when expr == binary.Left:
				if (!(expr is CiLiteral)) {
					type = this.System.PromoteNumericTypes(binary.Left.Type, binary.Right.Type);
					if (type != expr.Type) {
						WriteCoerced(type, expr, parent);
						return;
					}
				}
				break;
			case CiToken.Less:
			case CiToken.LessOrEqual:
			case CiToken.Greater:
			case CiToken.GreaterOrEqual:
			case CiToken.Equal:
			case CiToken.NotEqual:
				type = this.System.PromoteFloatingTypes(binary.Left.Type, binary.Right.Type);
				if (type != null && type != expr.Type) {
					WriteCoerced(type, expr, parent);
					return;
				}
				break;
			default:
				break;
			}
		}
		expr.Accept(this, parent);
	}

	void WriteEnumFlagsAnd(CiExpr left, string method, string notMethod, CiExpr right)
	{
		if (right is CiPrefixExpr negation && negation.Op == CiToken.Tilde)
			WriteMethodCall(left, notMethod, negation.Inner);
		else
			WriteMethodCall(left, method, right);
	}

	CiExpr WriteAssignNested(CiBinaryExpr expr)
	{
		if (expr.Right is CiBinaryExpr rightBinary && rightBinary.IsAssign()) {
			VisitBinaryExpr(rightBinary, CiPriority.Statement);
			WriteNewLine();
			return rightBinary.Left; // TODO: side effect
		}
		return expr.Right;
	}

	void WriteAssign(CiBinaryExpr expr, CiExpr right)
	{
		expr.Left.Accept(this, CiPriority.Assign);
		WriteChar(' ');
		Write(expr.GetOpString());
		WriteChar(' ');
		if (right is CiLiteralNull
		 && expr.Left is CiBinaryExpr leftBinary
		 && leftBinary.Op == CiToken.LeftBracket
		 && leftBinary.Left.Type is CiClassType dict
		 && dict.Class.TypeParameterCount == 2) {
			WriteType(dict.GetValueType());
			Write(".none");
		}
		else
			WriteCoerced(expr.Type, right, CiPriority.Argument);
	}

	public override void VisitBinaryExpr(CiBinaryExpr expr, CiPriority parent)
	{
		CiExpr right;
		switch (expr.Op) {
		case CiToken.ShiftLeft:
			WriteBinaryExpr(expr, parent > CiPriority.Mul, CiPriority.Primary, " << ", CiPriority.Primary);
			break;
		case CiToken.ShiftRight:
			WriteBinaryExpr(expr, parent > CiPriority.Mul, CiPriority.Primary, " >> ", CiPriority.Primary);
			break;
		case CiToken.And:
			if (expr.Type.Id == CiId.BoolType)
				WriteCall("{ a, b in a && b }", expr.Left, expr.Right);
			else if (expr.Type is CiEnumFlags)
				WriteEnumFlagsAnd(expr.Left, "intersection", "subtracting", expr.Right);
			else
				WriteBinaryExpr(expr, parent > CiPriority.Mul, CiPriority.Mul, " & ", CiPriority.Primary);
			break;
		case CiToken.Or:
			if (expr.Type.Id == CiId.BoolType)
				WriteCall("{ a, b in a || b }", expr.Left, expr.Right);
			else if (expr.Type is CiEnumFlags)
				WriteMethodCall(expr.Left, "union", expr.Right);
			else
				WriteBinaryExpr(expr, parent > CiPriority.Add, CiPriority.Add, " | ", CiPriority.Mul);
			break;
		case CiToken.Xor:
			if (expr.Type.Id == CiId.BoolType)
				WriteEqual(expr, parent, true);
			else if (expr.Type is CiEnumFlags)
				WriteMethodCall(expr.Left, "symmetricDifference", expr.Right);
			else
				WriteBinaryExpr(expr, parent > CiPriority.Add, CiPriority.Add, " ^ ", CiPriority.Mul);
			break;
		case CiToken.Assign:
		case CiToken.AddAssign:
		case CiToken.SubAssign:
		case CiToken.MulAssign:
		case CiToken.DivAssign:
		case CiToken.ModAssign:
		case CiToken.ShiftLeftAssign:
		case CiToken.ShiftRightAssign:
			WriteAssign(expr, WriteAssignNested(expr));
			break;
		case CiToken.AndAssign:
			right = WriteAssignNested(expr);
			if (expr.Type.Id == CiId.BoolType) {
				Write("if ");
				if (right is CiPrefixExpr negation && negation.Op == CiToken.ExclamationMark) {
					// avoid swiftc "error: unary operators must not be juxtaposed; parenthesize inner expression"
					negation.Inner.Accept(this, CiPriority.Argument);
				}
				else {
					WriteChar('!');
					right.Accept(this, CiPriority.Primary);
				}
				OpenChild();
				expr.Left.Accept(this, CiPriority.Assign);
				WriteLine(" = false");
				this.Indent--;
				WriteChar('}');
			}
			else if (expr.Type is CiEnumFlags)
				WriteEnumFlagsAnd(expr.Left, "formIntersection", "subtract", right);
			else
				WriteAssign(expr, right);
			break;
		case CiToken.OrAssign:
			right = WriteAssignNested(expr);
			if (expr.Type.Id == CiId.BoolType) {
				Write("if ");
				right.Accept(this, CiPriority.Argument);
				OpenChild();
				expr.Left.Accept(this, CiPriority.Assign);
				WriteLine(" = true");
				this.Indent--;
				WriteChar('}');
			}
			else if (expr.Type is CiEnumFlags)
				WriteMethodCall(expr.Left, "formUnion", right);
			else
				WriteAssign(expr, right);
			break;
		case CiToken.XorAssign:
			right = WriteAssignNested(expr);
			if (expr.Type.Id == CiId.BoolType) {
				expr.Left.Accept(this, CiPriority.Assign);
				Write(" = ");
				expr.Left.Accept(this, CiPriority.Equality); // TODO: side effect
				Write(" != ");
				expr.Right.Accept(this, CiPriority.Equality);
			}
			else if (expr.Type is CiEnumFlags)
				WriteMethodCall(expr.Left, "formSymmetricDifference", right);
			else
				WriteAssign(expr, right);
			break;
		default:
			base.VisitBinaryExpr(expr, parent);
			break;
		}
	}

	protected override void WriteResource(string name, int length)
	{
		if (length >= 0) // reference as opposed to definition
			Write("CiResource.");
		foreach (char c in name)
			WriteChar(CiLexer.IsLetterOrDigit(c) ? c : '_');
	}

	static bool Throws(CiExpr expr)
	{
		switch (expr) {
		case CiVar _:
		case CiLiteral _:
		case CiLambdaExpr _:
			return false;
		case CiAggregateInitializer init:
			return init.Items.Any(field => Throws(field));
		case CiInterpolatedString interp:
			return interp.Parts.Any(part => Throws(part.Argument));
		case CiSymbolReference symbol:
			return symbol.Left != null && Throws(symbol.Left);
		case CiUnaryExpr unary:
			return unary.Inner != null /* new C() */ && Throws(unary.Inner);
		case CiBinaryExpr binary:
			return Throws(binary.Left) || Throws(binary.Right);
		case CiSelectExpr select:
			return Throws(select.Cond) || Throws(select.OnTrue) || Throws(select.OnFalse);
		case CiCallExpr call:
			return (call.Method.Left != null && Throws(call.Method.Left))
				|| call.Arguments.Any(arg => Throws(arg))
				|| ((CiMethod) call.Method.Symbol).Throws;
		default:
			throw new NotImplementedException(expr.GetType().Name);
		}
	}

	protected override void WriteExpr(CiExpr expr, CiPriority parent)
	{
		if (Throws(expr))
			Write("try ");
		base.WriteExpr(expr, parent);
	}

	protected override void WriteCoercedExpr(CiType type, CiExpr expr)
	{
		if (Throws(expr))
			Write("try ");
		base.WriteCoercedExpr(type, expr);
	}

	protected override void StartTemporaryVar(CiType type) => Write("var ");

	public override void VisitExpr(CiExpr statement)
	{
		WriteTemporaries(statement);
		if (statement is CiCallExpr call && statement.Type.Id != CiId.VoidType)
			Write("_ = ");
		base.VisitExpr(statement);
	}

	void InitVarsAtIndent()
	{
		while (this.VarsAtIndent.Count <= this.Indent) {
			this.VarsAtIndent.Add(new HashSet<string>());
			this.VarBytesAtIndent.Add(false);
		}
		this.VarsAtIndent[this.Indent].Clear();
		this.VarBytesAtIndent[this.Indent] = false;
	}

	bool AddVar(string name) => this.VarsAtIndent[this.Indent].Add(name);

	protected override void OpenChild()
	{
		WriteChar(' ');
		OpenBlock();
		InitVarsAtIndent();
	}

	protected override void CloseChild() => CloseBlock();

	protected override void WriteVar(CiNamedValue def)
	{
		if (def is CiField || AddVar(def.Name)) {
			Write((def.Type is CiClass ? !def.IsAssignableStorage()
				: def.Type is CiArrayStorageType array ? IsArrayRef(array)
				: (def is CiVar local && !local.IsAssigned && !(def.Type is CiStorageType))) ? "let " : "var ");
			base.WriteVar(def);
		}
		else {
			WriteName(def);
			WriteVarInit(def);
		}
	}

	protected override void WriteStatements(List<CiStatement> statements)
	{
		// Encoding.UTF8.GetBytes returns void, so it can only be called as a statement
		this.VarBytesAtIndent[this.Indent] = statements.Count(s => s is CiCallExpr call && call.Method.Symbol.Id == CiId.UTF8GetBytes) > 1;
		base.WriteStatements(statements);
	}

	public override void VisitLambdaExpr(CiLambdaExpr expr)
	{
		Write("{ ");
		WriteName(expr.First);
		Write(" in ");
		expr.Body.Accept(this, CiPriority.Statement);
		Write(" }");
	}

	protected override void WriteAssertCast(CiBinaryExpr expr)
	{
		Write("let ");
		CiVar def = (CiVar) expr.Right;
		WriteCamelCaseNotKeyword(def.Name);
		Write(" = ");
		expr.Left.Accept(this, CiPriority.Equality /* TODO? */);
		Write(" as! ");
		WriteLine(def.Type.Name);
	}

	protected override void WriteAssert(CiAssert statement)
	{
		Write("assert(");
		WriteExpr(statement.Cond, CiPriority.Argument);
		if (statement.Message != null) {
			Write(", ");
			WriteExpr(statement.Message, CiPriority.Argument);
		}
		WriteCharLine(')');
	}

	public override void VisitBreak(CiBreak statement) => WriteLine("break");

	protected override bool NeedCondXcrement(CiLoop loop)
		=> loop.Cond != null && (!loop.HasBreak || !VisitXcrement<CiPostfixExpr>(loop.Cond, false));

	protected override string GetIfNot() => "if !";

	protected override void WriteContinueDoWhile(CiExpr cond)
	{
		VisitXcrement<CiPrefixExpr>(cond, true);
		WriteLine("continue");
	}

	public override void VisitDoWhile(CiDoWhile statement)
	{
		if (VisitXcrement<CiPostfixExpr>(statement.Cond, false))
			base.VisitDoWhile(statement);
		else {
			Write("repeat");
			OpenChild();
			statement.Body.AcceptStatement(this);
			if (statement.Body.CompletesNormally())
				VisitXcrement<CiPrefixExpr>(statement.Cond, true);
			CloseChild();
			Write("while ");
			WriteExpr(statement.Cond, CiPriority.Argument);
			WriteNewLine();
		}
	}

	protected override void WriteElseIf() => Write("else ");

	protected override void OpenWhile(CiLoop loop)
	{
		if (NeedCondXcrement(loop))
			base.OpenWhile(loop);
		else {
			Write("while true");
			OpenChild();
			VisitXcrement<CiPrefixExpr>(loop.Cond, true);
			Write("let ciDoLoop = ");
			loop.Cond.Accept(this, CiPriority.Argument);
			WriteNewLine();
			VisitXcrement<CiPostfixExpr>(loop.Cond, true);
			Write("if !ciDoLoop");
			OpenChild();
			WriteLine("break");
			CloseChild();
		}
	}

	protected override void WriteForRange(CiVar iter, CiBinaryExpr cond, long rangeStep)
	{
		if (rangeStep == 1) {
			WriteExpr(iter.Value, CiPriority.Shift);
			switch (cond.Op) {
			case CiToken.Less:
				Write("..<");
				cond.Right.Accept(this, CiPriority.Shift);
				break;
			case CiToken.LessOrEqual:
				Write("...");
				cond.Right.Accept(this, CiPriority.Shift);
				break;
			default:
				throw new NotImplementedException(cond.Op.ToString());
			}
		}
		else {
			Write("stride(from: ");
			WriteExpr(iter.Value, CiPriority.Argument);
			switch (cond.Op) {
			case CiToken.Less:
			case CiToken.Greater:
				Write(", to: ");
				WriteExpr(cond.Right, CiPriority.Argument);
				break;
			case CiToken.LessOrEqual:
			case CiToken.GreaterOrEqual:
				Write(", through: ");
				WriteExpr(cond.Right, CiPriority.Argument);
				break;
			default:
				throw new NotImplementedException(cond.Op.ToString());
			}
			Write(", by: ");
			VisitLiteralLong(rangeStep);
			WriteChar(')');
		}
	}

	public override void VisitForeach(CiForeach statement)
	{
		Write("for ");
		if (statement.Count() == 2) {
			WriteChar('(');
			WriteName(statement.GetVar());
			Write(", ");
			WriteName(statement.GetValueVar());
			WriteChar(')');
		}
		else
			WriteName(statement.GetVar());
		Write(" in ");
		CiClassType klass = (CiClassType) statement.Collection.Type;
		switch (klass.Class.Id) {
		case CiId.StringClass:
			WritePostfix(statement.Collection, ".unicodeScalars");
			break;
		case CiId.SortedSetClass:
			WritePostfix(statement.Collection, ".sorted()");
			break;
		case CiId.SortedDictionaryClass:
			WritePostfix(statement.Collection, klass.GetKeyType().Nullable
				? ".sorted(by: { $0.key! < $1.key! })"
				: ".sorted(by: { $0.key < $1.key })");
			break;
		default:
			WriteExpr(statement.Collection, CiPriority.Argument);
			break;
		}
		WriteChild(statement.Body);
	}

	public override void VisitLock(CiLock statement)
	{
		statement.Lock.Accept(this, CiPriority.Primary);
		WriteLine(".lock()");
		Write("do");
		OpenChild();
		Write("defer { ");
		statement.Lock.Accept(this, CiPriority.Primary);
		WriteLine(".unlock() }");
		statement.Body.AcceptStatement(this);
		CloseChild();
	}

	protected override void WriteResultVar()
	{
		Write("let result : ");
		WriteType(this.CurrentMethod.Type);
	}

	void WriteSwitchCaseVar(CiVar def)
	{
		if (def.Name == "_")
			Write("is ");
		else {
			Write("let ");
			WriteCamelCaseNotKeyword(def.Name);
			Write(" as ");
		}
		WriteType(def.Type);
	}

	void WriteSwitchCaseBody(CiSwitch statement, List<CiStatement> body)
	{
		this.Indent++;
		VisitXcrement<CiPostfixExpr>(statement.Value, true);
		InitVarsAtIndent();
		WriteSwitchCaseBody(body);
		this.Indent--;
	}

	public override void VisitSwitch(CiSwitch statement)
	{
		VisitXcrement<CiPrefixExpr>(statement.Value, true);
		Write("switch ");
		WriteExpr(statement.Value, CiPriority.Argument);
		WriteLine(" {");
		foreach (CiCase kase in statement.Cases) {
			Write("case ");
			for (int i = 0; i < kase.Values.Count; i++) {
				WriteComma(i);
				switch (kase.Values[i]) {
				case CiBinaryExpr binary when binary.Op == CiToken.When:
					WriteSwitchCaseVar((CiVar) binary.Left);
					Write(" where ");
					WriteExpr(binary.Right, CiPriority.Argument);
					break;
				case CiVar def:
					WriteSwitchCaseVar(def);
					break;
				default:
					WriteCoerced(statement.Value.Type, kase.Values[i], CiPriority.Argument);
					break;
				}
			}
			WriteCharLine(':');
			WriteSwitchCaseBody(statement, kase.Body);
		}
		if (statement.DefaultBody.Count > 0) {
			WriteLine("default:");
			WriteSwitchCaseBody(statement, statement.DefaultBody);
		}
		WriteCharLine('}');
	}

	public override void VisitThrow(CiThrow statement)
	{
		this.Throw = true;
		VisitXcrement<CiPrefixExpr>(statement.Message, true);
		Write("throw CiError.error(");
		WriteExpr(statement.Message, CiPriority.Argument);
		WriteCharLine(')');
	}

	void WriteReadOnlyParameter(CiVar param)
	{
		Write("ciParam");
		WritePascalCase(param.Name);
	}

	protected override void WriteParameter(CiVar param)
	{
		Write("_ ");
		if (param.IsAssigned)
			WriteReadOnlyParameter(param);
		else
			WriteName(param);
		Write(" : ");
		WriteType(param.Type);
	}

	public override void VisitEnumValue(CiConst konst, CiConst previous)
	{
		WriteDoc(konst.Documentation);
		Write("static let ");
		WriteName(konst);
		Write(" = ");
		Write(konst.Parent.Name);
		WriteChar('(');
		int i = konst.Value.IntValue();
		if (i == 0)
			Write("[]");
		else {
			Write("rawValue: ");
			VisitLiteralLong(i);
		}
		WriteCharLine(')');
	}

	protected override void WriteEnum(CiEnum enu)
	{
		WriteNewLine();
		WriteDoc(enu.Documentation);
		WritePublic(enu);
		if (enu is CiEnumFlags) {
			Write("struct ");
			Write(enu.Name);
			WriteLine(" : OptionSet");
			OpenBlock();
			WriteLine("let rawValue : Int");
			enu.AcceptValues(this);
		}
		else {
			Write("enum ");
			Write(enu.Name);
			if (enu.HasExplicitValue)
				Write(" : Int");
			WriteNewLine();
			OpenBlock();
			Dictionary<int, CiConst> valueToConst = new Dictionary<int, CiConst>();
			for (CiConst konst = (CiConst) enu.First; konst != null; konst = (CiConst) konst.Next) {
				WriteDoc(konst.Documentation);
				int i = konst.Value.IntValue();
				if (valueToConst.TryGetValue(i, out CiConst duplicate)) {
					Write("static let ");
					WriteName(konst);
					Write(" = ");
					WriteName(duplicate);
				}
				else {
					Write("case ");
					WriteName(konst);
					if (!(konst.Value is CiImplicitEnumValue)) {
						Write(" = ");
						VisitLiteralLong(i);
					}
					valueToConst.Add(i, konst);
				}
				WriteNewLine();
			}
		}
		CloseBlock();
	}

	void WriteVisibility(CiVisibility visibility)
	{
		switch (visibility) {
		case CiVisibility.Private:
			Write("private ");
			break;
		case CiVisibility.Internal:
			Write("fileprivate ");
			break;
		case CiVisibility.Protected:
		case CiVisibility.Public:
			Write("public ");
			break;
		}
	}

	protected override void WriteConst(CiConst konst)
	{
		WriteNewLine();
		WriteDoc(konst.Documentation);
		WriteVisibility(konst.Visibility);
		Write("static let ");
		WriteName(konst);
		Write(" = ");
		if (konst.Type.Id == CiId.IntType || konst.Type is CiEnum || konst.Type.Id == CiId.StringPtrType)
			konst.Value.Accept(this, CiPriority.Argument);
		else {
			WriteType(konst.Type);
			WriteChar('(');
			konst.Value.Accept(this, CiPriority.Argument);
			WriteChar(')');
		}
		WriteNewLine();
	}

	protected override void WriteField(CiField field)
	{
		WriteNewLine();
		WriteDoc(field.Documentation);
		WriteVisibility(field.Visibility);
		if (field.Type is CiClassType klass && klass.Class.Id != CiId.StringClass && !(klass is CiDynamicPtrType) && !(klass is CiStorageType))
			Write("unowned ");
		WriteVar(field);
		if (field.Value == null && (field.Type is CiNumericType || field.Type is CiEnum || field.Type.Id == CiId.StringStorageType)) {
			Write(" = ");
			WriteDefaultValue(field.Type);
		}
		else if (field.IsAssignableStorage()) {
			Write(" = ");
			WriteName(((CiStorageType) field.Type).Class);
			Write("()");
		}
		WriteNewLine();
	}

	protected override void WriteParameterDoc(CiVar param, bool first)
	{
		Write("/// - parameter ");
		WriteName(param);
		WriteChar(' ');
		WriteDocPara(param.Documentation.Summary, false);
		WriteNewLine();
	}

	protected override void WriteMethod(CiMethod method)
	{
		WriteNewLine();
		WriteDoc(method.Documentation);
		WriteParametersDoc(method);
		switch (method.CallType) {
		case CiCallType.Static:
			WriteVisibility(method.Visibility);
			Write("static ");
			break;
		case CiCallType.Normal:
			WriteVisibility(method.Visibility);
			break;
		case CiCallType.Abstract:
		case CiCallType.Virtual:
			Write(method.Visibility == CiVisibility.Internal ? "fileprivate " : "open ");
			break;
		case CiCallType.Override:
			Write(method.Visibility == CiVisibility.Internal ? "fileprivate " : "open ");
			Write("override ");
			break;
		case CiCallType.Sealed:
			WriteVisibility(method.Visibility);
			Write("final override ");
			break;
		}
		if (method.Id == CiId.ClassToString)
			Write("var description : String");
		else {
			Write("func ");
			WriteName(method);
			WriteParameters(method, true);
			if (method.Throws)
				Write(" throws");
			if (method.Type.Id != CiId.VoidType) {
				Write(" -> ");
				WriteType(method.Type);
			}
		}
		WriteNewLine();
		OpenBlock();
		if (method.CallType == CiCallType.Abstract)
			WriteLine("preconditionFailure(\"Abstract method called\")");
		else {
			for (CiVar param = method.Parameters.FirstParameter(); param != null; param = param.NextParameter()) {
				if (param.IsAssigned) {
					Write("var ");
					WriteTypeAndName(param);
					Write(" = ");
					WriteReadOnlyParameter(param);
					WriteNewLine();
				}
			}
			InitVarsAtIndent();
			this.CurrentMethod = method;
			method.Body.AcceptStatement(this);
			this.CurrentMethod = null;
		}
		CloseBlock();
	}

	protected override void WriteClass(CiClass klass, CiProgram program)
	{
		WriteNewLine();
		WriteDoc(klass.Documentation);
		WritePublic(klass);
		if (klass.CallType == CiCallType.Sealed)
			Write("final ");
		StartClass(klass, "", " : ");
		if (klass.AddsToString()) {
			Write(klass.HasBaseClass() ? ", " : " : ");
			Write("CustomStringConvertible");
		}
		WriteNewLine();
		OpenBlock();

		if (NeedsConstructor(klass)) {
			if (klass.Constructor != null) {
				WriteDoc(klass.Constructor.Documentation);
				WriteVisibility(klass.Constructor.Visibility);
			}
			else
				Write("fileprivate ");
			if (klass.HasBaseClass())
				Write("override ");
			WriteLine("init()");
			OpenBlock();
			InitVarsAtIndent();
			WriteConstructorBody(klass);
			CloseBlock();
		}

		WriteMembers(klass, true);

		CloseBlock();
	}

	void WriteLibrary()
	{
		if (this.Throw) {
			WriteNewLine();
			WriteLine("public enum CiError : Error");
			OpenBlock();
			WriteLine("case error(String)");
			CloseBlock();
		}
		if (this.ArrayRef) {
			WriteNewLine();
			WriteLine("public class ArrayRef<T> : Sequence");
			OpenBlock();
			WriteLine("var array : [T]");
			WriteNewLine();
			WriteLine("init(_ array : [T])");
			OpenBlock();
			WriteLine("self.array = array");
			CloseBlock();
			WriteNewLine();
			WriteLine("init(repeating: T, count: Int)");
			OpenBlock();
			WriteLine("self.array = [T](repeating: repeating, count: count)");
			CloseBlock();
			WriteNewLine();
			WriteLine("init(factory: () -> T, count: Int)");
			OpenBlock();
			WriteLine("self.array = (1...count).map({_ in factory() })");
			CloseBlock();
			WriteNewLine();
			WriteLine("subscript(index: Int) -> T");
			OpenBlock();
			WriteLine("get");
			OpenBlock();
			WriteLine("return array[index]");
			CloseBlock();
			WriteLine("set(value)");
			OpenBlock();
			WriteLine("array[index] = value");
			CloseBlock();
			CloseBlock();
			WriteLine("subscript(bounds: Range<Int>) -> ArraySlice<T>");
			OpenBlock();
			WriteLine("get");
			OpenBlock();
			WriteLine("return array[bounds]");
			CloseBlock();
			WriteLine("set(value)");
			OpenBlock();
			WriteLine("array[bounds] = value");
			CloseBlock();
			CloseBlock();
			WriteNewLine();
			WriteLine("func fill(_ value: T)");
			OpenBlock();
			WriteLine("array = [T](repeating: value, count: array.count)");
			CloseBlock();
			WriteNewLine();
			WriteLine("func fill(_ value: T, _ startIndex : Int, _ count : Int)");
			OpenBlock();
			WriteLine("array[startIndex ..< startIndex + count] = ArraySlice(repeating: value, count: count)");
			CloseBlock();
			WriteNewLine();
			WriteLine("public func makeIterator() -> IndexingIterator<Array<T>>");
			OpenBlock();
			WriteLine("return array.makeIterator()");
			CloseBlock();
			CloseBlock();
		}
		if (this.StringCharAt) {
			WriteNewLine();
			WriteLine("fileprivate func ciStringCharAt(_ s: String, _ offset: Int) -> Int");
			OpenBlock();
			WriteLine("return Int(s.unicodeScalars[s.index(s.startIndex, offsetBy: offset)].value)");
			CloseBlock();
		}
		if (this.StringIndexOf) {
			WriteNewLine();
			WriteLine("fileprivate func ciStringIndexOf<S1 : StringProtocol, S2 : StringProtocol>(_ haystack: S1, _ needle: S2, _ options: String.CompareOptions = .literal) -> Int");
			OpenBlock();
			WriteLine("guard let index = haystack.range(of: needle, options: options) else { return -1 }");
			WriteLine("return haystack.distance(from: haystack.startIndex, to: index.lowerBound)");
			CloseBlock();
		}
		if (this.StringSubstring) {
			WriteNewLine();
			WriteLine("fileprivate func ciStringSubstring(_ s: String, _ offset: Int) -> Substring");
			OpenBlock();
			WriteLine("return s[s.index(s.startIndex, offsetBy: offset)...]");
			CloseBlock();
		}
	}

	void WriteResources(Dictionary<string, byte[]> resources)
	{
		if (resources.Count == 0)
			return;
		this.ArrayRef = true;
		WriteNewLine();
		WriteLine("fileprivate final class CiResource");
		OpenBlock();
		foreach (string name in resources.Keys.OrderBy(k => k)) {
			Write("static let ");
			WriteResource(name, -1);
			WriteLine(" = ArrayRef<UInt8>([");
			WriteChar('\t');
			WriteBytes(resources[name]);
			WriteLine(" ])");
		}
		CloseBlock();
	}

	public override void WriteProgram(CiProgram program)
	{
		this.System = program.System;
		this.Includes = new SortedSet<string>();
		this.Throw = false;
		this.ArrayRef = false;
		this.StringCharAt = false;
		this.StringIndexOf = false;
		this.StringSubstring = false;
		OpenStringWriter();
		WriteTypes(program);

		CreateFile(this.OutputFile);
		WriteIncludes("import ", "");
		CloseStringWriter();
		WriteLibrary();
		WriteResources(program.Resources);
		CloseFile();
	}
}

}
