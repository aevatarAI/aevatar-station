using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Brain;

namespace Aevatar.GAgents.SemanticKernel.VectorStores;

public interface IVectorStoreCollection
{
    Task InitializeAsync(string collectionName);
    Task UploadRecordAsync(List<BrainContent> files);
}