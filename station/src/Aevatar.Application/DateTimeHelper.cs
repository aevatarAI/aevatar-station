using System;

namespace Aevatar;

public class DateTimeHelper
{
    public static long ToUnixTimeMilliseconds(DateTime value)
    {
        var span = value - DateTime.UnixEpoch;
        return (long) span.TotalMilliseconds;
    }

    public static DateTime FromUnixTimeMilliseconds(long value)
    {
        return DateTime.UnixEpoch.AddMilliseconds(value);
    }
}