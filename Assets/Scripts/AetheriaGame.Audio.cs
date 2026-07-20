using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class AetheriaGame
{
    private enum AetheriaSfx
    {
        UiClick,
        Reject,
        Encounter,
        PlayerTurn,
        EnemyTurn,
        Physical,
        Magic,
        Hit,
        Critical,
        Miss,
        Heal,
        Victory,
        Defeat
    }

    private AudioSource aetheriaAudioSource;
    private readonly Dictionary<AetheriaSfx, AudioClip> aetheriaSfxClips = new Dictionary<AetheriaSfx, AudioClip>();
    private const float EncounterTurnCueGuardSeconds = 0.42f;
    private float nextTurnSfxTime;

    private void InitializeAetheriaAudio()
    {
        if (aetheriaAudioSource != null)
        {
            return;
        }

        aetheriaAudioSource = gameObject.AddComponent<AudioSource>();
        aetheriaAudioSource.playOnAwake = false;
        aetheriaAudioSource.loop = false;
        aetheriaAudioSource.spatialBlend = 0f;
        aetheriaAudioSource.volume = 0.42f;

        foreach (AetheriaSfx cue in Enum.GetValues(typeof(AetheriaSfx)))
        {
            aetheriaSfxClips[cue] = CreateAetheriaSfxClip(cue);
        }
    }

    private AudioClip CreateAetheriaSfxClip(AetheriaSfx cue)
    {
        const int sampleRate = 44100;
        float duration;
        switch (cue)
        {
            case AetheriaSfx.UiClick: duration = 0.075f; break;
            case AetheriaSfx.Reject: duration = 0.14f; break;
            case AetheriaSfx.Encounter: duration = 0.42f; break;
            case AetheriaSfx.PlayerTurn:
            case AetheriaSfx.EnemyTurn: duration = 0.22f; break;
            case AetheriaSfx.Physical: duration = 0.20f; break;
            case AetheriaSfx.Magic: duration = 0.38f; break;
            case AetheriaSfx.Hit: duration = 0.16f; break;
            case AetheriaSfx.Critical: duration = 0.34f; break;
            case AetheriaSfx.Miss: duration = 0.18f; break;
            case AetheriaSfx.Heal: duration = 0.34f; break;
            case AetheriaSfx.Victory: duration = 0.90f; break;
            default: duration = 0.72f; break;
        }

        var sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];
        var noise = new System.Random(1729 + (int)cue * 7919);
        for (var index = 0; index < sampleCount; index++)
        {
            var time = index / (float)sampleRate;
            var progress = Mathf.Clamp01(time / duration);
            var attack = Mathf.Clamp01(time / 0.012f);
            var release = Mathf.Pow(1f - progress, cue == AetheriaSfx.Hit || cue == AetheriaSfx.Physical ? 2.5f : 1.35f);
            var envelope = attack * release;
            var whiteNoise = (float)(noise.NextDouble() * 2.0 - 1.0);
            float value;

            switch (cue)
            {
                case AetheriaSfx.UiClick:
                    value = Mathf.Sin(2f * Mathf.PI * (720f + progress * 260f) * time) * 0.34f;
                    break;
                case AetheriaSfx.Reject:
                    value = (Mathf.Sin(2f * Mathf.PI * 165f * time) + Mathf.Sin(2f * Mathf.PI * 138f * time) * 0.6f) * 0.28f;
                    break;
                case AetheriaSfx.Encounter:
                    value = Mathf.Sin(2f * Mathf.PI * (150f + progress * 420f) * time) * 0.30f + whiteNoise * 0.08f;
                    break;
                case AetheriaSfx.PlayerTurn:
                    value = Mathf.Sin(2f * Mathf.PI * (520f + progress * 460f) * time) * 0.28f;
                    break;
                case AetheriaSfx.EnemyTurn:
                    value = Mathf.Sin(2f * Mathf.PI * (310f - progress * 110f) * time) * 0.32f;
                    break;
                case AetheriaSfx.Physical:
                    value = whiteNoise * 0.28f + Mathf.Sin(2f * Mathf.PI * (230f - progress * 150f) * time) * 0.25f;
                    break;
                case AetheriaSfx.Magic:
                    value = Mathf.Sin(2f * Mathf.PI * (360f + progress * 920f) * time) * 0.22f
                        + Mathf.Sin(2f * Mathf.PI * (540f + progress * 380f) * time) * 0.12f;
                    break;
                case AetheriaSfx.Hit:
                    value = whiteNoise * 0.38f + Mathf.Sin(2f * Mathf.PI * (118f - progress * 46f) * time) * 0.32f;
                    break;
                case AetheriaSfx.Critical:
                    value = whiteNoise * 0.31f + Mathf.Sin(2f * Mathf.PI * (92f - progress * 24f) * time) * 0.35f
                        + Mathf.Sin(2f * Mathf.PI * 880f * time) * 0.14f;
                    break;
                case AetheriaSfx.Miss:
                    value = whiteNoise * 0.13f + Mathf.Sin(2f * Mathf.PI * (680f - progress * 360f) * time) * 0.17f;
                    break;
                case AetheriaSfx.Heal:
                    value = Mathf.Sin(2f * Mathf.PI * (420f + progress * 540f) * time) * 0.22f
                        + Mathf.Sin(2f * Mathf.PI * (630f + progress * 330f) * time) * 0.11f;
                    break;
                case AetheriaSfx.Victory:
                {
                    var note = progress < 0.33f ? 523.25f : (progress < 0.66f ? 659.25f : 783.99f);
                    value = Mathf.Sin(2f * Mathf.PI * note * time) * 0.27f + Mathf.Sin(2f * Mathf.PI * note * 2f * time) * 0.07f;
                    break;
                }
                default:
                {
                    var note = progress < 0.5f ? 246.94f : 174.61f;
                    value = Mathf.Sin(2f * Mathf.PI * note * time) * 0.28f + whiteNoise * 0.035f;
                    break;
                }
            }

            samples[index] = Mathf.Clamp(value * envelope, -0.85f, 0.85f);
        }

        var clip = AudioClip.Create("Aetheria " + cue, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void PlayAetheriaSfx(AetheriaSfx cue, float volume = 1f)
    {
        if (aetheriaAudioSource == null)
        {
            InitializeAetheriaAudio();
        }

        AudioClip clip;
        if (aetheriaAudioSource != null && aetheriaSfxClips.TryGetValue(cue, out clip) && clip != null)
        {
            aetheriaAudioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }
    }

    private void PlayUiClickSound() { PlayAetheriaSfx(AetheriaSfx.UiClick, 0.62f); }
    private void PlayRejectSound() { PlayAetheriaSfx(AetheriaSfx.Reject, 0.75f); }
    private void PlayEncounterSound()
    {
        PlayAetheriaSfx(AetheriaSfx.Encounter, 0.88f);
        nextTurnSfxTime = Mathf.Max(nextTurnSfxTime, Time.unscaledTime + EncounterTurnCueGuardSeconds);
    }
    private void PlayTurnSound(bool enemyTurn)
    {
        if (Time.unscaledTime < nextTurnSfxTime)
        {
            return;
        }
        PlayAetheriaSfx(enemyTurn ? AetheriaSfx.EnemyTurn : AetheriaSfx.PlayerTurn, 0.76f);
    }
    private void PlayCombatActionSound(bool magic) { PlayAetheriaSfx(magic ? AetheriaSfx.Magic : AetheriaSfx.Physical, 0.88f); }
    private void PlayCombatHitSound(bool critical) { PlayAetheriaSfx(critical ? AetheriaSfx.Critical : AetheriaSfx.Hit, critical ? 1f : 0.86f); }
    private void PlayCombatMissSound() { PlayAetheriaSfx(AetheriaSfx.Miss, 0.72f); }
    private void PlayCombatHealSound() { PlayAetheriaSfx(AetheriaSfx.Heal, 0.72f); }
    private void PlayVictorySound() { PlayAetheriaSfx(AetheriaSfx.Victory, 0.95f); }
    private void PlayDefeatSound() { PlayAetheriaSfx(AetheriaSfx.Defeat, 0.86f); }

    private void ReleaseAetheriaAudio()
    {
        if (aetheriaAudioSource != null)
        {
            aetheriaAudioSource.Stop();
        }

        foreach (var clip in aetheriaSfxClips.Values)
        {
            if (clip != null)
            {
                Destroy(clip);
            }
        }

        aetheriaSfxClips.Clear();
        aetheriaAudioSource = null;
        nextTurnSfxTime = 0f;
    }
}
