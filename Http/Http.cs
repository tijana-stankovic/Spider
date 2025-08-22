namespace SpiderHttp;

using SpiderDB;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpiderView;

public class CrawlResult {
    public Dictionary<string, (HashSet<string> urlSet, string spName)> KeywordToUrls { get; } = [];
    public Dictionary<string, (HashSet<string> keywordsSet, string spName)> UrlToKeywords { get; } = [];
    public HashSet<string> VisitedUrls { get; } = [];
}

public static class WebCrawler {
    private static readonly HttpClient httpClient = new();

    public static async Task<CrawlResult> CrawlAsync(List<StartingPoint> startingPoints, List<string> keywords) {
        var result = new CrawlResult();
        var tasks = new Queue<(string url, int internalLeft, int externalLeft, string spName, string baseUrl, string spURL)>();

        foreach (var sp in startingPoints) {
            tasks.Enqueue((sp.URL, sp.InternalDepth, sp.ExternalDepth, sp.Name, sp.baseURL, sp.URL));
        }

        while (tasks.Count > 0) {
            var (url, internalLeft, externalLeft, name, baseUrl, spURL) = tasks.Dequeue(); // next URL

            if (result.VisitedUrls.Contains(url)) { // if URL is already visited, skip it
                View.LogPrint("Already visited link, skip it: " + url, false);
                View.LogPrint("    Remaining links: " + tasks.Count, false, DBData.LogLevel.Medium);
                continue;
            }

            if (baseUrl != "" && !url.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase)) {
                View.LogPrint("URL doesn't match base URL, skip it: " + url, false);
                View.LogPrint("    Base URL: " + baseUrl, false, DBData.LogLevel.High);
                View.LogPrint("    Remaining links: " + tasks.Count, false, DBData.LogLevel.Medium);
                continue;
            }

            View.LogPrint("Crawling: " + url, true);

            result.VisitedUrls.Add(url);

            // ************************************************************
            string? page = await FetchPageAsync(url); // fetch page content
            // ************************************************************

            if (page == null) { // if page could not be fetched, skip it
                View.LogPrint("    Page could not be fetched, skip it.", false, DBData.LogLevel.Medium);
                continue;
            }

            // Keyword match
            var foundKeywords = FindKeywords(page, keywords);
            if (foundKeywords.Count > 0) {
                View.LogPrint("    Keywords found: " + string.Join(", ", foundKeywords), true);
                result.UrlToKeywords[url] = (foundKeywords, name); 
            }
            foreach (var keyword in foundKeywords) {
                if (!result.KeywordToUrls.TryGetValue(keyword, out var value)) { // if keyword is already found in some of previous pages, get that Set
                    value = (new HashSet<string>(), name); // otherwise create a new Set
                    result.KeywordToUrls[keyword] = value; // add the Set to the dictionary
                }
                value.urlSet.Add(url); // add the current URL to the Set
            }

            // Link extraction
            int newInternalLinks = 0, newExternalLinks = 0;
            foreach (var link in ExtractAbsoluteLinks(page, url)) {
                if (result.VisitedUrls.Contains(link)) { // if URL is already visited, skip it
                    View.LogPrint("    Already visited link found, skip it: " + link, false, DBData.LogLevel.Medium);
                    continue;
                }

                string baseDomain = GetBaseDomain(spURL);
                string linkDomain = GetBaseDomain(link);

                if (string.Equals(baseDomain, linkDomain, StringComparison.OrdinalIgnoreCase)) {
                    if (internalLeft > 0) {
                        if (baseUrl != "" && !link.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase)) {
                            View.LogPrint("    New internal link found, but skipped (URL doesn't match base URL): " + link, false, DBData.LogLevel.Medium);
                            View.LogPrint("    Base URL: " + baseUrl, false, DBData.LogLevel.High);
                        } else {
                            View.LogPrint("    New internal link found and added: " + link, false, DBData.LogLevel.Medium);
                            tasks.Enqueue((link, internalLeft - 1, externalLeft, name, baseUrl, spURL));
                            newInternalLinks++;
                        }
                    } else {
                        View.LogPrint("    New internal link found, but skipped (too far from the starting point): " + link, false, DBData.LogLevel.Medium);
                    }
                } else if (externalLeft > 0)
                {
                    View.LogPrint("    New external link found and added: " + link, false, DBData.LogLevel.Medium);
                    tasks.Enqueue((link, internalLeft, externalLeft - 1, name, "", spURL)); // external link doesn't have to match base URL
                    newExternalLinks++;
                } else {
                    View.LogPrint("    New external link found, but skipped (too far from the starting point): " + link, false, DBData.LogLevel.Medium);
                }
            }
            if (newInternalLinks + newExternalLinks > 0) {
                View.LogPrint($"    New links added: {newInternalLinks + newExternalLinks} (internal: {newInternalLinks}, external: {newExternalLinks})", true);
            }

            View.LogPrint("    Remaining links: " + tasks.Count, true, DBData.LogLevel.Medium);
            View.LogPrint("    Remaining internal depth: " + internalLeft, false, DBData.LogLevel.High);
            View.LogPrint("    Remaining external depth: " + externalLeft, false, DBData.LogLevel.High);
        }

        return result;
    }

    public static async Task<string?> FetchPageAsync(string url) {
        try {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) {
                return null;
            }

            return await httpClient.GetStringAsync(uri);
        } catch {
            return null;
        }
    }

    public static HashSet<string> FindKeywords(string page, List<string> keywords) {
        HashSet<string> found = [];

        foreach (var keyword in keywords) {
            if (page.Contains(keyword, StringComparison.OrdinalIgnoreCase)) {
                found.Add(keyword);
            }
        }

        return found;
    }

    public static IEnumerable<string> ExtractAbsoluteLinks(string page, string baseUrl) {
        // Regex search in 'page' for all links (href attributes) in the form of: href="something"
        //      - href          the word 'href'
        //      - \s*=\s*       an equals sign '=' (possibly surrounded by any amount of whitespace)
        //      - ["']          either a double quote " or single quote '
        //      - ([^""']+)     one or more characters that are not quotes (actual URL)
        //      - ["']          the closing quote
        var matches = Regex.Matches(page, @"href\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? baseUri)) {
            yield break;
        }

        foreach (Match match in matches) {
            string href = match.Groups[1].Value;
            if (href.StartsWith("javascript", StringComparison.OrdinalIgnoreCase) || href.StartsWith('#')) // skip javascript and anchors
                continue;

            if (Uri.TryCreate(baseUri, href, out Uri? fullUri))
                yield return fullUri.ToString();
        }
    }

    public static string GetBaseDomain(string url) {
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) {
            url = "http://" + url;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) {
            return uri.Host.ToLowerInvariant();
        }

        return string.Empty;
    }
}
