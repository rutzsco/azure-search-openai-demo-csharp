// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Extensions;

public class SearchClientFacade
{
    public SearchClientFacade(SearchClient searchClientManualIndex)
    {
        SearchClient = searchClientManualIndex;
    }

    public SearchClient SearchClient { get; set; }

}

