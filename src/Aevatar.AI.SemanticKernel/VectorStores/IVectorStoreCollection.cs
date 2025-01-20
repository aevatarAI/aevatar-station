using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.AI.Brain;

namespace Aevatar.AI.VectorStores;

public interface IVectorStoreCollection
{
    Task InitializeAsync(string collectionName);
    Task UploadRecordAsync(List<FileData> files);
}