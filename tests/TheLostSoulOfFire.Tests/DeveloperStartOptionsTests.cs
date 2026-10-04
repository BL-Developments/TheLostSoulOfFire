using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Debugging;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class DeveloperStartOptionsTests
{
    [TestMethod]
    public void WithoutDev_NormalStartHasNoOptions()
    {
        Assert.IsTrue(DeveloperStartOptions.TryParse([], out DeveloperStartOptions? options, out string? error));
        Assert.IsNull(options);
        Assert.IsNull(error);

        Assert.IsTrue(DeveloperStartOptions.TryParse(["--antechamber-visual-test"], out options, out _));
        Assert.IsNull(options);
    }

    [TestMethod]
    public void DevAlone_StartsAtTitle()
    {
        Assert.IsTrue(DeveloperStartOptions.TryParse(["--dev"], out DeveloperStartOptions? options, out _));
        Assert.AreEqual(DeveloperStartArea.Title, options!.Area);
        Assert.AreEqual("DEV_START area=title", options.Describe());
    }

    [TestMethod]
    [DataRow("title", DeveloperStartArea.Title)]
    [DataRow("prologue", DeveloperStartArea.Prologue)]
    [DataRow("prologue:find-trace", DeveloperStartArea.PrologueFindTrace)]
    [DataRow("prologue:search", DeveloperStartArea.PrologueSearch)]
    [DataRow("prologue:devourer", DeveloperStartArea.PrologueDevourer)]
    [DataRow("prologue:transit", DeveloperStartArea.PrologueTransit)]
    [DataRow("hub", DeveloperStartArea.Hub)]
    [DataRow("HUB", DeveloperStartArea.Hub)]
    [DataRow("arena", DeveloperStartArea.Arena)]
    public void Start_MapsEveryArea(string name, DeveloperStartArea expected)
    {
        Assert.IsTrue(DeveloperStartOptions.TryParse(["--dev", "--start", name], out DeveloperStartOptions? options, out string? error), error);
        Assert.AreEqual(expected, options!.Area);
    }

    [TestMethod]
    public void EveryAreaNameParses()
    {
        foreach (string name in DeveloperStartOptions.AreaNames)
        {
            Assert.IsTrue(DeveloperStartOptions.TryParse(["--dev", "--start", name], out DeveloperStartOptions? options, out _), name);
            Assert.AreEqual(name, options!.AreaName);
        }
    }

    [TestMethod]
    public void Arena_DefaultsToWaveOne_AndAcceptsChosenWave()
    {
        Assert.IsTrue(DeveloperStartOptions.TryParse(["--dev", "--start", "arena"], out DeveloperStartOptions? options, out _));
        Assert.AreEqual(1, options!.Wave);

        Assert.IsTrue(DeveloperStartOptions.TryParse(["--start", "arena", "--wave", "3", "--dev"], out options, out _));
        Assert.AreEqual(3, options!.Wave);
        Assert.AreEqual("DEV_START area=arena wave=3", options.Describe());
    }

    [TestMethod]
    [DataRow("--dev", "--start", "dungeon")]
    [DataRow("--dev", "--start")]
    [DataRow("--dev", "--start", "--wave", "2")]
    [DataRow("--dev", "--start", "hub", "--wave", "2")]
    [DataRow("--dev", "--wave", "2")]
    [DataRow("--dev", "--start", "arena", "--wave", "0")]
    [DataRow("--dev", "--start", "arena", "--wave", "5")]
    [DataRow("--dev", "--start", "arena", "--wave", "two")]
    [DataRow("--start", "arena")]
    [DataRow("--wave", "2")]
    [DataRow("--dev", "--audio-gameplay-test")]
    public void InvalidArguments_AreRejectedWithMessage(params string[] args)
    {
        Assert.IsFalse(DeveloperStartOptions.TryParse(args, out DeveloperStartOptions? options, out string? error));
        Assert.IsNull(options);
        Assert.IsFalse(string.IsNullOrWhiteSpace(error));
    }

    [TestMethod]
    public void UnknownArea_IsNamedInError_AndUsageListsAllAreas()
    {
        DeveloperStartOptions.TryParse(["--dev", "--start", "dungeon"], out _, out string? error);
        StringAssert.Contains(error, "dungeon");
        foreach (string name in DeveloperStartOptions.AreaNames)
        {
            StringAssert.Contains(DeveloperStartOptions.Usage, name);
        }
    }

    [TestMethod]
    public void AttributeFlags_OverrideOnlyTheGivenAttributes()
    {
        Assert.IsTrue(DeveloperStartOptions.TryParse(["--dev", "--start", "arena", "--strength", "20", "--armor", "0"], out DeveloperStartOptions? options, out string? error), error);

        Assert.AreEqual(new PlayerAttributes(20, PlayerAttributes.Baseline, 0), options!.AttributeOverride);
        Assert.AreEqual("DEV_START area=arena wave=1 strength=20 ability-power=10 armor=0", options.Describe());
    }

    [TestMethod]
    public void WithoutAttributeFlags_KeepsCharacterAttributes()
    {
        Assert.IsTrue(DeveloperStartOptions.TryParse(["--dev", "--start", "hub"], out DeveloperStartOptions? options, out _));

        Assert.IsNull(options!.AttributeOverride);
    }

    [TestMethod]
    [DataRow("--strength", "-1")]
    [DataRow("--ability-power", "100")]
    [DataRow("--armor", "viel")]
    public void AttributeFlags_RejectValuesOutsideTheRange(string flag, string value)
    {
        Assert.IsFalse(DeveloperStartOptions.TryParse(["--dev", flag, value], out DeveloperStartOptions? options, out string? error));
        Assert.IsNull(options);
        StringAssert.Contains(error, flag);
    }

    [TestMethod]
    public void AttributeFlags_RequireDev()
    {
        Assert.IsFalse(DeveloperStartOptions.TryParse(["--strength", "20"], out _, out string? error));
        StringAssert.Contains(error, "--dev");
    }
}
