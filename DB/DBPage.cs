namespace SpiderDB;

using System;
using System.Collections.Generic;

public class DBPage {
    private int _id = 0;
    private string _name = ""; // page 'origin' (the name of the page we started from to get to this page)
    private string _url = ""; // full page URL
    private HashSet<string> _keywords = []; // all the keywords the page contains

    // for JSON deserialization (in DB.WriteDB()) to work properly,
    // we need this default (parameterless) constructor.
    // without this, JsonSerializer.Deserialize() in ReadDB() will generate exception
    public DBPage() {}

    public DBPage(int id, 
                string url, string name, 
                HashSet<String> keywords) {
        ID = id;
        URL = url;
        Name = name;
        Keywords = keywords;
    }

    public int ID {
        get => _id;
        set {
            if (value <= 0) {
                throw new ArgumentException("HTML page ID must be positive!");
            }
            _id = value;
        }
    }

    public string Name {
        get => _name;
        set {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new ArgumentException("Page name must be specified!");
            }
            _name = value;
        }
    }

    public string URL {
        get => _url;
        set {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new ArgumentException("URL must be specified!");
            }
            _url = value;
        }
    }

    public HashSet<string> Keywords {
        get => _keywords;
        // convert all keywords to uppercase
        set => _keywords = new HashSet<string>(value.Select(k => k.ToUpper())) ?? [];
    }

    public void AddKeyword(string keyword) {
        if (string.IsNullOrWhiteSpace(keyword)) {
            throw new ArgumentException("Keyword must be specified!");
        }

        Keywords.Add(keyword.ToUpper());
    }

    public void RemoveKeyword(string keyword) {
        Keywords.Remove(keyword.ToUpper());
    }
}
