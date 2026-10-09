using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class PlayerPlaceAtTests
{
    [TestMethod]
    public void PlaceAt_KeepsHealth()
    {
        Player player = new(Vector2.Zero);
        player.ApplyDamage(GameBalance.PlayerMaxHealth / 2, Vector2.Zero, new ScreenEffects(), ignoreArmor: true);
        int healthBefore = player.Health;

        player.PlaceAt(new Vector2(120f, 240f));

        Assert.AreEqual(new Vector2(120f, 240f), player.Position);
        Assert.AreEqual(healthBefore, player.Health);
    }
}
