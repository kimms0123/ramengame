using System;
using UnityEngine;

namespace RamenTycoon.Core
{
    /// <summary>
    /// 설정값 (배경음, 효과음, 전체 화면). PlayerPrefs에 저장된다.
    /// 소리를 재생하는 쪽(나중에 만들 AudioManager)은 Bgm/Sfx 값과 Changed 이벤트를 쓰면 된다.
    /// </summary>
    public static class GameSettings
    {
        public const float DefaultBgm = 0.7f;
        public const float DefaultSfx = 0.8f;
        public const bool DefaultFullscreen = true;

        const string KeyBgm = "settings.bgm";
        const string KeySfx = "settings.sfx";
        const string KeyFullscreen = "settings.fullscreen";

        public static float Bgm { get; private set; } = DefaultBgm;
        public static float Sfx { get; private set; } = DefaultSfx;
        public static bool Fullscreen { get; private set; } = DefaultFullscreen;

        public static event Action Changed;

        static bool loaded;

        public static void Load()
        {
            if (loaded) return;
            loaded = true;
            Bgm = PlayerPrefs.GetFloat(KeyBgm, DefaultBgm);
            Sfx = PlayerPrefs.GetFloat(KeySfx, DefaultSfx);
            Fullscreen = PlayerPrefs.GetInt(KeyFullscreen, DefaultFullscreen ? 1 : 0) == 1;
        }

        public static void Apply(float bgm, float sfx, bool fullscreen)
        {
            Bgm = Mathf.Clamp01(bgm);
            Sfx = Mathf.Clamp01(sfx);
            Fullscreen = fullscreen;

            PlayerPrefs.SetFloat(KeyBgm, Bgm);
            PlayerPrefs.SetFloat(KeySfx, Sfx);
            PlayerPrefs.SetInt(KeyFullscreen, Fullscreen ? 1 : 0);
            PlayerPrefs.Save();

            if (!Application.isEditor) Screen.fullScreen = Fullscreen; // 에디터에서는 창 크기를 바꾸지 않음
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            loaded = false;
            Changed = null;
        }
    }
}
