﻿using EmmyLua.CodeAnalysis.Compilation.Type;
using LanguageServer.Server;
using LanguageServer.Util;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace LanguageServer.WorkspaceSymbol;

public class WorkspaceSymbolBuilder
{
    public List<OmniSharp.Extensions.LanguageServer.Protocol.Models.WorkspaceSymbol> Build(string query,
        ServerContext context, CancellationToken cancellationToken)
    {
        var result = new List<OmniSharp.Extensions.LanguageServer.Protocol.Models.WorkspaceSymbol>();
        try
        {
            var luaWorkspace = context.LuaWorkspace;
            var globals = context.LuaWorkspace.Compilation.DbManager.GetGlobals();
            foreach (var global in globals)
            {
                if (global.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                {
                     cancellationToken.ThrowIfCancellationRequested();
                     var document = luaWorkspace.GetDocument(global.Info.Ptr.DocumentId);
                     if (document is not null && global.Info.Ptr.ToNode(document) is { } node)
                     {
                         result.Add(new OmniSharp.Extensions.LanguageServer.Protocol.Models.WorkspaceSymbol()
                         {
                             Name = global.Name,
                             Kind = ToSymbolKind(global.Info.DeclarationType),
                             Location = node.Range.ToLspLocation(document)
                         });
                     }
                }
            }
            var members = context.LuaWorkspace.Compilation.DbManager.GetAllMembers();
            foreach (var member in members)
            {
                if (member.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var document = luaWorkspace.GetDocument(member.Info.Ptr.DocumentId);
                    if (document is not null && member.Info.Ptr.ToNode(document) is { } node)
                    {
                        result.Add(new OmniSharp.Extensions.LanguageServer.Protocol.Models.WorkspaceSymbol()
                        {
                            Name = member.Name,
                            Kind = ToSymbolKind(member.Info.DeclarationType),
                            Location = node.Range.ToLspLocation(document)
                        });
                    }
                }
            }
            
            return result;
        }
        catch(OperationCanceledException)
        {
            return result;
        }
    }

    private static SymbolKind ToSymbolKind(LuaType? type)
    {
        return type switch
        {
            LuaNamedType => SymbolKind.Variable,
            LuaMethodType => SymbolKind.Method,
            _ => SymbolKind.Variable
        };
    }
}