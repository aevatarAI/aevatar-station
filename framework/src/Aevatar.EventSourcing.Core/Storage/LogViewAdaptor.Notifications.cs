using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.EventSourcing.Common;

namespace Aevatar.EventSourcing.Core.Storage;

public partial class LogViewAdaptor<TLogView, TLogEntry> where TLogView : class, new()
    where TLogEntry : class
{
    private readonly SortedList<long, UpdateNotificationMessage> _notifications = new();

    protected override INotificationMessage Merge(INotificationMessage earlierMessage,
        INotificationMessage laterMessage)
    {
        if (earlierMessage is UpdateNotificationMessage earlier
            && laterMessage is UpdateNotificationMessage later
            && earlier.Origin == later.Origin
            && earlier.Version + later.Updates.Count == later.Version
            && earlier.Updates.Count + later.Updates.Count < AevatarEventSourcingConstants.MaxEntriesInNotifications)
        {
            return new UpdateNotificationMessage
            {
                Version = later.Version,
                Origin = later.Origin,
                Updates = earlier.Updates.Concat(later.Updates).ToList(),
                ETag = later.ETag
            };
        }

        return base.Merge(earlierMessage, laterMessage); // keep only the version number
    }

    protected override void ProcessNotifications()
    {
        // discard notifications that are behind our already confirmed state
        while (_notifications.Count > 0 && _notifications.ElementAt(0).Key < _globalVersion)
        {
            Services.Log(LogLevel.Debug, "discarding notification {0}", _notifications.ElementAt(0).Value);
            _notifications.RemoveAt(0);
        }

        // process notifications that reflect next global version
        while (_notifications.Count > 0 && _notifications.ElementAt(0).Key == _globalVersion)
        {
            var updateNotification = _notifications.ElementAt(0).Value;
            _notifications.RemoveAt(0);
            _globalSnapshot.State.FlipBit(updateNotification.Origin);
            _globalSnapshot.ETag = updateNotification.ETag;
            _globalVersion = updateNotification.Version;
            UpdateConfirmedView(updateNotification.Updates);
            Services.Log(LogLevel.Debug, "notification success ({0} updates) {1}", updateNotification.Updates.Count,
                _globalSnapshot);
        }

        Services.Log(LogLevel.Trace, "unprocessed notifications in queue: {0}", _notifications.Count);
        base.ProcessNotifications();
    }

    protected override void OnNotificationReceived(INotificationMessage payload)
    {
        if (payload is UpdateNotificationMessage um)
        {
            _notifications.Add(um.Version - um.Updates.Count, um);
        }
        else
        {
            base.OnNotificationReceived(payload);
        }
    }

    [Serializable]
    [GenerateSerializer]
    protected internal sealed class UpdateNotificationMessage : INotificationMessage
    {
        /// <inheritdoc />
        [Id(0)]
        public int Version { get; set; }

        /// <summary> The cluster that performed the update </summary>
        [Id(1)]
        public string Origin { get; set; } = null!;

        /// <summary> The list of updates that were applied </summary>
        [Id(2)]
        public List<TLogEntry> Updates { get; set; } = null!;

        /// <summary> The e-tag of the storage after applying the updates</summary>
        [Id(3)]
        public string ETag { get; set; } = null!;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"v{Version} ({Updates.Count} updates by {Origin}) etag={ETag}";
        }
    }
}