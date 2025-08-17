namespace PhotoDB;

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
    public int LastFileID { get; set; } = 0;
    public Dictionary<int, DBFile> Files { get; set; } = new();
    public Dictionary<string, int> Fullpaths { get; set; } = new();
    public Dictionary<string, HashSet<int>> Locations { get; set; } = new();
    public Dictionary<string, HashSet<int>> Filenames { get; set; } = new();
    public Dictionary<string, HashSet<int>> Extensions { get; set; } = new();
    public Dictionary<string, HashSet<int>> Timestamps { get; set; } = new();
    public Dictionary<long, HashSet<int>> Sizes { get; set; } = new();
    public Dictionary<int, HashSet<int>> Checksums { get; set; } = new();
    public Dictionary<string, HashSet<int>> Keywords { get; set; } = new();
    public Dictionary<string, HashSet<int>> MetadataTags { get; set; } = new();
    public HashSet<int> Duplicates { get; set; } = new();
    public HashSet<int> PotentialDuplicates { get; set; } = new();

    public int NextFileID() {
        LastFileID++;
        return LastFileID;
    }

    // return a sorted list of all keywords in the database
    public List<string> GetKeywords() {
        var keys = new List<string>(Keywords.Keys);
        keys.Sort();
        return keys;
    }

    // return a sorted list of all directories in the database
    public List<string> GetDirectories() {
        var dirs = new List<string>(Locations.Keys);
        dirs.Sort();
        return dirs;
    }

    public DBFile? GetFile(int fileID) {
        return Files.TryGetValue(fileID, out var file) ? file : null;
    }

    public void AddFile(DBFile file) {
        Files[file.ID] = file;
    }

    public void RemoveFile(int fileID) {
        Files.Remove(fileID);
    }

    public void AddFilePath(string fullpath, int fileID) {
        if (string.IsNullOrEmpty(fullpath)) {
            throw new ArgumentException("Path must be specified!");
        }
        Fullpaths[fullpath] = fileID;
    }

    public void RemoveFilePath(string fullpath) {
        Fullpaths.Remove(fullpath);
    }

    public void AddFileLocation(string location, int fileID) {
        if (string.IsNullOrEmpty(location)) {
            throw new ArgumentException("File location must be specified!");
        }

        if (!Locations.TryGetValue(location, out var set)) {
            set = new HashSet<int>();
            Locations[location] = set;
        }
        set.Add(fileID);
    }

    public void RemoveFileLocation(string location, int fileID) {
        if (Locations.TryGetValue(location, out var set)) {
            set.Remove(fileID);
            if (set.Count == 0) {
                Locations.Remove(location);
            }
        }
    }

    public void AddFileFilename(string filename, int fileID) {
        if (string.IsNullOrEmpty(filename)) {
            throw new ArgumentException("Filename must be specified!");
        }

        if (!Filenames.TryGetValue(filename, out var set)) {
            set = new HashSet<int>();
            Filenames[filename] = set;
        }
        set.Add(fileID);
    }

    public void RemoveFileFilename(string filename, int fileID) {
        if (Filenames.TryGetValue(filename, out var set)) {
            set.Remove(fileID);
            if (set.Count == 0) {
                Filenames.Remove(filename);
            }
        }
    }

    public void AddFileExtension(string extension, int fileID) {
        if (string.IsNullOrEmpty(extension)) {
            throw new ArgumentException("File extension must be specified!");
        }

        if (!Extensions.TryGetValue(extension, out var set)) {
            set = new HashSet<int>();
            Extensions[extension] = set;
        }
        set.Add(fileID);
    }

    public void RemoveFileExtension(string extension, int fileID) {
        if (Extensions.TryGetValue(extension, out var set)) {
            set.Remove(fileID);
            if (set.Count == 0) {
                Extensions.Remove(extension);
            }
        }
    }

    public void AddFileTimestamp(string timestamp, int fileID) {
        if (string.IsNullOrEmpty(timestamp)) {
            throw new ArgumentException("File timestamp must be specified!");
        }

        if (!Timestamps.TryGetValue(timestamp, out var set)) {
            set = new HashSet<int>();
            Timestamps[timestamp] = set;
        }
        set.Add(fileID);
    }

    public void RemoveFileTimestamp(string timestamp, int fileID) {
        if (Timestamps.TryGetValue(timestamp, out var set)) {
            set.Remove(fileID);
            if (set.Count == 0) {
                Timestamps.Remove(timestamp);
            }
        }
    }

    public void AddFileSize(long size, int fileID) {
        if (size < 0) {
            throw new ArgumentException("Size must not be negative!");
        }

        if (!Sizes.TryGetValue(size, out var set)) {
            set = new HashSet<int>();
            Sizes[size] = set;
        }
        set.Add(fileID);
    }

    public void RemoveFileSize(long size, int fileID) {
        if (Sizes.TryGetValue(size, out var set)) {
            set.Remove(fileID);
            if (set.Count == 0) {
                Sizes.Remove(size);
            }
        }
    }

    public void AddFileChecksum(int checksum, int fileID) {
        if (!Checksums.TryGetValue(checksum, out var set)) {
            set = new HashSet<int>();
            Checksums[checksum] = set;
        }
        set.Add(fileID);
    }

    public void RemoveFileChecksum(int checksum, int fileID) {
        if (Checksums.TryGetValue(checksum, out var set)) {
            set.Remove(fileID);
            if (set.Count == 0) {
                Checksums.Remove(checksum);
            }
        }
    }

    public void AddFileKeyword(string keyword, int fileID) {
        if (string.IsNullOrEmpty(keyword)) {
            throw new ArgumentException("Keyword must be specified!");
        }

        string key = keyword.ToUpper();
        if (!Keywords.TryGetValue(key, out var set)) {
            set = new HashSet<int>();
            Keywords[key] = set;
        }
        set.Add(fileID);
    }

    public void RemoveFileKeyword(string keyword, int fileID) {
        string key = keyword.ToUpper();
        if (Keywords.TryGetValue(key, out var set)) {
            set.Remove(fileID);
            if (set.Count == 0) {
                Keywords.Remove(key);
            }
        }
    }
    public void AddFileMetadataTag(string metadataTag, int fileID) {
        if (string.IsNullOrEmpty(metadataTag)) {
            throw new ArgumentException("Metadata tag must be specified!");
        }

        if (!MetadataTags.TryGetValue(metadataTag, out var set)) {
            set = new HashSet<int>();
            MetadataTags[metadataTag] = set;
        }
        set.Add(fileID);
    }

    public void RemoveFileMetadataTag(string metadataTag, int fileID) {
        if (MetadataTags.TryGetValue(metadataTag, out var set)) {
            set.Remove(fileID);
            if (set.Count == 0) {
                MetadataTags.Remove(metadataTag);
            }
        }
    }

    public int GetFileID(string fullpath) {
        return Fullpaths.TryGetValue(fullpath, out var id) ? id : 0;
    }

    public int GetFileID(string location, string filename, string extension) {
        var filenameIDs = Filenames.TryGetValue(filename, out var set1) ? set1 : new HashSet<int>();
        var locationIDs = Locations.TryGetValue(location, out var set2) ? set2 : new HashSet<int>();
        var extensionIDs = Extensions.TryGetValue(extension, out var set3) ? set3 : new HashSet<int>();

        foreach (var fileID in filenameIDs) {
            if (locationIDs.Contains(fileID) && extensionIDs.Contains(fileID))
                return fileID;
        }

        return 0;
    }

    public HashSet<int> FindPotentialDuplicatesIDs(long size, int checksum) {
        var foundIDs = new HashSet<int>();
        var sizeIDs = Sizes.TryGetValue(size, out var set1) ? set1 : new HashSet<int>();
        var checksumIDs = Checksums.TryGetValue(checksum, out var set2) ? set2 : new HashSet<int>();;

        foreach (var fileID in sizeIDs) {
            if (checksumIDs.Contains(fileID)) {
                foundIDs.Add(fileID);
            }
        }

        return foundIDs;
    }

    public HashSet<int>? GetFileIDsInLocation(string location) {
        return Locations.TryGetValue(location, out var set) ? set : null;
    }

    public HashSet<int>? GetFileIDsWithKeyword(string keyword) { 
        return Keywords.TryGetValue(keyword.ToUpper(), out var set) ? set : null;
    }

    public void AddDuplicate(int duplicateFileID) {
        if (duplicateFileID <= 0) {
            throw new ArgumentException("Duplicate file ID must be positive!");
        }
        Duplicates.Add(duplicateFileID);
    }

    public void RemoveDuplicate(int duplicateFileID) { 
        Duplicates.Remove(duplicateFileID);
    }

    public void AddPotentialDuplicate(int potentialDuplicateFileID) {
        if (potentialDuplicateFileID <= 0) {
            throw new ArgumentException("Potential duplicate file ID must be positive!");
        }
        PotentialDuplicates.Add(potentialDuplicateFileID);
    }

    public void RemovePotentialDuplicate(int potentialDuplicateFileID) {
        PotentialDuplicates.Remove(potentialDuplicateFileID);
    }

    public Dictionary<string, int> GetDBStatistics() {
        var dbStatistics = new Dictionary<string, int>();
        dbStatistics.Add("FILES", Files.Count);
        dbStatistics.Add("DIRS", Locations.Count);
        dbStatistics.Add("KEYS", Keywords.Count);
        dbStatistics.Add("DUPS", Duplicates.Count);
        dbStatistics.Add("DUP?S", PotentialDuplicates.Count);
        return dbStatistics;
   }
}
