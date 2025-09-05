using System.Text;

namespace ipdb
{

    public class BaseStationInfo
    {

        private readonly string[] data;

        public BaseStationInfo(string[] data)
        {
            this.data = data;
        }

        public string getCountryName()
        {
            return data[0];
        }

        public string getRegionName()
        {
            return data[1];
        }

        public string getCityName()
        {
            return data[2];
        }

        public string getOwnerDomain()
        {
            return data[3];
        }

        public string getIspDomain()
        {
            return data[4];
        }

        public string getBaseStation()
        {
            return data[5];
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("country_name:");
            sb.Append(getCountryName());
            sb.Append("\n");
            sb.Append("region_name:");
            sb.Append(getRegionName());
            sb.Append("\n");
            sb.Append("city_name:");
            sb.Append(getCityName());
            sb.Append("\n");
            sb.Append("owner_domain:");
            sb.Append(getOwnerDomain());
            sb.Append("\n");
            sb.Append("isp_domain:");
            sb.Append(getIspDomain());
            sb.Append("\n");
            sb.Append("base_station:");
            sb.Append(getBaseStation());

            return sb.ToString();
        }
    }

}