namespace ipdb
{
    public class IDCInfo : Info
    {
        public IDCInfo(string[] data, string[] fields) : base(data, fields)
        {
        }

        public string CountryName => get("country_name");
        public string RegionName => get("region_name");
        public string CityName => get("city_name");
        public string OwnerDomain => get("owner_domain");
        public string IspDomain => get("isp_domain");
        public string IDC => get("idc");
    }
}