using System.Text.Json;

namespace Serilog.Extensions.Formatting
{
    internal class DefaultNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return name;
        }
    }
}
