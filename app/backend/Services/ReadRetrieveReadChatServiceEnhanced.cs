// Copyright (c) Microsoft. All rights reserved.

using System.Reactive.Joins;
using Azure.AI.OpenAI;
using Microsoft.Identity.Client;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Planning.Sequential;
using Microsoft.SemanticKernel.TemplateEngine;
using Microsoft.SemanticKernel.TemplateEngine.Prompt;
using MinimalApi.Extensions;
using MinimalApi.Services.Prompts;
using MinimalApi.Services.Skills;

namespace MinimalApi.Services;

internal sealed class ReadRetrieveReadChatServiceEnhanced
{
    private readonly SearchClientFacade _searchClientFacade;
    private readonly OpenAIClient _openAIClient;

    private readonly ILogger<ReadRetrieveReadChatServiceEnhanced> _logger;
    private readonly IConfiguration _configuration;

    private readonly OpenAIClientFacade _openAIClientFacade;

    public Approach Approach => Approach.ReadRetrieveRead;

    public ReadRetrieveReadChatServiceEnhanced(
        OpenAIClientFacade openAIClientFacade,
        SearchClientFacade searchClientFacade,
        OpenAIClient openAIClient,
        ILogger<ReadRetrieveReadChatServiceEnhanced> logger,
        IConfiguration configuration)
    {
        _searchClientFacade = searchClientFacade;
        _openAIClientFacade = openAIClientFacade;
        _openAIClient = openAIClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ApproachResponse> ReplyAsync(ChatTurn[] history, RequestOverrides overrides, CancellationToken cancellationToken = default)
    {
        var kernel = _openAIClientFacade.GetKernel(overrides.ChatGPT4);
        var chatGpt = _openAIClientFacade.GetChatGPT(overrides.ChatGPT4);
        var documentLookupSkill = kernel.ImportSkill(new RetrieveRelatedDocumentSkill(_searchClientFacade, _openAIClient, overrides), "Query")["Query"];

        var context = new ContextVariables();
        var historyText = history.GetChatHistoryAsText2(includeLastTurn: true);
        context["chat_history"] = historyText;
        if (history.LastOrDefault()?.User is { } userQuestion)
        {
            context["question"] = $"{userQuestion}";
        }
        else
        {
            throw new InvalidOperationException("User question is null");
        }
        var promptRenderer = new PromptTemplateEngine();

        // INTENT - Create chat history starting with system message
        var intentChatHistory = chatGpt.CreateNewChat(PromptService.GetPromptByName(PromptService.SearchSystemPrompt));

        // INTENT - Load history
        foreach (var chatTurn in history.SkipLast(1))
        {
            intentChatHistory.AddUserMessage(chatTurn.User);
            if (chatTurn.Bot != null)
            {
                intentChatHistory.AddAssistantMessage(chatTurn.Bot);
            }
        }

        // INTENT - Execute
        // Chat Implementation
        var intentChatContext = kernel.CreateNewContext();
        intentChatContext.Variables["question"] = context["question"];
        string intentUserMessage = await promptRenderer.RenderAsync(PromptService.GetPromptByName(PromptService.SearchUserPrompt), intentChatContext);
        intentChatHistory.AddUserMessage(intentUserMessage);

        var searchAnswer = await chatGpt.GenerateMessageAsync(intentChatHistory, new ChatRequestSettings { Temperature = DefaultSettings.Temperature, MaxTokens = DefaultSettings.MaxTokens });
        context["searchQuery"] = searchAnswer;

        // SOURCES - Generate search query and execute sources search
        await kernel.RunAsync(context, documentLookupSkill);


        // CHAT - Implementation
        var chatContext = kernel.CreateNewContext();
        chatContext.Variables["knowledge"] = context["knowledge"];
        chatContext.Variables["question"] = context["question"];

        // Create chat history starting with system message
        var systemMessagePrompt = PromptService.GetPromptByName(PromptService.ChatSystemPrompt);
        var chatHistory = chatGpt.CreateNewChat(systemMessagePrompt);

        // Load history
        foreach (var chatTurn in history.SkipLast(1))
        {
            chatHistory.AddUserMessage(chatTurn.User);
            if (chatTurn.Bot != null)
            {
                chatHistory.AddAssistantMessage(chatTurn.Bot);
            }
        }

        // Add latest message and source content
        string userMessage = await promptRenderer.RenderAsync(PromptService.GetPromptByName(PromptService.ChatUserPrompt), chatContext);
        chatHistory.AddUserMessage(userMessage);

        var answer = await chatGpt.GenerateMessageAsync(chatHistory, new ChatRequestSettings { Temperature = DefaultSettings.Temperature, MaxTokens = DefaultSettings.MaxTokens });
        var dataSources = JsonSerializer.Deserialize<KnowledgeSource[]>(context["knowledge-json"], new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

        return new ApproachResponse(
            DataPoints: dataSources.Select(x => new SupportingContentRecord(x.sourcepage, x.content)).ToArray(),
            Answer: answer.Replace("\n", "<br>"),
            Thoughts: $"Searched for:<br>{context["intent"]}<br><br>System:<br>{systemMessagePrompt.Replace("\n", "<br>")}<br>{userMessage.Replace("\n", "<br>")}<br><br>{answer.Replace("\n", "<br>")}",
            CitationBaseUrl: _configuration.ToCitationBaseUrl());
    }

    private static string PlanToString(Plan originalPlan)
    {
        return $"Goal: {originalPlan.Description}\n\nSteps:\n" + string.Join("\n", originalPlan.Steps.Select(s => $"- {s.SkillName}.{s.Name} {string.Join(" ", s.Parameters.Select(p => $"{p.Key}='{p.Value}'"))}{" => " + string.Join(" ", s.Outputs.Where(s => s.ToUpper(System.Globalization.CultureInfo.CurrentCulture) != "INPUT").Select(p => $"{p}"))}"
        ));
    }
}
