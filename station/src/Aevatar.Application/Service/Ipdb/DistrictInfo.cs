namespace ipdb
{
    public class DistrictInfo : Info
    {
        private readonly string[] fields;
        private readonly string[] data;

        public DistrictInfo(string[] data, string[] fields) : base(data, fields)
        {
        }

        public string CountryName => get("country_name");
        public string RegionName => get("region_name");
        public string CityName => get("city_name");
        public string DistrictName => get("district_name");
        public string ChinaAdminCode => get("china_admin_code");
        public string CoveringRadius => get("covering_radius");
        public string Latitude => get("latitude");
        public string Longitude => get("longitude");
    }
}