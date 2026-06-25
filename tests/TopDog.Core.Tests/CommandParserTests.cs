using TopDog.Sim.Order;

namespace TopDog.Tests;

public sealed class CommandParserTests
{
    private readonly CommandParser _parser = new();

    [Test]
    public void ParsesHelpAndGo()
    {
        Assert.That(_parser.Parse("help").Verb, Is.EqualTo(OrderVerb.HELP));
        var go = _parser.Parse("go sys_hub");
        Assert.That(go.Verb, Is.EqualTo(OrderVerb.GO_SYSTEM));
        Assert.That(go.TargetName, Is.EqualTo("sys_hub"));
    }
}
