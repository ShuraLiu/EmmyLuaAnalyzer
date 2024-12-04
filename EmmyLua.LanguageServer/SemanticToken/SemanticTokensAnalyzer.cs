using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.EMMA;
using EmmyLua.CodeAnalysis.Compilation.Semantic;
using EmmyLua.CodeAnalysis.Compilation.Symbol;
using EmmyLua.CodeAnalysis.Syntax.Kind;
using EmmyLua.CodeAnalysis.Syntax.Node;
using EmmyLua.CodeAnalysis.Syntax.Node.SyntaxNodes;
using EmmyLua.CodeAnalysis.Type;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Common;
using EmmyLua.LanguageServer.Framework.Protocol.Message.SemanticToken;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Util;

namespace EmmyLua.LanguageServer.SemanticToken;

public class SemanticTokensAnalyzer
{
    public SemanticTokensLegend Legend { get; }

    public bool MultiLineTokenSupport { get; set; }

    public SemanticTokensAnalyzer()
    {
        Legend = new()
        {
            TokenTypes = TokenTypes,
            TokenModifiers = TokenModifiers
        };
    }

    private List<string> TokenTypes { get; } =
    [
        SemanticTokenTypes.Namespace,
        SemanticTokenTypes.Type,
        SemanticTokenTypes.Class,
        SemanticTokenTypes.Enum,
        SemanticTokenTypes.Interface,
        SemanticTokenTypes.Struct,
        SemanticTokenTypes.TypeParameter,
        SemanticTokenTypes.Parameter,
        SemanticTokenTypes.Variable,
        SemanticTokenTypes.Property,
        SemanticTokenTypes.EnumMember,
        SemanticTokenTypes.Event,
        SemanticTokenTypes.Function,
        SemanticTokenTypes.Method,
        SemanticTokenTypes.Macro,
        SemanticTokenTypes.Keyword,
        SemanticTokenTypes.Modifier,
        SemanticTokenTypes.Comment,
        SemanticTokenTypes.String,
        SemanticTokenTypes.Number,
        SemanticTokenTypes.Regexp,
        SemanticTokenTypes.Operator,
        SemanticTokenTypes.Decorator,
        EmmySemanticTokenTypes.Self,
        EmmySemanticTokenTypes.G,
        EmmySemanticTokenTypes.UpValue
    ];

    private List<string> TokenModifiers { get; } =
    [
        SemanticTokenModifiers.Declaration,
        SemanticTokenModifiers.Definition,
        SemanticTokenModifiers.Readonly,
        SemanticTokenModifiers.Static,
        SemanticTokenModifiers.Deprecated,
        SemanticTokenModifiers.Abstract,
        SemanticTokenModifiers.Async,
        SemanticTokenModifiers.Modification,
        SemanticTokenModifiers.Documentation,
        SemanticTokenModifiers.DefaultLibrary,
    ];

    public List<uint> Tokenize(SemanticModel semanticModel, bool isVscode, CancellationToken cancellationToken)
    {
        var innerBuilder = new SemanticTokensBuilder(TokenTypes, TokenModifiers);
        var builder = new SemanticBuilderWrapper(innerBuilder, semanticModel.Document, MultiLineTokenSupport);
        var document = semanticModel.Document;
        var syntaxTree = document.SyntaxTree;
        try
        {
            var commentNodeOrToken = syntaxTree.SyntaxRoot.Descendants.OfType<LuaCommentSyntax>()
                .SelectMany(it => it.DescendantsWithToken);
            foreach (var nodeOrToken in commentNodeOrToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return [];
                }

                switch (nodeOrToken)
                {
                    case LuaSyntaxToken token:
                    {
                        TokenizeToken(builder, token, isVscode);
                        break;
                    }
                    case LuaSyntaxNode node:
                    {
                        TokenizeNode(builder, node);
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }

        try
        { 
            LuaSymbol? moduleClassSymbol = null;
            LuaBlockSyntax? block = syntaxTree.SyntaxRoot.Descendants.OfType<LuaBlockSyntax>().FirstOrDefault();
            if (block is not null)
            {
                LuaReturnStatSyntax? returnStat = block.FirstChild<LuaReturnStatSyntax>();
                if (returnStat is not null)
                {
                    var node = returnStat.ChildrenNode.First();
                    moduleClassSymbol = semanticModel.Context.FindDeclaration(node);
                    builder.Push(node, EmmySemanticTokenTypes.Class);
                }
            }
            
            var isModuleFile = (moduleClassSymbol is not null);
            
            var nodes = syntaxTree.SyntaxRoot.Descendants.Where(e => e is not LuaCommentSyntax)
                .SelectMany(it => it.DescendantsWithToken);
            foreach (var nodeOrToken in nodes)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return [];
                }

                switch (nodeOrToken)
                {
                    case LuaSyntaxToken token:
                    {
                        TokenizeToken(semanticModel, builder, token, isVscode);
                        break;
                    }
                    case LuaSyntaxNode node:
                    {
                        // if (semanticModel.Context.FindDeclaration(node) == moduleClassSymbol && node.Parent is LuaBlockSyntax)
                        // {
                        //     builder.Push(node, EmmySemanticTokenTypes.Class);
                        // }
                        // else
                        {
                            TokenizeNode(semanticModel, builder, node, moduleClassSymbol);
                        }
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }

        return builder.Build();
    }

    public List<uint> TokenizeByRange(SemanticModel semanticModel, bool isVscode, DocumentRange range,
        CancellationToken cancellationToken)
    {
        var innerBuilder = new SemanticTokensBuilder(TokenTypes, TokenModifiers);
        var builder = new SemanticBuilderWrapper(innerBuilder, semanticModel.Document, MultiLineTokenSupport);
        var document = semanticModel.Document;
        var syntaxTree = document.SyntaxTree;
        try
        {
            var sourceRange = range.ToSourceRange(document);
            foreach (var nodeOrToken in syntaxTree.SyntaxRoot.DescendantsInRange(sourceRange))
            {
                switch (nodeOrToken)
                {
                    case LuaSyntaxToken token:
                    {
                        TokenizeToken(builder, token, isVscode);
                        break;
                    }
                    case LuaSyntaxNode node:
                    {
                        TokenizeNode(builder, node);
                        break;
                    }
                }
            }
            
            if (cancellationToken.IsCancellationRequested)
            {
                return [];
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }

        return builder.Build();
    }

    private void TokenizeToken(SemanticModel semanticModel, SemanticBuilderWrapper builder, LuaSyntaxToken token, bool isVscode)
    {
        if (token is {Kind:(LuaTokenKind.TkFunction or LuaTokenKind.TkAnd or LuaTokenKind.TkBreak
                or LuaTokenKind.TkDo or LuaTokenKind.TkElse or LuaTokenKind.TkElseIf or LuaTokenKind.TkEnd
                or LuaTokenKind.TkFalse or LuaTokenKind.TkFor or LuaTokenKind.TkIf or LuaTokenKind.TkGoto
                or LuaTokenKind.TkIn or LuaTokenKind.TkLocal or LuaTokenKind.TkNil or LuaTokenKind.TkNot
                or LuaTokenKind.TkOr or LuaTokenKind.TkRepeat or LuaTokenKind.TkReturn or LuaTokenKind.TkThen
                or LuaTokenKind.TkTrue or LuaTokenKind.TkUntil or LuaTokenKind.TkWhile)})
        {
            builder.Push(token, EmmySemanticTokenTypes.Keyword);
        }

        if (token is {Kind: LuaTokenKind.TkString})
        {
            builder.Push(token, EmmySemanticTokenTypes.String);
        }
        else if (token is { Kind: LuaTokenKind.TkInt or LuaTokenKind.TkFloat })
        {
            builder.Push(token, EmmySemanticTokenTypes.Number);
        }
    }
    private void TokenizeToken(SemanticBuilderWrapper builder, LuaSyntaxToken token, bool isVscode)
    {
        var tokenKind = token.Kind;
        switch (tokenKind)
        {
            case LuaTokenKind.TkString:
            case LuaTokenKind.TkLongString:
            {
                builder.Push(token, SemanticTokenTypes.String);
                break;
            }
            case LuaTokenKind.TkDocOr:
            case LuaTokenKind.TkConcat:
            case LuaTokenKind.TkEq:
            case LuaTokenKind.TkNe:
            case LuaTokenKind.TkLe:
            case LuaTokenKind.TkGe:
            case LuaTokenKind.TkDocMatch:
            case LuaTokenKind.TkLeftBracket:
            case LuaTokenKind.TkRightBracket:
            case LuaTokenKind.TkLeftParen:
            case LuaTokenKind.TkRightParen:
            case LuaTokenKind.TkLeftBrace:
            case LuaTokenKind.TkRightBrace:
            case LuaTokenKind.TkDots:
            case LuaTokenKind.TkComma:
            case LuaTokenKind.TkDot:
            {
                builder.Push(token, SemanticTokenTypes.Operator);
                break;
            }
            case LuaTokenKind.TkInt:
            case LuaTokenKind.TkFloat:
            case LuaTokenKind.TkComplex:
            {
                builder.Push(token, SemanticTokenTypes.Number);
                break;
            }
            case LuaTokenKind.TkDocDetail:
            case LuaTokenKind.TkUnknown:
            {
                builder.Push(token, SemanticTokenTypes.Comment);
                break;
            }
            case LuaTokenKind.TkTypeTemplate:
            {
                builder.Push(token, SemanticTokenTypes.String, SemanticTokenModifiers.Abstract);
                break;
            }
            case LuaTokenKind.TkTagAlias:
            case LuaTokenKind.TkTagClass:
            case LuaTokenKind.TkTagEnum:
            case LuaTokenKind.TkTagAs:
            case LuaTokenKind.TkTagField:
            case LuaTokenKind.TkTagInterface:
            case LuaTokenKind.TkTagModule:
            case LuaTokenKind.TkTagParam:
            case LuaTokenKind.TkTagReturn:
            case LuaTokenKind.TkTagSee:
            case LuaTokenKind.TkTagType:
            case LuaTokenKind.TkTagAsync:
            case LuaTokenKind.TkTagCast:
            case LuaTokenKind.TkTagDeprecated:
            case LuaTokenKind.TkTagGeneric:
            case LuaTokenKind.TkTagNodiscard:
            case LuaTokenKind.TkTagOperator:
            case LuaTokenKind.TkTagOther:
            case LuaTokenKind.TkTagOverload:
            case LuaTokenKind.TkTagVisibility:
            case LuaTokenKind.TkTagDiagnostic:
            case LuaTokenKind.TkTagMeta:
            case LuaTokenKind.TkTagVersion:
            case LuaTokenKind.TkTagMapping:
            case LuaTokenKind.TkDocEnumField:
            case LuaTokenKind.TkDocVisibility:
            {
                if (!isVscode)
                {
                    builder.Push(token, SemanticTokenTypes.Decorator, SemanticTokenModifiers.Documentation);
                }

                break;
            }
            case LuaTokenKind.TkNormalStart: // -- or ---
            case LuaTokenKind.TkLongCommentStart: // --[[
            case LuaTokenKind.TkDocLongStart: // --[[@
            case LuaTokenKind.TkDocStart: // ---@
            {
                builder.Push(token, SemanticTokenTypes.Comment);
                break;
            }
        }
    }

    private void TokenizeNode(SemanticBuilderWrapper builder, LuaSyntaxNode node)
    {
        switch (node)
        {
            case LuaDocTagClassSyntax docTagClassSyntax:
            {
                if (docTagClassSyntax.Name is { } name)
                {
                    builder.Push(name, SemanticTokenTypes.Class, SemanticTokenModifiers.Declaration);
                }

                break;
            }
            case LuaDocTagEnumSyntax docTagEnumSyntax:
            {
                if (docTagEnumSyntax.Name is { } name)
                {
                    builder.Push(name, SemanticTokenTypes.Enum, SemanticTokenModifiers.Declaration);
                }

                break;
            }
            case LuaDocTagInterfaceSyntax docTagInterfaceSyntax:
            {
                if (docTagInterfaceSyntax.Name is { } name)
                {
                    builder.Push(name, SemanticTokenTypes.Interface, SemanticTokenModifiers.Declaration);
                }

                break;
            }
            case LuaDocTagAliasSyntax docTagAliasSyntax:
            {
                if (docTagAliasSyntax.Name is { } name)
                {
                    builder.Push(name, SemanticTokenTypes.Type, SemanticTokenModifiers.Declaration);
                }

                break;
            }
            case LuaDocNameTypeSyntax nameTypeSyntax:
            {
                if (nameTypeSyntax.Name is { } name)
                {
                    builder.Push(name, SemanticTokenTypes.Type);
                }

                break;
            }
            case LuaDocTagParamSyntax docTagParamSyntax:
            {
                if (docTagParamSyntax.Name is {} name)
                {
                    builder.Push(name, SemanticTokenTypes.Parameter, SemanticTokenModifiers.Documentation);
                }
                break;
            }
            case LuaDocFieldSyntax docFieldSyntax:
            {
                if (docFieldSyntax.FieldElement is { } name)
                {
                    builder.Push(name, SemanticTokenTypes.Property, SemanticTokenModifiers.Documentation);
                }

                break;
            }
        }
    }

    private void TokenizeNode(SemanticModel semanticModel, SemanticBuilderWrapper builder, LuaSyntaxNode node, LuaSymbol? moduleClassSymbol)
    {
        switch (node)
        {
            case LuaFuncStatSyntax funcStatSyntax:
            {
                if (funcStatSyntax.IsLocal)
                {
                    if (funcStatSyntax.NameElement is { } name)
                    {
                        builder.Push(name, EmmySemanticTokenTypes.Function, SemanticTokenModifiers.Declaration);
                    }
                }
                else
                {
                    if (funcStatSyntax.NameElement is { } name)
                    {
                        builder.Push(name, EmmySemanticTokenTypes.Method, SemanticTokenModifiers.Declaration);
                    }
                }
                break;
            }
            case LuaLocalStatSyntax localStatSyntax:
            {
                if (localStatSyntax.IsLocalDeclare)
                {
                    foreach (var name in localStatSyntax.NameList)
                    {
                        builder.Push(name, EmmySemanticTokenTypes.Variable);
                    }
                }
                break;
            }
            case LuaTableFieldSyntax tableFieldSyntax:
            {
                if (tableFieldSyntax.KeyElement is { } name)
                {
                    builder.Push(name, EmmySemanticTokenTypes.Property);
                }
                break;
            }
            case LuaIndexExprSyntax indexExprSyntax:
            {
                LuaSymbol? symbol = semanticModel.Context.FindDeclaration(indexExprSyntax);
                if (symbol is not null)
                {
                    switch (symbol.Info)
                    {
                        case MethodInfo:
                        {
                            if (indexExprSyntax.DotOrColonIndexName is { } name)
                            {
                                if (symbol.Type is LuaMethodType)
                                {
                                    builder.Push(name, EmmySemanticTokenTypes.Method);
                                }
                            }

                            break;
                        }
                        case DocFieldInfo:
                        case IndexInfo:
                        {
                            if (indexExprSyntax.DotOrColonIndexName is { } name)
                            {
                                if (symbol.Type is LuaMethodType)
                                {
                                    builder.Push(name, EmmySemanticTokenTypes.Method);
                                }
                                else
                                {
                                    builder.Push(name, EmmySemanticTokenTypes.Property);
                                }
                            }
                            break;
                        }
                    }
                }

                break;
            }
            case LuaParamDefSyntax paramDefSyntax:
            {
                if (paramDefSyntax.Name is { } name)
                {
                    builder.Push(name, EmmySemanticTokenTypes.Parameter, SemanticTokenModifiers.Declaration);
                    // LuaSymbol? symbol = semanticModel.Context.FindDeclaration(paramDefSyntax);
                    // if (symbol != null)
                    // {
                    //     foreach (var referenceResult in semanticModel.Context.FindReferences(symbol))
                    //     {
                    //         builder.Push(referenceResult.Element, EmmySemanticTokenTypes.Parameter);
                    //     }
                    // }
                    
                }
                break;
            }
            // case LuaCallExprSyntax callExprSyntax:
            // {
            //     builder.Push(callExprSyntax, EmmySemanticTokenTypes.Method);
            //     Console.WriteLine("1");
            //     break;
            // }
            case LuaNameExprSyntax nameExprSyntax:
            {
                if (nameExprSyntax.Name is { } name)
                {
                    LuaSymbol? symbol = semanticModel.Context.FindDeclaration(nameExprSyntax);
                    if (symbol is not null)
                    {
                        switch (symbol.Info)
                        {
                            case ParamInfo paramInfo:
                            {
                                builder.Push(name, EmmySemanticTokenTypes.Parameter);
                                break;
                            }
                            case MethodInfo methodInfo:
                            {
                                if (symbol.Type is LuaGenericMethodType)
                                {
                                    builder.Push(name, EmmySemanticTokenTypes.Function, SemanticTokenModifiers.DefaultLibrary);
                                }
                                else if (symbol.Type is LuaMethodType)
                                {
                                    if (symbol.IsGlobal)
                                    {
                                        builder.Push(name, EmmySemanticTokenTypes.Function, SemanticTokenModifiers.DefaultLibrary);
                                    }
                                    else
                                    {
                                        builder.Push(name, EmmySemanticTokenTypes.Method);
                                    }
                                }
                                break;
                            }
                            case GlobalInfo globalInfo:
                            case LocalInfo localInfo:
                            {
                                if (name.Text is EmmySemanticTokenTypes.Self)
                                {
                                    builder.Push(name, EmmySemanticTokenTypes.Self);
                                }
                                else if (name.Text is EmmySemanticTokenTypes.G)
                                {
                                    builder.Push(name, EmmySemanticTokenTypes.G);
                                }
                                else
                                {
                                    if (symbol.UniqueId == moduleClassSymbol?.UniqueId)
                                    {
                                        builder.Push(name, EmmySemanticTokenTypes.Class);
                                    }
                                    else if (symbol.Type is LuaNamedType namedType)
                                    {
                                        var typeInfo = semanticModel.Context.Compilation.TypeManager.FindTypeInfo(namedType);
                                        if (typeInfo?.Kind is NamedTypeKind.Class)
                                        {
                                            builder.Push(name, EmmySemanticTokenTypes.Class);
                                        }
                                        else if (typeInfo?.Kind is NamedTypeKind.Interface)
                                        {
                                            builder.Push(name, EmmySemanticTokenTypes.Interface);
                                        }
                                        else if (typeInfo?.Kind is NamedTypeKind.Enum)
                                        {
                                            builder.Push(name, EmmySemanticTokenTypes.Enum);
                                        }
                                    }
                                    else if (symbol.Type is LuaGenericMethodType genericMethodType)
                                    {
                                        builder.Push(name, EmmySemanticTokenTypes.Function, SemanticTokenModifiers.DefaultLibrary);
                                    }
                                    else if (symbol.Type is GlobalNameType globalNameType)
                                    {
                                        builder.Push(name, EmmySemanticTokenTypes.G);
                                    }
                                    else if (symbol.Type is LuaElementType elementType)
                                    {
                                        LuaType? type = semanticModel.Context.Compilation.TypeManager.GetBaseType(elementType.Id);
                                        if (type is LuaNamedType namedType1)
                                        {
                                            var typeInfo = semanticModel.Context.Compilation.TypeManager.FindTypeInfo(namedType1);
                                            if (typeInfo?.Kind is NamedTypeKind.Class)
                                            {
                                                builder.Push(name, EmmySemanticTokenTypes.Class);
                                            }
                                            else if (typeInfo?.Kind is NamedTypeKind.Interface)
                                            {
                                                builder.Push(name, EmmySemanticTokenTypes.Interface);
                                            }
                                            else if (typeInfo?.Kind is NamedTypeKind.Enum)
                                            {
                                                builder.Push(name, EmmySemanticTokenTypes.Enum);
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                            default:
                            {
                                // if (symbol.IsLocal)
                                // {
                                //     //需要判断是不是一个Class
                                //     builder.Push(name, EmmySemanticTokenTypes.Variable);
                                // }
                                // else
                                // {
                                //     builder.Push(name, EmmySemanticTokenTypes.Variable, SemanticTokenModifiers.Static);
                                // }
                            }
                                break;
                        }
                    }
                }
                break;
            }
        }
    }
    
    private void TokenizeNodeInternal(SemanticModel semanticModel, SemanticBuilderWrapper builder,
        LuaFuncStatSyntax funcStatSyntax)
    {
        if (funcStatSyntax.NameElement is { } name)
        {
            builder.Push(name, EmmySemanticTokenTypes.Method);
        }
        
        // if (funcStatSyntax.IsMethod)
        // {
        //     if (funcStatSyntax.IndexExpr is not null)
        //     {
        //         foreach (LuaSyntaxElement child in funcStatSyntax.ChildrenWithTokens)
        //         {
        //             if (child is LuaIndexExprSyntax indexExprSyntax)
        //             {
        //                 builder.Push(indexExprSyntax, EmmySemanticTokenTypes.Method);
        //                 if (indexExprSyntax.PrefixExpr is {} prefixExpr)
        //                 {
        //                     builder.Push(prefixExpr, EmmySemanticTokenTypes.Class);
        //                 }
        //             }
        //             else if (child is LuaClosureExprSyntax closureExprSyntax)
        //             {
        //                 TokenizeNodeInternal(semanticModel, builder, closureExprSyntax);
        //             }
        //         }
        //     }
        // }
    }

    private void TokenizeNodeInternal(SemanticModel semanticModel, SemanticBuilderWrapper builder,
        LuaClosureExprSyntax closureExprSyntax)
    {
        Collection<LuaSymbol> ParamSymbols = new Collection<LuaSymbol>();
        if (closureExprSyntax.ParamList is {} paramList)
        {
            foreach (LuaParamDefSyntax Param in paramList.Params)
            {
                builder.Push(Param, EmmySemanticTokenTypes.Parameter);
                var Symbol = semanticModel.Context.FindDeclaration(Param);
                if (Symbol is not null)
                {
                    ParamSymbols.Add(Symbol);
                }
            }
        }
        
        var elements = closureExprSyntax.ChildrenWithTokens.SelectMany(it => it.DescendantsWithToken);
        foreach (var element in elements)
        {
            var symbol = semanticModel.Context.FindDeclaration(element);
            if (symbol is not null && ParamSymbols.Contains(symbol))
            {
                builder.Push(element, EmmySemanticTokenTypes.Parameter);
            }
        }
    }
}