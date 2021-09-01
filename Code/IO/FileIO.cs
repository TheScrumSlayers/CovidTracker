using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace CovidTracker.Code.IO
{
    public static class FileIO
    {
        /// <summary>Max entries before the system considers removing old information.</summary>
        public static uint MaxEntries { get; set; } = 10_000_000;
        /// <summary>Current amount of entries stored within the system.</summary>
        public static uint CurrentEntries { get; private set; } = 0;

        public static string StorageDirectory { get; private set; }
        public static string StorageCacheFile { get; private set; }
        
        private static JsonSerializerOptions jsonOptions;
        private static StorageCache storageCache;
        private static StorageThread storageThread;
        private static object storageLock;

        public static void Initialize()
        {
            if (Assembly.GetEntryAssembly() == null) {
                throw new Exception("No entry assembly - this should never happen!?");
            }

            jsonOptions = new JsonSerializerOptions {
                WriteIndented = true,
                NumberHandling = JsonNumberHandling.Strict,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never
            };

            StorageDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location), "Storage");
            StorageCacheFile = StorageDirectory + Path.DirectorySeparatorChar + "_CACHE.txt";

            if (!Directory.Exists(StorageDirectory)) {
                Directory.CreateDirectory(StorageDirectory);
            }

            ReadCacheFile();

            // Create maintenance thread.
            storageThread = new StorageThread(storageLock);
        }

        private static void ReadCacheFile()
        {
            if (!File.Exists(StorageCacheFile)) {
                storageCache = new StorageCache();
                WriteJson(StorageCacheFile, storageCache, true);
                return;
            }

            try {
                storageCache = ReadJson<StorageCache>(StorageCacheFile).Value;
            } catch (Exception e) {
                // TODO: Reaching here could be a critical error. Report it to an error logger or something.

                storageCache = new StorageCache();
                WriteJson(StorageCacheFile, storageCache, true);
            }
        }

        public static IOReturn WriteJson<T>(string path, T obj, bool overwrite = false)
        {
            return Write(path, JsonSerializer.Serialize<T>(obj, jsonOptions), overwrite);
        }

        public static IOReturn Write(string path, string content, bool overwrite = false)
        {
            FileStream fileStream = null;
            StreamWriter streamWriter = null;
            Exception except = null;
            try {
                FileInfo fi = new FileInfo(path);
                FileMode fm;

                /* Determine which FileMode to use.
                   IF overwrite = true THEN the file will either be created if it does not exist, or appended to.
                   IF overwrite = false THEN the file will be created if it does not exist, or overwritten. */
                if (overwrite) {
                    fm = fi.Exists ? FileMode.Truncate : FileMode.CreateNew;
                } else {
                    fm = fi.Exists ? FileMode.Append : FileMode.CreateNew;
                }

                fileStream = new FileStream(path, fm, FileAccess.Write);
                streamWriter = new StreamWriter(fileStream);
                streamWriter.Write(content);
            } catch (Exception e) {
                except = e;
            } finally {
                if (streamWriter == null) {
                    fileStream?.Close();
                } else {
                    streamWriter?.Close();
                }
            }

            return except != null ? new IOReturn(IOReturnStatus.Fail, except) : new IOReturn(IOReturnStatus.Success);
        }

        public static IOReturn<string> Read(string path)
        {
            FileStream fileStream = null;
            StreamReader streamReader = null;
            Exception except = null;
            string str = null;
            try {
                FileInfo fi = new FileInfo(path);
                if (!fi.Exists)
                    throw new FileNotFoundException();

                fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                streamReader = new StreamReader(fileStream);
                str = streamReader.ReadToEnd();
            } catch (Exception e) {
                except = e;
            } finally {
                if (streamReader == null) {
                    fileStream?.Close();
                } else {
                    streamReader?.Close();
                }
            }

            return except != null ? new IOReturn<string>(IOReturnStatus.Fail, str, except) : new IOReturn<string>(IOReturnStatus.Success, str);
        }

        public static IOReturn<T> ReadJson<T>(string path)
        {
            FileStream fileStream = null;
            StreamReader streamReader = null;
            Exception except = null;
            T val = default;
            try {
                FileInfo fi = new FileInfo(path);
                if (!fi.Exists)
                    throw new FileNotFoundException();

                fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                streamReader = new StreamReader(fileStream);
                string data = streamReader.ReadToEnd();
                val = JsonSerializer.Deserialize<T>(data, jsonOptions);
            } catch (Exception e) {
                except = e;
            } finally {
                if (streamReader == null) {
                    fileStream?.Close();
                } else {
                    streamReader?.Close();
                }
            }

            return except != null ? new IOReturn<T>(IOReturnStatus.Fail, val, except) : new IOReturn<T>(IOReturnStatus.Success, val);
        }
    }

    public class StorageCache
    {
        public uint CurrentEntries { get; set; }

        public StorageCache()
        {
            CurrentEntries = 0;
        }
    }
}
