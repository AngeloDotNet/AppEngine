using System.Reflection;

namespace AppEngine.Validations.Tests;

public class SmokeTests
{
    [Fact]
    public void Assembly_UnderTest_IsLoadable() =>
        Assert.NotNull(Assembly.Load("AppEngine.Validations"));
}
