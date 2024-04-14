﻿using EmmyLua.CodeAnalysis.Kind;
using EmmyLua.CodeAnalysis.Syntax.Node.SyntaxNodes;
using LanguageServer.ExecuteCommand.Commands;
using LanguageServer.Util;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace LanguageServer.Completion.CompleteProvider;

public class KeywordsProvider : ICompleteProviderBase
{
    private List<CompletionItem> Keywords { get; } = new()
    {
        new CompletionItem()
        {
            Label = "if",
            Kind = CompletionItemKind.Snippet,
            InsertTextMode = InsertTextMode.AdjustIndentation,
            InsertText = "if ${1:condition} then\n\t${0}\nend",
            LabelDetails = new()
            {
                Detail = " (if condition then ... end)"
            },
            InsertTextFormat = InsertTextFormat.Snippet
        },
        new CompletionItem()
        {
            Label = "else",
            Kind = CompletionItemKind.Snippet,
            InsertTextMode = InsertTextMode.AdjustIndentation,
            InsertText = "else\n\t${0}",
            LabelDetails = new()
            {
                Detail = " (else ...)"
            },
            InsertTextFormat = InsertTextFormat.Snippet
        },
        new CompletionItem()
        {
            Label = "elseif",
            Kind = CompletionItemKind.Snippet,
            InsertTextMode = InsertTextMode.AdjustIndentation,
            InsertText = "elseif ${1:condition} then\n\t${0}",
            LabelDetails = new()
            {
                Detail = " (elseif condition then ... )"
            },
            InsertTextFormat = InsertTextFormat.Snippet
        },
        new CompletionItem()
        {
            Label = "then",
            Kind = CompletionItemKind.Snippet,
            InsertTextMode = InsertTextMode.AdjustIndentation,
            InsertText = "then\n\t${0}",
            LabelDetails = new()
            {
                Detail = " (then ... )"
            },
            InsertTextFormat = InsertTextFormat.Snippet
        },
        new CompletionItem() { Label = "end", Kind = CompletionItemKind.Keyword, Detail = "keyword" },
        new CompletionItem()
        {
            Label = "fori",
            Kind = CompletionItemKind.Snippet,
            InsertTextMode = InsertTextMode.AdjustIndentation,
            InsertText = "for ${1:var} = ${2:start}, ${3:finish} do\n\t${0}\nend",
            LabelDetails = new()
            {
                Detail = " (for var = start, finish do ... end)"
            },
            InsertTextFormat = InsertTextFormat.Snippet
        },
        new CompletionItem()
        {
            Label = "forp",
            Kind = CompletionItemKind.Snippet,
            InsertTextMode = InsertTextMode.AdjustIndentation,
            InsertText = "for ${1:var} in pairs(${2:table}) do\n\t${0}\nend",
            LabelDetails = new()
            {
                Detail = " (for k,v in pairs(table) do ... end)"
            },
            InsertTextFormat = InsertTextFormat.Snippet
        },
        new CompletionItem()
        {
            Label = "forip",
            Kind = CompletionItemKind.Snippet,
            InsertTextMode = InsertTextMode.AdjustIndentation,
            InsertText = "for ${1:var} in ipairs(${2:table}) do\n\t${0}\nend",
            LabelDetails = new()
            {
                Detail = " (for i,v in ipairs(table) do ... end)"
            },
            InsertTextFormat = InsertTextFormat.Snippet
        },
        new CompletionItem()
        {
            Label = "in",
            Kind = CompletionItemKind.Snippet,
            InsertTextMode = InsertTextMode.AdjustIndentation,
            InsertText = "in pairs(${1:table}) do\n\t${0}\nend",
            LabelDetails = new()
            {
                Detail = " (in pairs(table) do ... end)"
            },
            InsertTextFormat = InsertTextFormat.Snippet
        },
        new CompletionItem()
        {
            Label = "do",
            Kind = CompletionItemKind.Snippet,
            InsertTextMode = InsertTextMode.AdjustIndentation,
            InsertText = "do\n\t${0}\nend",
            LabelDetails = new()
            {
                Detail = " (do ... end)"
            },
            InsertTextFormat = InsertTextFormat.Snippet
        },
        new CompletionItem()
        {
            Label = "while",
            Kind = CompletionItemKind.Snippet,
            InsertTextMode = InsertTextMode.AdjustIndentation,
            InsertText = "while ${1:condition} do\n\t${0}\nend",
            LabelDetails = new()
            {
                Detail = " (while condition do ... end)"
            },
            InsertTextFormat = InsertTextFormat.Snippet
        },
        new CompletionItem()
        {
            Label = "repeat",
            Kind = CompletionItemKind.Snippet,
            InsertTextMode = InsertTextMode.AdjustIndentation,
            InsertText = "repeat\n\t${0}\nuntil ${1:condition}",
            LabelDetails = new()
            {
                Detail = " (repeat ... until condition)"
            },
            InsertTextFormat = InsertTextFormat.Snippet
        },
        new CompletionItem()
        {
            Label = "until",
            Kind = CompletionItemKind.Snippet,
            InsertTextMode = InsertTextMode.AdjustIndentation,
            InsertText = "until ${1:condition}",
            LabelDetails = new()
            {
                Detail = " (until condition)"
            },
            InsertTextFormat = InsertTextFormat.Snippet
        },
        new CompletionItem() { Label = "break", Kind = CompletionItemKind.Keyword, Detail = "keyword" },
        new CompletionItem() { Label = "return", Kind = CompletionItemKind.Keyword, Detail = "keyword" },
        new CompletionItem()
        {
            Label = "function",
            Kind = CompletionItemKind.Snippet,
            InsertTextMode = InsertTextMode.AdjustIndentation,
            InsertText = "function ${1:name}(${2:...})\n\t${0}\nend",
            LabelDetails = new()
            {
                Detail = " (function name(...) ... end)"
            },
            InsertTextFormat = InsertTextFormat.Snippet
        },
        new CompletionItem()
        {
            Label = "function",
            Kind = CompletionItemKind.Snippet,
            InsertTextMode = InsertTextMode.AdjustIndentation,
            InsertText = "function (${1:...})\n\t${0}\nend",
            LabelDetails = new()
            {
                Detail = " (function (...) ... end)"
            },
            InsertTextFormat = InsertTextFormat.Snippet
        },
        new CompletionItem() { Label = "local", Kind = CompletionItemKind.Keyword, Detail = "keyword" },
        new CompletionItem()
        {
            Label = "local function",
            Kind = CompletionItemKind.Snippet,
            InsertTextMode = InsertTextMode.AdjustIndentation,
            InsertText = "local function ${1:name}(${2:...})\n\t${0}\nend",
            LabelDetails = new()
            {
                Detail = " (local function name(...) ... end)"
            },
            InsertTextFormat = InsertTextFormat.Snippet
        },
        new CompletionItem() { Label = "nil", Kind = CompletionItemKind.Constant, Detail = "nil" },
        new CompletionItem() { Label = "true", Kind = CompletionItemKind.Constant, Detail = "true" },
        new CompletionItem() { Label = "false", Kind = CompletionItemKind.Constant, Detail = "false" },
        new CompletionItem() { Label = "and", Kind = CompletionItemKind.Keyword, Detail = "keyword" },
        new CompletionItem()
        {
            Label = "and or",
            Kind = CompletionItemKind.Snippet,
            InsertTextMode = InsertTextMode.AdjustIndentation,
            InsertText = "and ${1:result1} or ${2:result2}",
            LabelDetails = new()
            {
                Detail = " (and result1 or result2)"
            },
            InsertTextFormat = InsertTextFormat.Snippet
        },
        new CompletionItem() { Label = "or", Kind = CompletionItemKind.Keyword, Detail = "keyword" },
        new CompletionItem() { Label = "not", Kind = CompletionItemKind.Keyword, Detail = "keyword" },
        new CompletionItem() { Label = "goto", Kind = CompletionItemKind.Keyword, Detail = "keyword" },
    };

    public void AddCompletion(CompleteContext context)
    {
        if (context.TriggerToken?.Parent is not LuaNameExprSyntax)
        {
            return;
        }

        context.AddRange(Keywords);
        ContinueCompletion(context);
    }

    private void ContinueCompletion(CompleteContext context)
    {
        var triggerToken = context.TriggerToken;
        var stats = triggerToken!.Ancestors.OfType<LuaStatSyntax>();
        foreach (var stat in stats)
        {
            if (stat is LuaForStatSyntax or LuaForRangeStatSyntax or LuaWhileStatSyntax)
            {
                context.Add(new CompletionItem()
                {
                    Label = "continue",
                    Kind = CompletionItemKind.Keyword,
                    LabelDetails = new()
                    {
                        Detail = " (goto continue)"
                    },
                    InsertTextMode = InsertTextMode.AdjustIndentation,
                    InsertText = "goto continue",
                    AdditionalTextEdits = GetContinueLabelTextEdit(stat) is { } textEdit
                        ? new TextEditContainer(textEdit)
                        : null
                });
                break;
            }
        }
    }

    private TextEdit? GetContinueLabelTextEdit(LuaStatSyntax loopStat)
    {
        var endToken = loopStat.FirstChildToken(LuaTokenKind.TkEnd);
        if (endToken is not null)
        {
            var document = loopStat.Tree.Document;
            var blockIndentText = string.Empty;
            if (loopStat.FirstChild<LuaBlockSyntax>()?.StatList.LastOrDefault() is { } lastStat)
            {
                var indentToken = lastStat.GetPrevSibling();
                if (indentToken is LuaWhitespaceToken
                    {
                        Kind: LuaTokenKind.TkWhitespace, RepresentText: { } indentText2
                    })
                {
                    blockIndentText = indentText2;
                }
            }

            var endIndentText = string.Empty;
            if (endToken.GetPrevToken() is LuaWhitespaceToken
                {
                    Kind: LuaTokenKind.TkWhitespace, RepresentText: { } indentText
                })
            {
                endIndentText = indentText;
            }
            if (blockIndentText.Length > 0 && endIndentText.Length > 0 && blockIndentText.Length > endIndentText.Length)
            {
                blockIndentText = blockIndentText[endIndentText.Length..];
            }

            var newText = $"{blockIndentText}::continue::\n{endIndentText}end";
            return new TextEdit()
            {
                Range = endToken.Range.ToLspRange(document),
                NewText = newText
            };
        }

        return null;
    }
}