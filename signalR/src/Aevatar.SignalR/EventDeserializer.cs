using Aevatar.Core.Abstractions;
using Newtonsoft.Json;

namespace Aevatar.SignalR;

public class EventDeserializer
{
    private readonly List<Type> _eventTypes = [];

    public EventDeserializer()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(EventBase)) && t is { IsClass: true, IsAbstract: false });
            _eventTypes.AddRange(types);
        }
    }

    public EventBase DeserializeEvent(string eventJson, string eventTypeName)
    {
        var eventType = _eventTypes.FirstOrDefault(t => t.FullName == eventTypeName);
        if (eventType == null)
        {
            throw new InvalidOperationException($"Event type '{eventTypeName}' not found.");
        }

        return (EventBase)JsonConvert.DeserializeObject(eventJson, eventType)!;
    }
}