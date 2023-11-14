// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Extensions;

public class KnowledgeSource
{
    public required string sourcepage { get; set; }

    public string? sourcefile { get; set; }

    public required string content { get; set; }

    public string FormatAsOpenAISourceText()
    {
        return $"<source><name>{sourcepage}</name><content> {content.Replace('\r', ' ').Replace('\n', ' ')}</content></source>";
    }
}
