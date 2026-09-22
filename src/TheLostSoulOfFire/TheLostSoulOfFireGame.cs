using TheLostSoulOfFire.Core;
using TheLostSoulOfFire.Gameplay;
using TheLostSoulOfFire.Scenes;

namespace TheLostSoulOfFire;

public sealed class TheLostSoulOfFireGame : GameCore
{
    public TheLostSoulOfFireGame() : base("The Lost Soul of Fire", 960, 540) { }

    public GameSession Session { get; private set; } = null!;

    protected override void PostInitialize()
    {
        Session = new GameSession();
        Scenes.RequestChange(new HubScene(Session));
    }
}
