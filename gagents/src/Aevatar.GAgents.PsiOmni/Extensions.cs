using System.Text.Json;
using Aevatar.GAgents.PsiOmni.Interfaces;
using Aevatar.GAgents.PsiOmni.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Aevatar.GAgents.PsiOmni;

public static class Extensions
{
    /// <summary>
    /// Converts a ChatMessage to a Semantic Kernel ChatMessageContent.
    /// </summary>
    /// <param name="message">The ChatMessage to convert.</param>
    /// <returns>A ChatMessageContent based on the input message.</returns>
    public static ChatMessageContent ToSkMessage(this PsiOmniChatMessage message)
    {
        var role = message.Role.ToLowerInvariant() switch
        {
            "system" => AuthorRole.System,
            "user" => AuthorRole.User,
            "assistant" => AuthorRole.Assistant,
            "tool" => AuthorRole.Tool,
            _ => AuthorRole.User,
        };

        var metadata = new Dictionary<string, object?>();
        if (message.Metadata is { Count: > 0 })
        {
            foreach (var kvp in message.Metadata)
            {
                metadata[kvp.Key] = kvp.Value;
            }
        }

        ChatMessageContent chatMessage;

        if (message.Serialized != null && !string.IsNullOrEmpty(message.Serialized.TypeFullName) &&
            message.Serialized.TypeFullName == typeof(OpenAIChatMessageContent).FullName)
        {
            chatMessage = JsonSerializer.Deserialize<OpenAIChatMessageContent>(message.Serialized.Json);
        }
        else if (message.ToolCalls is { Count: > 0 })
        {
            var items = new ChatMessageContentItemCollection();
            if (!string.IsNullOrEmpty(message.Content))
            {
                items.Add(new TextContent(message.Content));
            }

            foreach (var toolCall in message.ToolCalls)
            {
                var kernelArgs = new KernelArguments();
                if (!string.IsNullOrEmpty(toolCall.FunctionArguments))
                {
                    try
                    {
                        var args = JsonSerializer.Deserialize<Dictionary<string, object>>(toolCall.FunctionArguments);
                        if (args != null)
                        {
                            foreach (var arg in args)
                            {
                                kernelArgs[arg.Key] = arg.Value;
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        kernelArgs["arguments"] = toolCall.FunctionArguments;
                    }
                }

                items.Add(new FunctionCallContent(toolCall.FunctionName, arguments: kernelArgs,
                    id: Guid.NewGuid().ToString()));
            }

            chatMessage = new ChatMessageContent(role, items, metadata: metadata);
        }
        else
        {
            chatMessage = new ChatMessageContent(
                role: role,
                content: message.Content,
                metadata: metadata);
        }

#pragma warning disable SKEXP0001
        if (!string.IsNullOrEmpty(message.Name))
        {
            chatMessage.AuthorName = message.Name;
        }
#pragma warning restore SKEXP0001

        return chatMessage;
    }

    public static T DeepClone<T>(this T obj) where T : new()
    {
        var options = new JsonSerializerOptions();
        var json = JsonSerializer.Serialize(obj, options);
        try
        {
            return JsonSerializer.Deserialize<T>(json, options) ?? new T();
        }
        catch (Exception e)
        {
            return new T();
        }
    }

    public static List<JsonElement> GetAllToolDefinitions(this IKernelFunctionRegistry kernelFunctionRegistry)
    {
        return (from toolName in kernelFunctionRegistry.GetAllAvailableToolNames()
            let kernelFunction = kernelFunctionRegistry.GetToolByQualifiedName(toolName)!
            select JsonSerializer.SerializeToDocument(kernelFunction.ToToolDefinition()).RootElement.Clone()).ToList();
    }

    public static ToolDefinition ToToolDefinition(this KernelFunction kernelFunction)
    {
        var toolName = kernelFunction.PluginName.IsNullOrEmpty()
            ? kernelFunction.Name
            : $"{kernelFunction.PluginName}.{kernelFunction.Name}";
        return new ToolDefinition
        {
            Name = toolName,
            Description = kernelFunction.Description,
            Parameters = kernelFunction.Metadata.Parameters.Select(p => new ToolParameter
            {
                Name = p.Name,
                Description = p.Description,
                IsRequired = p.IsRequired,
                Schema = p.Schema?.RootElement.Clone().ToString() ?? string.Empty
            }).ToList()
        };
    }

    public static string ToYaml(this List<JsonElement> jsonElements)
    {
        // This is our robust, manual converter.
        object? ConvertToPlainObject(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    // For objects, create a Dictionary
                    var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    foreach (var property in element.EnumerateObject())
                    {
                        dict[property.Name] = ConvertToPlainObject(property.Value);
                    }

                    return dict;

                case JsonValueKind.Array:
                    // For arrays, create a List
                    var list = new List<object?>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(ConvertToPlainObject(item));
                    }

                    return list;

                case JsonValueKind.String:
                    return element.GetString();

                case JsonValueKind.Number:
                    return element.GetDecimal(); // Or GetDouble(), GetInt32(), etc. as appropriate

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                case JsonValueKind.Null:
                    return null;

                case JsonValueKind.Undefined:
                default:
                    return null; // Or throw an exception if you want to be strict
            }
        }

        // Manually convert each JsonElement to a plain .NET object.
        var listOfDotnetObjects = jsonElements.Select(el => ConvertToPlainObject(el)).ToList();

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return serializer.Serialize(listOfDotnetObjects);
    }
}