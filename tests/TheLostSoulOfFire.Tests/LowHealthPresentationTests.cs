using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class LowHealthPresentationTests
{
    private const float Step = 1f / 60f;

    private static List<float> BeatTimes(float health, bool active, float seconds)
    {
        var presentation = new LowHealthPresentation();
        var beats = new List<float>();
        for (int frame = 0; frame < seconds / Step; frame++)
        {
            presentation.Update(Step, health, active);
            if (presentation.BeatStarted)
            {
                beats.Add(frame * Step);
            }
        }

        return beats;
    }

    [TestMethod]
    public void AboveThreshold_NeverThrobs()
    {
        Assert.AreEqual(0, BeatTimes(0.31f, active: true, seconds: 5f).Count);
    }

    [TestMethod]
    public void Inactive_NeverThrobs()
    {
        Assert.AreEqual(0, BeatTimes(0.1f, active: false, seconds: 5f).Count);
    }

    [TestMethod]
    public void FirstBeat_WaitsForTheHurtSound()
    {
        List<float> beats = BeatTimes(0.2f, active: true, seconds: 2f);
        Assert.IsTrue(beats.Count > 0);
        Assert.IsTrue(beats[0] >= 0.4f, $"first beat at {beats[0]:0.00} s");
    }

    [TestMethod]
    public void Throb_QuickensAsHealthFalls()
    {
        List<float> atThreshold = BeatTimes(0.3f, active: true, seconds: 6f);
        List<float> nearDeath = BeatTimes(0.01f, active: true, seconds: 6f);
        float slow = atThreshold[2] - atThreshold[1];
        float fast = nearDeath[2] - nearDeath[1];
        Assert.AreEqual(1.04f, slow, 0.03f);
        Assert.AreEqual(0.78f, fast, 0.03f);
    }

    [TestMethod]
    public void Healing_FadesTheLowStateOut()
    {
        var presentation = new LowHealthPresentation();
        for (int frame = 0; frame < 120; frame++)
        {
            presentation.Update(Step, 0.1f, true);
        }

        Assert.IsTrue(presentation.Amount > 0.7f);
        for (int frame = 0; frame < 60; frame++)
        {
            presentation.Update(Step, 1f, true);
            Assert.IsFalse(presentation.BeatStarted);
        }

        Assert.AreEqual(0f, presentation.Amount, 0.001f);
    }
}
