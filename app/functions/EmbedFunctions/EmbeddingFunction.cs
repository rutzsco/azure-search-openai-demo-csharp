// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.Functions.Worker;

public sealed class EmbeddingFunction(
    EmbeddingAggregateService embeddingAggregateService,
    ILoggerFactory loggerFactory)
{
    private readonly ILogger<EmbeddingFunction> _logger = loggerFactory.CreateLogger<EmbeddingFunction>();

    [Function(name: "embed-blob")]
    public Task EmbedAsync(
        [BlobTrigger(
            blobPath: "content-source/{name}",
            Connection = "AZURE_STORAGE_BLOB_CONNECTION_STRING")] Stream blobStream,
        string name) => embeddingAggregateService.EmbedBlobAsync(blobStream, blobName: name);
}
