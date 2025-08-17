namespace PhotoDB;

using PhotoStatus;
using PhotoUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public class DB {
    public static readonly string DefaultDbFilename = "photo_db.pdb";
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
            if (string.IsNullOrEmpty(value)) {
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

    public int AddFile(DBFile file) {
        var keywords = new HashSet<string>();
        int oldFileID = Data.GetFileID(file.Fullpath);

        if (oldFileID != 0) {
            file.ID = oldFileID;
            var f = GetFile(oldFileID);
            if (f != null) {
                keywords = f.Keywords;
            }
            RemoveFile(oldFileID);
        } else {
            file.ID = NextFileID();
        }

        Data.AddFile(file);
        int fileID = file.ID;
        Data.AddFilePath(file.Fullpath, fileID);
        Data.AddFileLocation(file.Location, fileID);
        Data.AddFileFilename(file.Filename, fileID);
        Data.AddFileExtension(file.Extension, fileID);
        Data.AddFileTimestamp(file.Timestamp, fileID);
        Data.AddFileSize(file.Size, fileID);
        Data.AddFileChecksum(file.Checksum, fileID);

        if (oldFileID != 0) {
            foreach (var keyword in keywords) {
                AddKeyword(keyword, fileID);
            }
        }

        foreach (var meta in file.Metadata) {
            Data.AddFileMetadataTag(meta.Tag, fileID);
        }

        foreach (var potentialDuplicateFileID in Data.FindPotentialDuplicatesIDs(file.Size, file.Checksum)) {
            if (potentialDuplicateFileID != fileID) {
                file.AddPotentialDuplicate(potentialDuplicateFileID);
                var potentialDuplicateFile = Data.GetFile(potentialDuplicateFileID);
                potentialDuplicateFile?.AddPotentialDuplicate(fileID);
                Data.AddPotentialDuplicate(fileID);
                Data.AddPotentialDuplicate(potentialDuplicateFileID);
                AddKeyword("DUP?", fileID);
                AddKeyword("DUP?", potentialDuplicateFileID);
            }
        }

        DataChanged = true;

        return oldFileID;
    }

    public void RemoveFile(int fileID) {
        var file = Data.GetFile(fileID);
        if (file == null) return;

        Data.RemoveFile(fileID);
        Data.RemoveFilePath(file.Fullpath);
        Data.RemoveFileLocation(file.Location, fileID);
        Data.RemoveFileFilename(file.Filename, fileID);
        Data.RemoveFileExtension(file.Extension, fileID);
        Data.RemoveFileTimestamp(file.Timestamp, fileID);
        Data.RemoveFileSize(file.Size, fileID);
        Data.RemoveFileChecksum(file.Checksum, fileID);

        foreach (var keyword in file.Keywords) {
            Data.RemoveFileKeyword(keyword, fileID);
        }
        foreach (var metadataInfo in file.Metadata) {
            Data.RemoveFileMetadataTag(metadataInfo.Tag, fileID);
        }

        RemoveFileDuplicateInformation(file);

        DataChanged = true;
    }

    public void RemoveFileDuplicateInformation(DBFile file) {
        int fileID = file.ID;

        foreach (var duplicateFileID in file.Duplicates) {
            var duplicateFile = Data.GetFile(duplicateFileID);
            duplicateFile?.RemoveDuplicate(fileID);
            if (duplicateFile?.Duplicates.Count == 0) {
                Data.RemoveDuplicate(duplicateFileID);
                RemoveKeyword("DUP", duplicateFileID);
            }
        }

        foreach (var potentialDuplicateFileID in file.PotentialDuplicates) {
            var potentialDuplicateFile = Data.GetFile(potentialDuplicateFileID);
            potentialDuplicateFile?.RemovePotentialDuplicate(fileID);
            if (potentialDuplicateFile?.PotentialDuplicates.Count == 0) {
                Data.RemovePotentialDuplicate(potentialDuplicateFileID);
                RemoveKeyword("DUP?", potentialDuplicateFileID);
            }
        }

        file.Duplicates = new();
        file.PotentialDuplicates = new();
        Data.RemoveDuplicate(fileID);
        RemoveKeyword("DUP", fileID);
        Data.RemovePotentialDuplicate(fileID);
        RemoveKeyword("DUP?", fileID);

        DataChanged = true;
    }

    public Dictionary<int, int> ProcessDuplicates(int fileID) {
        HashSet<int> duplicatesIDs = new();
        duplicatesIDs.Add(fileID);
        DBFile file = Data.GetFile(fileID)!;
        foreach (int duplicateFileID in Data.FindPotentialDuplicatesIDs(file.Size, file.Checksum)) {
            if (duplicateFileID != fileID) {
                DBFile duplicateFile = Data.GetFile(duplicateFileID)!;
                if (FileSystem.CompareFiles(file.Fullpath, duplicateFile.Fullpath)) {
                    duplicatesIDs.Add(duplicateFileID);
                }
            }
        }

        Dictionary<int, int> duplicatesFound = new();
        int numOfDuplicates = duplicatesIDs.Count - 1;
        if (numOfDuplicates > 0) {
            foreach (int fID in duplicatesIDs) {
                RemoveFileDuplicateInformation(Data.GetFile(fID)!);
                duplicatesFound[fID] = numOfDuplicates;
            }

            foreach (int fID in duplicatesIDs) {
                file = Data.GetFile(fID)!;
                foreach (int fDupID in duplicatesIDs) {
                    if (fID != fDupID) {
                        file.AddDuplicate(fDupID);
                        AddKeyword("DUP", fID);
                        Data.AddDuplicate(fID);
                    }
                }
            }
        } else {
            RemoveFileDuplicateInformation(file);
        }

        DataChanged = true;

        return duplicatesFound;
    }

    public int NextFileID() { 
        return Data.NextFileID();
    }

    public int GetFileID(string fullpath) {
        return Data.GetFileID(fullpath);
    }

    public int GetFileID(string location, string filename, string extension) {
        return Data.GetFileID(location, filename, extension);
    }

    public DBFile? GetFile(int fileID) { 
        return Data.GetFile(fileID);
    }

    public HashSet<int>? GetFileIDs(string key, char where) {
        HashSet<int>? fileIDs = null;
        switch (char.ToUpper(where)) {
            case 'F': {
                int fileID = Data.GetFileID(key);
                if (fileID != 0) {
                    fileIDs = new HashSet<int>();
                    fileIDs.Add(fileID);
                }
                break;
            }
            case 'D': {
                fileIDs = Data.GetFileIDsInLocation(key);
                break;
            }
            case 'K': {
                fileIDs = Data.GetFileIDsWithKeyword(key);
                break;
            }
            default: {
                throw new ArgumentException("Method DB.GetFileIDs() - Invalid 'where' parameter value!");
            }
        }
        return fileIDs;
    }

    public void AddKeyword(string keyword, int fileID) {
        var file = Data.GetFile(fileID);
        if (file != null) {
            file.AddKeyword(keyword);
            Data.AddFileKeyword(keyword, fileID);
            DataChanged = true;
        }
    }

    public void RemoveKeyword(string keyword, int fileID) {
        var file = Data.GetFile(fileID);
        if (file != null) {
            file.RemoveKeyword(keyword);
            Data.RemoveFileKeyword(keyword, fileID);
            DataChanged = true;
        }
    }

    public List<string> GetKeywords() {
        return Data.GetKeywords();
    }

    public List<string> GetDirectories() { 
        return Data.GetDirectories(); 
    }

    public Dictionary<string, int> GetDBStatistics() { 
        return Data.GetDBStatistics();
    }
}
