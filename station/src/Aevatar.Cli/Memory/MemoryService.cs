using Volo.Abp.DependencyInjection;
using Volo.Abp.IO;

namespace Aevatar.Cli.Memory;

public class MemoryService : ITransientDependency
{
    private const string KeyValueSeparator = "|||";
    private static readonly string MemoryPath = AevatarCliConstants.Paths.Memory;

    public async Task<string?> GetAsync(string key)
    {
        if (!File.Exists(MemoryPath))
        {
            return null;
        }

        return (await FileHelper.ReadAllTextAsync(MemoryPath))
            .Split([Environment.NewLine, "\n"], StringSplitOptions.None)
            .FirstOrDefault(x => x.StartsWith($"{key} "))?.Split(KeyValueSeparator).Last().Trim();
    }

    public async Task SetAsync(string key, string value)
    {
        if (!File.Exists(MemoryPath))
        {
            await File.WriteAllTextAsync(MemoryPath,
                $"{key} {KeyValueSeparator} {value}"
            );
            return;
        }

        var memoryContentLines = (await FileHelper.ReadAllTextAsync(MemoryPath))
            .Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.None)
            .ToList();

        memoryContentLines.RemoveAll(x => x.StartsWith(key));
        memoryContentLines.Add($"{key} {KeyValueSeparator} {value}");

        await File.WriteAllTextAsync(MemoryPath,
            memoryContentLines.JoinAsString(Environment.NewLine)
        );
    }
}