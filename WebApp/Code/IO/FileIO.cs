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
        private static object storageLock = new object();

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

        #if DEBUG
            // If we are debugging, the storage should be in the project directory.
            // Otherwise storage is in the output directory.
            string tmp = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            string up1 = Directory.GetParent(tmp).ToString();
            string up2 = Directory.GetParent(up1).ToString();
            string up3 = Directory.GetParent(up2).ToString();
            StorageDirectory = Path.Combine(up3, "Storage");
        #else
            StorageDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location), "Storage");
        #endif

            StorageCacheFile = StorageDirectory + Path.DirectorySeparatorChar + "_CACHE.txt";

            if (!Directory.Exists(StorageDirectory)) {
                Directory.CreateDirectory(StorageDirectory);
            }

            ReadCacheFile();
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

        /// <summary>
        /// Writes a serialized json string to a path.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="path">Path of the file.</param>
        /// <param name="obj">Object to serialize.</param>
        /// <param name="overwrite">Overwrite or append contents of the file.</param>
        /// <returns>IOReturn object represeting the status of the operation.</returns>
        public static IOReturn WriteJson<T>(string path, T obj, bool overwrite = false)
        {
            return Write(path, JsonSerializer.Serialize(obj, jsonOptions), overwrite);
        }

        /// <summary>
        /// Writes a string to a path.
        /// </summary>
        /// <param name="path">Path of the file.</param>
        /// <param name="content">String to write.</param>
        /// <param name="overwrite">Overwrite or append contents of the file.</param>
        /// <returns>IOReturn object represeting the status of the operation.</returns>
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

            return except != null 
                ? new IOReturn(IOReturnStatus.Fail, except) 
                : new IOReturn(IOReturnStatus.Success);
        }

        /// <summary>
        /// Reads a string from a file at a given path.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <returns>IOReturn object representing the status of the operation. Contains the string if successful.</returns>
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

            return except != null 
                ? new IOReturn<string>(IOReturnStatus.Fail, str, except) 
                : new IOReturn<string>(IOReturnStatus.Success, str);
        }

        /// <summary>
        /// Reads a string from a file at a given path.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <returns>IOReturn object representing the status of the operation. Contains the string if successful.</returns>
        public static IOReturn<byte[]> ReadBytes(string path)
        {
            FileStream fileStream = null;
            BinaryReader streamReader = null;
            Exception except = null;
            byte[] bytes = null;
            try {
                FileInfo fi = new FileInfo(path);
                if (!fi.Exists)
                    throw new FileNotFoundException();

               bytes = File.ReadAllBytes(path);
            }
            catch (Exception e) {
                except = e;
            } finally {
                if (streamReader == null) {
                    fileStream?.Close();
                } else {
                    streamReader?.Close();
                }
            }

            return except != null
                ? new IOReturn<byte[]>(IOReturnStatus.Fail, bytes, except)
                : new IOReturn<byte[]>(IOReturnStatus.Success, bytes);
        }

        /// <summary>
        /// Reads and deserializes a json file at a given path.
        /// </summary>
        /// <typeparam name="T">Type to deserialize as.</typeparam>
        /// <param name="path">Path to the json file.</param>
        /// <returns>IOReturn object representing the status of the operation. Contains the deserialized object if successful.</returns>
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

            return except != null
                ? new IOReturn<T>(IOReturnStatus.Fail, val, except) 
                : new IOReturn<T>(IOReturnStatus.Success, val);
        }
    }

    /// <summary>
    /// Contains data which is stored between sessions.
    /// </summary>
    public class StorageCache
    {
        public uint CurrentEntries { get; set; }

        public StorageCache()
        {
            CurrentEntries = 0;
        }
    }
}
