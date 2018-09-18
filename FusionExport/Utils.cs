using Ionic.Zip;
using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using NDepend.Path;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace FusionCharts.FusionExport.Utils
{
    public static class Utils
    {
        public static List<T> EmptyListIfNull<T>(IEnumerable<T> input)
        {
            if (input != null)
            {
                return input.ToList();
            }
            else
            {
                return new List<T>();
            }
        }

        public static string GetTempFolderName(bool ensureDirectoryExist = false)
        {
            var folderName = Path.GetFileNameWithoutExtension(Path.GetTempFileName());
            string tempPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), folderName));

            if (ensureDirectoryExist && !Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }
            return tempPath;
        }

        public static string GetTempFileName()
        {
            var fileName = Path.GetTempFileName();
            return Path.GetFullPath(Path.Combine(Path.GetTempPath(), fileName));
        }

        public static string GetCommonAncestorDirectory(string[] s)
        {
            // Will be a directory name without trailing slash
            s = s.Select((pathString) => Path.GetFullPath(pathString)).ToArray();

            // Source: Modified from https://stackoverflow.com/a/24867012/2534890

            int k = s[0].Length;
            for (int i = 1; i < s.Length; i++)
            {
                k = Math.Min(k, s[i].Length);
                for (int j = 0; j < k; j++)
                    if (s[i][j] != s[0][j])
                    {
                        k = j;
                        break;
                    }
            }
            var commonSubString = s[0].Substring(0, k);

            try
            {
                var commonDirectoryPath = Path.GetDirectoryName(commonSubString);

                return commonDirectoryPath;
            }
            catch (Exception ex)
            {
                if ((ex.GetType() == typeof(ArgumentException)) ||
                    (ex.GetType() == typeof(PathTooLongException)))
                {

                    return null;
                }
                else
                {
                    throw ex;
                }
            }

        }

        public static string GetAbsolutePathFrom(string filePath)
        {
            return filePath.ToAbsoluteFilePath().FileInfo.FullName;
        }

        public static string GetRelativePathFrom(string absoluteFilePath, string baseDirectoryPath)
        {
            var absoluteNDependFilePath = absoluteFilePath.ToAbsoluteFilePath();
            var baseNDependDirectoryPath = baseDirectoryPath.ToAbsoluteDirectoryPath();

            var relativeNDependFilePath = absoluteNDependFilePath.GetRelativePathFrom(baseNDependDirectoryPath);

            return relativeNDependFilePath.ToString();
        }

        public static bool IsWithinPath(string checkeePath, string parentDirectoryPath)
        {
            // Normalize parentDirectoryPath
            parentDirectoryPath = Path.GetFullPath(parentDirectoryPath);

            var commonDirectoryPath = GetCommonAncestorDirectory(new string[] { checkeePath, parentDirectoryPath });
            return (commonDirectoryPath == Path.GetDirectoryName(parentDirectoryPath));
        }

        public static string GetFullPathWrtBasePath(string extraPath, string basePath)
        {
            if (Path.GetFullPath(extraPath) == extraPath)
            {
                return extraPath;
            }
            else
            {
                return Path.Combine(basePath, extraPath);
            }
        }

        public static string GetRandomFileNameWithExtension(string targetExtension)
        {
            return String.Join("",
                new string[] {
                        Path.GetRandomFileName(),
                        Path.GetExtension(targetExtension) });
        }

        public static bool isLocalResource(string testResourceFilePath)
        {
            Regex remoteResourcePattern = new Regex(@"^http(s)?:\/\/");
            return !remoteResourcePattern.IsMatch(testResourceFilePath.Trim());
        }

        public static string ReadFileContent(string potentiallyFilePath, bool encodeBase64 = false)
        {
            if (isLocalResource(potentiallyFilePath))
            {
                try
                {
                    string content;
                    if (encodeBase64)
                    {
                        Byte[] bytes = File.ReadAllBytes(potentiallyFilePath);
                        content = Convert.ToBase64String(bytes);
                    }
                    else
                    {
                        content = File.ReadAllText(potentiallyFilePath);
                    }
                    return content;
                }
                catch (Exception ex)
                {
                    if (
                        (ex is PathTooLongException) ||
                        (ex is DirectoryNotFoundException) ||
                        (ex is IOException) ||
                        (ex is FileNotFoundException) ||
                        (ex is ArgumentException)
                        )
                    {
                        return potentiallyFilePath;
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }
            else
            {
                return potentiallyFilePath;
            }

        }

        public static bool CopyFile(string sourceFilePath, string destFilePath)
        {
            if (!File.Exists(destFilePath))
            {
                File.Copy(sourceFilePath, destFilePath);
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void CreateZipFromDirectory(string sourceFolderPath, string destinationZipFolder)
        {
            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(sourceFolderPath);
                zip.Save(destinationZipFolder);
            }
        }

        public static List<String> ExtractZipInDirectory(string zipFullName, string destinationZipFolder)
        {
            List<String> files = new List<string>();

            if (File.Exists(zipFullName))
            {
                if (!Directory.Exists(destinationZipFolder))
                {
                    Directory.CreateDirectory(destinationZipFolder);
                }

                using (ZipFile zip = ZipFile.Read(zipFullName))
                {
                    zip.ExtractExistingFile = ExtractExistingFileAction.OverwriteSilently;
                    zip.ExtractAll(destinationZipFolder);

                    foreach(ZipEntry entry in zip.Entries)
                    {
                        if (!entry.IsDirectory)
                        {
                            string filePath = Path.Combine(destinationZipFolder, entry.FileName.ToString());
                            files.Add(Path.Combine(filePath.ToAbsoluteFilePath().FileInfo.FullName));
                        }
                    }
                }
            }

            return files;
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue defaultValue)
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }

        public static string GetBaseDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }
    }
}
