using System.Collections.Generic;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace ipdb
{
    /**
     * @copyright IPIP.net
     */
    public class MetaData
    {
        [J("build")]public int Build;
        [J("ip_version")] public int IPVersion;
        [J("node_count")] public int nodeCount;
        [J("languages")] public Dictionary<string, int> Languages;
        [J("fields")] public string[] Fields;
        [J("total_size")] public int totalSize;
    }
}