using Microsoft.Xna.Framework;

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
    float LightScale = 1f,
    float DisplaySize = 108f)
{
    /// <summary>The protagonist. Newly dead, unstable, warm violet.</summary>
    public static readonly WardenIdentity Younger = new(
        "THE LOST SOUL",
        "P1",
        BodyTint: Color.White,
        Flame: new Color(145, 71, 255),
        FlameBright: new Color(221, 190, 255),
        Accent: new Color(198, 158, 255),
        LightScale: 1f,
        DisplaySize: 108f);

    /// <summary>
    /// The brother. Already a Warden, so the same flame reads colder, paler and
    /// steadier. The body tint pushes his cloth and iron toward blue-steel so the
    /// two silhouettes separate even with every effect switched off.
    /// </summary>
    public static readonly WardenIdentity Elder = new(
        "THE WARDEN BROTHER",
        "P2",
        BodyTint: new Color(196, 214, 255),
        Flame: new Color(122, 156, 232),
        FlameBright: new Color(206, 226, 255),
        Accent: new Color(158, 200, 255),
        // A cold flame separates from this violet-grey room at a much lower
        // intensity than a violet one. Matched by inspection of the two-player
        // captures so neither brother out-reads the other.
        LightScale: 0.74f,
        // Colour cannot be the only difference: the brothers have to stay
        // readable in grayscale and with effects reduced. The elder carries more
        // silhouette mass, which survives both.
        DisplaySize: 120f);
}
