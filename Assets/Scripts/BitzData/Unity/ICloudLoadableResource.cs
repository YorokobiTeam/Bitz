using Cysharp.Threading.Tasks;

public abstract class GenericCloudResource<T>
{
    public T Resource
    {
        get;
        private set;
    }
    public StorageObject resourceSO;
    GenericCloudResource(StorageObject resourceStorageObject)
    {
        this.resourceSO = resourceStorageObject;
    }

    /// <summary>
    /// The loader method takes the local path of resource and loads it into memory and in the resource field
    /// </summary>
    /// <param name="localPath">
    ///     The local file path of the resource
    /// </param>
    public abstract void Loader(string localPath);
    public async void Hydrate()
    {
        BitzFileService.GetInstance().GetStorageObject()
    
    }


}