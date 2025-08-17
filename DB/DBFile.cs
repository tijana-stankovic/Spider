namespace PhotoDB;

using PhotoUtil;
using System;
using System.Collections.Generic;

public class DBFile {
    private int _id = 0;
    private string _fullpath = "";
    private string _location = "";
    private string _filename = "";
    private string _extension = "";
    private string _timestamp = "";
    private long _size = 0L;
    private HashSet<string> _keywords = new();
    private HashSet<MetadataInfo> _metadata = new();
    private HashSet<int> _duplicates = new();
    private HashSet<int> _potentialDuplicates = new();

    public DBFile() {
        Checksum = 0;
    }

    public DBFile(int id, 
                string fullpath, string location, string filename, string extension, 
                string timestamp, long size, int checksum, 
                HashSet<String> keywords, HashSet<MetadataInfo> metadata,
                HashSet<int> duplicates, HashSet<int> potentialDuplicates) {
        ID = id;
        Fullpath = fullpath;
        Location = location;
        Filename= filename;
        Extension = extension;
        Timestamp = timestamp;
        Size = size;
        Checksum = checksum;
        Keywords = keywords;
        Metadata = metadata;
        Duplicates = duplicates;
        PotentialDuplicates = potentialDuplicates;
    }

    public int ID {
        get => _id;
        set {
            if (value <= 0) {
                throw new ArgumentException("File ID must be positive!");
            }
            _id = value;
        }
    }

    public string Fullpath {
        get => _fullpath;
        set {
            if (string.IsNullOrEmpty(value)) {
                throw new ArgumentException("File path must be specified!");
            }
            _fullpath = value;
        }
    }

    public string Location {
        get => _location;
        set {
            if (string.IsNullOrEmpty(value)) {
                throw new ArgumentException("File location must be specified!");
            }
            _location = value;
        }
    }

    public string Filename {
        get => _filename;
        set {
            if (string.IsNullOrEmpty(value)) {
                throw new ArgumentException("Filename must be specified!");
            }
            _filename = value;
        }
    }

    public string Extension {
        get => _extension;
        set {
            if (value == null) {
                throw new ArgumentException("Extension must be specified!");
            }
            _extension = value;
        }
    }

    public string Timestamp {
        get => _timestamp;
        set {
            if (string.IsNullOrEmpty(value)) {
                throw new ArgumentException("Timestamp must be specified!");
            }
            _timestamp = value;
        }
    }

    public long Size {
        get => _size;
        set {
            if (value < 0) {
                throw new ArgumentException("Size must not be negative!");
            }
            _size = value;
        }
    }

    public int Checksum { get; set; }
   
    public HashSet<string> Keywords {
        get => _keywords;
        set => _keywords = value ?? new HashSet<string>();
    }

    public HashSet<MetadataInfo> Metadata {
        get => _metadata;
        set => _metadata = value ?? new HashSet<MetadataInfo>();
    }

    public HashSet<int> Duplicates {
        get => _duplicates;
        set => _duplicates = value ?? new HashSet<int>();
    }

    public HashSet<int> PotentialDuplicates {
        get => _potentialDuplicates;
        set => _potentialDuplicates = value ?? new HashSet<int>();
    }

    public void AddKeyword(string keyword) {
        if (string.IsNullOrEmpty(keyword)) {
            throw new ArgumentException("Keyword must be specified!");
        }

        Keywords.Add(keyword.ToUpper());
    }

    public void RemoveKeyword(string keyword) {
        Keywords.Remove(keyword.ToUpper());
    }

    public void AddMetadata(MetadataInfo metadataInfo) {
        if (metadataInfo == null) {
            throw new ArgumentException("Metadata must be specified!");
        }
        Metadata.Add(metadataInfo);
    }

    public void RemoveMetadata(MetadataInfo metadataInfo) {
        Metadata.Remove(metadataInfo);
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
}
