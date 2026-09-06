using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Entities;

/// <summary>
/// Who a Warden is, expressed as the light they carry.
///
/// Both brothers burn with the same Death Flame, so both stay violet-white — the
/// canon colour. They are told apart by temperature and discipline: the younger
/// brother's flame is warm-violet and still guttering, the elder's has been held
/// for long enough to burn pale and cold. This is the whole visual identity seam
/// for local co-op; there is deliberately no second character class behind it.
/// </summary>
public sealed record WardenIdentity(
    string Name,
    string ShortName,
    Color BodyTint,
    Color Flame,
    Color FlameBright,
    Color Accent,
    string SheetFamily,
    float LightScale = 1f,
    float DisplaySize = GameBalance.WardenDisplaySize)
{
    /// <summary>The protagonist. Newly dead, unstable, warm violet.</summary>
    public static readonly WardenIdentity Younger = new(
        "THE LOST SOUL",
        "P1",
        BodyTint: Color.White,
        Flame: new Color(145, 71, 255),
        FlameBright: new Color(221, 190, 255),
        Accent: new Color(198, 158, 255),
        SheetFamily: "warden",
        LightScale: 1f);

    /// <summary>
    /// The brother. Already a Warden, so the same flame reads colder, paler and
    /// steadier.
    ///
    /// Session 2 separated the two brothers by tint and by display size, and the
    /// capture review recorded that as the weakest result in the whole co-op
    /// pass: in grayscale they differed only in mass. He now has his own sheet
    /// from the same rig — hood up, long mantle, no scarf — so the difference is
    /// structural and survives grayscale, reduced effects and distance. Both
    /// brothers are drawn at the same exact 1:1 pixel scale.
    /// </summary>
    public static readonly WardenIdentity Elder = new(
        "THE WARDEN BROTHER",
        "P2",
        BodyTint: new Color(196, 214, 255),
        Flame: new Color(122, 156, 232),
        FlameBright: new Color(206, 226, 255),
        Accent: new Color(158, 200, 255),
        SheetFamily: "warden_elder",
        // A cold flame separates from this violet-grey room at a much lower
        // intensity than a violet one. Matched by inspection of the two-player
        // captures so neither brother out-reads the other.
        LightScale: 0.74f);
}
