﻿using EmmyLua.CodeAnalysis.Compilation.Semantic;
using EmmyLua.CodeAnalysis.Kind;
using EmmyLua.CodeAnalysis.Syntax.Node;
using EmmyLua.CodeAnalysis.Syntax.Node.SyntaxNodes;
using LanguageServer.ExecuteCommand.Commands;
using LanguageServer.Server;
using LanguageServer.Util;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace LanguageServer.CodeLens;

public class CodeLensBuilder
{
    public CodeLensContainer Build(SemanticModel semanticModel, ServerContext context)
    {
        var codeLens = new List<OmniSharp.Extensions.LanguageServer.Protocol.Models.CodeLens>();
        var funcStats = semanticModel.Document
            .SyntaxTree
            .SyntaxRoot
            .Descendants
            .OfType<LuaFuncStatSyntax>();

        var document = semanticModel.Document;
        foreach (var funcStat in funcStats)
        {
            if (funcStat.FirstChildToken(LuaTokenKind.TkFunction) is { } funcToken)
            {
                codeLens.Add(new OmniSharp.Extensions.LanguageServer.Protocol.Models.CodeLens()
                {
                    Range = funcToken.Range.ToLspRange(document),
                    Data = funcStat.UniqueId.ToString()
                });
            }
        }

        return new CodeLensContainer(codeLens);
    }

    public OmniSharp.Extensions.LanguageServer.Protocol.Models.CodeLens Resolve(
        OmniSharp.Extensions.LanguageServer.Protocol.Models.CodeLens codeLens, ServerContext context)
    {
        if (codeLens.Data?.Type == JTokenType.String && codeLens.Data.Value<string>() is { } uniqueIdString)
        {
            try
            {
                var ptr = LuaElementPtr<LuaSyntaxElement>.From(uniqueIdString);
                if (ptr.DocumentId is { } documentId)
                {
                    if (ptr.ToNode(context.LuaWorkspace) is LuaFuncStatSyntax { NameElement.Parent: {} element })
                    {
                        var semanticModel = context.GetSemanticModel(documentId);
                        if (semanticModel is not null)
                        {
                            var renferences = semanticModel.FindReferences(element);
                            codeLens = codeLens with
                            {
                                Command = MakeCommand(renferences.Count() - 1)
                            };
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        return codeLens;
    }

    private static Command MakeCommand(int count)
    {
        return new Command
        {
            Title = $"usage:{count}"
        };
    }
}