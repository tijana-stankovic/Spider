namespace SpiderDB;

using SpiderStatus;
using SpiderView;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public class DB {
    public static readonly string DefaultDbFilename = "spider_db.sdb";
    public static readonly string DefaultDbExtension = "sdb";
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

    public string DbFilenameExtension { get; } = DefaultDbExtension;

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
            RestoreLogParameters();
            DataChanged = false;
            StatusCode = StatusCode.NoError;
            
        } catch (FileNotFoundException) { // File not found
            WriteDB(); // try to create new file
            if (StatusCode == StatusCode.NoError) {
                StatusCode = StatusCode.DbFileDoesNotExist; // show warning (that new file is created)
            } else {
                StatusCode = StatusCode.DbFileDoesNotExistError; // show error (that new file is not created)
            }
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
        var existingKeywords = new HashSet<string>();
        int oldPageID = Data.GetPageID(page.URL);

        if (oldPageID != 0) {
            page.ID = oldPageID;
            var p = GetPage(oldPageID);
            if (p != null) {
                existingKeywords = p.Keywords;
            }
            RemovePage(oldPageID);
        } else {
            page.ID = NextPageID();
        }

        Data.AddPage(page);
        int pageID = page.ID;
        Data.AddPageURL(page.URL, pageID);
        Data.AddPageName(page.Name, pageID);
        // add new found keywords
        foreach (var keyword in page.Keywords) {
            Data.AddPageKeyword(keyword, pageID);
        }
        // add old keywords
        foreach (var keyword in existingKeywords) {
            page.AddKeyword(keyword);
            Data.AddPageKeyword(keyword, pageID);
        }

        DataChanged = true;

        return oldPageID;
    }

    public void RemovePage(int pageID) {
        var page = Data.GetPage(pageID);
        if (page == null) return;

        Data.RemovePage(pageID);
        Data.RemovePageURL(page.URL);
        Data.RemovePageName(page.Name, pageID);

        foreach (var keyword in page.Keywords) {
            Data.RemovePageKeyword(keyword, pageID);
        }

        DataChanged = true;
    }

    public void RemovePageKeywordConnection(string keyword, int pageID) {
        var page = Data.GetPage(pageID);
        if (page != null) {
            page.RemoveKeyword(keyword);
        }
        Data.RemovePageKeyword(keyword, pageID);
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
            case 'K': {
                pageIDs = Data.GetPageIDsWithKeyword(key);
                break;
            }
            case 'N': {
                pageIDs = Data.GetPageIDsWithName(key);
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
        if (Data.RemoveStartingPoint(spName)) {

            var pageIDs = Data.GetPageIDsWithName(spName); // get all page IDs connected with this starting point
            if (pageIDs != null) {
                foreach (var pageID in pageIDs) { // remove all pages connected with this starting point
                    RemovePage(pageID);
                }
            }

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

    // Removes a keyword and all page/keyword connections
    // Returns true if keyword is removed, otherwise false
    public bool RemoveKeyword(string keyword) {

        if (Data.RemoveKeyword(keyword)) { // remove keyword from database

            var pageIDs = Data.GetPageIDsWithKeyword(keyword); // get all page IDs connected with this keyword
            if (pageIDs != null) {
                // remove all page/keyword connections from pages
                foreach (var pageID in pageIDs) {
                    var page = Data.GetPage(pageID);
                    if (page != null) {
                        if (page.Keywords.Count == 1) { // if this is the only keyword connected with this page
                            RemovePage(pageID); // remove whole page from the database
                        } else {
                            page.RemoveKeyword(keyword); // remove only this keyword/page connection
                        }
                    }
                }
                Data.KeywordToPages.Remove(keyword.ToUpper()); // remove keyword from keyword-to-pages index 
            }

            DataChanged = true;
            return true;
        }
        return false;
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

    public HashSet<int>? GetPageIDsWithName(string name) {
        return Data.GetPageIDsWithName(name);
    }

    public HashSet<int>? GetPageIDsWithKeyword(string keyword) { 
        return Data.GetPageIDsWithKeyword(keyword);
    }

    public Dictionary<string, int> GetDBStatistics() { 
        return Data.GetDBStatistics();
    }

    public void SaveLogParameters() {
        Data.LogFileName = View.LogFileName;
        DataChanged = true;
    }

    public void RestoreLogParameters() {
        View.LogFileName = Data.LogFileName;
    }
}
