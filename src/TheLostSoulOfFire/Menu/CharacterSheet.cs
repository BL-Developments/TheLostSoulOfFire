using System;
using TheLostSoulOfFire.Combat;

namespace TheLostSoulOfFire.Menu;

/// <summary>
/// The values the character page shows, read from the player and the wallet each frame.
/// Percentages come from <see cref="PlayerAttributes"/> so display and damage rules never drift.
/// </summary>
public readonly record struct CharacterSheet(
    int Health,
    int MaxHealth,
    PlayerAttributes Attributes,
    bool InRun,
    int RunGeld,
    int SecuredGeld,
    int RunGlut,
    int SecuredGlut)
{
    public string HealthText => $"{Health} / {MaxHealth}";

    public string WeaponDamageText => $"WAFFENSCHADEN {FormatSignedPercent(Attributes.StrengthMultiplier - 1f)}";

    public string AbilityDamageText => $"FÄHIGKEITSSCHADEN {FormatSignedPercent(Attributes.AbilityPowerMultiplier - 1f)}";

    public string ArmorReductionText => $"SCHADENSVERRINGERUNG {ToPercent(Attributes.ArmorReduction)} %";

    public string AttackSpeedText => $"ANGRIFFSTEMPO {FormatSignedPercent(Attributes.AttackSpeedMultiplier - 1f)}";

    public string CoreDamageText => $"KERNSCHADEN {FormatSignedPercent(Attributes.CoreSharpnessMultiplier - 1f)}";

    public string FocusText => $"ABKLINGTEMPO {FormatSignedPercent(Attributes.FocusMultiplier - 1f)}";

    public string KnockbackText => $"RÜCKSTOSS {FormatSignedPercent(Attributes.KnockbackTaken - 1f)}";

    public string AgilityText =>
        $"AUSWEICHEN {FormatSignedPercent(Attributes.AgilityMultiplier - 1f)} / LAUFEN {FormatSignedPercent(Attributes.MoveSpeedMultiplier - 1f)}";

    public string AttunementText => $"RESONANZAUFBAU {FormatSignedPercent(Attributes.AttunementMultiplier - 1f)}";

    public string LuckText => $"BEUTE {FormatSignedPercent(Attributes.LuckMultiplier - 1f)}";

    /// <summary>Run balance, only shown in the arena; outside it there is no run.</summary>
    public string? GeldRunText => InRun ? $"IM LAUF {RunGeld}" : null;

    public string GeldSecuredText => $"GESICHERT {SecuredGeld}";

    public string? GlutRunText => InRun ? $"IM LAUF {RunGlut}" : null;

    public string GlutSecuredText => $"GESICHERT {SecuredGlut}";

    private static string FormatSignedPercent(float share)
    {
        int percent = ToPercent(share);
        return percent >= 0 ? $"+{percent} %" : $"{percent} %";
    }

    private static int ToPercent(float share) => (int)MathF.Round(share * 100f, MidpointRounding.AwayFromZero);
}
