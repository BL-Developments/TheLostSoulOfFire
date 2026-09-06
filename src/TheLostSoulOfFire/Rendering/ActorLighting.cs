using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// The Soulfire actor light grammar, drawn between the environment and the
/// fighting plane.
///
/// Inspection of the Session 1 captures showed the real readability failure: the
/// Warden sheet has a median luminance of 22/255 and the casting floor sits at
/// 21/255, so the protagonist had no value separation from the ground at all and
/// read as the least present thing in his own game. Adding bloom would not fix
/// that — the missing ingredient is local figure/ground contrast.
///
/// The rule this establishes, and that later content should inherit:
///
///   * Living Death Flame light — violet-white — belongs to Wardens and Souls.
///   * Manifestations are separated by cold ash light only. They do not glow;
///     they are merely lit enough to have a silhouette. Their violet appears at
///     the Anchor and the fractures, which is what the Player is asked to read.
///
/// Everything here is painted (soft brush and alpha-mask stamps under an additive
/// blend), never stroked, per the 2026-09-06 owner revision.
/// </summary>
public static class ActorLighting
{
    /// <summary>Cold light that gives a manifestation a body without giving it flame.</summary>
    public static readonly Color AshLight = new(126, 124, 148);

    private static float Pulse(float time, float speed) => 0.5f + 0.5f * MathF.Sin(time * speed);

    /// <summary>
    /// A Warden. The silhouette is lit by their own flame, and the floor under
    /// them takes a contact pool so they are standing in the room rather than
    /// pasted onto it.
    /// </summary>
    public static void DrawWarden(
        SpriteBatch batch,
        ArtAssets art,
        Texture2D brush,
        Player player,
        float time)
    {
        ActorFrame frame = art.ResolvePlayerFrame(player);
        if (!frame.IsValid)
        {
            return;
        }

        WardenIdentity identity = player.Identity;
        float breathe = 0.86f + 0.14f * Pulse(time + player.LightPhase, 3.1f);

        // Base presence. Enough to own a silhouette in a dark room, not enough to
        // turn the character into a lamp.
        //
        // Session 3 halved this. The number above was set when the Warden sheet
        // had a median luminance of 22/255 against a floor of 21 — he had no
        // value of his own, so the rim was carrying the entire read. The
        // authored sheet measures 56/255 with a face at 140, so the light can go
        // back to being light. Left at the old strength it washed the new pixel
        // art into a violet blob, which is the "too shiny" note again.
        float radius = 1.7f * breathe;
        float strength = 0.24f;

        if (player.IsDowned)
        {
            // A guttering Warden. The light collapses and flickers, but it stays
            // his own colour and carries a slow wide beacon: a brother has to be
            // findable across a busy arena or the rescue rule is theoretical.
            float flicker = 0.5f + 0.5f * Pulse(time, 9f);
            float beacon = 0.35f + 0.65f * Pulse(time, 1.5f);
            SoftShapes.Pool(batch, brush, player.Position + new Vector2(0f, 26f), 108f, 44f,
                identity.Flame * (0.09f * beacon));
            DrawContactPool(batch, brush, player.Position, 54f, identity.Flame * 0.12f);
            art.DrawSilhouetteLight(batch, frame, identity.Flame * (0.18f + 0.18f * flicker), 2.0f);
            art.DrawSilhouetteLight(batch, frame, identity.FlameBright * (0.10f * flicker), 1.2f);
            return;
        }

        if (player.SoulSenseActive)
        {
            strength += 0.07f;
        }

        if (player.IsResonanceReady)
        {
            strength += 0.08f;
        }

        if (player.ResonanceActive)
        {
            radius += 0.8f;
            strength += 0.18f;
        }

        // Severance: the flame is drawn taut. It brightens the *edge* of the
        // Warden rather than washing the body out, so the read stays readable as
        // a pose instead of a lens flare.
        if (player.SeveranceReady)
        {
            float life = MathHelper.Clamp(player.SeveranceRemaining / GameBalance.SeveranceWindowDuration, 0f, 1f);
            float taut = 0.62f + 0.38f * Pulse(time, 14f);
            radius += 1.1f * life * taut;
            strength += 0.28f * life;
        }

        strength *= identity.LightScale;
        DrawContactPool(batch, brush, player.Position, 58f * breathe, identity.Flame * (0.085f * identity.LightScale));
        art.DrawSilhouetteLight(batch, frame, identity.Flame * (strength * 0.62f), radius * 1.55f);
        art.DrawSilhouetteLight(batch, frame, identity.FlameBright * (strength * 0.5f), radius);
    }

    /// <summary>
    /// A manifestation. Ash light only: enough separation to fight against, no
    /// flame of its own. The Devourer is given slightly more because its mass is
    /// the threat the Player has to track through a crowded frame.
    /// </summary>
    public static void DrawManifestations(
        SpriteBatch batch,
        ArtAssets art,
        IReadOnlyList<Enemy> enemies,
        float time)
    {
        foreach (Enemy enemy in enemies)
        {
            if (!enemy.IsAlive)
            {
                continue;
            }

            ActorFrame frame = art.ResolveEnemyFrame(enemy);
            if (!frame.IsValid)
            {
                continue;
            }

            float strength = enemy is Devourer ? 0.4f : 0.31f;
            float radius = enemy is Devourer ? 3.1f : 2.3f;

            // A committed attacker is briefly given more edge, so the thing about
            // to hit you is the thing you can see best.
            float remaining = enemy.CommitmentRemaining;
            if (remaining >= 0f && remaining <= GameBalance.SeveranceMarkLead)
            {
                float closing = 1f - MathHelper.Clamp(remaining / GameBalance.SeveranceMarkLead, 0f, 1f);
                strength += 0.26f * closing;
                radius += 0.9f * closing;
            }

            if (enemy.HitFlashRemaining > 0f)
            {
                strength += 0.5f * MathHelper.Clamp(enemy.HitFlashRemaining / 0.16f, 0f, 1f);
            }

            art.DrawSilhouetteLight(batch, frame, AshLight * strength, radius, 8);
        }
    }

    /// <summary>
    /// The light an actor casts on the floor it is standing on. Flattened to the
    /// ground so it reads as a pool rather than a halo around the body.
    /// </summary>
    private static void DrawContactPool(SpriteBatch batch, Texture2D brush, Vector2 position, float width, Color color)
    {
        SoftShapes.Pool(batch, brush, position + new Vector2(0f, 34f), width, width * 0.38f, color);
    }
}
