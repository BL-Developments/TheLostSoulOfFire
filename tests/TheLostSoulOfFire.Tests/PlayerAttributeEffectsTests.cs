using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Input;

namespace TheLostSoulOfFire.Tests;

/// <summary>The later attributes acting on the player's weapons, movement and abilities.</summary>
[TestClass]
public sealed class PlayerAttributeEffectsTests
{
    private const float Frame = 1f / 60f;
    private readonly Rectangle _bounds = new(0, 0, 2000, 2000);
    private readonly ParticleSystem _particles = new();
    private readonly ScreenEffects _screen = new();

    [TestMethod]
    [DataRow(10, 1)]
    [DataRow(20, 0)]
    public void AttackSpeed_ShortensScytheSwings(int attackSpeed, int expectedStep)
    {
        ScytheCombat scythe = new();
        PlayerAttributes attributes = PlayerAttributes.Default with { AttackSpeed = attackSpeed };
        InputState press = new();
        press.InjectMousePresses(left: true, right: false);
        scythe.Update(Frame, press, Vector2.UnitX, Vector2.Zero, _particles, true, false, attributes);

        // The first swing lasts 0.205 s; at Tempo 20 0.15 s are enough to finish it.
        scythe.Update(0.15f, new InputState(), Vector2.UnitX, Vector2.Zero, _particles, true, false, attributes);

        Assert.AreEqual(expectedStep, scythe.ActiveStep);
    }

    [TestMethod]
    [DataRow(10, 0.5f)]
    [DataRow(20, 0.75f)]
    public void AttackSpeed_ChargesTheCannonFaster(int attackSpeed, float expectedCharge)
    {
        SoulCannon cannon = new();
        PlayerAttributes attributes = PlayerAttributes.Default with { AttackSpeed = attackSpeed };
        UpdateCannonHeld(cannon, Frame, attributes);
        UpdateCannonHeld(cannon, GameBalance.CannonDrawDuration, attributes);

        UpdateCannonHeld(cannon, GameBalance.CannonFullChargeTime * 0.5f, attributes);

        Assert.AreEqual(expectedCharge, cannon.ChargeProgress, 0.001f);
    }

    [TestMethod]
    public void Attunement_RaisesResonanceGain()
    {
        Player player = new(Vector2.Zero) { Attributes = PlayerAttributes.Default with { Attunement = 20 } };

        player.AddResonance(GameBalance.ResonancePerSoulRelease);

        Assert.AreEqual(GameBalance.ResonancePerSoulRelease * 1.5f, player.Resonance, 0.001f);
    }

    [TestMethod]
    [DataRow(10, 1f)]
    [DataRow(20, 1.5f)]
    public void Agility_ShortensDodgeCooldown(int agility, float divisor)
    {
        Player player = new(new Vector2(1000, 1000)) { Attributes = PlayerAttributes.Default with { Agility = agility } };
        InputState dash = new();
        dash.InjectKeyPress(Keys.Space);

        UpdatePlayer(player, dash);

        Assert.IsTrue(player.IsDashing);
        Assert.AreEqual(GameBalance.DashCooldown / divisor, player.DashCooldownRemaining, 0.001f);
    }

    [TestMethod]
    public void Agility_RaisesMovementSpeed()
    {
        float baseline = WalkedDistance(PlayerAttributes.Default);

        float agile = WalkedDistance(PlayerAttributes.Default with { Agility = 20 });

        Assert.AreEqual(baseline * (1f + 10 * PlayerAttributes.MoveSpeedPerPoint), agile, 0.01f);
    }

    [TestMethod]
    public void Steadiness_ReducesKnockbackTaken()
    {
        float baseline = KnockbackDistance(PlayerAttributes.Default);

        float steady = KnockbackDistance(PlayerAttributes.Default with { Steadiness = 20 });

        Assert.AreEqual(baseline * 2f / 3f, steady, 0.01f);
    }

    [TestMethod]
    public void Focus_RunsAbilityCooldownsDownFaster()
    {
        Player player = new(new Vector2(500, 350)) { Attributes = PlayerAttributes.Default with { Focus = 20 } };
        CurrencyWallet wallet = new();
        wallet.BeginRun(30);
        RunAbilities abilities = new();
        Assert.IsTrue(abilities.TryCast(RunAbility.Vortex, player, wallet, new Vector2(650, 350), _bounds, [], _particles));

        abilities.Update(1f, player, _bounds, [], _particles, (enemy, damage) => enemy.ApplyDamage(damage));

        float cooldown = RunAbilities.Definitions[(int)RunAbility.Vortex].Cooldown;
        Assert.AreEqual(cooldown - 1.5f, abilities.Cooldown(RunAbility.Vortex), 0.001f);
    }

    private void UpdateCannonHeld(SoulCannon cannon, float dt, PlayerAttributes attributes)
    {
        InputState held = new();
        held.InjectMousePresses(left: false, right: true);
        cannon.Update(dt, held, Vector2.Zero, Vector2.UnitX, true, false, _particles, false, attributes);
    }

    private void UpdatePlayer(Player player, InputState input) =>
        player.Update(Frame, input, player.Position + Vector2.UnitX * 200f, _bounds, _particles, _screen);

    private float WalkedDistance(PlayerAttributes attributes)
    {
        Player player = new(new Vector2(1000, 1000)) { Attributes = attributes };
        InputState walk = new();
        walk.InjectKeyDown(Keys.D);
        float start = player.Position.X;

        UpdatePlayer(player, walk);

        return player.Position.X - start;
    }

    private float KnockbackDistance(PlayerAttributes attributes)
    {
        Player player = new(new Vector2(1000, 1000)) { Attributes = attributes };
        player.ApplyDamage(10, new Vector2(300f, 0f), _screen);
        float start = player.Position.X;

        UpdatePlayer(player, new InputState());

        return player.Position.X - start;
    }
}
