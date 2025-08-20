using ZimCom.Core.Modules.Dynamic.Misc;

namespace ZimCom.Tests;

public class Tests
{
    private DynamicManagerModuleClientExtras _dynamicManagerModule = new DynamicManagerModuleClientExtras();

    [Test]
    [Arguments(1, 2, 3)]
    [Arguments(2, 3, 5)]
    public async Task DataDrivenArguments(int a, int b, int c)
    {
        // TODO: Make real test
        Console.WriteLine("This one can accept arguments from an attribute");

        var result = a + b;

        await Assert.That(result).IsEqualTo(c);
    }

    [Test]
    public async Task OffsetTest()
    {
        // TODO: Make real test
        var result = 0;
        await Assert.That(result).IsEqualTo(0);
    }
}