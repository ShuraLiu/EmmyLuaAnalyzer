﻿using LuaLanguageServer.CodeAnalysis.Compile;
using LuaLanguageServer.CodeAnalysis.Compile.Source;
using LuaLanguageServer.CodeAnalysis.Syntax.Tree;

namespace LuaLanguageServer.CodeAnalysis.Workspace;

public class DocumentId(string path)
{
    public string Path { get; } = path;

    public string Url { get; } = new Uri(path).AbsoluteUri;

    public string Guid { get; } = System.Guid.NewGuid().ToString();
}

public class LuaDocument
{
    public LuaSource Source { get; }

    public DocumentId Id { get; }

    private LuaSyntaxTree? _syntaxTree;

    public static LuaDocument OpenDocument(string path, LuaLanguage language)
    {
        var fileText = File.ReadAllText(path);
        var luaSource = LuaSourceFile.From(path, fileText, language);
        var documentId = new DocumentId(path);
        return new LuaDocument(luaSource, documentId);
    }

    public static LuaDocument From(string path, string text, LuaLanguage language)
    {
        var luaSource = LuaSourceFile.From(path, text, language);
        var documentId = new DocumentId(path);
        return new LuaDocument(luaSource, documentId);
    }

    private LuaDocument(LuaSource luaSource, DocumentId id)
    {
        Source = luaSource;
        Id = id;
    }

    public string GetText()
    {
        return Source.Text;
    }

    public LuaSyntaxTree SyntaxTree => _syntaxTree ??= LuaSyntaxTree.Create(Source);
}
