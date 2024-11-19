using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using EmmyLua.CodeAnalysis.Compilation;
using EmmyLua.CodeAnalysis.Compilation.Symbol;
using EmmyLua.CodeAnalysis.Syntax.Node.SyntaxNodes;
using EmmyLua.CodeAnalysis.Type;

namespace EmmyLua.CodeAnalysis.Diagnostics.Checkers;

[method: SetsRequiredMembers]
public class ReferenceInfo(LuaSymbol caller, LuaSymbol function, LuaSymbol target, LuaCallExprSyntax syntax)
{
    public required LuaSymbol Caller = caller;
    public required LuaSymbol Function = function;
    public required LuaSymbol Target = target;
    public required LuaCallExprSyntax Syntax = syntax;
}
public class ReferenceLeakChecker(LuaCompilation compilation)
    : DiagnosticCheckerBase(compilation, [DiagnosticCode.ReferenceLeak])
{
    public override void Check(DiagnosticContext context)
    {
        Collection<ReferenceInfo> eventBusGlobalEventIncreaseInfos = new();
        Collection<ReferenceInfo> eventBusGlobalEventDecreaseInfos = new();
        Collection<ReferenceInfo> eventBusTargetEventIncreaseInfos = new();
        Collection<ReferenceInfo> eventBusTargetEventDecreaseInfos = new();
        var document = context.Document;
        foreach (var callExpr in document.SyntaxTree.SyntaxRoot.Descendants.OfType<LuaCallExprSyntax>())
        {
            var declaration = context.SearchContext.FindDeclaration(callExpr);
            if (declaration is null)
            {
                continue;
            }

            if (callExpr.PrefixExpr is LuaIndexExprSyntax { PrefixExpr : {} prefixExpr })
            {
                if (prefixExpr is LuaIndexExprSyntax { IsColonIndex: true} || prefixExpr is LuaNameExprSyntax)
                {
                    var nameDeclaration = context.SearchContext.FindDeclaration(prefixExpr);
                    if (nameDeclaration?.Name == "EventBus")
                    {
                        if (callExpr.Name == "RegisterGlobalEvent" || callExpr.Name == "UnRegisterGlobalEvent")
                        {
                            var arg = callExpr.ArgList?.ArgList.ToList()[0];
                            var argDeclaration = context.SearchContext.FindDeclaration(arg) ?? new LuaSymbol(arg.ToString(), Builtin.Unknown, new VirtualInfo());
                            if (arg is not null && argDeclaration.Type == Builtin.Unknown)
                            {
                                context.Report(
                                    DiagnosticCode.ReferenceLeak,
                                    $"parameter is nil",
                                    arg.Range
                                );
                                continue;
                            }

                            ReferenceInfo info = new(nameDeclaration, declaration, argDeclaration, callExpr);
                            if (callExpr.Name == "RegisterGlobalEvent")
                            {
                                eventBusGlobalEventIncreaseInfos.Add(info);
                            }
                            else if (callExpr.Name == "UnRegisterGlobalEvent")
                            {
                                eventBusGlobalEventDecreaseInfos.Add(info);
                            }
                        }

                        if (callExpr.Name == "RegisterTargetEvent" || callExpr.Name == "UnRegisterTargetEvent")
                        {
                            var arg = callExpr.ArgList?.ArgList.ToList()[1];
                            var argDeclaration = context.SearchContext.FindDeclaration(arg) ?? new LuaSymbol(arg.ToString(), Builtin.Unknown, new VirtualInfo());

                            if (arg is not null && argDeclaration.Type == Builtin.Unknown)
                            {
                                context.Report(
                                    DiagnosticCode.ReferenceLeak,
                                    $"parameter is nil",
                                    arg.Range
                                );
                                continue;
                            }

                            ReferenceInfo info = new(nameDeclaration, declaration, argDeclaration, callExpr);
                            if (callExpr.Name == "RegisterTargetEvent")
                            {
                                eventBusTargetEventIncreaseInfos.Add(info);
                            }
                            else if (callExpr.Name == "UnRegisterTargetEvent")
                            {
                                eventBusTargetEventDecreaseInfos.Add(info);
                            }
                        }
                    }
                }
            }
        }

        foreach (var info in eventBusGlobalEventIncreaseInfos)
        {
            if (eventBusGlobalEventDecreaseInfos.All(i => (i.Caller != info.Caller || (!info.Target.IsLocal && i.Target != info.Target))))
            {
                context.Report(
                    DiagnosticCode.ReferenceLeak,
                    $"Missing UnRegisterGlobalEvent call for {info.Target.Name}",
                    info.Syntax.PrefixExpr.Range
                );
            }
        }
        foreach (var info in eventBusTargetEventIncreaseInfos)
        {
            if (eventBusTargetEventDecreaseInfos.All(i => (i.Caller != info.Caller || (!info.Target.IsLocal && i.Target != info.Target))))
            {
                context.Report(
                    DiagnosticCode.ReferenceLeak,
                    $"Missing UnRegisterTargetEvent call for {info.Target.Name}",
                    info.Syntax.PrefixExpr.Range
                );
            }
        }
    }
}
