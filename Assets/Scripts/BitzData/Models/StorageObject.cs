using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

public class StorageObject
{
    public string Bucket { get; set; } = "bitz-files";

    public string ObjectId { get; private set; }

    // The storage object might not be hydrated.
    [AllowNull]
    public string localPath = null;

    [NotNull]
    public StorageObjectMetadata Metadata { get; set; }

    public string LocalPath
    {
        get
        {
            if (localPath is null)
            {
                throw new Exception("Do not initialize this object outside of BitzFileService.");
            }
            return localPath;
        }
        set { localPath = value; }
    }

    public StorageObject(string objectId)
    {
        // Sanity check
        if (objectId is null || objectId is "")
            throw new InvalidDataException("Empty object id is not permitted.");

        ObjectId = objectId;
        Metadata =
            BitzFileService.GetInstance().GetObjectMetadata(objectId).Result
            ?? throw new Exception("File not found on remote server.");
    }
}

public class ProgressCallbackArgs
{
    /// <summary>
    /// This represents the progress and goes from 0-1 (inclusive)
    /// </summary>
    public float CurrentProgress { get; set; }
    public string CallMessage { get; set; }

    public ProgressCallbackArgs(string initialMessage)
    {
        CallMessage = initialMessage;
    }
}
