using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class RunAbilityTests
{
    private readonly Rectangle _bounds = new(0, 0, 1000, 700);
    private Player _player = null!;
    private CurrencyWallet _wallet = null!;
    private RunAbilities _abilities = null!;
    private readonly ParticleSystem _particles = new();
    private readonly ScreenEffects _screen = new();
    [TestInitialize] public void Initialize()
    {
        _player = new(new Vector2(500, 350));
        _wallet = new(); _wallet.BeginRun(30);
        _abilities = new();
    }
    private bool Cast(RunAbility ability, params Enemy[] enemies) =>
        _abilities.TryCast(ability, _player, _wallet, new Vector2(650, 350), _bounds, enemies, _particles);
    private void Update(float dt, params Enemy[] enemies) =>
        _abilities.Update(dt, _player, _bounds, enemies, _particles, (enemy, damage) => enemy.ApplyDamage(damage));

    [TestMethod] public void Healing_ClampsAndFullHealthCostsNothing()
    {
        Assert.IsFalse(Cast(RunAbility.SecondWind));
        Assert.AreEqual(30, _wallet.Run(Currency.Glut));
        _player.ApplyDamage(10, Vector2.Zero, _screen, true);
        Assert.IsTrue(Cast(RunAbility.SecondWind));
        Assert.AreEqual(GameBalance.PlayerMaxHealth, _player.Health);
        Assert.AreEqual(27, _wallet.Run(Currency.Glut));
    }
    [TestMethod] public void CooldownAndInsufficientFunds_DoNotSpendAgainOrTouchSecured()
    {
        _wallet.LoadSecured(new PlayerProfile { SecuredGlut = 90 });
        Assert.IsTrue(Cast(RunAbility.PiercingShot));
        Assert.IsFalse(Cast(RunAbility.PiercingShot));
        Assert.AreEqual(27, _wallet.Run(Currency.Glut));
        _wallet.BeginRun(1); Update(2);
        Assert.IsFalse(Cast(RunAbility.PiercingShot));
        Assert.AreEqual(1, _wallet.Run(Currency.Glut));
        Assert.AreEqual(90, _wallet.Secured(Currency.Glut));
    }
    [TestMethod] public void Projectile_PiercesAndHitsEachEnemyOnce()
    {
        Enemy first = new Hollow(new Vector2(570, 350), 1);
        Enemy second = new Hollow(new Vector2(620, 350), 2);
        Assert.IsTrue(Cast(RunAbility.PiercingShot));
        Update(0.1f, first, second);
        Assert.AreEqual(first.MaxHealth - 40, first.Health);
        Assert.AreEqual(second.MaxHealth - 40, second.Health);
        Update(0.001f, first, second);
        Assert.AreEqual(first.MaxHealth - 40, first.Health);
        Assert.AreEqual(second.MaxHealth - 40, second.Health);
    }
    [TestMethod] public void Projectile_CannotHitBeyondBoundary()
    {
        AbilityProjectile projectile = new(new Vector2(950, 350), Vector2.UnitX, 40);
        projectile.Update(0.2f, _bounds);
        Assert.IsTrue(projectile.IsFinished);
        Assert.IsFalse(projectile.TryHit(new Hollow(new Vector2(1100, 350), 1)));
    }
    [TestMethod] public void Retreat_ClampsAndDoesNotGrantInvulnerability()
    {
        Vector2 before = _player.Position;
        Assert.IsTrue(Cast(RunAbility.Retreat));
        Update(0.22f);
        Assert.AreEqual(before.X - 180, _player.Position.X, 0.01f);
        Assert.IsFalse(_player.IsInvulnerable);
        Assert.IsFalse(_player.IsDashing);
    }
    [TestMethod] public void BlockedRetreat_RejectsWithoutCost()
    {
        _player.Reset(new Vector2(_player.Radius, 350));
        Assert.IsFalse(Cast(RunAbility.Retreat));
        Assert.AreEqual(30, _wallet.Run(Currency.Glut));
    }
    [TestMethod] public void Retreat_PushesOrdinaryEnemyButNotBoss()
    {
        Enemy hollow = new Hollow(new Vector2(550, 350), 1);
        Enemy boss = new Devourer(new Vector2(570, 350));
        Vector2 bossBefore = boss.Position;
        Assert.IsTrue(Cast(RunAbility.Retreat, hollow, boss));
        Assert.IsTrue(hollow.Position.X > 550);
        Assert.AreEqual(bossBefore, boss.Position);
    }
    [TestMethod] public void Vortex_PullsWithoutDamageAndLeavesBossStill()
    {
        Enemy hollow = new Hollow(new Vector2(740, 350), 1);
        Enemy boss = new Devourer(new Vector2(720, 350));
        Assert.IsTrue(Cast(RunAbility.Vortex));
        Update(0.2f, hollow, boss);
        Assert.IsTrue(hollow.Position.X < 740);
        Assert.AreEqual(hollow.MaxHealth, hollow.Health);
        Assert.AreEqual(new Vector2(720, 350), boss.Position);
    }
    [TestMethod] public void Revenge_BlocksOnceAndBonusExpires()
    {
        Assert.IsTrue(Cast(RunAbility.Revenge));
        _player.ApplyDamage(20, Vector2.Zero, _screen);
        Assert.AreEqual(GameBalance.PlayerMaxHealth, _player.Health);
        Assert.AreEqual(0f, _player.AbilityEffects.GuardRemaining);
        Assert.AreEqual(24, _player.AbilityEffects.ConsumeRevenge());
        Assert.AreEqual(0, _player.AbilityEffects.ConsumeRevenge());
        _player.AbilityEffects.Guard(); _player.AbilityEffects.TryBlock();
        _player.AbilityEffects.Update(6);
        Assert.AreEqual(0, _player.AbilityEffects.ConsumeRevenge());
    }
    [TestMethod] public void GuardTimeout_GivesNoFreeBonus()
    {
        _player.AbilityEffects.Guard(); _player.AbilityEffects.Update(3);
        Assert.IsFalse(_player.AbilityEffects.TryBlock());
        Assert.AreEqual(0, _player.AbilityEffects.ConsumeRevenge());
    }
    [TestMethod] public void Setup_ArmsOnceAndMarkExpires()
    {
        Assert.IsTrue(Cast(RunAbility.Setup));
        Assert.IsTrue(_player.AbilityEffects.ConsumeSetup());
        Assert.IsFalse(_player.AbilityEffects.ConsumeSetup());
        Enemy enemy = new Hollow(new Vector2(650, 350), 1);
        enemy.MarkForFollowup();
        Assert.AreEqual(25, enemy.ConsumeAbilityMark());
        Assert.AreEqual(0, enemy.ConsumeAbilityMark());
        enemy.MarkForFollowup();
        enemy.Update(6, _player, [], _bounds, _particles, _screen);
        Assert.AreEqual(0, enemy.ConsumeAbilityMark());
    }
    [TestMethod] public void DeathAndReset_ClearTransientStateAndPreventCasting()
    {
        Cast(RunAbility.Vortex);
        _player.AbilityEffects.Guard(); _player.AbilityEffects.PrepareSetup();
        _player.ApplyDamage(1000, Vector2.Zero, _screen, true);
        Assert.IsTrue(_player.IsDead);
        Assert.AreEqual(0f, _player.AbilityEffects.SetupRemaining);
        Assert.IsFalse(Cast(RunAbility.PiercingShot));
        Update(0.01f);
        Assert.AreEqual(0f, _abilities.VortexRemaining);
        _player.Reset(new Vector2(500, 350));
        Assert.AreEqual(0f, _player.AbilityEffects.GuardRemaining);
    }
    [TestMethod] public void Equipping_PreventsDuplicatesAndRetainsTwoDistinctSlots()
    {
        Assert.IsFalse(_abilities.Equip(1, RunAbility.SecondWind));
        Assert.IsTrue(_abilities.Equip(0, RunAbility.Vortex));
        Assert.AreNotEqual(_abilities.Slots[0], _abilities.Slots[1]);
    }

    [TestMethod] public void WeaponFollowup_DoesNotConsumeNewMarkOnFirstHit()
    {
        Enemy enemy = new Hollow(new Vector2(600, 350), 1);
        _player.AbilityEffects.PrepareSetup();
        DamageInfo first = RunAbilities.ResolveWeaponHit(_player, enemy, new DamageInfo(10, Vector2.Zero, enemy.Position));
        Assert.AreEqual(10, first.Damage);
        Assert.IsTrue(enemy.AbilityMarkRemaining > 0);
        DamageInfo followup = RunAbilities.ResolveWeaponHit(_player, enemy, new DamageInfo(10, Vector2.Zero, enemy.Position));
        Assert.AreEqual(35, followup.Damage);
        DamageInfo third = RunAbilities.ResolveWeaponHit(_player, enemy, new DamageInfo(10, Vector2.Zero, enemy.Position));
        Assert.AreEqual(10, third.Damage);
    }
    [TestMethod] public void WeaponBonus_IsConsumedByOneHitOnly()
    {
        Enemy first = new Hollow(new Vector2(600, 350), 1);
        Enemy second = new Hollow(new Vector2(620, 350), 2);
        _player.AbilityEffects.Guard(); _player.AbilityEffects.TryBlock();
        DamageInfo hit = new(10, Vector2.Zero, first.Position);
        Assert.AreEqual(34, RunAbilities.ResolveWeaponHit(_player, first, hit).Damage);
        Assert.AreEqual(10, RunAbilities.ResolveWeaponHit(_player, second, hit).Damage);
    }
}

