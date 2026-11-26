using System.Collections.Generic;
using RamenTycoon.Core;
using RamenTycoon.Logic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RamenTycoon.Audio
{
    /// <summary>
    /// 소리 담당. 씬에 따로 놓지 않아도 게임이 시작되면 자동으로 생긴다 (_Audio, 씬이 바뀌어도 유지).
    ///
    /// 소리 파일 위치: Assets/_Project/Resources/Sounds/SFX (효과음), Sounds/BGM (배경음)
    ///   → 파일 이름으로 찾는다. 같은 이름으로 파일을 바꾸면 코드 수정 없이 소리가 바뀐다.
    /// 볼륨: 설정 창의 배경음/효과음 슬라이더 × 아래 배율 × (소리별 볼륨)
    /// 배경음은 상황에 따라 자동으로 바뀐다: 메인화면·준비·정산 = bgm_title, 영업 = bgm_business,
    ///   러시아워 = bgm_rush, 굿/노멀 엔딩 = bgm_ending_good, 배드 엔딩 = bgm_ending_bad
    ///
    /// 값을 Inspector에서 고치고 싶으면: 씬에 빈 오브젝트를 만들고 이 컴포넌트를 붙여서 값을 바꾸면 그게 쓰인다.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("볼륨 배율 (설정 창 값에 곱해짐)")]
        [Range(0f, 1f)] [SerializeField] float musicScale = 0.45f;
        [Range(0f, 1f)] [SerializeField] float sfxScale = 0.9f;

        [Tooltip("배경음이 바뀔 때 겹쳐서 넘어가는 시간 (초)")]
        [SerializeField] float musicFade = 0.8f;

        [Tooltip("효과음마다 음높이를 살짝 바꿔서 반복해도 덜 질리게 (0 = 끔)")]
        [Range(0f, 0.2f)] [SerializeField] float pitchVariation = 0.04f;

        [Tooltip("소리별 볼륨 조절 (이름 = 파일 이름, 예: pour, bgm_business)")]
        [SerializeField] List<VolumeOverride> volumeOverrides = new();

        [System.Serializable]
        public class VolumeOverride
        {
            public string name;
            [Range(0f, 2f)] public float volume = 1f;
        }

        const string SfxPath = "Sounds/SFX/";
        const string BgmPath = "Sounds/BGM/";
        const int SfxVoices = 10;

        readonly Dictionary<string, AudioClip> cache = new();
        readonly HashSet<string> missing = new();
        readonly List<AudioSource> sfxSources = new();
        AudioSource musicA, musicB;
        AudioSource musicCurrent;
        string musicName;
        float musicFadeT = 1f;
        float clickScanTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null || FindAnyObjectByType<AudioManager>() != null) return;
            new GameObject("_Audio").AddComponent<AudioManager>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            GameSettings.Load();

            musicA = NewSource("Music A", true);
            musicB = NewSource("Music B", true);
            musicCurrent = musicA;
            for (int i = 0; i < SfxVoices; i++) sfxSources.Add(NewSource("SFX " + i, false));

            GameSettings.Changed += ApplyVolumes;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            GameSettings.Changed -= ApplyVolumes;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }

        AudioSource NewSource(string n, bool loop)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = 0f;
            return s;
        }

        // ───────── 효과음 ─────────

        /// <summary>효과음 재생. 이름 = Resources/Sounds/SFX 안의 파일 이름</summary>
        public static void Play(string name, float volume = 1f)
        {
            if (Instance != null) Instance.PlaySfx(name, volume);
        }

        void PlaySfx(string name, float volume)
        {
            var clip = Load(SfxPath, name);
            if (clip == null) return;

            AudioSource src = null;
            foreach (var s in sfxSources)
                if (!s.isPlaying) { src = s; break; }
            if (src == null) src = sfxSources[0]; // 다 쓰는 중이면 가장 먼저 것을 끊고 재생

            src.clip = clip;
            src.volume = GameSettings.Sfx * sfxScale * volume * VolumeOf(name);
            src.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            src.Play();
        }

        // ───────── 배경음 ─────────

        /// <summary>배경음 바꾸기 (같은 곡이면 그대로). 보통은 상황에 따라 자동으로 바뀜</summary>
        public static void PlayMusic(string name)
        {
            if (Instance != null) Instance.SwitchMusic(name);
        }

        void SwitchMusic(string name)
        {
            if (name == musicName) return;
            var clip = Load(BgmPath, name);
            musicName = name;
            var next = musicCurrent == musicA ? musicB : musicA;
            next.clip = clip;
            next.volume = 0f;
            if (clip != null) next.Play();
            musicCurrent = next;
            musicFadeT = 0f;
        }

        void Update()
        {
            SwitchMusic(PickMusic());

            // 겹쳐서 넘어가기
            if (musicFadeT < 1f)
            {
                musicFadeT = Mathf.Min(1f, musicFadeT + Time.unscaledDeltaTime / Mathf.Max(0.01f, musicFade));
                var other = musicCurrent == musicA ? musicB : musicA;
                other.volume = MusicVolume(other.clip) * (1f - musicFadeT);
                if (musicFadeT >= 1f) other.Stop();
            }
            musicCurrent.volume = MusicVolume(musicCurrent.clip) * musicFadeT;

            // 나중에 만들어진 버튼에도 클릭 소리 (1초마다 확인)
            clickScanTimer -= Time.unscaledDeltaTime;
            if (clickScanTimer <= 0f)
            {
                clickScanTimer = 1f;
                AttachClickSounds();
            }
        }

        string PickMusic()
        {
            var dm = DayManager.Instance;
            if (dm == null) return "bgm_title"; // 메인화면
            switch (dm.Phase)
            {
                case DayPhase.Business: return dm.InRush ? "bgm_rush" : "bgm_business";
                case DayPhase.Closing: return "bgm_business";
                case DayPhase.Ending: return dm.Ending == EndingType.Closed ? "bgm_ending_bad" : "bgm_ending_good";
                default: return "bgm_title"; // 준비, 정산
            }
        }

        float MusicVolume(AudioClip clip) =>
            clip == null ? 0f : GameSettings.Bgm * musicScale * VolumeOf(clip.name);

        void ApplyVolumes()
        {
            musicCurrent.volume = MusicVolume(musicCurrent.clip) * musicFadeT;
        }

        // ───────── 공통 ─────────

        float VolumeOf(string name)
        {
            foreach (var v in volumeOverrides)
                if (v != null && v.name == name) return v.volume;
            return 1f;
        }

        AudioClip Load(string folder, string name)
        {
            string key = folder + name;
            if (cache.TryGetValue(key, out var clip)) return clip;
            clip = Resources.Load<AudioClip>(key);
            if (clip == null && missing.Add(key))
                Debug.LogWarning($"[AudioManager] 소리 파일이 없어요: Assets/_Project/Resources/{key}.wav (이 소리는 건너뜀)");
            cache[key] = clip;
            return clip;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AttachClickSounds();
            EnsureListener();
        }

        /// <summary>소리를 들으려면 씬에 AudioListener가 하나 있어야 한다 (보통 Main Camera). 없으면 여기에 붙임</summary>
        void EnsureListener()
        {
            var mine = GetComponent<AudioListener>();
            int count = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length;
            if (mine == null && count == 0) gameObject.AddComponent<AudioListener>();
            else if (mine != null && count > 1) Destroy(mine);
        }

        static void AttachClickSounds()
        {
            foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include))
            {
                if (b.GetComponent<UIClickSound>() != null) continue;
                // 투명한 클릭 영역(고명 통, 엔딩 화면 넘기기 등)은 각자 소리가 따로 있어서 제외
                if (b.targetGraphic != null && b.targetGraphic.color.a < 0.01f) continue;
                string n = b.gameObject.name;
                if (n.StartsWith("Bowl") || n.StartsWith("Ending") || n.StartsWith("BinButton")) continue;
                b.gameObject.AddComponent<UIClickSound>();
            }
        }
    }
}
