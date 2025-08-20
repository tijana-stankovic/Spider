namespace SpiderHttp;

using SpiderDB;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
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
        var queue = new Queue<(string url, int internalLeft, int externalLeft, string spName, string baseUrl, string spURL)>();

        foreach (var sp in startingPoints) {
            queue.Enqueue((sp.URL, sp.InternalDepth, sp.ExternalDepth, sp.Name, sp.baseURL, sp.URL));
        }

        while (queue.Count > 0) {
            var (url, internalLeft, externalLeft, name, baseUrl, spURL) = queue.Dequeue(); // dequeue next URL

            if (result.VisitedUrls.Contains(url)) { // if URL is already visited, skip it
                View.Print("Already visited link, skip it: " + url);
                View.Print("    Remaining links: " + queue.Count);
                continue;
            }

            if (baseUrl != "" && !url.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase)) {
                View.Print("URL doesn't match base URL, skip it: " + url);
                View.Print("    Base URL: " + baseUrl);
                View.Print("         URL: " + url);
                continue;
            }

            View.Print("Crawling: " + url);
            View.Print("    Remaining internal links: " + internalLeft);
            View.Print("    Remaining external links: " + externalLeft);
            View.Print("    Remaining links: " + queue.Count);

            result.VisitedUrls.Add(url);

            string? page = await FetchPageAsync(url); // fetch page content

            if (page == null) { // if page could not be fetched, skip it
                View.Print("    Page could not be fetched, skip it.");
                continue;
            }

            // Keyword match
            var foundKeywords = FindKeywords(page, keywords);
            if (foundKeywords.Count > 0) {
                View.Print("    Keywords found: " + string.Join(", ", foundKeywords));
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
            foreach (var link in ExtractAbsoluteLinks(page, url)) {
                if (result.VisitedUrls.Contains(link)) { // if URL is already visited, skip it
                    View.Print("    Already visited link found, skip it: " + link);
                    continue;
                }

                string baseDomain = GetBaseDomain(spURL);
                string linkDomain = GetBaseDomain(link);

                if (string.Equals(baseDomain, linkDomain, StringComparison.OrdinalIgnoreCase)) {
                    if (internalLeft > 0) {
                        if (baseUrl != "" && !link.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase)) {
                            View.Print("        New internal link found, but skipped (URL doesn't match base URL): " + link);
                            View.Print("    Base URL: " + baseUrl);
                            View.Print("         URL: " + link);
                        } else {
                            View.Print("    New internal link found and added: " + link);
                            queue.Enqueue((link, internalLeft - 1, externalLeft, name, baseUrl, spURL));
                        }
                    } else {
                        View.Print("    New internal link found, but skipped (too far from the starting point): " + link);
                    }
                } else if (externalLeft > 0)
                {
                    View.Print("    New external link found and added: " + link);
                    queue.Enqueue((link, internalLeft, externalLeft - 1, name, "", spURL)); // external link doesn't have to match base URL
                } else {
                    View.Print("    New external link found, but skipped (too far from the starting point): " + link);
                }
            }
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
        var matches = Regex.Matches(page, @"href\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? baseUri)) {
            yield break;
        }

        foreach (Match match in matches) {
            string href = match.Groups[1].Value;
            if (href.StartsWith("javascript", StringComparison.OrdinalIgnoreCase) || href.StartsWith('#'))
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
