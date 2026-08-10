using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class AetheriaGame
{
    private enum AetheriaSfx
    {
        UiClick,
        Reject,
        PageOpen,
        ItemSelect,
        InventorySort,
        Equip,
        Unequip,
        Coin,
        Save,
        ForgeSuccess,
        ForgeFail,
        Craft,
        Rest,
        SkillUpgrade,
        MapOpen,
        DungeonInspect,
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
        Defeat,
        UiHover,
        TitleStart,
        CharacterSelect,
        CharacterConfirm,
        Loot,
        LevelUp,
        BossEncounter
    }

    private enum AetheriaMusic
    {
        None,
        Title,
        Menu,
        Town,
        Dungeon,
        Combat,
        BossCombat,
        Victory,
        Defeat,
        Finale
    }

    private AudioSource aetheriaAudioSource;
    private AudioSource aetheriaMusicSource;
    private readonly Dictionary<AetheriaSfx, AudioClip> aetheriaSfxClips = new Dictionary<AetheriaSfx, AudioClip>();
    private readonly HashSet<AudioClip> generatedAetheriaSfxClips = new HashSet<AudioClip>();
    private const float EncounterTurnCueGuardSeconds = 0.42f;
    private const float AetheriaMusicVolume = 0.16f;
    private float nextTurnSfxTime;
    private float nextUiHoverSfxTime;
    private AetheriaMusic currentAetheriaMusic = AetheriaMusic.None;
    private AetheriaMusic requestedAetheriaMusic = AetheriaMusic.None;
    private Coroutine aetheriaMusicTransition;

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
        aetheriaAudioSource.priority = 96;

        aetheriaMusicSource = gameObject.AddComponent<AudioSource>();
        aetheriaMusicSource.playOnAwake = false;
        aetheriaMusicSource.loop = true;
        aetheriaMusicSource.spatialBlend = 0f;
        aetheriaMusicSource.volume = 0f;
        aetheriaMusicSource.priority = 160;

        foreach (AetheriaSfx cue in Enum.GetValues(typeof(AetheriaSfx)))
        {
            var clip = Resources.Load<AudioClip>("Audio/SFX/" + cue);
            if (clip == null)
            {
                clip = CreateAetheriaSfxClip(cue);
                generatedAetheriaSfxClips.Add(clip);
            }
            aetheriaSfxClips[cue] = clip;
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
            case AetheriaSfx.PageOpen: duration = 0.24f; break;
            case AetheriaSfx.ItemSelect: duration = 0.13f; break;
            case AetheriaSfx.InventorySort: duration = 0.24f; break;
            case AetheriaSfx.Equip: duration = 0.34f; break;
            case AetheriaSfx.Unequip: duration = 0.27f; break;
            case AetheriaSfx.Coin: duration = 0.42f; break;
            case AetheriaSfx.Save: duration = 0.42f; break;
            case AetheriaSfx.ForgeSuccess: duration = 0.68f; break;
            case AetheriaSfx.ForgeFail: duration = 0.44f; break;
            case AetheriaSfx.Craft: duration = 0.72f; break;
            case AetheriaSfx.Rest: duration = 0.82f; break;
            case AetheriaSfx.SkillUpgrade: duration = 0.62f; break;
            case AetheriaSfx.MapOpen: duration = 0.56f; break;
            case AetheriaSfx.DungeonInspect: duration = 0.30f; break;
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
            case AetheriaSfx.UiHover: duration = 0.055f; break;
            case AetheriaSfx.TitleStart: duration = 0.56f; break;
            case AetheriaSfx.CharacterSelect: duration = 0.32f; break;
            case AetheriaSfx.CharacterConfirm: duration = 0.66f; break;
            case AetheriaSfx.Loot: duration = 0.54f; break;
            case AetheriaSfx.LevelUp: duration = 0.86f; break;
            case AetheriaSfx.BossEncounter: duration = 0.82f; break;
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
            var releasePower = cue == AetheriaSfx.Hit || cue == AetheriaSfx.Physical
                ? 2.5f
                : UsesLayeredAetheriaEnvelope(cue) ? 0.55f : 1.35f;
            var release = Mathf.Pow(1f - progress, releasePower);
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
                case AetheriaSfx.PageOpen:
                    value = AetheriaToneBurst(time, 0f, 0.20f, 360f, 260f, 0.24f)
                        + AetheriaToneBurst(time, 0.055f, 0.17f, 620f, 180f, 0.18f)
                        + whiteNoise * 0.025f * (1f - progress);
                    break;
                case AetheriaSfx.ItemSelect:
                    value = AetheriaToneBurst(time, 0f, 0.105f, 980f, 220f, 0.25f)
                        + AetheriaToneBurst(time, 0.018f, 0.105f, 1480f, 140f, 0.16f);
                    break;
                case AetheriaSfx.InventorySort:
                    value = AetheriaToneBurst(time, 0f, 0.105f, 560f, 80f, 0.20f)
                        + AetheriaToneBurst(time, 0.055f, 0.105f, 720f, 90f, 0.20f)
                        + AetheriaToneBurst(time, 0.11f, 0.115f, 920f, 120f, 0.23f);
                    break;
                case AetheriaSfx.Equip:
                    value = AetheriaNoiseBurst(time, 0f, 0.09f, whiteNoise, 0.24f)
                        + AetheriaToneBurst(time, 0f, 0.16f, 118f, -25f, 0.34f)
                        + AetheriaToneBurst(time, 0.035f, 0.29f, 740f, -80f, 0.23f)
                        + AetheriaToneBurst(time, 0.055f, 0.24f, 1110f, -120f, 0.11f);
                    break;
                case AetheriaSfx.Unequip:
                    value = AetheriaNoiseBurst(time, 0f, 0.07f, whiteNoise, 0.14f)
                        + AetheriaToneBurst(time, 0f, 0.23f, 790f, -330f, 0.22f)
                        + AetheriaToneBurst(time, 0.035f, 0.18f, 1180f, -440f, 0.10f);
                    break;
                case AetheriaSfx.Coin:
                    value = AetheriaToneBurst(time, 0f, 0.18f, 1480f, 80f, 0.21f)
                        + AetheriaToneBurst(time, 0.075f, 0.19f, 1770f, -100f, 0.24f)
                        + AetheriaToneBurst(time, 0.16f, 0.22f, 1320f, 120f, 0.22f);
                    break;
                case AetheriaSfx.Save:
                    value = AetheriaToneBurst(time, 0f, 0.25f, 659.25f, 30f, 0.23f)
                        + AetheriaToneBurst(time, 0.12f, 0.27f, 880f, 35f, 0.25f)
                        + AetheriaToneBurst(time, 0.14f, 0.23f, 1320f, 20f, 0.08f);
                    break;
                case AetheriaSfx.ForgeSuccess:
                    value = AetheriaNoiseBurst(time, 0f, 0.12f, whiteNoise, 0.34f)
                        + AetheriaToneBurst(time, 0f, 0.22f, 92f, -22f, 0.42f)
                        + AetheriaToneBurst(time, 0.13f, 0.35f, 523.25f, 70f, 0.20f)
                        + AetheriaToneBurst(time, 0.25f, 0.34f, 659.25f, 90f, 0.22f)
                        + AetheriaToneBurst(time, 0.37f, 0.28f, 987.77f, 80f, 0.24f);
                    break;
                case AetheriaSfx.ForgeFail:
                    value = AetheriaNoiseBurst(time, 0f, 0.13f, whiteNoise, 0.30f)
                        + AetheriaToneBurst(time, 0f, 0.20f, 96f, -28f, 0.38f)
                        + AetheriaToneBurst(time, 0.10f, 0.31f, 246.94f, -92f, 0.25f)
                        + AetheriaToneBurst(time, 0.15f, 0.25f, 185f, -50f, 0.16f);
                    break;
                case AetheriaSfx.Craft:
                    value = Mathf.Sin(2f * Mathf.PI * (280f + progress * 720f) * time) * 0.10f * Mathf.Sin(Mathf.PI * progress)
                        + AetheriaToneBurst(time, 0.06f, 0.28f, 440f, 260f, 0.16f)
                        + AetheriaToneBurst(time, 0.23f, 0.30f, 659.25f, 250f, 0.19f)
                        + AetheriaToneBurst(time, 0.43f, 0.26f, 1046.5f, 120f, 0.25f)
                        + whiteNoise * 0.025f * Mathf.Sin(Mathf.PI * progress);
                    break;
                case AetheriaSfx.Rest:
                    value = AetheriaToneBurst(time, 0f, 0.42f, 392f, 20f, 0.18f)
                        + AetheriaToneBurst(time, 0.16f, 0.43f, 523.25f, 24f, 0.19f)
                        + AetheriaToneBurst(time, 0.34f, 0.43f, 659.25f, 26f, 0.20f)
                        + Mathf.Sin(2f * Mathf.PI * (240f + progress * 180f) * time) * 0.055f * Mathf.Sin(Mathf.PI * progress);
                    break;
                case AetheriaSfx.SkillUpgrade:
                    value = AetheriaToneBurst(time, 0f, 0.25f, 523.25f, 60f, 0.19f)
                        + AetheriaToneBurst(time, 0.10f, 0.25f, 659.25f, 70f, 0.20f)
                        + AetheriaToneBurst(time, 0.21f, 0.25f, 783.99f, 90f, 0.21f)
                        + AetheriaToneBurst(time, 0.34f, 0.25f, 1046.5f, 90f, 0.24f);
                    break;
                case AetheriaSfx.MapOpen:
                    value = whiteNoise * 0.13f * Mathf.Sin(Mathf.PI * progress)
                        + Mathf.Sin(2f * Mathf.PI * (180f + progress * 440f) * time) * 0.13f * Mathf.Sin(Mathf.PI * progress)
                        + AetheriaToneBurst(time, 0.28f, 0.25f, 740f, 160f, 0.18f);
                    break;
                case AetheriaSfx.DungeonInspect:
                    value = AetheriaToneBurst(time, 0f, 0.18f, 132f, -18f, 0.31f)
                        + AetheriaToneBurst(time, 0.035f, 0.24f, 620f, 80f, 0.17f)
                        + AetheriaNoiseBurst(time, 0f, 0.065f, whiteNoise, 0.10f);
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
                case AetheriaSfx.UiHover:
                    value = AetheriaToneBurst(time, 0f, 0.05f, 1120f, 180f, 0.16f);
                    break;
                case AetheriaSfx.TitleStart:
                    value = AetheriaToneBurst(time, 0f, 0.30f, 170f, 330f, 0.28f)
                        + AetheriaToneBurst(time, 0.10f, 0.34f, 523.25f, 180f, 0.20f)
                        + AetheriaToneBurst(time, 0.24f, 0.28f, 783.99f, 180f, 0.22f)
                        + whiteNoise * 0.035f * Mathf.Sin(Mathf.PI * progress);
                    break;
                case AetheriaSfx.CharacterSelect:
                    value = AetheriaToneBurst(time, 0f, 0.23f, 440f, 130f, 0.21f)
                        + AetheriaToneBurst(time, 0.075f, 0.21f, 659.25f, 100f, 0.20f);
                    break;
                case AetheriaSfx.CharacterConfirm:
                    value = AetheriaToneBurst(time, 0f, 0.30f, 392f, 90f, 0.18f)
                        + AetheriaToneBurst(time, 0.13f, 0.30f, 523.25f, 100f, 0.20f)
                        + AetheriaToneBurst(time, 0.27f, 0.32f, 783.99f, 130f, 0.24f)
                        + AetheriaToneBurst(time, 0.39f, 0.23f, 1174.66f, 80f, 0.12f);
                    break;
                case AetheriaSfx.Loot:
                    value = AetheriaNoiseBurst(time, 0f, 0.10f, whiteNoise, 0.20f)
                        + AetheriaToneBurst(time, 0f, 0.17f, 145f, -18f, 0.28f)
                        + AetheriaToneBurst(time, 0.10f, 0.27f, 1174.66f, 70f, 0.19f)
                        + AetheriaToneBurst(time, 0.20f, 0.27f, 1480f, 80f, 0.21f)
                        + AetheriaToneBurst(time, 0.31f, 0.20f, 1760f, 60f, 0.19f);
                    break;
                case AetheriaSfx.LevelUp:
                    value = AetheriaToneBurst(time, 0f, 0.31f, 392f, 100f, 0.18f)
                        + AetheriaToneBurst(time, 0.14f, 0.31f, 523.25f, 120f, 0.19f)
                        + AetheriaToneBurst(time, 0.29f, 0.31f, 659.25f, 130f, 0.21f)
                        + AetheriaToneBurst(time, 0.44f, 0.35f, 1046.5f, 160f, 0.26f)
                        + Mathf.Sin(2f * Mathf.PI * (220f + progress * 420f) * time) * 0.055f * Mathf.Sin(Mathf.PI * progress);
                    break;
                case AetheriaSfx.BossEncounter:
                    value = AetheriaNoiseBurst(time, 0f, 0.22f, whiteNoise, 0.34f)
                        + AetheriaToneBurst(time, 0f, 0.42f, 72f, -16f, 0.45f)
                        + AetheriaToneBurst(time, 0.18f, 0.43f, 110f, -28f, 0.31f)
                        + AetheriaToneBurst(time, 0.42f, 0.31f, 370f, -120f, 0.22f);
                    break;
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

    private static bool UsesLayeredAetheriaEnvelope(AetheriaSfx cue)
    {
        return ((int)cue >= (int)AetheriaSfx.PageOpen && (int)cue <= (int)AetheriaSfx.DungeonInspect)
            || cue == AetheriaSfx.TitleStart
            || cue == AetheriaSfx.CharacterSelect
            || cue == AetheriaSfx.CharacterConfirm
            || cue == AetheriaSfx.Loot
            || cue == AetheriaSfx.LevelUp
            || cue == AetheriaSfx.BossEncounter;
    }

    private static float AetheriaToneBurst(float time, float start, float length, float frequency, float sweep, float volume)
    {
        var localTime = time - start;
        if (localTime < 0f || localTime >= length)
        {
            return 0f;
        }

        var progress = localTime / length;
        var envelope = Mathf.Clamp01(localTime / 0.008f) * Mathf.Pow(1f - progress, 1.8f);
        var phaseFrequency = frequency + sweep * progress * 0.5f;
        return Mathf.Sin(2f * Mathf.PI * phaseFrequency * localTime) * envelope * volume;
    }

    private static float AetheriaNoiseBurst(float time, float start, float length, float noise, float volume)
    {
        var localTime = time - start;
        if (localTime < 0f || localTime >= length)
        {
            return 0f;
        }

        var progress = localTime / length;
        return noise * Mathf.Clamp01(localTime / 0.004f) * Mathf.Pow(1f - progress, 2.6f) * volume;
    }

    private void PlayAetheriaSfx(AetheriaSfx cue, float volume = 1f, bool replaceCurrent = false)
    {
        if (aetheriaAudioSource == null)
        {
            InitializeAetheriaAudio();
        }

        AudioClip clip;
        if (aetheriaAudioSource != null && aetheriaSfxClips.TryGetValue(cue, out clip) && clip != null)
        {
            if (replaceCurrent)
            {
                aetheriaAudioSource.Stop();
            }
            aetheriaAudioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }
    }

    private void PlayUiClickSound() { PlayAetheriaSfx(AetheriaSfx.UiClick, 0.62f); }
    private void PlayUiHoverSound()
    {
        if (Time.unscaledTime < nextUiHoverSfxTime)
        {
            return;
        }
        nextUiHoverSfxTime = Time.unscaledTime + 0.055f;
        PlayAetheriaSfx(AetheriaSfx.UiHover, 0.28f);
    }
    private void PlayRejectSound() { PlayAetheriaSfx(AetheriaSfx.Reject, 0.75f, true); }
    private void PlayPageOpenSound() { PlayAetheriaSfx(AetheriaSfx.PageOpen, 0.68f, true); }
    private void PlayItemSelectSound() { PlayAetheriaSfx(AetheriaSfx.ItemSelect, 0.62f, true); }
    private void PlayInventorySortSound() { PlayAetheriaSfx(AetheriaSfx.InventorySort, 0.64f, true); }
    private void PlayEquipSound() { PlayAetheriaSfx(AetheriaSfx.Equip, 0.84f, true); }
    private void PlayUnequipSound() { PlayAetheriaSfx(AetheriaSfx.Unequip, 0.72f, true); }
    private void PlayCoinSound() { PlayAetheriaSfx(AetheriaSfx.Coin, 0.76f, true); }
    private void PlaySaveSound() { PlayAetheriaSfx(AetheriaSfx.Save, 0.66f, true); }
    private void PlayForgeSound(bool success) { PlayAetheriaSfx(success ? AetheriaSfx.ForgeSuccess : AetheriaSfx.ForgeFail, success ? 0.92f : 0.84f, true); }
    private void PlayCraftSound() { PlayAetheriaSfx(AetheriaSfx.Craft, 0.82f, true); }
    private void PlayRestSound() { PlayAetheriaSfx(AetheriaSfx.Rest, 0.72f, true); }
    private void PlaySkillUpgradeSound() { PlayAetheriaSfx(AetheriaSfx.SkillUpgrade, 0.78f, true); }
    private void PlayMapOpenSound() { PlayAetheriaSfx(AetheriaSfx.MapOpen, 0.72f, true); }
    private void PlayDungeonInspectSound() { PlayAetheriaSfx(AetheriaSfx.DungeonInspect, 0.72f, true); }
    private void PlayTitleStartSound() { PlayAetheriaSfx(AetheriaSfx.TitleStart, 0.82f, true); }
    private void PlayCharacterSelectSound() { PlayAetheriaSfx(AetheriaSfx.CharacterSelect, 0.68f, true); }
    private void PlayCharacterConfirmSound() { PlayAetheriaSfx(AetheriaSfx.CharacterConfirm, 0.84f, true); }
    private void PlayLootSound() { PlayAetheriaSfx(AetheriaSfx.Loot, 0.80f); }
    private void PlayLevelUpSound() { PlayAetheriaSfx(AetheriaSfx.LevelUp, 0.86f); }
    private void PlayBossEncounterSound()
    {
        PlayAetheriaSfx(AetheriaSfx.BossEncounter, 0.96f, true);
        nextTurnSfxTime = Mathf.Max(nextTurnSfxTime, Time.unscaledTime + 0.72f);
    }
    private void PlayEncounterSound()
    {
        PlayAetheriaSfx(AetheriaSfx.Encounter, 0.88f, true);
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

    private void PlayVictoryRewardSounds(bool lootEarned, bool leveledUp)
    {
        PlayVictorySound();
        if (lootEarned || leveledUp)
        {
            StartCoroutine(PlayVictoryRewardSequence(lootEarned, leveledUp));
        }
    }

    private IEnumerator PlayVictoryRewardSequence(bool lootEarned, bool leveledUp)
    {
        if (lootEarned)
        {
            yield return new WaitForSecondsRealtime(0.30f);
            PlayLootSound();
        }
        if (leveledUp)
        {
            yield return new WaitForSecondsRealtime(lootEarned ? 0.30f : 0.38f);
            PlayLevelUpSound();
        }
    }

    private void SetAetheriaMusic(AetheriaMusic cue)
    {
        if (aetheriaMusicSource == null)
        {
            InitializeAetheriaAudio();
        }
        if (aetheriaMusicSource == null)
        {
            return;
        }
        if (requestedAetheriaMusic == cue
            && (aetheriaMusicTransition != null
                || currentAetheriaMusic == cue && (cue == AetheriaMusic.None || aetheriaMusicSource.isPlaying)))
        {
            return;
        }

        requestedAetheriaMusic = cue;
        if (aetheriaMusicTransition != null)
        {
            StopCoroutine(aetheriaMusicTransition);
        }
        aetheriaMusicTransition = StartCoroutine(TransitionAetheriaMusic(cue));
    }

    private IEnumerator TransitionAetheriaMusic(AetheriaMusic cue)
    {
        const float fadeOutDuration = 0.24f;
        const float fadeInDuration = 0.55f;
        var startVolume = aetheriaMusicSource.volume;
        var elapsed = 0f;
        while (elapsed < fadeOutDuration && aetheriaMusicSource.isPlaying)
        {
            elapsed += Mathf.Max(0f, Time.unscaledDeltaTime);
            aetheriaMusicSource.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / fadeOutDuration));
            yield return null;
        }

        aetheriaMusicSource.Stop();
        aetheriaMusicSource.clip = null;
        currentAetheriaMusic = cue;
        if (cue == AetheriaMusic.None)
        {
            aetheriaMusicTransition = null;
            yield break;
        }

        var clip = Resources.Load<AudioClip>("Audio/Music/" + cue);
        if (clip == null)
        {
            currentAetheriaMusic = AetheriaMusic.None;
            requestedAetheriaMusic = AetheriaMusic.None;
            aetheriaMusicTransition = null;
            yield break;
        }

        aetheriaMusicSource.clip = clip;
        aetheriaMusicSource.volume = 0f;
        aetheriaMusicSource.Play();
        elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Mathf.Max(0f, Time.unscaledDeltaTime);
            aetheriaMusicSource.volume = Mathf.Lerp(0f, AetheriaMusicVolume, Mathf.Clamp01(elapsed / fadeInDuration));
            yield return null;
        }
        aetheriaMusicSource.volume = AetheriaMusicVolume;
        aetheriaMusicTransition = null;
    }

    private void ReleaseAetheriaAudio()
    {
        if (aetheriaAudioSource != null)
        {
            aetheriaAudioSource.Stop();
        }
        if (aetheriaMusicTransition != null)
        {
            StopCoroutine(aetheriaMusicTransition);
            aetheriaMusicTransition = null;
        }
        if (aetheriaMusicSource != null)
        {
            aetheriaMusicSource.Stop();
            aetheriaMusicSource.clip = null;
        }

        foreach (var clip in aetheriaSfxClips.Values)
        {
            if (clip != null && generatedAetheriaSfxClips.Contains(clip))
            {
                Destroy(clip);
            }
        }

        aetheriaSfxClips.Clear();
        generatedAetheriaSfxClips.Clear();
        aetheriaAudioSource = null;
        aetheriaMusicSource = null;
        nextTurnSfxTime = 0f;
        nextUiHoverSfxTime = 0f;
        currentAetheriaMusic = AetheriaMusic.None;
        requestedAetheriaMusic = AetheriaMusic.None;
    }

}
