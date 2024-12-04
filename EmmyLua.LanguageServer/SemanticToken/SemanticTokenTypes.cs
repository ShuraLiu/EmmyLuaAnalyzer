using EmmyLua.LanguageServer.Framework.Protocol.Message.SemanticToken;

namespace EmmyLua.LanguageServer.SemanticToken;

public static class EmmySemanticTokenTypes
{
    public const string Namespace = SemanticTokenTypes.Namespace;
    public const string Type = SemanticTokenTypes.Type;
    public const string Class = SemanticTokenTypes.Class;
    public const string Enum = SemanticTokenTypes.Enum;
    public const string Interface = SemanticTokenTypes.Interface;
    public const string Struct = SemanticTokenTypes.Struct;
    public const string TypeParameter = SemanticTokenTypes.TypeParameter;
    public const string Parameter = SemanticTokenTypes.Parameter;
    public const string Variable = SemanticTokenTypes.Variable;
    public const string Property = SemanticTokenTypes.Property;
    public const string EnumMember = SemanticTokenTypes.EnumMember;
    public const string Event = SemanticTokenTypes.Event;
    public const string Function = SemanticTokenTypes.Function;
    public const string Method = SemanticTokenTypes.Method;
    public const string Macro = SemanticTokenTypes.Macro;
    public const string Keyword = SemanticTokenTypes.Keyword;
    public const string Modifier = SemanticTokenTypes.Modifier;
    public const string Comment = SemanticTokenTypes.Comment;
    public const string String = SemanticTokenTypes.String;
    public const string Number = SemanticTokenTypes.Number;
    public const string Regexp = SemanticTokenTypes.Regexp;
    public const string Operator = SemanticTokenTypes.Operator;
    public const string Decorator = SemanticTokenTypes.Decorator;

    public const string Self = "self";
    public const string G = "_G";
    public const string UpValue = "upvalue";
}

