using EmmyLua.CodeAnalysis.Compilation;
using EmmyLua.CodeAnalysis.Syntax.Node.SyntaxNodes;
using EmmyLua.CodeAnalysis.Diagnostics.Checkers.PairwiseFunctionCheckers;

namespace EmmyLua.CodeAnalysis.Diagnostics.Checkers;

public class ReferenceLeakChecker(LuaCompilation compilation)
    : DiagnosticCheckerBase(compilation, [DiagnosticCode.ReferenceLeak])
{
    private List<PairwiseFunctionCheckerBase> PairwiseFunctionCheckers { get; } =
    [
        new EventBusChecker()
    ];
    public override void Check(DiagnosticContext context)
    {
        foreach (var pairwiseFunctionChecker in PairwiseFunctionCheckers)
        {
            pairwiseFunctionChecker.Prepare();
        }
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
                    foreach (var pairwiseFunctionChecker in PairwiseFunctionCheckers)
                    {
                        if (pairwiseFunctionChecker.CanCheck(callExpr, nameDeclaration))
                        {
                            pairwiseFunctionChecker.AnalysisCallSyntax(context, callExpr, declaration);
                        }
                    }
                }
            }
        }

        foreach (var pairwiseFunctionChecker in PairwiseFunctionCheckers)
        {
            pairwiseFunctionChecker.PostAynlysis(context);
        }
    }
}
