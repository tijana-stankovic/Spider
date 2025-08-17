namespace PhotoUtil;

public class MetadataInfo {
    private string _directory = "";
    private string _tag = "";
    private string _description = "";

    public MetadataInfo(string directory, string tag, string description) {
        Directory = directory;
        Tag = tag;
        Description = description;
    }

    public string Directory {
        get => _directory;
        set => _directory = value ?? "";
    }

    public string Tag {
        get => _tag;
        set => _tag = value ?? "";
    }

    public string Description {
        get => _description;
        set => _description = value ?? "";
    }
}
