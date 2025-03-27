using System;
using System.Linq;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using Hypostasis.Game.Structures;
using Camera = FFXIVClientStructs.FFXIV.Client.Game.Camera;
using GameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace ReActionEx;

[HypostasisInjection]
public static unsafe class Game
{
    public const uint InvalidObjectID = 0xE0000000;

    // movzx eax, dl -> xor al, al
    public static readonly AsmPatch queueGroundTargetsPatch = new("0F B6 C2 34 01 84 C0 74 8C", [ 0x90, 0x32, 0xC0 ], ReActionEx.Config.EnableGroundTargetQueuing);

    // test byte ptr [rbp+3A], 04 (CanTargetSelf)
    // jnz 79h
    public static readonly AsmPatch spellAutoAttackPatch = new(
        "41 B0 01 44 0F B6 CA 41 0F B6 D0 E9 ?? ?? ?? ?? 41 B0 01",
        [0xF6, 0x46, 0x3A, 0x04, 0x0F, 0x85, 0x7A, 0x00, 0x00, 0x00, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90],
        ReActionEx.Config.EnableSpellAutoAttacks && ReActionEx.Config.EnableSpellAutoAttacksOutOfCombat);


    public static readonly AsmPatch allowUnassignableActionsPatch = new("75 07 32 C0 E9 ?? ?? ?? ?? 48 8B 00", [0xEB], ReActionEx.Config.EnableUnassignableActions);

    public static readonly AsmPatch waitSyntaxDecimalPatch = new("F3 0F 58 05 ?? ?? ?? ?? F3 48 0F 2C C0 69 C8",
        [
            0xB8, 0x00, 0x00, 0x7A, 0x44,
            0x66, 0x0F, 0x6E, 0xC8,
            0xF3, 0x0F, 0x59, 0xC1,
            0xF3, 0x48, 0x0F, 0x2C, 0xC8,
            0x90,
            0x90, 0x90, 0x90, 0x90, 0x90
        ],
        ReActionEx.Config.EnableFractionality);

    public static readonly AsmPatch waitCommandDecimalPatch = new("F3 0F 58 0D ?? ?? ?? ?? F3 48 0F 2C C1 69 C8",
        [
            0xB8, 0x00, 0x00, 0x7A, 0x44,
            0x66, 0x0F, 0x6E, 0xC0,
            0xF3, 0x0F, 0x59, 0xC8,
            0xF3, 0x48, 0x0F, 0x2C, 0xC9,
            0x90,
            0x89, 0x4B, 0x58,
            0x90, 0x90, 0x90, 0x90, 0x90, 0x90,
            0xEB // 0x1F
        ],
        ReActionEx.Config.EnableFractionality);

    public static readonly AsmPatch queueACCommandPatch = new("02 00 00 00 41 8B D7 89", [0x64], ReActionEx.Config.EnableMacroQueue);

    public static ulong GetObjectID(GameObject* o)
    {
        if (o == null) return InvalidObjectID;

        var id = o->GetGameObjectId();
        return (ulong)((id.Type * 0x1_0000_0000) | id.ObjectId);
    }

    public static GameObject* GetLowestHPEnemy()
    {
        var allObjects = Common.GetGameObjects(); // Hypothetical method to retrieve all entities.
        if (allObjects == null || !allObjects.Any())
            return null;

        // Filter to find the enemy with the lowest HP
        var lowestHPEnemy = allObjects
            .Where(o => IsEnemy(o)) // Filter to include only enemies.
            .OrderBy(o => ((Character*)o)->CharacterData.Health)
            .FirstOrDefault();

        return lowestHPEnemy;
    }

    private static bool IsEnemy(GameObject* obj)
    {
        if (obj == null) return false;

        var localPlayer = DalamudApi.ClientState.LocalPlayer;
        return localPlayer != null &&
               obj->ObjectKind == ObjectKind.BattleNpc && // Ensure it’s a combat NPC.
               obj->FactionId != localPlayer.FactionId;  // Check against the player's faction.
    }

    public static GameObject* GetMouseOverObject(GameObjectArray* array)
    {
        if (array->Length == 0) return null;

        var targetSystem = TargetSystem.Instance();
        var camera = (Camera*)Common.CameraManager->worldCamera;
        if (targetSystem == null || camera == null || targetSystem->MouseOverTarget == null) return null;

        var nameplateTarget = targetSystem->MouseOverNameplateTarget;
        if (nameplateTarget != null)
        {
            for (int i = 0; i < array->Length; i++)
            {
                if ((*array)[i] == nameplateTarget)
                    return nameplateTarget;
            }
        }

        return targetSystem->GetMouseOverObject(Common.InputData->GetAxisInput(0), Common.InputData->GetAxisInput(1), array, camera);
    }

    public static void SetHotbarSlot(int hotbar, int slot, byte type, uint id)
    {
        if (hotbar is < 0 or > 17 || (hotbar < 10 ? slot is < 0 or > 11 : slot is < 0 or > 15)) return;
        Framework.Instance()->GetUIModule()->GetRaptureHotbarModule()->SetAndSaveSlot((uint)hotbar, (uint)slot, (RaptureHotbarModule.HotbarSlotType)type, id, false, false);
    }

    public delegate Bool UseActionDelegate(ActionManager* actionManager, uint actionType, uint actionID, ulong targetObjectID, uint param, uint useType, int pvp, bool* isGroundTarget);
    public static Hook<UseActionDelegate> UseActionHook;
    private static Bool UseActionDetour(ActionManager* actionManager, uint actionType, uint actionID, ulong targetObjectID, uint param, uint useType, int pvp, bool* isGroundTarget) =>
        ActionStackManager.OnUseAction(actionManager, actionType, actionID, targetObjectID, param, useType, pvp, isGroundTarget);

    public static void Initialize()
    {
        if (Common.ActionManager == null)
            throw new ApplicationException("ActionManager is not initialized!");
        Common.getGameObjectFromPronounID.CreateHook(GetGameObjectFromPronounIDDetour);
    }

    public static void Dispose() { }
}
