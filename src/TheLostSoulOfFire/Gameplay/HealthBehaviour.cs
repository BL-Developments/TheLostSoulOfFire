using TheLostSoulOfFire.Ecs;

namespace TheLostSoulOfFire.Gameplay;

public sealed class HealthBehaviour : GameBehaviour, IDamageable
{
    private float _invulnerabilityRemaining;

    public HealthBehaviour(int maximum, float invulnerabilitySeconds = 0f)
    {
        if (maximum <= 0) throw new ArgumentOutOfRangeException(nameof(maximum));
        Maximum = maximum;
        Health = maximum;
        InvulnerabilitySeconds = invulnerabilitySeconds;
    }

    public int Maximum { get; }
    public int Health { get; private set; }
    public float InvulnerabilitySeconds { get; }
    public bool IsDead => Health <= 0;
    public bool WasHitThisFrame { get; private set; }

    public bool TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead || _invulnerabilityRemaining > 0f) return false;
        Health = Math.Max(0, Health - amount);
        _invulnerabilityRemaining = InvulnerabilitySeconds;
        WasHitThisFrame = true;
        return true;
    }

    public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
    {
        WasHitThisFrame = false;
        if (_invulnerabilityRemaining > 0f)
        {
            _invulnerabilityRemaining -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        }
    }
}
