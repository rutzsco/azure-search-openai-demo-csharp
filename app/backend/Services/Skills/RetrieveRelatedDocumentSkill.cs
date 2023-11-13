// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.ComponentModel;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using MinimalApi.Extensions;
using TiktokenSharp;

namespace MinimalApi.Services.Skills;

public sealed class RetrieveRelatedDocumentSkill
{
    private readonly SearchClientFacade _searchClientFacade;
    private readonly RequestOverrides? _requestOverrides;
    private readonly OpenAIClient _openAIClient;

    public RetrieveRelatedDocumentSkill(SearchClientFacade searchClientFacade, OpenAIClient openAIClient, RequestOverrides? requestOverrides)
    {
        _searchClientFacade = searchClientFacade;
        _openAIClient = openAIClient;
        _requestOverrides = requestOverrides;
    }

    [SKFunction, Description("Search more information")]
    [SKParameter("searchQuery", "search query")]
    [SKParameter("aircraft", "selected aircraft")]
    [SKParameter("sourceType", "selected knowledge source")]
    public async Task<string> QueryAsync(SKContext context)
    {
        var searchQuery = context.Variables["searchQuery"].Replace("\"", string.Empty);

        context.Variables["intent"] = searchQuery;
        var aircraft = context.Variables["aircraft"];
        var sourceType = context.Variables["sourceType"];

        IReadOnlyList<KnowledgeSource> sources = new List<KnowledgeSource>();
        sources = await _searchClientFacade.SearchClientManualIndex.SimpleHybridSearchAsync(_openAIClient, searchQuery, aircraft);
        if (!sources.Any())
        {
            throw new InvalidOperationException("fail to get search result");
        }

        int sourceSize = 0;
        int tokenSize = 0;
        var documents = new List<KnowledgeSource>();
        var sb = new StringBuilder();
        var tikToken = TikToken.EncodingForModel("gpt-3.5-turbo");
        foreach (var document in sources)
        {
            var text = document.FormatAsOpenAISourceText();
            sourceSize += text.Length;
            tokenSize += tikToken.Encode(text).Count;
            if (tokenSize > 12000)
            {
                break;
            }
            documents.Add(document);
            sb.AppendLine(text);
        }
        var documentContents = sb.ToString();

        var result = sb.ToString();
        context.Variables["knowledge"] = result;
        context.Variables["knowledge-json"] = JsonSerializer.Serialize(documents);
        return result;
    }
}
