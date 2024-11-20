using System.Collections.ObjectModel;
using EmmyLua.CodeAnalysis.Compilation.Symbol;
using EmmyLua.CodeAnalysis.Syntax.Node.SyntaxNodes;
using EmmyLua.CodeAnalysis.Type;

namespace EmmyLua.CodeAnalysis.Diagnostics.Checkers.PairwiseFunctionCheckers;

public class InputSystemChecker() : PairwiseFunctionCheckerBase()
{
    private Collection<FunctionData> _bindActionData = new();
    private Collection<FunctionData> _unbindActionData = new();

    public override void Prepare()
    {
        _bindActionData.Clear();
        _unbindActionData.Clear();
    }

    public override bool CanCheck(LuaCallExprSyntax callExprSyntax, LuaSymbol prefixExprSymbol)
    {
        return prefixExprSymbol?.Name is "InputSystem";
    }

    public override void AnalysisCallSyntax(DiagnosticContext context, LuaCallExprSyntax callExprSyntax, LuaSymbol callSymbol)
    {
        if (callExprSyntax.Name is "BindAction" or "UnbindAction")
        {
            var firstArg = callExprSyntax.ArgList?.ArgList.ToList()[0];
            var argDeclaration = context.SearchContext.FindDeclaration(firstArg) ?? new LuaSymbol(firstArg.ToString(), Builtin.Unknown, new VirtualInfo());

            if (firstArg is not null && argDeclaration.Type == Builtin.Unknown)
            {
                context.Report(
                    DiagnosticCode.ReferenceLeak,
                    $"input action is nil: {firstArg.Text}",
                    firstArg.Range
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
            case "BindAction":
                _bindActionData.Add(data);
                break;
            case "UnbindAction":
                _unbindActionData.Add(data);
                break;
        }
    }

    public override void PostAynlysis(DiagnosticContext context)
    {
        var valid = true;
        foreach (var registerData in _bindActionData)
        {
            valid = false;
            if (_unbindActionData.Any(unregisterData =>
                {
                    if (registerData.Arguments.Count >= 6 && unregisterData.Arguments.Count >= 4)
                    {
                        return registerData.Arguments[0] == unregisterData.Arguments[0] &&
                               registerData.Arguments[1] == unregisterData.Arguments[1] &&
                               registerData.Arguments[4] == unregisterData.Arguments[2] &&
                               registerData.Arguments[5] == unregisterData.Arguments[3];
                    }
                    else if (registerData.Arguments.Count >= 5 && unregisterData.Arguments.Count >= 3)
                    {
                        return registerData.Arguments[0] == unregisterData.Arguments[0] &&
                               registerData.Arguments[1] == unregisterData.Arguments[1] &&
                               registerData.Arguments[4] == unregisterData.Arguments[2];
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
                    $"Missing UnbindAction call for {registerData.Arguments[0].Name}",
                    registerData.CallExprSyntax.PrefixExpr.Range
                );
            }
        }
    }
}
