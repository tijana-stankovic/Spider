namespace SpiderDB;

using System.Collections.Generic;

public class DBData {
    // for JSON deserialization (in DB.WriteDB()) to work properly,
    // we need this default (parameterless) constructor.
    // without this, JsonSerializer.Deserialize() in ReadDB() will generate exception
    public DBData() {}

    public DBData(int version) {
        DbVersion = version;
    }

    // for JSON serialization and deserialization (in DB.ReadDB() and DB.WriteDB()) to 
    // work properly, the properties must be public and have both getter and setter
    public int DbVersion { get; set; } = 0;
    public int LastPageID { get; set; } = 0;
    public Dictionary<int, DBPage> Pages { get; set; } = []; // index: page ID -> page (this is the place all pages are stored) (1:1)
    public Dictionary<string, int> URLs { get; set; } = []; // index: full, unique page URL -> page ID (1:1)
    public Dictionary<string, HashSet<int>> WebsiteToPages { get; set; } = []; // index: website -> page ID (1:N)
    public Dictionary<string, HashSet<int>> NameToPages { get; set; } = []; // index: starting point name -> page ID (1:N)
    public Dictionary<string, HashSet<int>> KeywordToPages { get; set; } = []; // index: keyword -> page ID (1:N)

    public Dictionary<string, string> Keywords { get; set; } = []; // all the keywords the user is searching for (the user is interested in)
                                                                   // keyword.upper() -> the original keyword entered by the user
    public Dictionary<string, StartingPoint> SPNames { get; set; } = []; // starting point names

    // Log parameters
    public enum LogLevel { // log details level
        Low, // minimal logging
        Medium, // moderate logging
        High, // detailed logging
        NoLogging // no logging
    }
    public string LogFileName { get; set; } = "spider_log.txt";
    public bool LogActive { get; set; } = true;
    public LogLevel CurrentLogLevel { get; set; } = LogLevel.Medium;

    public int NextPageID() {
        LastPageID++;
        return LastPageID;
    }

    // return a sorted list of all keywords in the database
    public List<string> GetKeywords() {
        List<string> keys = [];
        foreach (var keyword in Keywords.Values) {
            keys.Add(keyword);
        }
        keys.Sort();
        return keys;
    }

    // return a sorted list of all keywords contained in scanned pages.
    public List<string> GetFoundKeywords() {
        var keys = new List<string>(KeywordToPages.Keys);
        keys.Sort();
        return keys;
    }

    // return a sorted list of all starting points names in the database
    public List<string> GetSPNames() {
        List<string> names = [];
        foreach (var sp in SPNames.Values) {
            names.Add(sp.Name);
        }
        names.Sort();
        return names;
    }

    // return Starting Point with the specified name
    // if not found, return null
    public StartingPoint? GetStartingPoint(string spName) {
        if (string.IsNullOrWhiteSpace(spName)) {
            throw new ArgumentException("Starting Point name must be specified!");
        }
        string key = spName.ToUpper();
        return SPNames.TryGetValue(key, out var sp) ? sp : null;
    }

    // return a sorted list of all sites contained in scanned pages.
    public List<string> GetFoundWebsites() {
        var websites = new List<string>(WebsiteToPages.Keys);
        websites.Sort();
        return websites;
    }

    // return 
    public DBPage? GetPage(int pageID) {
        return Pages.TryGetValue(pageID, out var file) ? file : null;
    }

    public void AddPage(DBPage page) {
        Pages[page.ID] = page;
    }

    public void RemovePage(int pageID) {
        Pages.Remove(pageID);
    }

    public void AddPageURL(string URL, int pageID) {
        if (string.IsNullOrWhiteSpace(URL)) {
            throw new ArgumentException("URL must be specified!");
        }
        URLs[URL.ToLower()] = pageID;
    }

    public void RemovePageURL(string URL) {
        URLs.Remove(URL.ToLower());
    }

    public void AddPageWebsite(string website, int pageID) {
        if (string.IsNullOrWhiteSpace(website)) {
            throw new ArgumentException("Website must be specified!");
        }

        if (!WebsiteToPages.TryGetValue(website.ToLower(), out var set)) {
            set = [];
            WebsiteToPages[website.ToLower()] = set;
        }
        set.Add(pageID);
    }

    public void RemovePageWebsite(string website, int pageID) {
        if (WebsiteToPages.TryGetValue(website.ToLower(), out var set)) {
            set.Remove(pageID);
            if (set.Count == 0) {
                WebsiteToPages.Remove(website.ToLower());
            }
        }
    }

    public void AddPageName(string name, int pageID) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("Page name must be specified!");
        }

        if (!NameToPages.TryGetValue(name.ToUpper(), out var set)) {
            set = [];
            NameToPages[name.ToUpper()] = set;
        }
        set.Add(pageID);
    }

    public void RemovePageName(string name, int pageID) {
        if (NameToPages.TryGetValue(name.ToUpper(), out var set)) {
            set.Remove(pageID);
            if (set.Count == 0) {
                NameToPages.Remove(name.ToUpper());
            }
        }
    }

    public void AddPageKeyword(string keyword, int pageID) {
        if (string.IsNullOrWhiteSpace(keyword)) {
            throw new ArgumentException("Keyword must be specified!");
        }

        string key = keyword.ToUpper();
        if (!KeywordToPages.TryGetValue(key, out var set)) {
            set = [];
            KeywordToPages[key] = set;
        }
        set.Add(pageID);
    }

    public void RemovePageKeyword(string keyword, int pageID) {
        string key = keyword.ToUpper();
        if (KeywordToPages.TryGetValue(key, out var set)) {
            set.Remove(pageID);
            if (set.Count == 0) {
                KeywordToPages.Remove(key);
            }
        }
    }

    // Add or update a starting point information
    // Returns true if it was added, or false if it was updated
    public bool AddStartingPoint(StartingPoint sp) {
        bool added = true;
        if (string.IsNullOrWhiteSpace(sp.Name)) {
            throw new ArgumentException("Starting Point name must be specified!");
        }
        string name = sp.Name.ToUpper();
        if (SPNames.ContainsKey(name)) {
            added = false;
        }
        SPNames[name] = sp;
        return added;
    }

    // Removes a starting point
    // Returns true if it was removed, or false if starting point was not found
    public bool RemoveStartingPoint(string spName) {
        if (string.IsNullOrWhiteSpace(spName)) {
            throw new ArgumentException("Starting Point name must be specified!");
        }
        return SPNames.Remove(spName.ToUpper());
    }

    // Add or update a keyword
    // Returns the old keyword if it was updated, or null if it was added
    public string? AddKeyword(string keyword) {
        if (string.IsNullOrWhiteSpace(keyword)) {
            throw new ArgumentException("Keyword must be specified!");
        }
        string key = keyword.ToUpper();
        Keywords.TryGetValue(key, out string? oldKeyword);
        if (oldKeyword == null || oldKeyword != keyword) {
            Keywords[key] = keyword;
        }
        return oldKeyword;
    }

    // Removes a keyword
    // Returns true if it was removed, otherwise false
    public bool RemoveKeyword(string keyword) {
        if (string.IsNullOrWhiteSpace(keyword)) {
            throw new ArgumentException("Keyword must be specified!");
        }
        return Keywords.Remove(keyword.ToUpper());
    }

    public int GetPageID(string URL) {
        return URLs.TryGetValue(URL.ToLower(), out var id) ? id : 0;
    }

    public HashSet<int>? GetPageIDsFromWebsite(string website) {
        return WebsiteToPages.TryGetValue(website.ToLower(), out var set) ? set : null;
    }

    public HashSet<int>? GetPageIDsWithName(string name) {
        return NameToPages.TryGetValue(name.ToUpper(), out var set) ? set : null;
    }

    public HashSet<int>? GetPageIDsWithKeyword(string keyword) { 
        return KeywordToPages.TryGetValue(keyword.ToUpper(), out var set) ? set : null;
    }

    public Dictionary<string, int> GetDBStatistics() {
        var dbStatistics = new Dictionary<string, int>
        {
            { "NAMES", SPNames.Count },
            { "KEYS", Keywords.Count },
            { "WEBSITES", WebsiteToPages.Count },
            { "PAGES", Pages.Count },
        };
        return dbStatistics;
   }
}
