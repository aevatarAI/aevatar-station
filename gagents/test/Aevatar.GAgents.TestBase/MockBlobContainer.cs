using System.Collections.Concurrent;
using System.Text;
using Volo.Abp.BlobStoring;

namespace Aevatar.GAgents.TestBase;

public class MockBlobContainer : IBlobContainer
{
    private readonly ConcurrentDictionary<string, Stream> _blobs = new();

    public MockBlobContainer()
    {
        _blobs["image1.png"] = new MemoryStream(Encoding.UTF8.GetBytes("image1.png"));
        _blobs["image2.png"] = new MemoryStream(Encoding.UTF8.GetBytes("image2.png"));
        _blobs["image3.png"] = new MemoryStream(Encoding.UTF8.GetBytes("image3.png"));
    }

    public async Task SaveAsync(string name, Stream stream, bool overrideExisting = false,
        CancellationToken cancellationToken = new CancellationToken())
    {
        _blobs[name] = stream;
    }

    public async Task<bool> DeleteAsync(string name, CancellationToken cancellationToken = new CancellationToken())
    {
        return _blobs.Remove(name, out _);
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = new CancellationToken())
    {
        return _blobs.ContainsKey(name);
    }

    public async Task<Stream> GetAsync(string name, CancellationToken cancellationToken = new CancellationToken())
    {
        return _blobs[name];
    }

    public async Task<Stream?> GetOrNullAsync(string name, CancellationToken cancellationToken = new CancellationToken())
    {
        if (_blobs.TryGetValue(name, out var value))
        {
            return value;
        }

        return null;
    }
}