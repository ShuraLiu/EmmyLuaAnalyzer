﻿using LuaLanguageServer.CodeAnalysis.Compilation.Symbol;
using LuaLanguageServer.CodeAnalysis.Compilation.Type;
using LuaLanguageServer.CodeAnalysis.Compilation.TypeOperator;
using LuaLanguageServer.CodeAnalysis.Syntax.Node;
using LuaLanguageServer.CodeAnalysis.Workspace;

namespace LuaLanguageServer.CodeAnalysis.Compilation.Analyzer.Stub;

public class StubIndexImpl(LuaCompilation compilation)
{
    public LuaCompilation Compilation { get; } = compilation;

    public StubIndex<string, ILuaSymbol> ShortNameIndex { get; } = new();

    public StubIndex<string, Declaration.Declaration> Members { get; } = new();

    public StubIndex<string, ILuaNamedType> NamedTypeIndex { get; } = new();

    public StubIndex<LuaSyntaxElement, ILuaSymbol> SyntaxIndex { get; } = new();

    public StubIndex<string, Declaration.Declaration> GlobalDeclaration { get; } = new();

    public StubIndex<string, ILuaOperator> TypeOperators { get; } = new();

    public void Remove(DocumentId documentId)
    {
        ShortNameIndex.RemoveStub(documentId);
        Members.RemoveStub(documentId);
        SyntaxIndex.RemoveStub(documentId);
        NamedTypeIndex.RemoveStub(documentId);
        GlobalDeclaration.RemoveStub(documentId);
        TypeOperators.RemoveStub(documentId);
    }
}
