using System.Collections.ObjectModel;
using EmmyLua.CodeAnalysis.Compilation.Symbol;
using EmmyLua.CodeAnalysis.Syntax.Node.SyntaxNodes;
using EmmyLua.CodeAnalysis.Type;

namespace EmmyLua.CodeAnalysis.Diagnostics.Checkers.PairwiseFunctionCheckers;

public class EventBusChecker() : PairwiseFunctionCheckerBase()
{
    private Collection<FunctionData> _globalEventRegisterData = new();
    private Collection<FunctionData> _globalEventUnRegisterData = new();

    private Collection<FunctionData> _targetEventRegisterData = new();
    private Collection<FunctionData> _targetEventUnRegisterData = new();

    public override void Prepare()
    {
        _globalEventRegisterData.Clear();
        _globalEventUnRegisterData.Clear();
        _targetEventRegisterData.Clear();
        _targetEventUnRegisterData.Clear();
    }

    public override bool CanCheck(LuaCallExprSyntax callExprSyntax, LuaSymbol prefixExprSymbol)
    {
        return prefixExprSymbol?.Name is "EventBus";
    }

    public override void AnalysisCallSyntax(DiagnosticContext context, LuaCallExprSyntax callExprSyntax, LuaSymbol callSymbol)
    {
        if (callExprSyntax.Name is "RegisterGlobalEvent" or "UnRegisterGlobalEvent")
        {
            var firstArg = callExprSyntax.ArgList?.ArgList.ToList()[0];
            var argDeclaration = context.SearchContext.FindDeclaration(firstArg) ?? new LuaSymbol(firstArg.ToString(), Builtin.Unknown, new VirtualInfo());

            if (firstArg is not null && argDeclaration.Type == Builtin.Unknown)
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
            if (eventArg is not null && argDeclaration.Type == Builtin.Unknown)
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

    public override void PostAynlysis(DiagnosticContext context)
    {
        var valid = true;
        foreach (var registerData in _globalEventRegisterData)
        {
            valid = false;
            if (_globalEventUnRegisterData.Any(unregisterData =>
                {
                    if (registerData.Arguments.Count == 4 && unregisterData.Arguments.Count == 3)
                    {
                        return registerData.Arguments[0] == unregisterData.Arguments[0] &&
                               registerData.Arguments[2] == unregisterData.Arguments[1] &&
                               registerData.Arguments[3] == unregisterData.Arguments[2];
                    }
                    if (registerData.Arguments.Count == 3 && unregisterData.Arguments.Count == 2)
                    {
                        return registerData.Arguments[0] == unregisterData.Arguments[0] &&
                               registerData.Arguments[2] == unregisterData.Arguments[1];
                    }

                    return false;
                }))
            {
                valid = true;
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
                    if (registerData.Arguments.Count == 5 && unregisterData.Arguments.Count == 4)
                    {
                        return registerData.Arguments[1] == unregisterData.Arguments[1] &&
                               registerData.Arguments[3] == unregisterData.Arguments[2] &&
                               registerData.Arguments[4] == unregisterData.Arguments[3];
                    }
                    if (registerData.Arguments.Count == 4 && unregisterData.Arguments.Count == 3)
                    {
                        return registerData.Arguments[1] == unregisterData.Arguments[1] &&
                               registerData.Arguments[3] == unregisterData.Arguments[2];
                    }

                    return false;
                }))
            {
                valid = true;
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
}
