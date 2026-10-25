using System.Reflection;

namespace AppEngine.EFCore.Tests;

public class SmokeTests
{
    [Fact]
    public void Assembly_UnderTest_IsLoadable() =>
        Assert.NotNull(Assembly.Load("AppEngine.EFCore"));
}
