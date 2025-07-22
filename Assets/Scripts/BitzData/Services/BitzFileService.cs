using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// This class manages IO and file related operations in Bitz.
/// </summary>
public class BitzFileService : GenericSupabaseService
{
    // Singleton boilerplate
    internal static BitzFileService Instance { get; private set; }

    public static BitzFileService GetInstance()
    {
        if (Instance is null)
        {
            Initialize();
        }
        return Instance!;
    }

    private static void Initialize() => Instance ??= new BitzFileService();

    // You can't instantiate this.
    private BitzFileService() { }

#nullable enable
    public async Task<StorageObjectMetadata?> GetObjectMetadata(string objectId)
    {
        return JsonSerializer.Deserialize<StorageObjectMetadata>(
            (
                await supabase.Rpc(
                    "get_object_metadata",
                    new Dictionary<string, string> { { "object_id", objectId } }
                )
            ).Content ?? ""
        );
    }

    public async Task DownloadObject(
        StorageObject @object,
        string relativeDir = "/cache/files",
        IProgress<float>? progressCb = null
    )
    {
        // Sanity checks
        if (@object.Metadata is null || @object.Metadata.ServerFilePath == "")
            throw new InvalidDataException("The object was not found on remote server.");
        var dir = Path.Join(Constants.APPLICATION_DATA, relativeDir);
        var path = Path.Join(
            dir,
            $"{@object.ObjectId}.{BitzFileUtils.GetExtension(@object.Metadata.ServerFilePath)}"
        );
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        var bytes = await supabase
            .Storage.From(@object.Bucket)
            .Download(
                @object.Metadata.ServerFilePath,
                (obj, progress) =>
                {
                    progressCb?.Report(progress);
                }
            );
        try
        {
            using var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
            fs.Write(bytes, 0, bytes.Length);
            @object.LocalPath = path;
            await CacheObject(@object, relativeDir);
        }
        catch (Exception e)
        {
            TelemetryService.RecordException(e);
            throw;
        }
    }

    public async Task<StorageObject?> TryFromCache(string objectId)
    {
        var metadataPath = Path.Join(Constants.CACHE_METADATA, $"{objectId}.bmeta");
        // Checks and attempts to deserialize metadata file
        try
        {
            StorageObjectMetadata localMetadata =
                JsonSerializer.Deserialize<StorageObjectMetadata>(File.ReadAllText(metadataPath))
                ?? throw new FileNotFoundException(
                    "Caching metadata was not found on the file system."
                );

            // Gets server side metadata
            StorageObjectMetadata serverMetadata =
                JsonSerializer.Deserialize<StorageObjectMetadata>(
                    (
                        await supabase.Rpc(
                            "get_object_metadata",
                            new Dictionary<string, string> { { "object_id", objectId } }
                        )
                    ).Content
                )
                ?? throw new FileNotFoundException(
                    "Caching metadata was not found on the remote host."
                );

            // Recalculate Hash
            var localMD5 = BitzFileUtils.GetFileMD5(
                Path.Join(
                    Constants.APPLICATION_DATA,
                    localMetadata.RelativeLocalDirectory,
                    $"{serverMetadata.Id}.{BitzFileUtils.GetExtension(serverMetadata.ServerFilePath)}"
                )
            );

            // Verify hashes match
            if (localMD5.Equals(serverMetadata.MD5))
            {
                var filePath = Path.Join(
                    Constants.APPLICATION_DATA,
                    localMetadata.RelativeLocalDirectory,
                    $"{serverMetadata.Id}.{BitzFileUtils.GetExtension(Path.GetFileName(serverMetadata.ServerFilePath))}"
                );
                // Checks if file actually exists or not
                if (!File.Exists(filePath))
                {
                    File.Delete(metadataPath);
                    throw new FileNotFoundException("File does not exist on disk");
                }
                Console.WriteLine("Cache hit");
                return new StorageObject(objectId) { LocalPath = filePath };
            }
            Console.WriteLine("Hash mismatch");
            Console.WriteLine("LocalHash: " + localMD5);
            Console.WriteLine("ServerHash: " + serverMetadata.MD5);
            return null;
        }
        catch (Exception e)
        {
            Console.WriteLine("Cache miss");
            TelemetryService.RecordException(e);
            throw;
        }
    }

    public async Task CacheObject(StorageObject @object, string relativeDir = "/cache/files")
    {
        try
        {
            // Sanity checks
            if (@object.Metadata is null)
                throw new InvalidDataException("The object does not exist");
            if (@object.LocalPath is null)
                throw new InvalidDataException("The object is not hydrated");
            if (!File.Exists(@object.LocalPath))
                throw new FileNotFoundException("The object does not actually exist in disk.");

            // Writes the metadata to disk
            var metadataPath = Path.Join(Constants.CACHE_METADATA, @object.ObjectId + ".bmeta");
            var metadata = @object.Metadata;
            metadata.RelativeLocalDirectory = relativeDir;

            File.WriteAllText(metadataPath, JsonSerializer.Serialize(metadata));
            Console.WriteLine("Wrote to cache");
        }
        catch (DirectoryNotFoundException)
        {
            Directory.CreateDirectory(Constants.CACHE_METADATA);
            await CacheObject(@object, relativeDir);
        }
        catch (Exception e)
        {
            TelemetryService.RecordException(e);
            return;
        }
    }

    /// <summary>
    /// Retrieves a <see cref="StorageObject"/> by its ID, optionally downloading it from storage if not found in the local cache.
    /// </summary>
    /// <param name="objectId">The unique identifier of the object to retrieve.</param>
    /// <param name="relativeLocalCacheDir">
    /// The relative directory where the object should be downloaded to. Defaults to <c>"/cache/files"</c>.
    /// </param>
    /// <param name="bucket">
    /// The name of the storage bucket. Defaults to <c>"bitz-files"</c>.
    /// </param>
    /// <param name="progressCb">
    /// An optional callback to report download progress, where the value is a float between 0 and 1.
    /// </param>
    /// <returns>
    /// A <see cref="StorageObject"/> instance if found or successfully downloaded; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// If the object is found in cache, it is returned immediately without downloading. If not, the method
    /// attempts to download the object from the specified bucket and directory. Any exceptions during download
    /// are caught and recorded via <see cref="TelemetryService.RecordException(Exception)"/>.
    /// </remarks>
    public async Task<StorageObject?> GetStorageObject(
        string objectId,
        string relativeLocalCacheDir = "/cache/files",
        string bucket = "bitz-files",
        IProgress<float>? progressCb = null
    )
    {
        StorageObject? fromCache = await TryFromCache(objectId);
        if (fromCache is not null)
            return fromCache;
        try
        {
            var @object = new StorageObject(objectId) { Bucket = bucket };
            await DownloadObject(@object, relativeLocalCacheDir, progressCb);
            return @object;
        }
        catch (Exception e)
        {
            TelemetryService.RecordException(e);
            throw;
        }
    }

    public async void GetStorageObject(StorageObject @object, string bucket = "bitz-files", Action<int> progressCb = null)
    {
    }

    public void DeleteLocalStorageObject(StorageObject @object)
    {
        File.Delete(Path.Join(Constants.CACHE_METADATA, $"{@object.ObjectId}.bmeta"));
        File.Delete(
            Path.Join(
                Constants.CACHE_FILES,
                $"{@object.ObjectId}.{BitzFileUtils.GetExtension(@object.Metadata.ServerFilePath)}"
            )
        );
    }

    public async Task OverrideFile(
        StorageObject @object,
        string localFile,
        string bucket = "bitz-files",
        Action? callback = null
    )
    {
        if (
            @object.localPath is null
            || @object.Metadata.ServerFilePath is null
            || !File.Exists(@object.localPath)
            || !File.Exists(localFile)
        )
            throw new FileNotFoundException();
        byte[] bytes = File.ReadAllBytes(localFile);
        await supabase
            .Storage.From(bucket)
            .Upload(
                bytes,
                @object.Metadata.ServerFilePath,
                new Supabase.Storage.FileOptions { Upsert = true },
                (evt, progress) =>
                {
                    Console.WriteLine("Progress: " + progress);
                    Console.WriteLine("Event: " + evt);
                },
                true
            );
    }

    public async Task<StorageObject> UploadStorageObject(
        string localPath,
        string? remotePath,
        string bucket = "bitz-files",
        bool enableCache = false,
        string relativeCacheDir = "/cache/files"
    )
    {
        if (!File.Exists(localPath))
        {
            throw new FileNotFoundException("The file to be uploaded could not be found");
        }
        byte[] data = await File.ReadAllBytesAsync(localPath);
        var storageObjectId = await supabase
            .Storage.From(bucket)
            .Upload(data, remotePath ?? localPath);
        StorageObject newSO = new(storageObjectId);

        if (enableCache)
        {
            var cachePath = Path.Join(Constants.APPLICATION_DATA, relativeCacheDir);
            File.Copy(
                localPath,
                Path.Join(cachePath, $"{newSO.ObjectId}.{BitzFileUtils.GetExtension(localPath)}")
            );
            newSO.localPath = cachePath;
        }
        return newSO;
    }

#nullable disable
}
