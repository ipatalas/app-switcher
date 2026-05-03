using AppSwitcher.Tests;
using LiteDB;
using Xunit;

[assembly: AssemblyFixture(typeof(GlobalFixture))]

namespace AppSwitcher.Tests
{
    public class GlobalFixture
    {
        public GlobalFixture()
        {
            BsonMapper.Global.EnumAsInteger = true;
            BsonMapper.Global.RegisterType(
                serialize: d => d.ToString("yyyy-MM-dd"),
                deserialize: v => DateOnly.Parse(v.AsString));
        }
    }
}
