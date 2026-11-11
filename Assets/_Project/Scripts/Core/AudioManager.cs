using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LegacyOfTemples.Core
{
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager instance;
        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private AudioSource bgmSource;
        private AudioSource ambienceSource;
        private AudioSource sfxSource;
        private AudioSource footstepSource;
        private AudioSource dialogueSource;

        private Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);

        private float masterVolume = 1.0f;
        private float musicVolume = 0.8f;
        private float sfxVolume = 0.9f;
        private float dialogueVolume = 1.0f;

        private bool isDucking = false;
        private float duckFactor = 1.0f;
        private Coroutine duckCoroutine;
        private Coroutine bgmFadeCoroutine;

        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SFXVolume => sfxVolume;
        public float DialogueVolume => dialogueVolume;

        public bool IsBGMPlaying => bgmSource != null && bgmSource.isPlaying;
        public bool IsAmbiencePlaying => ambienceSource != null && ambienceSource.isPlaying;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
            LoadVolumePreferences();
            PreloadClips();

            EventBus.OnDialogueStart += HandleDialogueStart;
            EventBus.OnDialogueEnd += HandleDialogueEnd;
            EventBus.OnClueFound += HandleClueFound;
            EventBus.OnPuzzleSolved += HandlePuzzleSolved;
        }

        private void OnDestroy()
        {
            EventBus.OnDialogueStart -= HandleDialogueStart;
            EventBus.OnDialogueEnd -= HandleDialogueEnd;
            EventBus.OnClueFound -= HandleClueFound;
            EventBus.OnPuzzleSolved -= HandlePuzzleSolved;
        }

        private void InitializeAudioSources()
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.spatialBlend = 0.0f;

            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.loop = true;
            ambienceSource.playOnAwake = false;
            ambienceSource.spatialBlend = 0.0f;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0.0f;

            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.loop = false;
            footstepSource.playOnAwake = false;
            footstepSource.spatialBlend = 0.0f;

            dialogueSource = gameObject.AddComponent<AudioSource>();
            dialogueSource.loop = false;
            dialogueSource.playOnAwake = false;
            dialogueSource.spatialBlend = 0.0f;

            ApplyVolumes();
        }

        private void LoadVolumePreferences()
        {
            masterVolume = PlayerPrefs.GetFloat("Volume_Master", 1.0f);
            musicVolume = PlayerPrefs.GetFloat("Volume_Music", 0.8f);
            sfxVolume = PlayerPrefs.GetFloat("Volume_SFX", 0.9f);
            dialogueVolume = PlayerPrefs.GetFloat("Volume_Dialogue", 1.0f);
            ApplyVolumes();
        }

        private void PreloadClips()
        {
            string[] clipNames = new string[]
            {
                "bgm_menu", "bgm_exploration", "bgm_suspense",
                "ambience_temple",
                "sfx_footstep_stone1", "sfx_footstep_stone2", "sfx_footstep_stone3", "sfx_footstep_stone4",
                "sfx_ui_click", "sfx_ui_select",
                "sfx_clue_found", "sfx_puzzle_success", "sfx_puzzle_fail",
                "sfx_door_stone"
            };

            foreach (var name in clipNames)
            {
                AudioClip clip = Resources.Load<AudioClip>($"Audio/{name}");
                if (clip != null)
                {
                    clipCache[name] = clip;
                }
            }
        }

        public AudioClip GetClip(string clipName)
        {
            if (clipCache.TryGetValue(clipName, out AudioClip clip))
            {
                return clip;
            }

            clip = Resources.Load<AudioClip>($"Audio/{clipName}");
            if (clip != null)
            {
                clipCache[clipName] = clip;
            }
            return clip;
        }

        public void PlayBGM(string trackName, float fadeTime = 1.0f)
        {
            AudioClip clip = GetClip(trackName);
            if (clip == null) return;

            if (bgmSource.clip == clip && bgmSource.isPlaying) return;

            if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = StartCoroutine(FadeBGM(clip, fadeTime));
        }

        private IEnumerator FadeBGM(AudioClip newClip, float fadeTime)
        {
            float startVol = bgmSource.volume;
            if (bgmSource.isPlaying && fadeTime > 0f)
            {
                for (float t = 0f; t < fadeTime * 0.5f; t += Time.unscaledDeltaTime)
                {
                    bgmSource.volume = Mathf.Lerp(startVol, 0f, t / (fadeTime * 0.5f));
                    yield return null;
                }
            }

            bgmSource.clip = newClip;
            bgmSource.Play();

            float targetVol = masterVolume * musicVolume * duckFactor;
            for (float t = 0f; t < fadeTime * 0.5f; t += Time.unscaledDeltaTime)
            {
                bgmSource.volume = Mathf.Lerp(0f, targetVol, t / (fadeTime * 0.5f));
                yield return null;
            }
            bgmSource.volume = targetVol;
        }

        public void PlayAmbience(string trackName)
        {
            AudioClip clip = GetClip(trackName);
            if (clip == null) return;

            ambienceSource.clip = clip;
            ambienceSource.volume = masterVolume * musicVolume * 0.75f;
            ambienceSource.Play();
        }

        /// <summary>Send an AudioSource through a mixer group ("Music", "Ambience", "SFX", "Voice", "UI"...). Set by the audio director.</summary>
        public static System.Action<AudioSource, string> Router;
        public static void Route(AudioSource a, string group) { if (a != null) Router?.Invoke(a, group); }

        public void PlaySFX(string sfxName, float volumeScale = 1.0f, float pitch = 1.0f)
        {
            AudioClip clip = GetClip(sfxName);
            if (clip == null) return;

            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, masterVolume * sfxVolume * volumeScale);
        }

        public void PlayFootstep()
        {
            int idx = UnityEngine.Random.Range(1, 5);
            AudioClip clip = GetClip($"sfx_footstep_stone{idx}");
            if (clip != null)
            {
                footstepSource.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
                footstepSource.PlayOneShot(clip, masterVolume * sfxVolume * 0.55f);
            }
        }

        public void SetDialogueDucking(bool ducking)
        {
            isDucking = ducking;
            if (duckCoroutine != null) StopCoroutine(duckCoroutine);
            duckCoroutine = StartCoroutine(DuckBGM(ducking ? 0.3f : 1.0f, 0.4f));
        }

        private IEnumerator DuckBGM(float targetFactor, float duration)
        {
            float startFactor = duckFactor;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                duckFactor = Mathf.Lerp(startFactor, targetFactor, t / duration);
                bgmSource.volume = masterVolume * musicVolume * duckFactor;
                yield return null;
            }
            duckFactor = targetFactor;
            bgmSource.volume = masterVolume * musicVolume * duckFactor;
        }

        public void SetMasterVolume(float vol)
        {
            masterVolume = Mathf.Clamp01(vol);
            PlayerPrefs.SetFloat("Volume_Master", masterVolume);
            ApplyVolumes();
        }

        public void SetMusicVolume(float vol)
        {
            musicVolume = Mathf.Clamp01(vol);
            PlayerPrefs.SetFloat("Volume_Music", musicVolume);
            ApplyVolumes();
        }

        public void SetSFXVolume(float vol)
        {
            sfxVolume = Mathf.Clamp01(vol);
            PlayerPrefs.SetFloat("Volume_SFX", sfxVolume);
            ApplyVolumes();
        }

        public void SetDialogueVolume(float vol)
        {
            dialogueVolume = Mathf.Clamp01(vol);
            PlayerPrefs.SetFloat("Volume_Dialogue", dialogueVolume);
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            if (bgmSource != null) bgmSource.volume = masterVolume * musicVolume * duckFactor;
            if (ambienceSource != null) ambienceSource.volume = masterVolume * musicVolume * 0.75f;
            if (sfxSource != null) sfxSource.volume = masterVolume * sfxVolume;
            if (footstepSource != null) footstepSource.volume = masterVolume * sfxVolume * 0.6f;
            if (dialogueSource != null) dialogueSource.volume = masterVolume * dialogueVolume;
        }

        private void HandleDialogueStart(string characterId)
        {
            SetDialogueDucking(true);
        }

        private void HandleDialogueEnd(string characterId)
        {
            SetDialogueDucking(false);
        }

        private void HandleClueFound(string clueId)
        {
            PlaySFX("sfx_clue_found", 1.0f);
        }

        private void HandlePuzzleSolved(string puzzleId)
        {
            PlaySFX("sfx_puzzle_success", 1.0f);
        }
    }
}

