// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Extensions;

public class SearchClientFacade
{
    public SearchClientFacade(SearchClient searchClientManualIndex, SearchClient searchClientReportIndex, SearchClient searchClientYouTubeIndex)
    {
        SearchClientManualIndex = searchClientManualIndex;
        SearchClientReportIndex = searchClientReportIndex;
        SearchClientYouTubeIndex = searchClientYouTubeIndex;
    }

    public SearchClient SearchClientManualIndex { get; set; }
    public SearchClient SearchClientReportIndex { get; set; }
    public SearchClient SearchClientYouTubeIndex { get; set; }
}

