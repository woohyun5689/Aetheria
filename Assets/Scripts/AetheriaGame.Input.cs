using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed partial class AetheriaGame
{
    private void Update()
    {
        if (currentScreen == AetheriaScreen.Guide && CancelPressed())
        {
            if (guideReturnToMainMenu)
            {
                ShowMainMenu();
            }
            else
            {
                ShowTown("가이드를 닫았습니다. 던전 탐험부터 시작해 보세요.");
            }
            return;
        }

        var helpAvailable = currentScreen == AetheriaScreen.MainMenu
            || currentScreen == AetheriaScreen.ClassSelect
            || currentScreen == AetheriaScreen.Town;
        if (currentEnemy == null && helpAvailable && HotkeyPressed(KeyCode.F1, Hotkey.F1))
        {
            ShowHowToPlay(player == null || currentScreen == AetheriaScreen.MainMenu || currentScreen == AetheriaScreen.ClassSelect);
            return;
        }

        if (player == null)
        {
            return;
        }

        if (currentEnemy != null)
        {
            if (currentScreen == AetheriaScreen.Combat)
            {
                HandleCombatHotkeys();
            }
            return;
        }

        HandleTownHotkeys();
    }

    private void HandleCombatHotkeys()
    {
        if (actionLocked)
        {
            if (HotkeyPressed(KeyCode.Q, Hotkey.Q)
                || HotkeyPressed(KeyCode.W, Hotkey.W)
                || HotkeyPressed(KeyCode.E, Hotkey.E)
                || HotkeyPressed(KeyCode.R, Hotkey.R)
                || HotkeyPressed(KeyCode.T, Hotkey.T)
                || CancelPressed())
            {
                RejectCombatInput(CombatLockedMessage());
            }
            return;
        }

        if (HotkeyPressed(KeyCode.Q, Hotkey.Q))
        {
            PlayerAttack("기본 공격", 1.0f, 0, BasicAttackUsesMagic());
            return;
        }

        var skills = ScaledSkillsForPlayer();
        if (TryUseSkillHotkey(skills, 0, KeyCode.W, Hotkey.W)
            || TryUseSkillHotkey(skills, 1, KeyCode.E, Hotkey.E)
            || TryUseSkillHotkey(skills, 2, KeyCode.R, Hotkey.R)
            || TryUseSkillHotkey(skills, 3, KeyCode.T, Hotkey.T))
        {
            return;
        }

        if (CancelPressed())
        {
            ExitDungeon();
        }
    }

    private bool TryUseSkillHotkey(List<SkillState> skills, int index, KeyCode legacyKey, Hotkey hotkey)
    {
        if (index >= skills.Count || !HotkeyPressed(legacyKey, hotkey))
        {
            return false;
        }

        PlayerAttack(skills[index]);
        return true;
    }

    private void HandleTownHotkeys()
    {
        if (currentScreen != AetheriaScreen.Town)
        {
            if (CanEscapeToTown() && CancelPressed())
            {
                ShowTown("마을로 돌아왔습니다.");
            }
            return;
        }

        if (HotkeyPressed(KeyCode.I, Hotkey.I))
        {
            ShowInventory("장비와 가방을 열었습니다.");
        }
        else if (HotkeyPressed(KeyCode.W, Hotkey.W))
        {
            EnterDungeonSelectFromTown();
        }
        else if (HotkeyPressed(KeyCode.E, Hotkey.E))
        {
            ShowCrafting("같은 등급 장비 3개를 다음 등급 장비로 조합합니다.");
        }
        else if (HotkeyPressed(KeyCode.R, Hotkey.R))
        {
            ShowEnhancement("착용 중인 장비를 부위별로 강화하세요.");
        }
        else if (HotkeyPressed(KeyCode.K, Hotkey.K))
        {
            ShowSkillTraining("스킬을 강화해 위력과 상태이상 확률을 올립니다.");
        }
        else if (HotkeyPressed(KeyCode.S, Hotkey.S))
        {
            SaveGame();
            ShowTown("슬롯 " + (activeSlot + 1) + "에 저장했습니다.");
        }
    }

    private bool CanEscapeToTown()
    {
        return currentScreen == AetheriaScreen.Inventory
            || currentScreen == AetheriaScreen.Enhancement
            || currentScreen == AetheriaScreen.SkillTraining
            || currentScreen == AetheriaScreen.Crafting
            || currentScreen == AetheriaScreen.DungeonSelect;
    }

    private bool CancelPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            return true;
        }
#endif
        return HotkeyPressed(KeyCode.Escape, Hotkey.Escape);
    }

    private bool HotkeyPressed(KeyCode legacyKey, Hotkey hotkey)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && InputSystemHotkeyPressed(Keyboard.current, hotkey))
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(legacyKey);
#else
        return false;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private bool InputSystemHotkeyPressed(Keyboard keyboard, Hotkey hotkey)
    {
        switch (hotkey)
        {
            case Hotkey.Q: return keyboard.qKey.wasPressedThisFrame;
            case Hotkey.W: return keyboard.wKey.wasPressedThisFrame;
            case Hotkey.E: return keyboard.eKey.wasPressedThisFrame;
            case Hotkey.R: return keyboard.rKey.wasPressedThisFrame;
            case Hotkey.T: return keyboard.tKey.wasPressedThisFrame;
            case Hotkey.I: return keyboard.iKey.wasPressedThisFrame;
            case Hotkey.K: return keyboard.kKey.wasPressedThisFrame;
            case Hotkey.S: return keyboard.sKey.wasPressedThisFrame;
            case Hotkey.F1: return keyboard.f1Key.wasPressedThisFrame;
            case Hotkey.Escape: return keyboard.escapeKey.wasPressedThisFrame;
            default: return false;
        }
    }
#endif

    private enum Hotkey
    {
        Q,
        W,
        E,
        R,
        T,
        I,
        K,
        S,
        F1,
        Escape
    }
}
