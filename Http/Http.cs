namespace SpiderHttp;

using SpiderDB;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpiderView;

// result of crawling
public class CrawlResult {
    // for every found keyword, a set of URLs containing that keyword is stored (with their starting point name)
    public Dictionary<string, (HashSet<string> urlSet, string spName)> KeywordToUrls { get; } = [];

    // for every found URL, a set of keywords associated with that URL is stored (with URL starting point name)
    public Dictionary<string, (HashSet<string> keywordsSet, string spName)> UrlToKeywords { get; } = [];

    // list of all visited URLs
    public HashSet<string> VisitedUrls { get; } = [];
}

public static class WebCrawler {
    private static readonly HttpClient httpClient = new();

    // main crawling function
    public static async Task<CrawlResult> Crawl(List<StartingPoint> startingPoints, List<string> keywords) {
        var result = new CrawlResult();
        var tasks = new Queue<(string url, int internalLeft, int externalLeft, string spName, string baseUrl, string spURL)>();

        // first, put all starting points into the queue
        foreach (var sp in startingPoints) {
            tasks.Enqueue((sp.URL, sp.InternalDepth, sp.ExternalDepth, sp.Name, sp.BaseURL, sp.URL));
        }

        // process each URL in the queue, until all links are exhausted (queue is empty)
        while (tasks.Count > 0) {
            var (url, internalLeft, externalLeft, name, baseUrl, spURL) = tasks.Dequeue(); // next URL

            // main filter (filter out non-relevant pages (e.g. binary files like images, txt files with code snippets, etc.))
            if (IsNonRelevant(url)) {
                View.LogPrint("Non-relevant content for keyword extraction, skip it: " + url, false);
                View.LogPrint("    Remaining links: " + tasks.Count, false, DBData.LogLevel.Medium);
                continue;
            }

            // if URL is already visited, skip it
            if (result.VisitedUrls.Contains(UrlWithoutFragment(url))) { 
                View.LogPrint("Already visited link, skip it: " + url, false);
                View.LogPrint("    Remaining links: " + tasks.Count, false, DBData.LogLevel.Medium);
                continue;
            }

            // check base URL filter (if url doesn't match base URL, it is filtered out)
            if (baseUrl != "" && !url.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase)) {
                View.LogPrint("URL doesn't match base URL, skip it: " + url, false);
                View.LogPrint("    Base URL: " + baseUrl, false, DBData.LogLevel.High);
                View.LogPrint("    Remaining links: " + tasks.Count, false, DBData.LogLevel.Medium);
                continue;
            }

            View.LogPrint("Crawling: " + url, true);

            result.VisitedUrls.Add(UrlWithoutFragment(url)); // add URL to visited list

            // *******************************************************
            string? page = await FetchPage(url); // fetch page content
            // *******************************************************

            // if page could not be fetched, skip it
            if (page == null) { 
                View.LogPrint("    Page could not be fetched, skip it.", false, DBData.LogLevel.Medium);
                continue;
            }

            // search for keywords
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

            // link extraction (search/extract new links from the page)
            int newInternalLinks = 0, newExternalLinks = 0;
            foreach (var link in ExtractLinks(page, url)) { // process found links and add new ones to the queue

                // main filter (filter out non-relevant pages (e.g. binary files like images, txt files with code snippets, etc.))
                if (IsNonRelevant(link)) {
                    View.LogPrint("    Non-relevant content for keyword extraction, skip it: " + link, false, DBData.LogLevel.Medium);
                    continue;
                }

                // if URL is already visited, skip it
                if (result.VisitedUrls.Contains(UrlWithoutFragment(link))) { 
                    View.LogPrint("    Already visited link found, skip it: " + link, false, DBData.LogLevel.Medium);
                    continue;
                }

                string baseDomain = GetBaseDomain(spURL); // base of starting point URL
                string linkDomain = GetBaseDomain(link); // base of found link URL

                if (string.Equals(baseDomain, linkDomain, StringComparison.OrdinalIgnoreCase)) { // same base => internal link
                    if (internalLeft > 0) {
                        if (baseUrl != "" && !link.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase)) { // if the base URL is specified, check it
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
                } else if (externalLeft > 0) { // different base => external link
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

    // fetch page content
    public static async Task<string?> FetchPage(string url) {
        try {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) { // allow only HTTP and HTTPS 
                return null;
            }

            return await httpClient.GetStringAsync(uri); // fetch the page content 

        } catch {
            return null;
        }
    }

    // search for keywords in the page content and return the found keywords
    public static HashSet<string> FindKeywords(string page, List<string> keywords) {
        HashSet<string> found = [];

        foreach (var keyword in keywords) {
            if (page.Contains(keyword, StringComparison.OrdinalIgnoreCase)) {
                found.Add(keyword);
            }
        }

        return found;
    }

    // link extraction (search/extract new links from the page)
    public static IEnumerable<string> ExtractLinks(string page, string baseUrl) {
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
            if (Uri.TryCreate(baseUri, href, out Uri? fullUri))
                yield return fullUri.ToString();
        }
    }

    // get the base domain from a URL
    // e.g. https://example.com/page.html   -> example.com
    public static string GetBaseDomain(string url) {
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) {
            url = "http://" + url;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) {
            return uri.Host.ToLowerInvariant();
        }

        return string.Empty;
    }

    // return url without fragment part (# part), but keep query part (if exists)
    // e.g
    // https://example.com/page.html?lang=en#section2   -> https://example.com/page.html?lang=en
    public static string UrlWithoutFragment(string url) {
        int fragmentIndex = url.IndexOf('#');
        return fragmentIndex >= 0 ? url.Substring(0, fragmentIndex) : url;
    }

    // extract 'clean' URL from a complex URL, with query strings (?) and/or fragments (#)
    // (removes query parameters and fragment identifiers (e.g., ?id=123, #section) from the url)
    // e.g
    // https://example.com/media/video.mp4#t=30         -> https://example.com/media/video.mp4
    // https://example.com/images/photo.jpg?size=large  -> https://example.com/images/photo.jpg 
    // https://example.com/page.html?lang=en#section2   -> https://example.com/page.html
    static string CleanUrl(string url) {
        int index = url.IndexOfAny(new[] { '?', '#' });
        return index >= 0 ? url.Substring(0, index) : url;
    }

    // check if the content behind the URL is not relevant for keyword search
    public static bool IsNonRelevant(string url) {
        // binary file or non-relevant page (e.g. .css) extensions
        string[] nonRelevantExtensions = {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", // binary documents
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".ico", // image files
            ".mp3", ".wav", ".mp4", ".avi", // audio/video files
            ".zip", ".rar", ".exe", ".msi", // archive/installer files
            ".js", ".css" // script/style files
        };

        url = url.Trim().ToLower();
        // skip fragment identifiers and non-HTTP(S) URLs
        if (url.StartsWith('#') || url.StartsWith("javascript:") || url.StartsWith("mailto:"))
            return true;

        url = CleanUrl(url); // remove query parameters and fragment identifiers
        // skip non-relevant URLs (binary files, images, etc.)
        if (nonRelevantExtensions.Any(ext => url.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            return true;

        return false;
    }
}
