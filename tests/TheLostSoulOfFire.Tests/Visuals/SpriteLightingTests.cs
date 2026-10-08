using System;
using System.Linq;
using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Tests.Visuals;

[TestClass]
public sealed class SpriteLightingTests
{
    [TestMethod]
    public void FlatNormal_LeavesPaintedColourUnchanged()
    {
        // A flat normal (0, 0, 1) sees the key light at KeyLightDirection.Z.
        float flat = SpriteLighting.Ambient + SpriteLighting.KeyLightStrength * SpriteLighting.KeyLightDirection.Z;

        Assert.AreEqual(1f, flat, 0.0001f);
        Assert.IsTrue(SpriteLighting.KeyLightDirection.X < 0f && SpriteLighting.KeyLightDirection.Y < 0f, "key light from the upper left");
    }

    [TestMethod]
    public void SelectLights_TakesTheStrongestEight_StrongestFirst()
    {
        SceneLight[] lights = new SceneLight[12];
        for (int index = 0; index < lights.Length; index++)
        {
            lights[index] = new SceneLight(new Vector2(100f + index * 5f, 0f), 120f, Color.White, (index + 1) / 12f);
        }

        Span<SceneLight> chosen = new SceneLight[SpriteLighting.MaxPointLights];
        int count = SpriteLighting.SelectLights(lights, Vector2.Zero, 10f, chosen);

        Assert.AreEqual(SpriteLighting.MaxPointLights, count);
        for (int index = 1; index < count; index++)
        {
            Assert.IsTrue(Weight(chosen[index - 1]) >= Weight(chosen[index]));
        }
        Assert.IsFalse(chosen[..count].ToArray().Any(light => light.Intensity <= 4f / 12f), "the four weakest lights are dropped");
    }

    [TestMethod]
    public void SelectLights_SkipsOwnGlow_AndLightsOutOfReach()
    {
        SceneLight own = new(new Vector2(3f, 2f), 60f, Color.White, 1f);
        SceneLight far = new(new Vector2(1000f, 0f), 60f, Color.White, 1f);
        SceneLight near = new(new Vector2(-60f, 0f), 60f, Color.Violet, 0.5f);

        Span<SceneLight> chosen = new SceneLight[SpriteLighting.MaxPointLights];
        int count = SpriteLighting.SelectLights([own, far, near], Vector2.Zero, 20f, chosen);

        Assert.AreEqual(1, count);
        Assert.AreEqual(near, chosen[0]);
    }

    private static float Weight(SceneLight light)
    {
        float reach = light.Radius * SpriteLighting.LightRadiusScale;
        float falloff = 1f - light.Position.Length() / reach;
        return light.Intensity * falloff * falloff;
    }
}
