namespace PhotoUtil;

using PhotoStatus;
using PhotoDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using MetadataExtractor;

public class FileSystem {
    public static StatusCode StatusCode { get; set; }

    public static char CheckPath(string path) {
        if (File.Exists(path)) {
            return 'F'; // it's a file
        } else if (System.IO.Directory.Exists(path)) {
            return 'D'; // it's a directory
        } 
        return 'E'; // path doesn't exist
    }

    public static List<string> FilesInDirectory(string directory) {
        var listOfFiles = new List<string>();

        StatusCode = StatusCode.NoError;

        var dirInfo = new DirectoryInfo(directory);
        if (dirInfo.Exists) {
            try {
                // add the full absolute directory path as the first element in the result
                listOfFiles.Add(Path.GetFullPath(directory));

                foreach (var file in dirInfo.GetFiles()) {
                    listOfFiles.Add(file.FullName);
                }
            } catch (IOException) {
                StatusCode = StatusCode.FileSystemError;
            }
        }

        return listOfFiles;
    }

    public static DBFile GetFileInformation(string filename) {
        StatusCode = StatusCode.NoError;

        var dbFile = new DBFile();

        try {
            if (File.Exists(filename)) {
                var fileInfo = new FileInfo(filename);

                string fullpath = fileInfo.FullName;
                string location = fileInfo.DirectoryName ?? "";
                string fname = Path.GetFileNameWithoutExtension(filename);
                string extension = Path.GetExtension(filename).TrimStart('.');
                string timestamp = fileInfo.LastWriteTime.ToString("yyyyMMdd HHmmss"); // HH for 24-hour format
                long size = fileInfo.Length;
                int checksum = CalculateChecksum(fileInfo);
                HashSet<MetadataInfo> metadata = ReadMetadata(fileInfo);

                dbFile.Fullpath = fullpath;
                dbFile.Location = location;
                dbFile.Filename = fname;
                dbFile.Extension = extension;
                dbFile.Timestamp = timestamp;
                dbFile.Size = size;
                dbFile.Checksum = checksum;
                dbFile.Metadata = metadata;
            } else {
                StatusCode = StatusCode.FileSystemNotFile;
            }
        } catch (IOException) {
            StatusCode = StatusCode.FileSystemError;
        }

        return dbFile;
    }

    // calculates the checksum of a file using CRC32
    // Based on:
    //   https://stackoverflow.com/questions/8128/how-do-i-calculate-crc32-of-a-string
    public static int CalculateChecksum(FileInfo fileInfo) {
        StatusCode = StatusCode.NoError;
        var crc32 = new Crc32();
        try {
            using var stream = new BufferedStream(fileInfo.OpenRead());
            crc32.Append(stream);
        }
        catch (IOException) {
            StatusCode = StatusCode.FileSystemError;
        }
        byte[] hashBytes = crc32.GetCurrentHash();
        return BitConverter.ToInt32(hashBytes, 0);
    }

    public static string ExtractFilename(string filename) {
        return Path.GetFileName(filename);
    }

    public static string FormatFileSize(long sizeInBytes) {
        string[] units = {"B", "KB", "MB", "GB", "TB"};
        double size = sizeInBytes;
        int unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1) {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:F2} {units[unitIndex]}";
    }

    // Based on README information on:
    // https://github.com/drewnoakes/metadata-extractor-dotnet
    public static HashSet<MetadataInfo> ReadMetadata(FileInfo fileInfo) {
        StatusCode = StatusCode.NoError;
        var metadataSet = new HashSet<MetadataInfo>();

        try {
            var directories = ImageMetadataReader.ReadMetadata(fileInfo.FullName);
            foreach (var directory in directories) {
                foreach (var tag in directory.Tags) {
                    metadataSet.Add(new MetadataInfo(directory.Name, tag.Name, tag.Description ?? ""));
                }
            }
        } catch (ImageProcessingException) {
            StatusCode = StatusCode.FileSystemNotImage;
        } catch (IOException) {
            StatusCode = StatusCode.FileSystemError;
        }

        return metadataSet;
    }

    public static bool CompareFiles(string path1, string path2) {
        StatusCode = StatusCode.NoError;

        try {
            var file1 = new FileInfo(path1);
            var file2 = new FileInfo(path2);

            if (!file1.Exists || !file2.Exists || file1.Length != file2.Length) {
                return false;
            }

            using var stream1 = file1.OpenRead();
            using var stream2 = file2.OpenRead();

            var buffer1 = new byte[1024];
            var buffer2 = new byte[1024];

            int bytesRead1;
            int bytesRead2;

            while ((bytesRead1 = stream1.Read(buffer1, 0, buffer1.Length)) > 0) {
                bytesRead2 = stream2.Read(buffer2, 0, buffer2.Length);
                if (bytesRead1 != bytesRead2 || !buffer1.Take(bytesRead1).SequenceEqual(buffer2.Take(bytesRead2))) {
                    return false;
                }
            }            
        } catch (IOException) {
            StatusCode = StatusCode.FileSystemError;
            return false;
        }

        return true;
    }
}
