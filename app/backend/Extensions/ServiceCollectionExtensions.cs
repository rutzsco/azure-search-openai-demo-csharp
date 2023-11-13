// Copyright (c) Microsoft. All rights reserved.

using Azure;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;

namespace MinimalApi.Extensions;

internal static class ServiceCollectionExtensions
{
    private static readonly DefaultAzureCredential s_azureCredential = new();

    internal static IServiceCollection AddAzureServices(this IServiceCollection services)
    {
        services.AddSingleton<BlobServiceClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureStorageAccountEndpoint = config["AzureStorageAccountEndpoint"];
            ArgumentNullException.ThrowIfNullOrEmpty(azureStorageAccountEndpoint);

            var blobServiceClient = new BlobServiceClient(
                new Uri(azureStorageAccountEndpoint), s_azureCredential);

            return blobServiceClient;
        });

        services.AddSingleton<BlobContainerClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureStorageContainer = config["AzureStorageContainer"];
            return sp.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(azureStorageContainer);
        });

        services.AddSingleton<SearchClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var (azureSearchServiceEndpoint, azureSearchIndex) =
                (config["AzureSearchServiceEndpoint"], config["AzureSearchIndex"]);

            ArgumentNullException.ThrowIfNullOrEmpty(azureSearchServiceEndpoint);

            var searchClient = new SearchClient(
                new Uri(azureSearchServiceEndpoint), azureSearchIndex, s_azureCredential);

            return searchClient;
        });

        services.AddSingleton<DocumentAnalysisClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureOpenAiServiceEndpoint = config["AzureOpenAiServiceEndpoint"] ?? throw new ArgumentNullException();

            var documentAnalysisClient = new DocumentAnalysisClient(
                new Uri(azureOpenAiServiceEndpoint), s_azureCredential);
            return documentAnalysisClient;
        });

        services.AddSingleton<OpenAIClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureOpenAiServiceEndpoint = config["AzureOpenAiServiceEndpoint"];

            ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceEndpoint);

            var openAIClient = new OpenAIClient(
                new Uri(azureOpenAiServiceEndpoint), s_azureCredential);

            return openAIClient;
        });
        services.AddSingleton<IKernel>(sp =>
        {
            // Semantic Kernel doesn't support Azure AAD credential for now
            // so we implement our own text completion backend
            var config = sp.GetRequiredService<IConfiguration>();
            var deployedModelName = config["AzureOpenAi3ChatGptDeployment"];
            var azureOpenAiServiceEndpoint = config["AzureOpenAiServiceEndpoint"];
            var azureOpenAiServiceKey = config["AzureOpenAiServiceKey"];

            ArgumentNullException.ThrowIfNullOrEmpty(deployedModelName);
            ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceEndpoint);
            ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceKey);

            IKernel kernel = Kernel.Builder
               .WithAzureChatCompletionService(deployedModelName, azureOpenAiServiceEndpoint, azureOpenAiServiceKey)
               .Build();

            return kernel;
        });
        services.AddSingleton<OpenAIClientFacade>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var deployedModelName3 = config["AzureOpenAi3ChatGptDeployment"];
            var azureOpenAiServiceEndpoint3 = config["AzureOpenAi3ServiceEndpoint"];
            var azureOpenAiServiceKey3 = config["AzureOpenAi3ServiceKey"];

            ArgumentNullException.ThrowIfNullOrEmpty(deployedModelName3);
            ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceEndpoint3);
            ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceKey3);

            var deployedModelName4 = config["AzureOpenAi4ChatGptDeployment"];
            var azureOpenAiServiceEndpoint4 = config["AzureOpenAi4ServiceEndpoint"];
            var azureOpenAiServiceKey4 = config["AzureOpenAi4ServiceKey"];
            ArgumentNullException.ThrowIfNullOrEmpty(deployedModelName4);
            ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceEndpoint4);
            ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceKey4);

            IKernel kernel3 = Kernel.Builder
               .WithAzureChatCompletionService(deployedModelName3, azureOpenAiServiceEndpoint3, azureOpenAiServiceKey3)
               .Build();

            IKernel kernel4 = Kernel.Builder
               .WithAzureChatCompletionService(deployedModelName4, azureOpenAiServiceEndpoint4, azureOpenAiServiceKey4)
               .Build();

            AzureChatCompletion chatGPT3 = new(deployedModelName3, azureOpenAiServiceEndpoint3, azureOpenAiServiceKey3);
            AzureChatCompletion chatGPT4 = new(deployedModelName4, azureOpenAiServiceEndpoint4, azureOpenAiServiceKey4);

            return new OpenAIClientFacade(kernel3, kernel4, chatGPT3, chatGPT4);
        });
        services.AddSingleton<SearchClientFacade>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var (azureSearchServiceEndpoint, azureSearchManualIndex, azureSearchReportIndex, azureSearchYouTubeIndex, azureSearchServiceKey) =
                (config["AzureSearchServiceEndpoint"], config["AzureSearchManualIndex"], config["AzureSearchReportIndex"], config["AzureSearchYouTubeIndex"], config["AzureSearchServiceEndpointKey"]);

            ArgumentNullException.ThrowIfNullOrEmpty(azureSearchServiceEndpoint);
            ArgumentNullException.ThrowIfNullOrEmpty(azureSearchServiceKey);

            var searchManualClient = new SearchClient(new Uri(azureSearchServiceEndpoint), azureSearchManualIndex, new AzureKeyCredential(azureSearchServiceKey));
            var searchReportClient = new SearchClient(new Uri(azureSearchServiceEndpoint), azureSearchReportIndex, new AzureKeyCredential(azureSearchServiceKey));
            var searchYouTubeClient = new SearchClient(new Uri(azureSearchServiceEndpoint), azureSearchYouTubeIndex, new AzureKeyCredential(azureSearchServiceKey));

            return new SearchClientFacade(searchManualClient, searchReportClient, searchYouTubeClient);
        });
        services.AddSingleton<AzureBlobStorageService>();
        services.AddSingleton<ReadRetrieveReadChatService>();
        services.AddSingleton<ReadRetrieveReadChatServiceEnhanced>();
        return services;
    }

    internal static IServiceCollection AddCrossOriginResourceSharing(this IServiceCollection services)
    {
        services.AddCors(
            options =>
                options.AddDefaultPolicy(
                    policy =>
                        policy.AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod()));

        return services;
    }
}
