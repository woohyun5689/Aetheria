using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class AetheriaGame
{
    private enum CombatFxStyle
    {
        Paladin,
        Elementalist,
        Rogue,
        Priest,
        Bomber,
        Spirit,
        Archer,
        Monk,
        Enemy
    }

    private enum CombatFxElement
    {
        Slash,
        Fire,
        Ice,
        Lightning,
        Poison,
        Shadow,
        Holy,
        Spirit,
        Wind,
        Explosion,
        Fist,
        EnemyPhysical,
        EnemyMagic
    }

    private enum CombatFxMotion
    {
        Projectile,
        Melee,
        Vertical,
        Area,
        Beam
    }

    private struct CombatFxLook
    {
        public CombatFxStyle style;
        public CombatFxElement element;
        public Color primary;
        public Color secondary;
        public Color accent;

        public CombatFxLook(CombatFxStyle style, CombatFxElement element, Color primary, Color secondary, Color accent)
        {
            this.style = style;
            this.element = element;
            this.primary = primary;
            this.secondary = secondary;
            this.accent = accent;
        }
    }

    private CombatFxLook ResolveCombatFxLook(CombatPresentationEvent presentation)
    {
        if (presentation == null || !presentation.attackerIsPlayer || player == null)
        {
            var enemyAccent = currentEnemy != null ? EnemyThemeColor() : dangerColor;
            var enemyAction = presentation != null ? presentation.actionName ?? "" : "";
            if (presentation != null && presentation.magic)
            {
                if (HasCombatFxKeyword(enemyAction, "화염", "불", "폭염", "용암", "fire", "flame"))
                {
                    return new CombatFxLook(CombatFxStyle.Enemy, CombatFxElement.Fire, Rgb(241, 76, 43), Rgb(255, 173, 48), Color.white);
                }
                if (HasCombatFxKeyword(enemyAction, "빙결", "얼음", "서리", "한기", "ice", "frost"))
                {
                    return new CombatFxLook(CombatFxStyle.Enemy, CombatFxElement.Ice, Rgb(65, 193, 239), Rgb(194, 245, 255), Rgb(43, 109, 207));
                }
                if (HasCombatFxKeyword(enemyAction, "번개", "전류", "감전", "폭풍", "lightning", "shock"))
                {
                    return new CombatFxLook(CombatFxStyle.Enemy, CombatFxElement.Lightning, Rgb(255, 217, 58), Rgb(70, 181, 244), Color.white);
                }
                if (HasCombatFxKeyword(enemyAction, "독", "맹독", "역병", "poison", "venom"))
                {
                    return new CombatFxLook(CombatFxStyle.Enemy, CombatFxElement.Poison, Rgb(126, 202, 61), Rgb(81, 45, 132), Rgb(220, 244, 110));
                }
                if (HasCombatFxKeyword(enemyAction, "공허", "그림자", "어둠", "shadow", "void"))
                {
                    return new CombatFxLook(CombatFxStyle.Enemy, CombatFxElement.Shadow, Rgb(110, 69, 176), Rgb(38, 28, 76), Rgb(92, 211, 190));
                }
                if (HasCombatFxKeyword(enemyAction, "빛", "성광", "태양", "holy", "light"))
                {
                    return new CombatFxLook(CombatFxStyle.Enemy, CombatFxElement.Holy, Rgb(255, 214, 86), Rgb(255, 249, 207), Rgb(93, 181, 241));
                }
                return new CombatFxLook(CombatFxStyle.Enemy, CombatFxElement.EnemyMagic, enemyAccent, Rgb(126, 72, 187), Color.white);
            }
            return HasCombatFxKeyword(enemyAction, "검", "베기", "참격", "발톱", "slash", "claw")
                ? new CombatFxLook(CombatFxStyle.Enemy, CombatFxElement.Slash, dangerColor, Rgb(245, 133, 72), Color.white)
                : new CombatFxLook(CombatFxStyle.Enemy, CombatFxElement.EnemyPhysical, dangerColor, Rgb(245, 133, 72), Color.white);
        }

        var action = presentation.actionName ?? "";
        switch (GearClassId(player.heroClass))
        {
            case "mage":
                if (HasCombatFxKeyword(action, "\uD654\uC5FC", "\uBD88", "\uC5FC", "fire", "flame"))
                {
                    return new CombatFxLook(CombatFxStyle.Elementalist, CombatFxElement.Fire, Rgb(244, 78, 45), Rgb(255, 177, 45), Color.white);
                }
                if (HasCombatFxKeyword(action, "\uBE59\uACB0", "\uC5BC\uC74C", "\uC11C\uB9AC", "\uD55C\uAE30", "ice", "frost"))
                {
                    return new CombatFxLook(CombatFxStyle.Elementalist, CombatFxElement.Ice, Rgb(67, 201, 242), Rgb(187, 244, 255), Rgb(47, 112, 212));
                }
                return new CombatFxLook(CombatFxStyle.Elementalist, CombatFxElement.Lightning, Rgb(255, 220, 66), Rgb(74, 190, 246), Color.white);
            case "rogue":
                if (HasCombatFxKeyword(action, "\uB3C5", "\uB9F9\uB3C5", "poison", "venom"))
                {
                    return new CombatFxLook(CombatFxStyle.Rogue, CombatFxElement.Poison, Rgb(120, 204, 64), Rgb(79, 45, 133), Rgb(213, 244, 109));
                }
                return new CombatFxLook(CombatFxStyle.Rogue, CombatFxElement.Shadow, Rgb(91, 48, 148), Rgb(35, 29, 69), Rgb(94, 218, 185));
            case "priest":
                return new CombatFxLook(CombatFxStyle.Priest, CombatFxElement.Holy, Rgb(255, 218, 93), Rgb(255, 250, 210), Rgb(94, 184, 242));
            case "bomber":
                return new CombatFxLook(CombatFxStyle.Bomber, CombatFxElement.Explosion, Rgb(242, 79, 43), Rgb(255, 166, 45), Rgb(255, 235, 116));
            case "spirit":
                if (HasCombatFxKeyword(action, "화염", "불", "fire"))
                {
                    return new CombatFxLook(CombatFxStyle.Spirit, CombatFxElement.Fire, Rgb(244, 91, 48), Rgb(255, 177, 45), Color.white);
                }
                if (HasCombatFxKeyword(action, "물결", "물", "water"))
                {
                    return new CombatFxLook(CombatFxStyle.Spirit, CombatFxElement.Ice, Rgb(67, 181, 235), Rgb(190, 242, 255), Color.white);
                }
                if (HasCombatFxKeyword(action, "질풍", "바람", "wind"))
                {
                    return new CombatFxLook(CombatFxStyle.Spirit, CombatFxElement.Wind, Rgb(77, 204, 158), Rgb(130, 230, 241), Color.white);
                }
                if (HasCombatFxKeyword(action, "대지", "earth"))
                {
                    return new CombatFxLook(CombatFxStyle.Spirit, CombatFxElement.Fist, Rgb(212, 166, 60), Rgb(113, 183, 105), Color.white);
                }
                return new CombatFxLook(CombatFxStyle.Spirit, CombatFxElement.Spirit, Rgb(77, 194, 155), Rgb(88, 164, 229), Rgb(255, 197, 77));
            case "archer":
                return new CombatFxLook(CombatFxStyle.Archer, CombatFxElement.Wind, Rgb(86, 210, 166), Rgb(127, 225, 241), Color.white);
            case "monk":
                return new CombatFxLook(CombatFxStyle.Monk, CombatFxElement.Fist, Rgb(239, 145, 47), Rgb(255, 218, 92), Color.white);
            default:
                return new CombatFxLook(CombatFxStyle.Paladin, CombatFxElement.Slash, Rgb(245, 201, 81), Rgb(231, 242, 255), Rgb(75, 151, 222));
        }
    }

    private static bool HasCombatFxKeyword(string source, params string[] keywords)
    {
        for (var index = 0; index < keywords.Length; index++)
        {
            if (source.IndexOf(keywords[index], System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }
        return false;
    }

    private CombatFxMotion ResolveCombatFxMotion(CombatPresentationEvent presentation, CombatFxLook look)
    {
        var key = CombatFxSkillKey(presentation);
        switch (key)
        {
            case "knight_shield_bash":
            case "rogue_shadow_strike":
            case "rogue_poison_dagger":
            case "rogue_twilight_flurry":
            case "monk_iron_fist_combo":
            case "monk_pressure_lock":
                return CombatFxMotion.Melee;

            case "knight_holy_crush":
            case "mage_lightning_storm":
            case "priest_radiant_judgment":
                return CombatFxMotion.Vertical;

            case "bomber_chain_detonation":
            case "monk_inner_burst":
                return CombatFxMotion.Area;

            case "priest_salvation_ray":
            case "bomber_blazing_fire":
                return CombatFxMotion.Beam;

            case "bomber_shatter_bomb":
            case "spirit_earth_spirit":
                return CombatFxMotion.Projectile;
        }

        if (look.element == CombatFxElement.Lightning || look.element == CombatFxElement.Holy)
        {
            return CombatFxMotion.Vertical;
        }
        if (look.element == CombatFxElement.Fist
            || look.element == CombatFxElement.Slash
            || look.element == CombatFxElement.Shadow
            || look.element == CombatFxElement.Poison
            || look.element == CombatFxElement.EnemyPhysical)
        {
            return CombatFxMotion.Melee;
        }
        if (look.element == CombatFxElement.Explosion)
        {
            return CombatFxMotion.Area;
        }
        return CombatFxMotion.Projectile;
    }

    private IEnumerator PlayClassCombatAttackFx(CombatPresentationEvent presentation, int generation)
    {
        if (presentation == null || !CombatViewIsValid(generation) || combatFxLayer == null)
        {
            yield break;
        }

        var look = ResolveCombatFxLook(presentation);
        var motion = ResolveCombatFxMotion(presentation, look);
        var attackerAnchor = CombatFxAnchorFor(presentation.attackerIsPlayer, 0.035f);
        var targetAnchor = CombatFxAnchorFor(presentation.targetIsPlayer, 0.005f);
        if (presentation.missed)
        {
            targetAnchor.y += 0.17f;
        }
        var from = attackerAnchor;
        var to = targetAnchor;
        var beamLength = 560f;
        var beamAngle = 0f;
        switch (motion)
        {
            case CombatFxMotion.Melee:
                from = Vector2.Lerp(attackerAnchor, targetAnchor, 0.76f);
                break;
            case CombatFxMotion.Vertical:
            case CombatFxMotion.Area:
                from = targetAnchor;
                to = targetAnchor;
                break;
            case CombatFxMotion.Beam:
            {
                from = Vector2.Lerp(attackerAnchor, targetAnchor, 0.52f);
                to = from;
                var layerSize = combatFxLayer.rect.size;
                var beamDelta = new Vector2(
                    (targetAnchor.x - attackerAnchor.x) * layerSize.x,
                    (targetAnchor.y - attackerAnchor.y) * layerSize.y);
                beamLength = Mathf.Clamp(beamDelta.magnitude + 90f, 380f, 920f);
                beamAngle = Mathf.Atan2(beamDelta.y, beamDelta.x) * Mathf.Rad2Deg;
                break;
            }
        }

        var rootFx = CreateCombatFxRoot(
            "Class Attack FX - " + motion + " - " + look.element,
            from,
            motion == CombatFxMotion.Vertical || motion == CombatFxMotion.Area
                ? new Vector2(360f, 330f)
                : (motion == CombatFxMotion.Beam
                    ? new Vector2(beamLength, 230f)
                    : new Vector2(300f, 220f)));
        if (rootFx == null)
        {
            yield break;
        }
        if (motion == CombatFxMotion.Beam)
        {
            rootFx.localEulerAngles = new Vector3(0f, 0f, beamAngle);
        }
        var group = rootFx.gameObject.AddComponent<CanvasGroup>();
        var direction = presentation.attackerIsPlayer ? 1f : -1f;
        BuildCombatAttackGlyph(rootFx, look, direction, presentation);
        var skillAttackArt = !string.IsNullOrEmpty(CombatFxSkillPath(presentation, "action"));
        var classAttackArt = !string.IsNullOrEmpty(CombatFxClassPath(look, "attack"));
        var v2SpeedStreaks = CombatFxChild(rootFx, "V2 Action Speed Streaks");
        var v2Trail0 = CombatFxChild(rootFx, "V2 Action Trail 0");
        var v2Trail1 = CombatFxChild(rootFx, "V2 Action Trail 1");
        var v2SpeedImage = v2SpeedStreaks != null ? v2SpeedStreaks.GetComponent<Image>() : null;
        var v2Trail0Image = v2Trail0 != null ? v2Trail0.GetComponent<Image>() : null;
        var v2Trail1Image = v2Trail1 != null ? v2Trail1.GetComponent<Image>() : null;
        var v2SpeedAlpha = v2SpeedImage != null ? v2SpeedImage.color.a : 0f;
        var v2Trail0Alpha = v2Trail0Image != null ? v2Trail0Image.color.a : 0f;
        var v2Trail1Alpha = v2Trail1Image != null ? v2Trail1Image.color.a : 0f;
        var v2SpeedScaleSign = v2SpeedStreaks != null && v2SpeedStreaks.localScale.x < 0f ? -1f : 1f;
        var v2Trail0Start = v2Trail0 != null ? v2Trail0.anchoredPosition : Vector2.zero;
        var v2Trail1Start = v2Trail1 != null ? v2Trail1.anchoredPosition : Vector2.zero;

        var duration = motion == CombatFxMotion.Melee
            ? 0.18f
            : (motion == CombatFxMotion.Vertical
                ? 0.27f
                : (motion == CombatFxMotion.Area
                    ? 0.30f
                    : (motion == CombatFxMotion.Beam ? 0.25f : CombatFxTravelDuration(look.element))));
        var elapsed = 0f;
        while (elapsed < duration && CombatViewIsValid(generation) && rootFx != null)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            var eased = 1f - Mathf.Pow(1f - progress, 3f);
            var anchor = motion == CombatFxMotion.Area || motion == CombatFxMotion.Vertical
                ? targetAnchor
                : Vector2.LerpUnclamped(from, to, eased);
            rootFx.anchorMin = anchor;
            rootFx.anchorMax = anchor;
            if (motion == CombatFxMotion.Vertical)
            {
                rootFx.anchoredPosition = new Vector2(0f, Mathf.Lerp(128f, -8f, eased));
            }
            else if (motion == CombatFxMotion.Area)
            {
                rootFx.anchoredPosition = new Vector2(0f, Mathf.Sin(progress * Mathf.PI) * 12f);
            }
            else if (motion == CombatFxMotion.Beam)
            {
                rootFx.anchoredPosition = Vector2.zero;
            }
            else
            {
                rootFx.anchoredPosition = new Vector2(0f, Mathf.Sin(progress * Mathf.PI) * CombatFxArcHeight(look.element));
            }
            var pulse = motion == CombatFxMotion.Area
                ? Mathf.Lerp(0.46f, 1.18f, eased)
                : (motion == CombatFxMotion.Melee
                    ? Mathf.Lerp(0.72f, 1.18f, Mathf.Sin(progress * Mathf.PI * 0.5f))
                    : Mathf.Lerp(0.82f, 1.10f, Mathf.Sin(progress * Mathf.PI * 0.5f)));
            rootFx.localScale = Vector3.one * pulse;
            if (motion == CombatFxMotion.Vertical)
            {
                rootFx.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-4f, 3f, progress));
            }
            else if (motion == CombatFxMotion.Area)
            {
                rootFx.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-18f, 14f, progress));
            }
            if (v2SpeedStreaks != null)
            {
                v2SpeedStreaks.localScale = new Vector3(
                    v2SpeedScaleSign * Mathf.Lerp(0.66f, 1.32f, eased),
                    Mathf.Lerp(0.86f, 1.08f, progress),
                    1f);
                if (v2SpeedImage != null)
                {
                    var streakColor = v2SpeedImage.color;
                    streakColor.a = v2SpeedAlpha * Mathf.Sin(progress * Mathf.PI);
                    v2SpeedImage.color = streakColor;
                }
            }
            if (v2Trail0 != null)
            {
                var trail0Offset = motion == CombatFxMotion.Vertical
                    ? new Vector2(0f, 28f * progress)
                    : (motion == CombatFxMotion.Area
                        ? new Vector2(0f, 5f * Mathf.Sin(progress * Mathf.PI))
                        : new Vector2(-24f * direction * progress, 6f * Mathf.Sin(progress * Mathf.PI)));
                v2Trail0.anchoredPosition = v2Trail0Start + trail0Offset;
                if (v2Trail0Image != null)
                {
                    var trailColor = v2Trail0Image.color;
                    trailColor.a = v2Trail0Alpha * Mathf.Sin(progress * Mathf.PI);
                    v2Trail0Image.color = trailColor;
                }
            }
            if (v2Trail1 != null)
            {
                var trail1Offset = motion == CombatFxMotion.Vertical
                    ? new Vector2(0f, 52f * progress)
                    : (motion == CombatFxMotion.Area
                        ? new Vector2(0f, -5f * Mathf.Sin(progress * Mathf.PI))
                        : new Vector2(-42f * direction * progress, -5f * Mathf.Sin(progress * Mathf.PI)));
                v2Trail1.anchoredPosition = v2Trail1Start + trail1Offset;
                if (v2Trail1Image != null)
                {
                    var trailColor = v2Trail1Image.color;
                    trailColor.a = v2Trail1Alpha * Mathf.Sin(progress * Mathf.PI);
                    v2Trail1Image.color = trailColor;
                }
            }
            if (!skillAttackArt
                && !classAttackArt
                && (look.element == CombatFxElement.Spirit || look.element == CombatFxElement.Explosion))
            {
                rootFx.localEulerAngles = new Vector3(0f, 0f, progress * (presentation.attackerIsPlayer ? -120f : 120f));
            }
            group.alpha = progress < 0.88f ? 1f : Mathf.Clamp01((1f - progress) / 0.12f);
            yield return null;
        }

        if (rootFx != null)
        {
            Destroy(rootFx.gameObject);
        }
    }

    private IEnumerator PlayClassCombatImpactFx(CombatPresentationEvent presentation, int generation)
    {
        if (presentation == null || !CombatViewIsValid(generation) || combatFxLayer == null)
        {
            yield break;
        }

        var look = ResolveCombatFxLook(presentation);
        var anchor = CombatFxAnchorFor(presentation.targetIsPlayer, 0.005f);
        var rootFx = CreateCombatFxRoot("Class Impact FX - " + look.element, anchor, new Vector2(360f, 340f));
        if (rootFx == null)
        {
            yield break;
        }
        var group = rootFx.gameObject.AddComponent<CanvasGroup>();
        BuildCombatImpactGlyph(rootFx, look, presentation.critical, presentation);
        var v2Shockwave = CombatFxChild(rootFx, "V2 Impact Shockwave");
        var v2Sparks = CombatFxChild(rootFx, "V2 Impact Sparks");
        var v2ContactFlash = CombatFxChild(rootFx, "V2 Impact Contact Flash");
        var v2ShockwaveImage = v2Shockwave != null ? v2Shockwave.GetComponent<Image>() : null;
        var v2SparksImage = v2Sparks != null ? v2Sparks.GetComponent<Image>() : null;
        var v2ContactImage = v2ContactFlash != null ? v2ContactFlash.GetComponent<Image>() : null;
        var v2ShockwaveAlpha = v2ShockwaveImage != null ? v2ShockwaveImage.color.a : 0f;
        var v2SparksAlpha = v2SparksImage != null ? v2SparksImage.color.a : 0f;
        var v2ContactAlpha = v2ContactImage != null ? v2ContactImage.color.a : 0f;
        var layeredV2Impact = v2Shockwave != null || v2Sparks != null || v2ContactFlash != null;

        var duration = look.element == CombatFxElement.Explosion ? 0.38f : (presentation.critical ? 0.36f : 0.31f);
        var elapsed = 0f;
        while (elapsed < duration && CombatViewIsValid(generation) && rootFx != null)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            var snap = 1f - Mathf.Pow(1f - Mathf.Clamp01(progress * 3.4f), 3f);
            var peakScale = layeredV2Impact
                ? (presentation.critical ? 1.12f : 1.03f)
                : (presentation.critical ? 1.34f : 1.12f);
            var scale = Mathf.Lerp(layeredV2Impact ? 0.30f : 0.22f, peakScale, snap);
            if (progress > 0.34f)
            {
                var settleScale = layeredV2Impact
                    ? (presentation.critical ? 1.05f : 1.03f)
                    : (presentation.critical ? 1.16f : 1.08f);
                scale *= Mathf.Lerp(1f, settleScale, (progress - 0.34f) / 0.66f);
            }
            rootFx.localScale = Vector3.one * scale;
            rootFx.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-10f, 20f, progress));
            if (v2Shockwave != null)
            {
                v2Shockwave.localScale = Vector3.one * Mathf.Lerp(0.30f, presentation.critical ? 1.28f : 1.20f, progress);
                v2Shockwave.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-28f, 34f, progress));
                if (v2ShockwaveImage != null)
                {
                    var shockColor = v2ShockwaveImage.color;
                    shockColor.a = v2ShockwaveAlpha * Mathf.Clamp01(1f - Mathf.Max(0f, progress - 0.18f) / 0.82f);
                    v2ShockwaveImage.color = shockColor;
                }
            }
            if (v2Sparks != null)
            {
                v2Sparks.localScale = Vector3.one * Mathf.Lerp(0.48f, presentation.critical ? 1.12f : 1.06f, snap);
                v2Sparks.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-16f, 38f, progress));
                if (v2SparksImage != null)
                {
                    var sparkColor = v2SparksImage.color;
                    sparkColor.a = v2SparksAlpha * Mathf.Clamp01(1f - Mathf.Max(0f, progress - 0.46f) / 0.54f);
                    v2SparksImage.color = sparkColor;
                }
            }
            if (v2ContactFlash != null)
            {
                var contactSnap = 1f - Mathf.Pow(1f - Mathf.Clamp01(progress * 5.2f), 3f);
                v2ContactFlash.localScale = Vector3.one * Mathf.Lerp(0.18f, 1.34f, contactSnap);
                if (v2ContactImage != null)
                {
                    var contactColor = v2ContactImage.color;
                    contactColor.a = v2ContactAlpha * Mathf.Clamp01(1f - progress * 4.1f);
                    v2ContactImage.color = contactColor;
                }
            }
            group.alpha = Mathf.Clamp01(1f - Mathf.Max(0f, progress - 0.52f) / 0.48f);
            yield return null;
        }

        if (rootFx != null)
        {
            Destroy(rootFx.gameObject);
        }
    }

    private IEnumerator PlayClassCombatSupportFx(CombatPresentationEvent presentation, bool healing, int generation)
    {
        if (presentation == null || !CombatViewIsValid(generation) || combatFxLayer == null)
        {
            yield break;
        }

        var look = ResolveCombatFxLook(presentation);
        var anchor = CombatFxAnchorFor(presentation.attackerIsPlayer, -0.025f);
        var rootFx = CreateCombatFxRoot(healing ? "Class Healing FX" : "Class Guard FX", anchor, new Vector2(300f, 330f));
        if (rootFx == null)
        {
            yield break;
        }
        var group = rootFx.gameObject.AddComponent<CanvasGroup>();
        BuildCombatSupportGlyph(rootFx, look, healing, presentation);
        var v2SupportCast = CombatFxChild(rootFx, "V2 Support Cast");
        var v2GroundRing = CombatFxChild(rootFx, "V2 Support Ground Ring");
        var v2Motes = CombatFxChild(rootFx, "V2 Support Motes");
        var v2SupportCastImage = v2SupportCast != null ? v2SupportCast.GetComponent<Image>() : null;
        var v2GroundImage = v2GroundRing != null ? v2GroundRing.GetComponent<Image>() : null;
        var v2MotesImage = v2Motes != null ? v2Motes.GetComponent<Image>() : null;
        var v2SupportCastAlpha = v2SupportCastImage != null ? v2SupportCastImage.color.a : 0f;
        var v2GroundAlpha = v2GroundImage != null ? v2GroundImage.color.a : 0f;
        var v2MotesAlpha = v2MotesImage != null ? v2MotesImage.color.a : 0f;
        var v2GroundStart = v2GroundRing != null ? v2GroundRing.anchoredPosition : Vector2.zero;

        const float duration = 0.62f;
        var elapsed = 0f;
        while (elapsed < duration && CombatViewIsValid(generation) && rootFx != null)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            var rise = 30f * progress;
            rootFx.anchoredPosition = new Vector2(0f, rise);
            var reveal = 1f - Mathf.Pow(1f - Mathf.Clamp01(progress * 2.8f), 3f);
            var supportScale = Mathf.Lerp(0.52f, 1.14f, reveal);
            var supportRotation = Mathf.Sin(progress * Mathf.PI) * (look.style == CombatFxStyle.Spirit ? 18f : 5f);
            rootFx.localScale = Vector3.one * supportScale;
            rootFx.localEulerAngles = new Vector3(0f, 0f, supportRotation);
            if (v2SupportCast != null)
            {
                v2SupportCast.localScale = Vector3.one * Mathf.Lerp(0.52f, 1.28f, reveal);
                v2SupportCast.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-30f, 24f, progress));
                if (v2SupportCastImage != null)
                {
                    var castColor = v2SupportCastImage.color;
                    castColor.a = v2SupportCastAlpha * Mathf.Sin(progress * Mathf.PI);
                    v2SupportCastImage.color = castColor;
                }
            }
            if (v2GroundRing != null)
            {
                var safeSupportScale = Mathf.Max(0.01f, supportScale);
                var fixedGroundOffset = v2GroundStart - new Vector2(0f, rise);
                var localGroundOffset = Quaternion.Euler(0f, 0f, -supportRotation)
                    * new Vector3(
                        fixedGroundOffset.x / safeSupportScale,
                        fixedGroundOffset.y / safeSupportScale,
                        0f);
                v2GroundRing.anchoredPosition = new Vector2(localGroundOffset.x, localGroundOffset.y);
                v2GroundRing.localScale = new Vector3(
                    Mathf.Lerp(0.42f, 1.34f, reveal) / safeSupportScale,
                    Mathf.Lerp(0.62f, 1.12f, reveal) / safeSupportScale,
                    1f);
                v2GroundRing.localEulerAngles = new Vector3(0f, 0f, -supportRotation);
                if (v2GroundImage != null)
                {
                    var ringColor = v2GroundImage.color;
                    ringColor.a = v2GroundAlpha * Mathf.Sin(progress * Mathf.PI);
                    v2GroundImage.color = ringColor;
                }
            }
            if (v2Motes != null)
            {
                v2Motes.anchoredPosition = new Vector2(0f, Mathf.Lerp(-18f, 46f, progress));
                v2Motes.localScale = Vector3.one * Mathf.Lerp(0.64f, 1.20f, reveal);
                v2Motes.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(16f, -22f, progress));
                if (v2MotesImage != null)
                {
                    var moteColor = v2MotesImage.color;
                    moteColor.a = v2MotesAlpha * Mathf.Sin(progress * Mathf.PI);
                    v2MotesImage.color = moteColor;
                }
            }
            var supportFadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.16f));
            var supportFadeOut = Mathf.Clamp01(1f - Mathf.Max(0f, progress - 0.68f) / 0.32f);
            group.alpha = supportFadeIn * supportFadeOut;
            yield return null;
        }

        if (rootFx != null)
        {
            Destroy(rootFx.gameObject);
        }
    }

    private RectTransform CreateCombatFxRoot(string name, Vector2 anchor, Vector2 size)
    {
        if (combatFxLayer == null)
        {
            return null;
        }
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(combatFxLayer, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
        rect.SetAsLastSibling();
        return rect;
    }

    private Image AddCombatFxPart(Transform parent, string name, Vector2 position, Vector2 size, float rotation, Color color, bool soft)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localEulerAngles = new Vector3(0f, 0f, rotation);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        if (soft)
        {
            image.sprite = CombatGlowSprite();
            image.preserveAspect = false;
        }
        return image;
    }

    private void BuildCombatAttackGlyph(
        RectTransform rootFx,
        CombatFxLook look,
        float direction,
        CombatPresentationEvent presentation)
    {
        var skillAttackArt = !string.IsNullOrEmpty(CombatFxSkillPath(presentation, "action"));
        var classAttackArt = !string.IsNullOrEmpty(CombatFxClassPath(look, "attack"));
        if (AddGeneratedCombatAttackSprite(rootFx, look, direction, presentation))
        {
            if (!skillAttackArt && !classAttackArt)
            {
                AddGeneratedCombatAttackAccents(rootFx, look, direction);
            }
            return;
        }
        switch (look.element)
        {
            case CombatFxElement.Slash:
                AddCombatFxPart(rootFx, "Golden Slash Core", Vector2.zero, new Vector2(150f, 12f), direction > 0f ? 24f : -24f, look.primary, false);
                AddCombatFxPart(rootFx, "Golden Slash Edge", new Vector2(-8f * direction, 17f), new Vector2(126f, 6f), direction > 0f ? 38f : -38f, new Color(look.secondary.r, look.secondary.g, look.secondary.b, 0.86f), false);
                AddCombatFxPart(rootFx, "Paladin Blue Wake", new Vector2(-38f * direction, -20f), new Vector2(92f, 7f), direction > 0f ? 13f : -13f, new Color(look.accent.r, look.accent.g, look.accent.b, 0.62f), false);
                break;
            case CombatFxElement.Fire:
                AddCombatFxPart(rootFx, "Fire Outer", Vector2.zero, new Vector2(100f, 100f), 0f, new Color(look.primary.r, look.primary.g, look.primary.b, 0.68f), true);
                AddCombatFxPart(rootFx, "Fire Core", new Vector2(8f * direction, 0f), new Vector2(54f, 54f), 0f, look.secondary, true);
                AddCombatFxPart(rootFx, "Fire Ember A", new Vector2(-58f * direction, 21f), new Vector2(23f, 23f), 0f, look.secondary, true);
                AddCombatFxPart(rootFx, "Fire Ember B", new Vector2(-78f * direction, -18f), new Vector2(14f, 14f), 0f, look.primary, true);
                break;
            case CombatFxElement.Ice:
                AddCombatFxPart(rootFx, "Ice Main Shard", Vector2.zero, new Vector2(34f, 116f), direction > 0f ? -66f : 66f, look.secondary, false);
                AddCombatFxPart(rootFx, "Ice Blue Spine", new Vector2(5f * direction, 0f), new Vector2(12f, 106f), direction > 0f ? -66f : 66f, look.primary, false);
                AddCombatFxPart(rootFx, "Ice Side Shard A", new Vector2(-44f * direction, 25f), new Vector2(17f, 66f), direction > 0f ? -48f : 48f, new Color(look.primary.r, look.primary.g, look.primary.b, 0.82f), false);
                AddCombatFxPart(rootFx, "Ice Side Shard B", new Vector2(-50f * direction, -29f), new Vector2(14f, 52f), direction > 0f ? -79f : 79f, new Color(look.secondary.r, look.secondary.g, look.secondary.b, 0.72f), false);
                break;
            case CombatFxElement.Lightning:
                AddCombatFxPart(rootFx, "Lightning Segment A", new Vector2(-45f * direction, 19f), new Vector2(70f, 13f), direction > 0f ? -24f : 24f, look.primary, false);
                AddCombatFxPart(rootFx, "Lightning Segment B", Vector2.zero, new Vector2(65f, 13f), direction > 0f ? 27f : -27f, look.secondary, false);
                AddCombatFxPart(rootFx, "Lightning Segment C", new Vector2(45f * direction, -19f), new Vector2(70f, 13f), direction > 0f ? -24f : 24f, look.primary, false);
                AddCombatFxPart(rootFx, "Lightning White Core", Vector2.zero, new Vector2(164f, 5f), 0f, new Color(1f, 1f, 1f, 0.82f), false);
                break;
            case CombatFxElement.Poison:
                AddCombatFxPart(rootFx, "Poison Blade A", new Vector2(5f, 10f), new Vector2(128f, 12f), direction > 0f ? 24f : -24f, look.secondary, false);
                AddCombatFxPart(rootFx, "Poison Blade B", new Vector2(-4f, -12f), new Vector2(112f, 9f), direction > 0f ? -18f : 18f, look.primary, false);
                AddCombatFxPart(rootFx, "Poison Drop A", new Vector2(-55f * direction, 39f), new Vector2(27f, 27f), 0f, look.accent, true);
                AddCombatFxPart(rootFx, "Poison Drop B", new Vector2(-73f * direction, -31f), new Vector2(18f, 18f), 0f, look.primary, true);
                break;
            case CombatFxElement.Shadow:
                AddCombatFxPart(rootFx, "Shadow Cut A", Vector2.zero, new Vector2(162f, 18f), direction > 0f ? 27f : -27f, new Color(look.primary.r, look.primary.g, look.primary.b, 0.90f), false);
                AddCombatFxPart(rootFx, "Shadow Cut B", new Vector2(-9f * direction, -8f), new Vector2(142f, 8f), direction > 0f ? -23f : 23f, look.accent, false);
                AddCombatFxPart(rootFx, "Shadow Veil", new Vector2(-38f * direction, 0f), new Vector2(120f, 92f), 0f, new Color(look.secondary.r, look.secondary.g, look.secondary.b, 0.64f), true);
                break;
            case CombatFxElement.Holy:
                AddCombatFxPart(rootFx, "Holy Aura", Vector2.zero, new Vector2(112f, 112f), 0f, new Color(look.primary.r, look.primary.g, look.primary.b, 0.56f), true);
                AddCombatFxPart(rootFx, "Holy Ray Horizontal", Vector2.zero, new Vector2(134f, 13f), 0f, look.secondary, false);
                AddCombatFxPart(rootFx, "Holy Ray Vertical", Vector2.zero, new Vector2(13f, 104f), 0f, look.primary, false);
                AddCombatFxPart(rootFx, "Holy Blue Heart", Vector2.zero, new Vector2(33f, 33f), 45f, look.accent, false);
                break;
            case CombatFxElement.Spirit:
                AddCombatFxPart(rootFx, "Spirit Core", Vector2.zero, new Vector2(72f, 72f), 0f, new Color(1f, 1f, 1f, 0.66f), true);
                AddCombatFxPart(rootFx, "Fire Spirit", new Vector2(-48f, 0f), new Vector2(37f, 37f), 0f, Rgb(245, 105, 55), true);
                AddCombatFxPart(rootFx, "Water Spirit", new Vector2(0f, 45f), new Vector2(37f, 37f), 0f, look.secondary, true);
                AddCombatFxPart(rootFx, "Wind Spirit", new Vector2(48f, 0f), new Vector2(37f, 37f), 0f, look.primary, true);
                AddCombatFxPart(rootFx, "Earth Spirit", new Vector2(0f, -45f), new Vector2(37f, 37f), 0f, look.accent, true);
                break;
            case CombatFxElement.Wind:
                AddCombatFxPart(rootFx, "Wind Arrow Shaft", Vector2.zero, new Vector2(164f, 9f), 0f, look.secondary, false);
                AddCombatFxPart(rootFx, "Wind Arrow Head A", new Vector2(75f * direction, 10f), new Vector2(42f, 8f), direction > 0f ? 30f : -30f, look.primary, false);
                AddCombatFxPart(rootFx, "Wind Arrow Head B", new Vector2(75f * direction, -10f), new Vector2(42f, 8f), direction > 0f ? -30f : 30f, look.primary, false);
                AddCombatFxPart(rootFx, "Wind Wake A", new Vector2(-34f * direction, 24f), new Vector2(118f, 5f), 0f, new Color(look.primary.r, look.primary.g, look.primary.b, 0.62f), false);
                AddCombatFxPart(rootFx, "Wind Wake B", new Vector2(-48f * direction, -24f), new Vector2(92f, 4f), 0f, new Color(1f, 1f, 1f, 0.58f), false);
                break;
            case CombatFxElement.Explosion:
                AddCombatFxPart(rootFx, "Bomb Shell", Vector2.zero, new Vector2(78f, 78f), 0f, new Color(0.13f, 0.12f, 0.17f, 1f), true);
                AddCombatFxPart(rootFx, "Bomb Ember Core", new Vector2(4f * direction, -3f), new Vector2(44f, 44f), 0f, look.primary, true);
                AddCombatFxPart(rootFx, "Fuse", new Vector2(-30f * direction, 38f), new Vector2(52f, 7f), direction > 0f ? -38f : 38f, look.secondary, false);
                AddCombatFxPart(rootFx, "Fuse Spark", new Vector2(-50f * direction, 56f), new Vector2(31f, 31f), 0f, look.accent, true);
                break;
            case CombatFxElement.Fist:
                AddCombatFxPart(rootFx, "Fist One", new Vector2(24f * direction, 11f), new Vector2(70f, 55f), direction > 0f ? -12f : 12f, look.primary, true);
                AddCombatFxPart(rootFx, "Fist Two", new Vector2(-16f * direction, -17f), new Vector2(62f, 48f), direction > 0f ? 12f : -12f, look.secondary, true);
                AddCombatFxPart(rootFx, "Fist Speed A", new Vector2(-65f * direction, 24f), new Vector2(78f, 8f), 0f, new Color(look.accent.r, look.accent.g, look.accent.b, 0.82f), false);
                AddCombatFxPart(rootFx, "Fist Speed B", new Vector2(-75f * direction, -19f), new Vector2(58f, 6f), 0f, new Color(look.primary.r, look.primary.g, look.primary.b, 0.72f), false);
                break;
            case CombatFxElement.EnemyMagic:
                AddCombatFxPart(rootFx, "Enemy Magic Outer", Vector2.zero, new Vector2(102f, 102f), 0f, new Color(look.primary.r, look.primary.g, look.primary.b, 0.64f), true);
                AddCombatFxPart(rootFx, "Enemy Magic Core", Vector2.zero, new Vector2(48f, 48f), 45f, look.secondary, false);
                AddCombatFxPart(rootFx, "Enemy Magic Tail", new Vector2(-55f * direction, 0f), new Vector2(104f, 13f), 0f, new Color(look.secondary.r, look.secondary.g, look.secondary.b, 0.54f), false);
                break;
            default:
                AddCombatFxPart(rootFx, "Enemy Strike A", Vector2.zero, new Vector2(144f, 14f), direction > 0f ? 22f : -22f, look.primary, false);
                AddCombatFxPart(rootFx, "Enemy Strike B", new Vector2(-6f * direction, -16f), new Vector2(106f, 8f), direction > 0f ? -16f : 16f, look.secondary, false);
                break;
        }
    }

    private void BuildCombatImpactGlyph(
        RectTransform rootFx,
        CombatFxLook look,
        bool critical,
        CombatPresentationEvent presentation)
    {
        if (AddGeneratedCombatImpactSprite(rootFx, look, critical, presentation))
        {
            return;
        }
        var radius = look.element == CombatFxElement.Explosion ? 86f : 64f;
        var rayCount = look.element == CombatFxElement.Explosion ? 12 : (look.element == CombatFxElement.Fist ? 6 : 8);
        AddCombatFxPart(rootFx, "Impact Outer Glow", Vector2.zero, new Vector2(radius * 2.2f, radius * 2.2f), 0f, new Color(look.primary.r, look.primary.g, look.primary.b, 0.48f), true);
        AddCombatFxPart(rootFx, "Impact Inner Glow", Vector2.zero, new Vector2(radius, radius), 0f, new Color(look.secondary.r, look.secondary.g, look.secondary.b, 0.92f), true);
        AddCombatFxPart(rootFx, "Impact White Core", Vector2.zero, new Vector2(critical ? 50f : 36f, critical ? 50f : 36f), 0f, new Color(1f, 1f, 1f, 0.94f), true);
        for (var index = 0; index < rayCount; index++)
        {
            var angle = index * (360f / rayCount);
            var radians = angle * Mathf.Deg2Rad;
            var distance = radius * 0.76f;
            var position = new Vector2(Mathf.Cos(radians) * distance, Mathf.Sin(radians) * distance);
            var length = look.element == CombatFxElement.Explosion ? 80f : (index % 2 == 0 ? 68f : 46f);
            AddCombatFxPart(rootFx, "Impact Ray " + index, position, new Vector2(length, critical ? 10f : 7f), angle, index % 2 == 0 ? look.primary : look.accent, false);
        }

        if (look.element == CombatFxElement.Slash || look.element == CombatFxElement.Shadow || look.element == CombatFxElement.Poison)
        {
            AddCombatFxPart(rootFx, "Impact Cross Cut A", Vector2.zero, new Vector2(196f, 12f), 34f, look.secondary, false);
            AddCombatFxPart(rootFx, "Impact Cross Cut B", Vector2.zero, new Vector2(170f, 8f), -29f, look.accent, false);
        }
        else if (look.element == CombatFxElement.Lightning)
        {
            AddCombatFxPart(rootFx, "Impact Lightning Bar", Vector2.zero, new Vector2(210f, 9f), 22f, look.primary, false);
            AddCombatFxPart(rootFx, "Impact Lightning Fork", new Vector2(20f, -16f), new Vector2(130f, 7f), -31f, look.secondary, false);
        }
        else if (look.element == CombatFxElement.Wind)
        {
            AddCombatFxPart(rootFx, "Impact Wind Cut A", new Vector2(0f, 22f), new Vector2(210f, 7f), -8f, look.primary, false);
            AddCombatFxPart(rootFx, "Impact Wind Cut B", new Vector2(0f, -22f), new Vector2(175f, 5f), 9f, look.secondary, false);
        }
    }

    private void BuildCombatSupportGlyph(
        RectTransform rootFx,
        CombatFxLook look,
        bool healing,
        CombatPresentationEvent presentation)
    {
        if (AddGeneratedCombatSupportSprite(rootFx, look, healing, presentation))
        {
            return;
        }
        var supportPrimary = healing ? Color.Lerp(look.primary, goodColor, 0.48f) : look.primary;
        var supportSecondary = healing ? Color.Lerp(look.secondary, Color.white, 0.42f) : look.secondary;
        AddCombatFxPart(rootFx, "Support Ground Aura", new Vector2(0f, -86f), new Vector2(214f, 72f), 0f, new Color(supportPrimary.r, supportPrimary.g, supportPrimary.b, 0.48f), true);

        switch (look.style)
        {
            case CombatFxStyle.Paladin:
                AddCombatFxPart(rootFx, "Paladin Shield Glow", Vector2.zero, new Vector2(176f, 204f), 0f, new Color(supportPrimary.r, supportPrimary.g, supportPrimary.b, 0.42f), true);
                AddCombatFxPart(rootFx, "Paladin Shield Left", new Vector2(-45f, 2f), new Vector2(105f, 12f), 58f, supportPrimary, false);
                AddCombatFxPart(rootFx, "Paladin Shield Right", new Vector2(45f, 2f), new Vector2(105f, 12f), -58f, supportPrimary, false);
                AddCombatFxPart(rootFx, "Paladin Shield Base", new Vector2(0f, -55f), new Vector2(112f, 12f), 0f, supportSecondary, false);
                if (healing)
                {
                    AddCombatFxPart(rootFx, "Paladin Healing Cross H", Vector2.zero, new Vector2(74f, 13f), 0f, Color.white, false);
                    AddCombatFxPart(rootFx, "Paladin Healing Cross V", Vector2.zero, new Vector2(13f, 74f), 0f, Color.white, false);
                }
                break;
            case CombatFxStyle.Elementalist:
                AddCombatFxPart(rootFx, "Element Fire", new Vector2(-64f, -10f), new Vector2(60f, 60f), 0f, Rgb(245, 93, 48), true);
                AddCombatFxPart(rootFx, "Element Ice", new Vector2(0f, 58f), new Vector2(60f, 60f), 45f, Rgb(83, 207, 247), true);
                AddCombatFxPart(rootFx, "Element Lightning", new Vector2(64f, -10f), new Vector2(60f, 60f), 0f, Rgb(255, 221, 68), true);
                AddCombatFxPart(rootFx, "Element Guard Core", Vector2.zero, new Vector2(92f, 92f), 0f, new Color(supportSecondary.r, supportSecondary.g, supportSecondary.b, 0.64f), true);
                break;
            case CombatFxStyle.Rogue:
                AddCombatFxPart(rootFx, "Shadow Veil", Vector2.zero, new Vector2(190f, 224f), 0f, new Color(look.secondary.r, look.secondary.g, look.secondary.b, 0.50f), true);
                AddCombatFxPart(rootFx, "Shadow Guard Cut A", Vector2.zero, new Vector2(164f, 9f), 48f, look.primary, false);
                AddCombatFxPart(rootFx, "Shadow Guard Cut B", Vector2.zero, new Vector2(164f, 9f), -48f, look.accent, false);
                AddCombatFxPart(rootFx, "Poison Mote A", new Vector2(-58f, 62f), new Vector2(31f, 31f), 0f, Rgb(132, 216, 67), true);
                AddCombatFxPart(rootFx, "Poison Mote B", new Vector2(62f, -35f), new Vector2(23f, 23f), 0f, Rgb(132, 216, 67), true);
                break;
            case CombatFxStyle.Priest:
                AddCombatFxPart(rootFx, "Holy Column", Vector2.zero, new Vector2(92f, 260f), 0f, new Color(supportPrimary.r, supportPrimary.g, supportPrimary.b, 0.42f), true);
                AddCombatFxPart(rootFx, "Holy Cross H", new Vector2(0f, 16f), new Vector2(142f, 17f), 0f, supportSecondary, false);
                AddCombatFxPart(rootFx, "Holy Cross V", new Vector2(0f, 16f), new Vector2(17f, 148f), 0f, supportPrimary, false);
                AddCombatFxPart(rootFx, "Holy Heart", new Vector2(0f, 16f), new Vector2(47f, 47f), 45f, look.accent, false);
                break;
            case CombatFxStyle.Bomber:
                AddCombatFxPart(rootFx, "Alchemy Bubble A", new Vector2(-58f, 42f), new Vector2(68f, 68f), 0f, new Color(look.primary.r, look.primary.g, look.primary.b, 0.70f), true);
                AddCombatFxPart(rootFx, "Alchemy Bubble B", new Vector2(47f, 63f), new Vector2(52f, 52f), 0f, new Color(look.secondary.r, look.secondary.g, look.secondary.b, 0.74f), true);
                AddCombatFxPart(rootFx, "Alchemy Bubble C", new Vector2(38f, -31f), new Vector2(38f, 38f), 0f, new Color(look.accent.r, look.accent.g, look.accent.b, 0.76f), true);
                AddCombatFxPart(rootFx, "Blast Guard", Vector2.zero, new Vector2(150f, 150f), 0f, new Color(supportPrimary.r, supportPrimary.g, supportPrimary.b, 0.40f), true);
                break;
            case CombatFxStyle.Spirit:
                AddCombatFxPart(rootFx, "Spirit Guard Core", Vector2.zero, new Vector2(112f, 112f), 0f, new Color(1f, 1f, 1f, 0.56f), true);
                AddCombatFxPart(rootFx, "Spirit Fire", new Vector2(-72f, 0f), new Vector2(52f, 52f), 0f, Rgb(239, 103, 55), true);
                AddCombatFxPart(rootFx, "Spirit Water", new Vector2(0f, 76f), new Vector2(52f, 52f), 0f, Rgb(70, 171, 231), true);
                AddCombatFxPart(rootFx, "Spirit Wind", new Vector2(72f, 0f), new Vector2(52f, 52f), 0f, Rgb(79, 201, 154), true);
                AddCombatFxPart(rootFx, "Spirit Earth", new Vector2(0f, -58f), new Vector2(52f, 52f), 0f, Rgb(224, 174, 62), true);
                break;
            case CombatFxStyle.Archer:
                AddCombatFxPart(rootFx, "Wind Guard A", new Vector2(0f, 58f), new Vector2(204f, 8f), -10f, supportPrimary, false);
                AddCombatFxPart(rootFx, "Wind Guard B", new Vector2(0f, 8f), new Vector2(244f, 8f), 8f, supportSecondary, false);
                AddCombatFxPart(rootFx, "Wind Guard C", new Vector2(0f, -43f), new Vector2(174f, 6f), -7f, new Color(1f, 1f, 1f, 0.76f), false);
                AddCombatFxPart(rootFx, "Wind Guard Core", Vector2.zero, new Vector2(142f, 184f), 0f, new Color(supportPrimary.r, supportPrimary.g, supportPrimary.b, 0.38f), true);
                break;
            case CombatFxStyle.Monk:
                AddCombatFxPart(rootFx, "Chi Outer", Vector2.zero, new Vector2(208f, 208f), 0f, new Color(supportPrimary.r, supportPrimary.g, supportPrimary.b, 0.42f), true);
                AddCombatFxPart(rootFx, "Chi Inner", Vector2.zero, new Vector2(106f, 106f), 0f, new Color(supportSecondary.r, supportSecondary.g, supportSecondary.b, 0.76f), true);
                AddCombatFxPart(rootFx, "Chi Palm Left", new Vector2(-45f, 8f), new Vector2(63f, 48f), -18f, supportPrimary, true);
                AddCombatFxPart(rootFx, "Chi Palm Right", new Vector2(45f, 8f), new Vector2(63f, 48f), 18f, supportSecondary, true);
                break;
            default:
                AddCombatFxPart(rootFx, "Enemy Guard", Vector2.zero, new Vector2(184f, 218f), 0f, new Color(supportPrimary.r, supportPrimary.g, supportPrimary.b, 0.44f), true);
                AddCombatFxPart(rootFx, "Enemy Guard Bar", Vector2.zero, new Vector2(164f, 12f), 0f, supportSecondary, false);
                break;
        }
    }

    private static float CombatFxTravelDuration(CombatFxElement element)
    {
        switch (element)
        {
            case CombatFxElement.Lightning: return 0.20f;
            case CombatFxElement.Wind: return 0.22f;
            case CombatFxElement.Slash:
            case CombatFxElement.Shadow:
            case CombatFxElement.Poison:
            case CombatFxElement.Fist:
            case CombatFxElement.EnemyPhysical: return 0.24f;
            case CombatFxElement.Explosion: return 0.34f;
            default: return 0.28f;
        }
    }

    private static float CombatFxArcHeight(CombatFxElement element)
    {
        switch (element)
        {
            case CombatFxElement.Fire:
            case CombatFxElement.Spirit:
            case CombatFxElement.Explosion:
            case CombatFxElement.EnemyMagic: return 42f;
            case CombatFxElement.Wind:
            case CombatFxElement.Lightning: return 8f;
            default: return 18f;
        }
    }

    private Color CombatFxImpactFlashColor(CombatPresentationEvent presentation)
    {
        if (presentation != null && presentation.critical)
        {
            return new Color(1f, 0.82f, 0.28f, 0.58f);
        }
        var look = ResolveCombatFxLook(presentation);
        var color = Color.Lerp(look.primary, Color.white, 0.24f);
        color.a = look.element == CombatFxElement.Explosion ? 0.52f : 0.42f;
        return color;
    }

    private float CombatFxShakeMultiplier(CombatPresentationEvent presentation)
    {
        var look = ResolveCombatFxLook(presentation);
        var multiplier = 1f;
        switch (look.element)
        {
            case CombatFxElement.Explosion: multiplier = 1.45f; break;
            case CombatFxElement.Fist: multiplier = 1.22f; break;
            case CombatFxElement.Lightning: multiplier = 1.16f; break;
            case CombatFxElement.Slash: multiplier = 1.10f; break;
        }
        if (presentation != null && presentation.critical)
        {
            multiplier *= 1.18f;
        }
        return multiplier;
    }

    private void ApplyCombatRimLight(Image image, Color accent, bool playerSide)
    {
        if (image == null)
        {
            return;
        }
        var outline = image.GetComponent<Outline>();
        if (outline == null)
        {
            outline = image.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = new Color(accent.r, accent.g, accent.b, playerSide ? 0.72f : 0.58f);
        outline.effectDistance = playerSide ? new Vector2(3f, -3f) : new Vector2(-3f, -3f);
        outline.useGraphicAlpha = true;
    }

    private void AddCombatGroundShadowDetail(Transform parent, Color accent)
    {
        if (parent == null)
        {
            return;
        }

        var core = AddFlatPanel("Combat Ground Core", parent, new Color(0.01f, 0.025f, 0.04f, 0.26f));
        core.anchorMin = new Vector2(0.31f, 0.055f);
        core.anchorMax = new Vector2(0.69f, 0.135f);
        core.offsetMin = Vector2.zero;
        core.offsetMax = Vector2.zero;
        var coreImage = core.GetComponent<Image>();
        coreImage.sprite = CombatGlowSprite();
        coreImage.raycastTarget = false;

        var edge = AddFlatPanel("Combat Ground Rim", parent, new Color(accent.r, accent.g, accent.b, 0.16f));
        edge.anchorMin = new Vector2(0.22f, 0.035f);
        edge.anchorMax = new Vector2(0.78f, 0.18f);
        edge.offsetMin = Vector2.zero;
        edge.offsetMax = Vector2.zero;
        var edgeImage = edge.GetComponent<Image>();
        edgeImage.sprite = CombatGlowSprite();
        edgeImage.raycastTarget = false;
    }
}
