namespace SpiderDB;

// search starting point
public class StartingPoint(string name, string url, int intDepth, int extDepth, string baseUrl) {
    private string _name = name;
    private string _url = url;
    private int _internalDepth = intDepth;
    private int _externalDepth = extDepth;
    private string _baseUrl = baseUrl;
    private DateTime _lastScanTimestamp = DateTime.MinValue; // TODO

    // for JSON deserialization (in DB.WriteDB()) to work properly,
    // we need this default (parameterless) constructor.
    // without this, JsonSerializer.Deserialize() in ReadDB() will generate exception
    public StartingPoint() : this("", "", 0, 0, "") {}

    public string Name {
        get => _name;
        set {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new ArgumentException("Name must be specified!");
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

    public int InternalDepth {
        get => _internalDepth;
        set {
            if (value < 0) {
                throw new ArgumentException("Internal depth value must be non-negative!");
            }
            _internalDepth = value;
        }
    }

    public int ExternalDepth {
        get => _externalDepth;
        set
        {
            if (value < 0)
            {
                throw new ArgumentException("External depth value must be non-negative!");
            }
            _externalDepth = value;
        }
    }

    public string baseURL {
        get => _baseUrl;
        set {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new ArgumentException("Base URL must be specified!");
            }
            _baseUrl = value;
        }
    }

    public DateTime LastScanTimestamp {
        get => _lastScanTimestamp;
        set => _lastScanTimestamp = value;
    }
}
