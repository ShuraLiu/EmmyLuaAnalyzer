using System.Collections.ObjectModel;
using EmmyLua.CodeAnalysis.Compilation.Symbol;
using EmmyLua.CodeAnalysis.Syntax.Node.SyntaxNodes;
using EmmyLua.CodeAnalysis.Type;

namespace EmmyLua.CodeAnalysis.Diagnostics.Checkers.PairwiseFunctionCheckers;

public class SchedulerSystemChecker() : PairwiseFunctionCheckerBase()
{
    private Collection<FunctionData> _scheduleData = new();
    private Collection<FunctionData> _unscheduleData = new();
    private Collection<FunctionData> _unscheduleAllData = new();

    public override void Prepare()
    {
        _scheduleData.Clear();
        _unscheduleData.Clear();
    }

    public override bool CanCheck(LuaCallExprSyntax callExprSyntax, LuaSymbol prefixExprSymbol)
    {
        return prefixExprSymbol?.Name is "SchedulerSystem";
    }

    public override void AnalysisCallSyntax(DiagnosticContext context, LuaCallExprSyntax callExprSyntax, LuaSymbol callSymbol)
    {
        FunctionData data = new();
        data.CallExprSyntax = callExprSyntax;
        if (callExprSyntax.Name.StartsWith("Schedule"))
        {
            if (callExprSyntax.Parent is LuaAssignStatSyntax assignStatSyntax && assignStatSyntax.VarList.ToList().Count > 0)
            {
                var ret = assignStatSyntax.VarList.ToList().First();
                var retSymbol = context.SearchContext.FindDeclaration(ret) ?? new LuaSymbol(ret.ToString(), Builtin.Unknown, new VirtualInfo());
                data.Returns.Add(retSymbol);

            }
            var args = callExprSyntax.ArgList?.ArgList.ToList();
            foreach (var arg in args)
            {
                var declaration = context.SearchContext.FindDeclaration(arg) ?? new LuaSymbol(arg.ToString(), Builtin.Unknown, new VirtualInfo());
                data.Arguments.Add(declaration);
            }
            _scheduleData.Add(data);
        }
        else if (callExprSyntax.Name is "Unschedule")
        {
            var args = callExprSyntax.ArgList?.ArgList.ToList();
            foreach (var arg in args)
            {
                var declaration = context.SearchContext.FindDeclaration(arg) ?? new LuaSymbol(arg.ToString(), Builtin.Unknown, new VirtualInfo());
                data.Arguments.Add(declaration);
                _unscheduleData.Add(data);
            }
        }
        else if (callExprSyntax.Name is "UnscheduleAllWithTarget")
        {
            var args = callExprSyntax.ArgList?.ArgList.ToList();
            foreach (var arg in args)
            {
                var declaration = context.SearchContext.FindDeclaration(arg) ?? new LuaSymbol(arg.ToString(), Builtin.Unknown, new VirtualInfo());
                data.Arguments.Add(declaration);
                _unscheduleAllData.Add(data);
            }
        }
    }

    public override void PostAynlysis(DiagnosticContext context)
    {
        var valid = true;
        foreach (var scheduleData in _scheduleData)
        {
            valid = false;
            if (_unscheduleData.Any(unscheduleData =>
                {
                    if (scheduleData.Returns.Count == 1 && unscheduleData.Arguments.Count > 0 && scheduleData.Returns[0] == unscheduleData.Arguments[0])
                    {
                        return true;
                    }

                    return false;
                }))
            {
                valid = true;
            }

            if (!valid)
            {
                if (_unscheduleAllData.Any(unscheduleData =>
                    {
                        bool ret = false;
                        if (unscheduleData.Arguments.Count > 0 && scheduleData.Arguments.Count > 0)
                        {
                            switch (scheduleData.CallExprSyntax.Name)
                            {
                                case "Schedule":
                                {
                                    if (scheduleData.Arguments.Count >= 7 && scheduleData.Arguments[6] == unscheduleData.Arguments[0])
                                    {
                                        ret = true;
                                    }
                                }
                                    break;
                                case "ScheduleOnce":
                                {
                                    if (scheduleData.Arguments.Count >= 3 && scheduleData.Arguments[2] == unscheduleData.Arguments[0])
                                    {
                                        ret = true;
                                    }
                                }
                                    break;
                                case "ScheduleAfterFrames":
                                {
                                    if (scheduleData.Arguments.Count >= 3 && scheduleData.Arguments[2] == unscheduleData.Arguments[0])
                                    {
                                        ret = true;
                                    }
                                }
                                    break;
                                case "ScheduleNextFrame":
                                {
                                    if (scheduleData.Arguments.Count >= 2 && scheduleData.Arguments[1] == unscheduleData.Arguments[0])
                                    {
                                        ret = true;
                                    }
                                }
                                    break;
                                case "ScheduleTick":
                                {
                                    if (scheduleData.Arguments.Count >= 2 && scheduleData.Arguments[1] == unscheduleData.Arguments[0])
                                    {
                                        ret = true;
                                    }
                                }
                                    break;
                                case "ScheduleLoop":
                                {
                                    if (scheduleData.Arguments.Count >= 4 && scheduleData.Arguments[3] == unscheduleData.Arguments[0])
                                    {
                                        ret = true;
                                    }
                                }
                                    break;
                                case "ScheduleTimeLine":
                                {
                                    if (scheduleData.Arguments.Count >= 4 && scheduleData.Arguments[3] == unscheduleData.Arguments[0])
                                    {
                                        ret = true;
                                    }
                                }
                                    break;
                            }
                        }

                        return ret;
                    }))
                {
                    valid = true;
                }
            }

            if (valid is not true)
            {
                context.Report(
                    DiagnosticCode.ReferenceLeak,
                    $"Missing unschedule call for {scheduleData.CallExprSyntax.Name}",
                    scheduleData.CallExprSyntax.PrefixExpr.Range
                );
            }
        }
    }
}
