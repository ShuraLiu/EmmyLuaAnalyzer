using EmmyLua.CodeAnalysis.Compilation;
using EmmyLua.CodeAnalysis.Syntax.Kind;
using EmmyLua.CodeAnalysis.Syntax.Node;
using EmmyLua.CodeAnalysis.Syntax.Node.SyntaxNodes;
using EmmyLua.CodeAnalysis.Type;

namespace EmmyLua.CodeAnalysis.Diagnostics.Checkers;

public class MissingOverriddenChecker(LuaCompilation compilation) : DiagnosticCheckerBase(compilation, [DiagnosticCode.MissingOverridden])
{
    public override void Check(DiagnosticContext context)
    {
        LuaBlockSyntax? rootBlock = null;
        LuaReturnStatSyntax? rootRet = null;
        String? moduleName = null;
        //找根节点
        foreach (var block in context.Document.SyntaxTree.SyntaxRoot.Descendants.OfType<LuaBlockSyntax>())
        {
            if (block.Parent is not null && block.Parent.Equals(context.Document.SyntaxTree.SyntaxRoot))
            {
                rootBlock = block;
            }
        }

        //找当前文件返回的Module
        foreach (var ret in context.Document.SyntaxTree.SyntaxRoot.Descendants.OfType<LuaReturnStatSyntax>())
        {
            if (ret.Parent is not null && ret.Parent.Equals(rootBlock))
            {
                rootRet = ret;
            }
        }

        //判断Module是不是UnLuaInterface类
        foreach (var localStat in context.Document.SyntaxTree.SyntaxRoot.Descendants.OfType<LuaLocalStatSyntax>())
        {
            if (localStat.Parent is not null && localStat.Parent.Equals(rootBlock))
            {
                if (localStat.ChildrenNode.Count() == 2)
                {
                    var localName = localStat.ChildrenNode.ToArray()[0];
                    var expr = localStat.ChildrenNode.ToArray()[1];
                    if (localName is LuaLocalNameSyntax name && expr is LuaCallExprSyntax { Name : {} callName})
                    {
                        if ((callName.Equals("PaperUEClass") || callName.Equals("Class")) && rootRet is not null && name.Text.ToString() ==  rootRet.ChildrenNode.ToArray()[0].Text.ToString())
                        {
                            moduleName = name.Text.ToString();
                            break;
                        }
                    }
                }
            }
        }
        //检查需要覆写的函数
        foreach (var func in context.Document.SyntaxTree.SyntaxRoot.Descendants.OfType<LuaFuncStatSyntax>())
        {
            if (func.Parent is not null && func.Parent.Equals(rootBlock) && func.IsColonFunc && func.IsMethod)
            {
                var IndexExpr = func.IndexExpr;
                if (IndexExpr is not null && IndexExpr.PrefixExpr.Text.ToString() == moduleName)
                {
                    if (IndexExpr.Name == "Construct" || IndexExpr.Name == "Destruct")
                    {
                        bool find = FindOverriddenInFunc(func);
                        if (find is false)
                        {
                            context.Report(
                                DiagnosticCode.MissingOverridden,
                                $"Function missing overridden call self.Overridden.{IndexExpr.Name}(self)",
                                IndexExpr.Range
                            );
                        }
                    }
                }
            }
        }
    }

    protected bool FindOverriddenInFunc(LuaFuncStatSyntax func)
    {
        var funcName = func.IndexExpr.Name;
        var body = func.ClosureExpr;
        foreach (var callExpr in body.Descendants.OfType<LuaCallExprSyntax>())
        {
            if (callExpr.Name == funcName && callExpr.PrefixExpr is LuaIndexExprSyntax prefixExpr)
            {
                if (prefixExpr.PrefixExpr is LuaIndexExprSyntax callerExpr && callerExpr.Name == "Overridden")
                {
                    return true;
                }
            }
        }

        return false;
    }
}
