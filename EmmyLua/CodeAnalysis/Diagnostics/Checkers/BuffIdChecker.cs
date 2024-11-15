using System.Collections.ObjectModel;
using EmmyLua.CodeAnalysis.Compilation;
using EmmyLua.CodeAnalysis.Syntax.Node.SyntaxNodes;

namespace EmmyLua.CodeAnalysis.Diagnostics.Checkers;

public class BuffIdChecker(LuaCompilation compilation) : DiagnosticCheckerBase(compilation, [DiagnosticCode.InvalidBuffID])
{
    public override void Check(DiagnosticContext context)
    {
        Collection<long> validBuffIDs = Compilation.Project.Features.CustomDiagnosticConfig.BuffIdDiagnosticConfig.ValidBuffIds;
        var designerScriptPaths = Compilation.Project.Features.CustomDiagnosticConfig.BuffIdDiagnosticConfig.DesignerScriptPaths;

        var path = context.Document.Path;
        var needCheck = false;
        foreach (var root in designerScriptPaths)
        {
            if (path.Replace("\\", "/").StartsWith(root.Replace("\\", "/")))
            {
                needCheck = true;
                break;
            }
        }

        if (!needCheck) return;

        foreach (var func in context.Document.SyntaxTree.SyntaxRoot.Descendants.OfType<LuaCallExprSyntax>())
        {
            var funcName = func.Name;
            var infos =Compilation.Project.Features.CustomDiagnosticConfig.BuffIdDiagnosticConfig.FunctionInfos
                .Where(info => info.FunctionName == funcName).ToList();
            foreach (var info in infos)
            {
                if (func.ArgList is null || func.ArgList.ArgList.ToList().Count < info.ArgPositions.Count)
                {
                    context.Report(
                        DiagnosticCode.InvalidBuffID,
                        $"Function argument count error",
                        func.Range
                    );
                    continue;
                }
                foreach (var pos in info.ArgPositions)
                {
                    LuaLiteralExprSyntax arg = func.ArgList.ArgList.ElementAt(pos) as LuaLiteralExprSyntax;
                    if (arg is not null)
                    {
                        LuaIntegerToken argToken = arg.Literal as LuaIntegerToken;
                        if (argToken is not null)
                        {
                            if (!validBuffIDs.Contains(argToken.Value))
                            {
                                context.Report(
                                    DiagnosticCode.InvalidBuffID,
                                    $"Buff id does not exists: {argToken.Value}",
                                    arg.Range
                                );
                            }
                        }
                    }
                }
            }
        }
    }
}
