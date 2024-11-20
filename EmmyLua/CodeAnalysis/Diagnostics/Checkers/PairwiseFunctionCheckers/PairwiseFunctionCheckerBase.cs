using System.Diagnostics.CodeAnalysis;
using EmmyLua.CodeAnalysis.Compilation;
using EmmyLua.CodeAnalysis.Compilation.Symbol;
using EmmyLua.CodeAnalysis.Syntax.Node.SyntaxNodes;
using EmmyLua.CodeAnalysis.Type;

namespace EmmyLua.CodeAnalysis.Diagnostics.Checkers.PairwiseFunctionCheckers;

public class FunctionData()
{
    public LuaCallExprSyntax CallExprSyntax  { get; set; }
    public List<LuaSymbol> Arguments { get; } = new();
    public List<LuaSymbol> Returns { get; } = new();
}

public abstract class PairwiseFunctionCheckerBase()
{
    public virtual void Prepare()
    {

    }

    public virtual bool CanCheck(LuaCallExprSyntax callExprSyntax, LuaSymbol prefixExprSymbol)
    {
        return true;
    }
    public virtual void AnalysisCallSyntax(DiagnosticContext context, LuaCallExprSyntax callExprSyntax, LuaSymbol callSymbol)
    {

    }

    public virtual void PostAynlysis(DiagnosticContext context)
    {

    }
}
