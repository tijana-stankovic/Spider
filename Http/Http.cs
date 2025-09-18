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
                View.LogPrint("    Remaining links: " + tasks.Count, false);
                continue;
            }

            // if URL is already visited, skip it
            if (result.VisitedUrls.Contains(UrlWithoutFragment(url))) { 
                View.LogPrint("Already visited link, skip it: " + url, false);
                View.LogPrint("    Remaining links: " + tasks.Count, false);
                continue;
            }

            // check base URL filter (if url doesn't match base URL, it is filtered out)
            if (baseUrl != "" && !url.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase)) {
                View.LogPrint("URL doesn't match base URL, skip it: " + url, false);
                View.LogPrint("    Base URL: " + baseUrl, false);
                View.LogPrint("    Remaining links: " + tasks.Count, false);
                continue;
            }

            View.LogPrint("Crawling: " + url, true);

            result.VisitedUrls.Add(UrlWithoutFragment(url)); // add URL to visited list

            // *******************************************************
            string? page = await FetchPage(url); // fetch page content
            // *******************************************************
            View.Print($"Fetched {url}: {page?.Length ?? 0} characters"); // TODO: remove this (testing)

            // if page could not be fetched, skip it
            if (page == null) { 
                View.LogPrint("    Page could not be fetched, skip it.", false);
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
                    View.LogPrint("    Non-relevant content for keyword extraction, skip it: " + link, false);
                    continue;
                }

                // if URL is already visited, skip it
                if (result.VisitedUrls.Contains(UrlWithoutFragment(link))) { 
                    View.LogPrint("    Already visited link found, skip it: " + link, false);
                    continue;
                }

                string baseDomain = GetBaseDomain(spURL); // base of starting point URL
                string linkDomain = GetBaseDomain(link); // base of found link URL

                if (string.Equals(baseDomain, linkDomain, StringComparison.OrdinalIgnoreCase)) { // same base => internal link
                    if (internalLeft > 0) {
                        if (baseUrl != "" && !link.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase)) { // if the base URL is specified, check it
                            View.LogPrint("    New internal link found, but skipped (URL doesn't match base URL): " + link, false);
                            View.LogPrint("    Base URL: " + baseUrl, false);
                        } else {
                            View.LogPrint("    New internal link found and added: " + link, false);
                            tasks.Enqueue((link, internalLeft - 1, externalLeft, name, baseUrl, spURL));
                            newInternalLinks++;
                        }
                    } else {
                        View.LogPrint("    New internal link found, but skipped (too far from the starting point): " + link, false);
                    }
                } else if (externalLeft > 0) { // different base => external link
                    View.LogPrint("    New external link found and added: " + link, false);
                    tasks.Enqueue((link, internalLeft, externalLeft - 1, name, "", spURL)); // external link doesn't have to match base URL
                    newExternalLinks++;
                } else {
                    View.LogPrint("    New external link found, but skipped (too far from the starting point): " + link, false);
                }
            }
            if (newInternalLinks + newExternalLinks > 0) {
                View.LogPrint($"    New links added: {newInternalLinks + newExternalLinks} (internal: {newInternalLinks}, external: {newExternalLinks})", true);
            }

            View.LogPrint("    Remaining links: " + tasks.Count, true);
            View.LogPrint("    Remaining internal depth: " + internalLeft, false);
            View.LogPrint("    Remaining external depth: " + externalLeft, false);
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
            // Match whole words only (if the word is part of another word, it won't be matched)
            string pattern = $@"\b{Regex.Escape(keyword)}\b";
            if (Regex.IsMatch(page, pattern, RegexOptions.IgnoreCase)) {
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
        if (url == null) return "";

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
        if (url == null) return "";

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

        if (url == null) // if URL is null, consider it non-relevant
            return true;

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

// ***************************************************************
// parallel web crawler
public static class PWebCrawler {
    public static readonly int MaxAllowedNumOfThreads = 99; // max allowed number of threads
    public static int MaxNumOfThreads { get; set; } = 1; // max currently allowed number of threads, 1 = no parallelism

    private static bool _pScanActive = false; // is parallel scanning active or not
    private static readonly object _pScanActiveLock = new(); // lock object for _pScanActive

    private static int _numOfActiveThreads = 0; // number of currently active threads
    private static readonly object _numOfActiveThreadsLock = new(); // lock object for _numOfActiveThreads

    // thread-safe property
    public static bool PScanActive {
        get {
            lock (_pScanActiveLock) {
                return _pScanActive;
            }
        }
        set {
            lock (_pScanActiveLock) {
                _pScanActive = value;
            }
        }
    }

    // thread-safe property
    public static int ActiveThreads {
        get {
            lock (_numOfActiveThreadsLock) {
                return _numOfActiveThreads;
            }
        }
        set {
            lock (_numOfActiveThreadsLock) {
                _numOfActiveThreads = value;
            }
        }
    }

    // thread-safe helper method to:
    // - check if parallel scanning is active and 
    // - return number of active threads
    public static bool IsPScanActive(out int activeThreads) {
        // lock both objects in a consistent order to avoid deadlocks
        lock (_pScanActiveLock) {
            lock (_numOfActiveThreadsLock) {
                activeThreads = _numOfActiveThreads;
                return _pScanActive;
            }
        }
    }

    private static readonly HttpClient httpClient = new();

    // main crawling function
    public static CrawlResult Crawl(List<StartingPoint> startingPoints, List<string> keywords) {
        var result = new CrawlResult();
        object resultLock = new();
        var tasks = new Queue<(string url, int internalLeft, int externalLeft, string spName, string baseUrl, string spURL)>();
        object tasksLock = new();

        // helper function to process URLs from the queue
        void ProcessURL() {
            while (true) {
                (string url, int internalLeft, int externalLeft, string spName, string baseUrl, string spURL) task;

                lock (tasksLock) {
                    if (tasks.Count == 0) break; // if the task queue is empty, exit the loop
                    task = tasks.Dequeue(); // get the next task (URL)
//                    View.Print($"Get URL: {task.url}");
                }

                // main filter (filter out non-relevant pages (e.g. binary files like images, txt files with code snippets, etc.))
                if (WebCrawler.IsNonRelevant(task.url)) {
                    continue;
                }

                lock (resultLock) {
                    // if URL is already visited, skip it
                    if (result.VisitedUrls.Contains(WebCrawler.UrlWithoutFragment(task.url))) {
                        continue;
                    }
                }

                // check base URL filter (if url doesn't match base URL, it is filtered out)
                if (task.baseUrl != "" && !task.url.StartsWith(task.baseUrl, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                lock (resultLock) {
                    result.VisitedUrls.Add(WebCrawler.UrlWithoutFragment(task.url)); // add URL to visited list
                }

                // *******************************************************
                string? page = FetchPage(task.url); // fetch page content
                // *******************************************************
                // View.Print($"Fetched {task.url}: {page?.Length ?? 0} characters");

                // if page could not be fetched, skip it
                if (page == null) { 
                    continue;
                }

                // search for keywords
                var foundKeywords = WebCrawler.FindKeywords(page, keywords);
                if (foundKeywords.Count > 0) {
                    lock (resultLock) {
                        result.UrlToKeywords[task.url] = (foundKeywords, task.spName); 
                    }
                }
                foreach (var keyword in foundKeywords) {
                    lock (resultLock) {
                        if (!result.KeywordToUrls.TryGetValue(keyword, out var value)) { // if keyword is already found in some of previous pages, get that Set
                            value = (new HashSet<string>(), task.spName); // otherwise create a new Set
                            result.KeywordToUrls[keyword] = value; // add the Set to the dictionary
                        }
                        value.urlSet.Add(task.url); // add the current URL to the Set
                    }
                }

                // link extraction (search/extract new links from the page)
                int newInternalLinks = 0, newExternalLinks = 0;
                foreach (var link in WebCrawler.ExtractLinks(page, task.url)) { // process found links and add new ones to the queue

                    // main filter (filter out non-relevant pages (e.g. binary files like images, txt files with code snippets, etc.))
                    if (WebCrawler.IsNonRelevant(link)) {
                        continue;
                    }

                    // if URL is already visited, skip it
                    lock (resultLock) {
                        if (result.VisitedUrls.Contains(WebCrawler.UrlWithoutFragment(link))) { 
                            continue;
                        }
                    }

                    string baseDomain = WebCrawler.GetBaseDomain(task.spURL); // base of starting point URL
                    string linkDomain = WebCrawler.GetBaseDomain(link); // base of found link URL

                    if (string.Equals(baseDomain, linkDomain, StringComparison.OrdinalIgnoreCase)) { // same base => internal link
                        if (task.internalLeft > 0) {
                            if (task.baseUrl != "" && !link.StartsWith(task.baseUrl, StringComparison.OrdinalIgnoreCase)) { // if the base URL is specified, check it
                                // New internal link found, but skipped (URL doesn't match base URL): "
                            } else {
                                lock (tasksLock) {
                                    tasks.Enqueue((link, task.internalLeft - 1, task.externalLeft, task.spName, task.baseUrl, task.spURL));
                                }
                                newInternalLinks++;
                            }
                        } else { // New internal link found, but skipped (too far from the starting point)
                            // do nothing -> skip this internal link
                        }
                    } else if (task.externalLeft > 0) { // different base => external link
                        lock (tasksLock) {
                            tasks.Enqueue((link, task.internalLeft, task.externalLeft - 1, task.spName, "", task.spURL)); // external link doesn't have to match base URL
                        }
                        newExternalLinks++;
                    } else { 
                        // New external link found, but skipped (too far from the starting point)
                        // do nothing -> skip this external link
                    }
                }
            }

            // this thread finishes, so decrement the active thread count
            lock (_numOfActiveThreadsLock) {
                _numOfActiveThreads--;
                // View.Print($"DEC Active threads: {_numOfActiveThreads}");
            }
        } // end of ProcessURL() helper function



        // *************************************
        // real starting point of Crawl() method
        // *************************************

        // first, put all starting points into the queue
        lock (tasksLock) {
            foreach (var sp in startingPoints) {
                tasks.Enqueue((sp.URL, sp.InternalDepth, sp.ExternalDepth, sp.Name, sp.BaseURL, sp.URL));
            }
        }

        // process URLs in the queue, 
        // until all links are exhausted (queue is empty) and no active threads are running
        // this is a main crawling loop, where parallel threads are created and launched
        while (true) {
            lock (tasksLock) {
                lock (_numOfActiveThreadsLock) {
                    if (tasks.Count == 0 && _numOfActiveThreads == 0) {
                        // finish processing when the task queue is empty (no more URLs) and 
                        // no active threads are running
                        break; 
                    }

                    if (tasks.Count > 0 && _numOfActiveThreads < MaxNumOfThreads && _numOfActiveThreads < MaxAllowedNumOfThreads) {
                        var thread = new Thread(ProcessURL); // create a new thread
                        _numOfActiveThreads++;
                        // View.Print($"INC Active threads: {_numOfActiveThreads}");
                        thread.Start(); // launch the thread
                    }
                }
            }

            Thread.Sleep(10); // short sleep
        }

        // View.Print($"---END OF PARALLEL CRAWLING---");
        return result;
    }

    // fetch page content, synchronous version
    private static string? FetchPage(string url) {
        try {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) { // allow only HTTP and HTTPS 
                return null;
            }

            return httpClient.GetStringAsync(uri).GetAwaiter().GetResult(); // fetch the page content 
        } catch {
            return null;
        }
    }
}
