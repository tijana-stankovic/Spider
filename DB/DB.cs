namespace SpiderDB;

using SpiderStatus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public class DB {
    public static readonly string DefaultDbFilename = "spider_db.sdb";
    public static readonly int DbVersion = 1;
    private string _dbFilename = DefaultDbFilename;

    public DB(string dbFilename) {
        StatusCode = StatusCode.NoError;
        DbFilename = dbFilename;
        Data = new DBData(DbVersion);
        DataChanged = true;
        ReadDB();
    }

    private DBData Data { get; set; }
    public StatusCode StatusCode { get; set; }

    public string DbFilename {
        get => _dbFilename;
        set {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new ArgumentException("DB filename must be specified!");
            }
            if (_dbFilename != value) {
                _dbFilename = value;
                DataChanged = true;
            }
        }
    }

    public bool DataChanged { get; set; }

    public bool IsChanged() {
        return DataChanged;
    }

    public void DataSaved(bool saved) {
        DataChanged = !saved;
    }

    public bool IsSaved() {
        return !DataChanged;
    }

    public void ReadDB() {
        try {
            var json = File.ReadAllText(DbFilename);
            var data = JsonSerializer.Deserialize<DBData>(json);

            if (data == null) {
                StatusCode = StatusCode.DbFileIncompatibleFormat;
                return;
            }

            if (data.DbVersion != DbVersion) {
                StatusCode = StatusCode.DbFileIncompatibleFormat;
                return;
            }

            Data = data;
            DataChanged = false;
            StatusCode = StatusCode.NoError;
            
        } catch (FileNotFoundException) { // File not found
            StatusCode = StatusCode.DbFileDoesNotExist;
        } catch (JsonException) { // e.g. file exists, but is empty
            StatusCode = StatusCode.DbFileIncompatibleFormat;
        } catch (Exception) { // Read error
            StatusCode = StatusCode.DbFileReadError;
        }
    }

    public void WriteDB() {
        try {
            var json = JsonSerializer.Serialize(Data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(DbFilename, json);
            DataSaved(true);
            StatusCode = StatusCode.NoError;
        } catch (Exception) { // Write error
            StatusCode = StatusCode.DbFileWriteError;
        }
    }

    public int AddPage(DBPage page) {
        var keywords = new HashSet<string>();
        int oldPageID = Data.GetPageID(page.URL);

        if (oldPageID != 0) {
            page.ID = oldPageID;
            var p = GetPage(oldPageID);
            if (p != null) {
                keywords = p.Keywords;
            }
            RemovePage(oldPageID);
        } else {
            page.ID = NextPageID();
        }

        Data.AddPage(page);
        int pageID = page.ID;
        Data.AddPageURL(page.URL, pageID);
        Data.AddPageWebsite(page.Website, pageID);
        Data.AddPageName(page.Name, pageID);

        if (oldPageID != 0) {
            foreach (var keyword in keywords) {
                //AddKeyword(keyword, pageID);
            }
        }

        DataChanged = true;

        return oldPageID;
    }

    public void RemovePage(int pageID) {
        var page = Data.GetPage(pageID);
        if (page == null) return;

        Data.RemovePage(pageID);
        Data.RemovePageURL(page.URL);
        Data.RemovePageWebsite(page.Website, pageID);
        Data.RemovePageName(page.Name, pageID);

        foreach (var keyword in page.Keywords) {
            Data.RemovePageKeyword(keyword, pageID);
        }

        DataChanged = true;
    }

    public int NextPageID() { 
        return Data.NextPageID();
    }

    public int GetPageID(string URL) {
        return Data.GetPageID(URL);
    }

    public DBPage? GetPage(int pageID) { 
        return Data.GetPage(pageID);
    }

    public HashSet<int>? GetPageIDs(string key, char where) {
        HashSet<int>? pageIDs = null;
        switch (char.ToUpper(where)) {
            case 'P': {
                int pageID = Data.GetPageID(key);
                if (pageID != 0) {
                    pageIDs = [];
                    pageIDs.Add(pageID);
                }
                break;
            }
            case 'S': {
                pageIDs = Data.GetPageIDsOnWebsite(key);
                break;
            }
            case 'K': {
                pageIDs = Data.GetPageIDsWithKeyword(key);
                break;
            }
            default: {
                throw new ArgumentException("Method DB.GetPageIDs() - Invalid 'where' parameter value!");
            }
        }
        return pageIDs;
    }

    // Add or update a starting point information
    // Returns true if it was added, or false if it was updated
    public bool AddStartingPoint(StartingPoint sp) {
        bool added = false;
        if (Data.AddStartingPoint(sp)) {
            added = true;
        }
        DataChanged = true;
        return added;
    }

    // Removes a starting point
    // Returns true if it was removed, or false if starting point was not found
    public bool RemoveStartingPoint(string spName) {
        // TODO:  ovde je kompleksnije - treba ukloniti i sve sto je povezano sa ovim starting point-om
        //        dakle, sve stranice koje imaju veze sa tim SP, a kad se uklanjaju stranice, onda se uklanjaju i ostale veze
        //        u sustini, treba proci kroz NameToPages i za sve stranice povezane sa tim SP, treba ih ukloniti
        //        a to znaci da treba otici u DBPage za svaku od tih stranica i ukloniti url, website i keywords i na kraju samu stranicu
        if (Data.RemoveStartingPoint(spName)) {
            DataChanged = true;
            return true;
        }
        return false;
    }

    // Add or update a keyword
    // Returns the old keyword if it was updated, or null if it was added
    public string? AddKeyword(string keyword) {
        var oldKeyword = Data.AddKeyword(keyword);
        if (oldKeyword == null || oldKeyword != keyword) {
            DataChanged = true;
        }
        return oldKeyword;
    }

    // Removes a keyword
    // Returns the old keyword if it was updated, or null if keyword was not found
    public string? RemoveKeyword(string keyword) {
        // TODO: ovo je kompleksnije - treba ukloniti i sve veze ovog keyworda sa svim stranicama
        //       znaci ici kroz sve stranice koje su u KeywordToPages povezane sa ovom keyword
        //       za svaku od tih stranica ukloniti keyword iz _keywords stranice,
        //       pa na kraju ukloniti keyword i iz KeywordToPages
        var oldKeyword = Data.RemoveKeyword(keyword);
        if (oldKeyword != null) {
            DataChanged = true;
        }
        return oldKeyword;
    }

    // return a sorted list of all keywords in the database
    public List<string> GetKeywords() {
        return Data.GetKeywords();
    }

    // return a sorted list of all keywords contained in scanned pages.
    public List<string> GetFoundKeywords() {
        return Data.GetFoundKeywords();
    }

    // return a sorted list of all starting points names in the database
    public List<string> GetSPNames() {
        return Data.GetSPNames();
    }

    // return Starting Point with the specified name
    // if not found, return null
    public StartingPoint? GetStartingPoint(string spName) {
        return Data.GetStartingPoint(spName);
    }

    public List<string> GetFoundWebsites() { 
        return Data.GetFoundWebsites(); 
    }

    public Dictionary<string, int> GetDBStatistics() { 
        return Data.GetDBStatistics();
    }

    // public void Test() {
    //     DBPage page = new DBPage();
    //     page.ID = 1;
    //     page.Name = "Test Page";
    //     page.URL = "http://www.test.com";
    //     page.Website = "http://www.test.com";
    //     page.Keywords = [];
    //     Data.AddPage(page);
    // }
}
