// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;

namespace MinimalApi.Extensions;

public class OpenAIClientFacade
{
    public OpenAIClientFacade(IKernel kernel3, IKernel kernel4, AzureChatCompletion azureChatCompletion3, AzureChatCompletion azureChatCompletion4)
    {
        Kernel3 = kernel3;
        Kernel4 = kernel4;
        AzureChatCompletion3 = azureChatCompletion3;
        AzureChatCompletion4 = azureChatCompletion4;
    }

    public IKernel Kernel3 { get; set; }
    public IKernel Kernel4 { get; set; }
    public AzureChatCompletion AzureChatCompletion3 { get; set; }
    public AzureChatCompletion AzureChatCompletion4 { get; set; }


    public IKernel GetKernel(bool chatGPT4)
    {
        if (chatGPT4)
        {
            return Kernel4;
        }

        return Kernel3;
    }

    public AzureChatCompletion GetChatGPT(bool chatGPT4)
    {
        if (chatGPT4)
        {
            return AzureChatCompletion4;
        }

        return AzureChatCompletion3;
    }

}
