using System.Text.Json;

namespace TransIP.Client.JsonConverters
{
    public class LowerCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name) || !char.IsUpper(name[0]))
                return name;

            return name.Length == 1 ? char.ToLower(name[0]).ToString() : char.ToLower(name[0]) + name[1..];
        }
    }
}
