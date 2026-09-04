using System.Runtime.InteropServices.JavaScript;

namespace MirEngine;

/// <summary>
/// 浏览器 Web Audio 音效后端封装。对应 jsengine/core/audio.js
/// （mir.initAudio / playSound / stopSound / stopAllSounds / setSoundVolume）。
/// </summary>
internal static partial class BrowserAudio
{
    [JSImport("mir.initAudio", "main.js")]
    private static partial void InitAudioImpl();

    [JSImport("mir.playSound", "main.js")]
    private static partial int PlaySoundImpl(string url, int volume, bool loop);

    [JSImport("mir.stopSound", "main.js")]
    private static partial void StopSoundImpl(int id);

    [JSImport("mir.stopAllSounds", "main.js")]
    private static partial void StopAllSoundsImpl();

    [JSImport("mir.setSoundVolume", "main.js")]
    private static partial void SetSoundVolumeImpl(int id, int volume);

    public static void InitAudio() => InitAudioImpl();
    public static int PlaySound(string url, int volume, bool loop) => PlaySoundImpl(url, volume, loop);
    public static void StopSound(int id) => StopSoundImpl(id);
    public static void StopAllSounds() => StopAllSoundsImpl();
    public static void SetSoundVolume(int id, int volume) => SetSoundVolumeImpl(id, volume);
}
