using System.Reflection;

namespace AppEngine.Tests;

public class SmokeTests
{
    [Fact]
    public void Assembly_UnderTest_IsLoadable() =>
        Assert.NotNull(Assembly.Load("AppEngine"));
}
