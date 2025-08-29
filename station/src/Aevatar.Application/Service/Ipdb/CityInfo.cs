namespace ipdb
{
    public class CityInfo : Info
    {
        public CityInfo(string[] data, string[] fields) : base(data, fields)
        {
        }

        public string CountryName => get("country_name");
        public string RegionName => get("region_name");
        public string CityName => get("city_name");
        public string OwnerDomain => get("owner_domain");
        public string IspDomain => get("isp_domain");
        public string Line => get("line");
        public string Latitude => get("latitude");
        public string Longitude => get("longitude");
        public string Timezone => get("timezone");
        public string UtcOffset => get("utc_offset");
        public string ChinaAdminCode => get("china_admin_code");
        public string IddCode => get("idd_code");
        public string CountryCode => get("country_code");
        public string ContinentCode => get("continent_code");
        public string IDC => get("idc");
        public string BaseStation => get("base_station");
        public string CountryCode3 => get("country_code3");
        public string EuropeanUnion => get("european_union");
        public string CurrencyCode => get("currency_code");
        public string CurrencyName => get("currency_name");
        public string Anycast => get("anycast");
        public string Backbone => get("backbone");
        public string Satellite => get("satellite");
    }
}