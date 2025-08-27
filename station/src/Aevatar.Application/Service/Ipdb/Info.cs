using System;
using System.Text;

namespace ipdb
{
    public class Info
    {
        protected readonly string[] fields;
        protected readonly string[] data;

        public Info(string[] data, string[] fields)
        {
            this.data = data;
            this.fields = fields;
        }

        protected string get(string field)
        {
            var idx = Array.IndexOf(fields, field);
            return idx >= 0 ? data[idx] : "";
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (var i = 0; i < fields.Length; i++)
            {
                sb.Append(fields[i]).Append(":").Append(data[i]).AppendLine();
            }

            return sb.ToString();
        }
    }
}