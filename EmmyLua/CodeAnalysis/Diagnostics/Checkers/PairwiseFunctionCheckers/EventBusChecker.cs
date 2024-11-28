using System.Collections.ObjectModel;
using EmmyLua.CodeAnalysis.Compilation.Symbol;
using EmmyLua.CodeAnalysis.Syntax.Kind;
using EmmyLua.CodeAnalysis.Syntax.Node.SyntaxNodes;
using EmmyLua.CodeAnalysis.Type;

namespace EmmyLua.CodeAnalysis.Diagnostics.Checkers.PairwiseFunctionCheckers;

public class UnRegisterAllData()
{
    public LuaCallExprSyntax CallExprSyntax  { get; set; }
    public LuaSymbol Target = new LuaSymbol("unknown", Builtin.Unknown, new VirtualInfo());
}
public class EventBusChecker() : PairwiseFunctionCheckerBase()
{
    private Collection<FunctionData> _globalEventRegisterData = new();
    private Collection<FunctionData> _globalEventUnRegisterData = new();

    private Collection<FunctionData> _targetEventRegisterData = new();
    private Collection<FunctionData> _targetEventUnRegisterData = new();

    private Collection<UnRegisterAllData> _unRegisterAllData = new();

    public override void Prepare()
    {
        _globalEventRegisterData.Clear();
        _globalEventUnRegisterData.Clear();
        _targetEventRegisterData.Clear();
        _targetEventUnRegisterData.Clear();
        _unRegisterAllData.Clear();
    }

    public override bool CanCheck(LuaCallExprSyntax callExprSyntax, LuaSymbol prefixExprSymbol)
    {
        return prefixExprSymbol?.Name is "EventBus";
    }

    public override void AnalysisCallSyntax(DiagnosticContext context, LuaCallExprSyntax callExprSyntax, LuaSymbol callSymbol)
    {
        AnalyzePairCallSyntax(context, callExprSyntax, callSymbol);
        AnalyzeUnRegisterAllCallSyntax(context, callExprSyntax, callSymbol);
    }

    public override void PostAynlysis(DiagnosticContext context)
    {
        var valid = true;
        foreach (var registerData in _globalEventRegisterData)
        {
            valid = false;
            if (_globalEventUnRegisterData.Any(unregisterData =>
                {
                    if (registerData.Arguments[0].IsLocal is false &&
                        registerData.Arguments[0].UniqueId != unregisterData.Arguments[0].UniqueId)
                    {
                        return false;
                    }

                    if (registerData.Returns.Count > 0 && unregisterData.Arguments.Count == 2)
                    {
                        if (registerData.Returns[0].UniqueId == unregisterData.Arguments[1].UniqueId)
                        {
                            return true;
                        }
                    }

                    if (registerData.Arguments.Count >= 4)
                    {
                        if (unregisterData.Arguments.Count == 3)
                        {
                            for (var idx = 1; idx < unregisterData.Arguments.Count; ++idx)
                            {
                                if (registerData.Arguments[idx + 1].IsLocal is false &&
                                    registerData.Arguments[idx + 1].UniqueId != unregisterData.Arguments[idx].UniqueId)
                                {
                                    return false;
                                }
                            }
                        }
                        else if (unregisterData.Arguments.Count > 3)
                        {
                            if (registerData.Arguments.Count != unregisterData.Arguments.Count + 1)
                            {
                                return false;
                            }

                            for (var idx = 2; idx < registerData.Arguments.Count; ++idx)
                            {
                                if (registerData.Arguments[idx].IsLocal is false &&
                                    registerData.Arguments[idx].UniqueId != unregisterData.Arguments[idx - 1].UniqueId)
                                {
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return true;
                }))
            {
                valid = true;
            }

            if (valid is not true)
            {
                if (_unRegisterAllData.Any(d =>
                    {
                        return registerData.Arguments.Count >= 4 && registerData.Arguments[3].UniqueId == d.Target.UniqueId;
                    }))
                {
                    valid = true;
                }
            }

            if (valid is not true)
            {
                context.Report(
                    DiagnosticCode.ReferenceLeak,
                    $"Missing UnRegisterGlobalEvent call for {registerData.Arguments[0].Name}",
                    registerData.CallExprSyntax.PrefixExpr.Range
                );
            }
        }

        foreach (var registerData in _targetEventRegisterData)
        {
            valid = false;
            if (_targetEventUnRegisterData.Any(unregisterData =>
                {
                    if (registerData.Arguments[1].IsLocal is false &&
                        registerData.Arguments[1].UniqueId != unregisterData.Arguments[1].UniqueId)
                    {
                        return false;
                    }

                    if (registerData.Returns.Count > 0 && unregisterData.Arguments.Count == 3)
                    {
                        if (registerData.Returns[0].UniqueId == unregisterData.Arguments[2].UniqueId)
                        {
                            return true;
                        }
                    }

                    if (registerData.Arguments.Count >= 5)
                    {
                        if (unregisterData.Arguments.Count == 4)
                        {
                            for (var idx = 2; idx < unregisterData.Arguments.Count; ++idx)
                            {
                                if (registerData.Arguments[idx + 1].IsLocal is false &&
                                    registerData.Arguments[idx + 1].UniqueId != unregisterData.Arguments[idx].UniqueId)
                                {
                                    return false;
                                }
                            }
                        }
                        else if (unregisterData.Arguments.Count > 4)
                        {
                            if (registerData.Arguments.Count != unregisterData.Arguments.Count + 1)
                            {
                                return false;
                            }

                            for (var idx = 3; idx < registerData.Arguments.Count; ++idx)
                            {
                                if (registerData.Arguments[idx].IsLocal is false &&
                                    registerData.Arguments[idx].UniqueId != unregisterData.Arguments[idx - 1].UniqueId)
                                {
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }

                    return true;
                }))
            {
                valid = true;
            }

            if (valid is not true)
            {
                if (_unRegisterAllData.Any(d =>
                    {
                        return registerData.Arguments.Count >= 5 && registerData.Arguments[4].UniqueId == d.Target.UniqueId;
                    }))
                {
                    valid = true;
                }
            }

            if (valid is not true)
            {
                context.Report(
                    DiagnosticCode.ReferenceLeak,
                    $"Missing UnRegisterTargetEvent call for {registerData.Arguments[1].Name}",
                    registerData.CallExprSyntax.PrefixExpr.Range
                );
            }
        }
    }

    private void AnalyzePairCallSyntax(DiagnosticContext context, LuaCallExprSyntax callExprSyntax,
        LuaSymbol callSymbol)
    {
        if (!(callExprSyntax.Name is "RegisterGlobalEvent" or "UnRegisterGlobalEvent" or "RegisterTargetEvent" or "UnRegisterTargetEvent"))
        {
            return;
        }
        if (callExprSyntax.Name is "RegisterGlobalEvent" or "UnRegisterGlobalEvent")
        {
            var firstArg = callExprSyntax.ArgList?.ArgList.ToList()[0];
            var argDeclaration = context.SearchContext.FindDeclaration(firstArg) ?? new LuaSymbol(firstArg.ToString(), Builtin.Unknown, new VirtualInfo());

            if (firstArg is LuaLiteralExprSyntax {Literal.Kind: LuaTokenKind.TkNil})
            {
                context.Report(
                    DiagnosticCode.ReferenceLeak,
                    $"event is nil: {firstArg.Text}",
                    firstArg.Range
                );
                return;
            }
        }
        else if (callExprSyntax.Name is "RegisterTargetEvent" or "UnRegisterTargetEvent")
        {
            var eventArg = callExprSyntax.ArgList?.ArgList.ToList()[1];
            var argDeclaration = context.SearchContext.FindDeclaration(eventArg) ?? new LuaSymbol(eventArg.ToString(), Builtin.Unknown, new VirtualInfo());
            if (eventArg is LuaLiteralExprSyntax {Literal.Kind: LuaTokenKind.TkNil})
            {
                context.Report(
                    DiagnosticCode.ReferenceLeak,
                    $"event is nil: {eventArg.Text}",
                    eventArg.Range
                );
                return;
            }
        }

        FunctionData data = new();
        data.CallExprSyntax = callExprSyntax;
        var args = callExprSyntax.ArgList?.ArgList.ToList();
        foreach (var arg in args)
        {
            var declaration = context.SearchContext.FindDeclaration(arg) ?? new LuaSymbol(arg.ToString(), Builtin.Unknown, new VirtualInfo());
            data.Arguments.Add(declaration);
        }

        if (callExprSyntax.Parent is LuaAssignStatSyntax assignStatSyntax && assignStatSyntax.VarList.ToList().Count > 0)
        {
            var ret = assignStatSyntax.VarList.ToList().First();
            var retSymbol = context.SearchContext.FindDeclaration(ret) ?? new LuaSymbol(ret.ToString(), Builtin.Unknown, new VirtualInfo());
            data.Returns.Add(retSymbol);
        }

        switch (callExprSyntax.Name)
        {
            case "RegisterGlobalEvent":
                _globalEventRegisterData.Add(data);
                break;
            case "UnRegisterGlobalEvent":
                _globalEventUnRegisterData.Add(data);
                break;
            case "RegisterTargetEvent":
                _targetEventRegisterData.Add(data);
                break;
            case "UnRegisterTargetEvent":
                _targetEventUnRegisterData.Add(data);
                break;
        }
    }

    private void AnalyzeUnRegisterAllCallSyntax(DiagnosticContext context, LuaCallExprSyntax callExprSyntax,
        LuaSymbol callSymbol)
    {
        if (callExprSyntax.Name is "UnRegisterAll")
        {
            UnRegisterAllData data = new();
            var firstArg = callExprSyntax.ArgList?.ArgList.ToList()[0];
            var declaration = context.SearchContext.FindDeclaration(firstArg);
            if (declaration is not null)
            {
                data.Target = declaration;
            }

            _unRegisterAllData.Add(data);
        }
    }
}
