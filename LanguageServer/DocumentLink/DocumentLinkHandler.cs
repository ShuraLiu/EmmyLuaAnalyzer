﻿using LanguageServer.Server;
using LanguageServer.Util;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace LanguageServer.DocumentLink;

public class DocumentLinkHandler(ServerContext context) : DocumentLinkHandlerBase
{
    private DocumentLinkBuilder Builder { get; } = new();

    protected override DocumentLinkRegistrationOptions CreateRegistrationOptions(DocumentLinkCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new DocumentLinkRegistrationOptions
        {
            DocumentSelector = ToSelector.ToTextDocumentSelector(context.LuaWorkspace),
            ResolveProvider = true
        };
    }

    public override Task<DocumentLinkContainer?> Handle(DocumentLinkParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToUnencodedString();
        DocumentLinkContainer? container = null;
        context.ReadyRead(() =>
        {
            var semanticModel = context.LuaWorkspace.Compilation.GetSemanticModel(uri);
            if (semanticModel is not null)
            {
                var document = semanticModel.Document;
                var links = Builder.Build(document, context.ResourceManager);
                container = new DocumentLinkContainer(links);
            }
        });

        return Task.FromResult(container);
    }

    public override Task<OmniSharp.Extensions.LanguageServer.Protocol.Models.DocumentLink> Handle(
        OmniSharp.Extensions.LanguageServer.Protocol.Models.DocumentLink request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}