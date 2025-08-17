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
    public Dictionary<string, HashSet<string>> KeywordToUrls { get; } = [];
    public HashSet<string> VisitedUrls { get; } = [];
}

public static class WebCrawler {
    private static readonly HttpClient httpClient = new();

    public static async Task<CrawlResult> CrawlAsync(List<StartingPoint> startingPoints, List<string> keywords) {
        var result = new CrawlResult();
        var queue = new Queue<(string url, int internalLeft, int externalLeft)>();

        foreach (var sp in startingPoints) {
            queue.Enqueue((sp.URL, sp.InternalDepth, sp.ExternalDepth));
        }

        while (queue.Count > 0) {
            var (url, internalLeft, externalLeft) = queue.Dequeue();
            if (result.VisitedUrls.Contains(url)) {
                continue;
            }

            View.Print("Crawling: " + url);
            View.Print("    Remaining internal links: " + internalLeft);
            View.Print("    Remaining external links: " + externalLeft);

            result.VisitedUrls.Add(url);

            string? page = await FetchPageAsync(url);

            if (page == null) {
                continue;
            }

            // Keyword match
            var found = FindKeywords(page, keywords);
            foreach (var kw in found) {
                if (!result.KeywordToUrls.TryGetValue(kw, out var urlSet)) {
                    urlSet = [];
                    result.KeywordToUrls[kw] = urlSet;
                }
                urlSet.Add(url);
                // TODO brisi ovaj ispis
                Console.WriteLine(kw + " -> " + url);
            }

            // Link extraction
            foreach (var link in ExtractAbsoluteLinks(page, url)) {
                if (result.VisitedUrls.Contains(link)) continue;

                string baseDomain = GetBaseDomain(url);
                string linkDomain = GetBaseDomain(link);

                if (string.Equals(baseDomain, linkDomain, StringComparison.OrdinalIgnoreCase) && internalLeft > 0) {
                    // TODO - brisi ovaj if i ispis
                    // if (link.Contains(".js"))
                        Console.WriteLine(url + " -> " + link);

                    queue.Enqueue((link, internalLeft - 1, externalLeft));
                }
                else if (externalLeft > 0) {
                    queue.Enqueue((link, internalLeft, externalLeft - 1));
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
