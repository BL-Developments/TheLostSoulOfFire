using System;
using TheLostSoulOfFire.Debugging;

if (!DeveloperStartOptions.TryParse(args, out DeveloperStartOptions? developerStart, out string? developerStartError))
{
    Console.Error.WriteLine(developerStartError);
    Console.Error.WriteLine(DeveloperStartOptions.Usage);
    return 2;
}

bool audioRuntimeTest = Array.Exists(args, argument => argument == "--audio-runtime-test");
bool audioLoopRuntimeTest = Array.Exists(args, argument => argument == "--audio-loop-runtime-test");
bool audioGameplayTest = Array.Exists(args, argument => argument == "--audio-gameplay-test");
bool audioDeathRestartTest = Array.Exists(args, argument => argument == "--audio-death-restart-test");
bool antechamberVisualTest = Array.Exists(args, argument => argument == "--antechamber-visual-test");
bool currencyVisualTest = Array.Exists(args, argument => argument == "--currency-visual-test");
bool expectAudioFallback = Array.Exists(args, argument => argument == "--expect-audio-fallback");

using (Microsoft.Xna.Framework.Game game = audioRuntimeTest || audioLoopRuntimeTest
    ? new AudioRuntimeTestGame(expectAudioFallback, audioLoopRuntimeTest)
    : new TheLostSoulOfFire.Game1(audioGameplayTest, audioDeathRestartTest, antechamberVisualTest, developerStart, currencyVisualTest))
{
    game.Run();
}

return Environment.ExitCode;
