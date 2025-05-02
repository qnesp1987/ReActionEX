using System;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Hypostasis.Game.Structures;
using GameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace ReActionEx.Modules;

public unsafe class AutoTarget : PluginModule
{
    public override bool ShouldEnable => ReActionEx.Config.EnableAutoTarget;

    protected override bool Validate() => Game.fpGetGameObjectFromObjectID != null && ActionManager.canUseActionOnGameObject.IsValid;
    protected override void Enable() => ActionStackManager.PostActionStack += PostActionStack;
    protected override void Disable() => ActionStackManager.PostActionStack -= PostActionStack;

    private static void TargetEnemy()
    {
        if (DalamudApi.ClientState.LocalPlayer is not { } p) return;

        var worldCamera = Common.CameraManager->worldCamera;
        if (worldCamera == null) return;

        var hRotation = worldCamera->currentHRotation + Math.PI * 1.5;
        if (worldCamera->IsHRotationOffset)
            hRotation -= Math.PI;

        const double doublePI = Math.PI * 2;
        const double halfCone = Math.PI * 0.35;
        var minRotation = (hRotation + doublePI - halfCone) % doublePI;
        var maxRotation = (hRotation + halfCone) % doublePI;

        static bool IsBetween(double val, double a, double b)
        {
            if (a > b)
                return val >= a || val <= b;
            return val >= a && val <= b;
        }

        IGameObject closest = null;
        foreach (var o in DalamudApi.ObjectTable.Where(o => o is { YalmDistanceX: < 30, ObjectKind: ObjectKind.Player or ObjectKind.BattleNpc }
            && ((IBattleChara)o).CurrentHp > 0
            && ActionManager.CanUseActionOnGameObject(7, (GameObject*)o.Address)))
        {
            var posDiff = o.Position - p.Position;
            var angle = Math.Atan2(-posDiff.Z, posDiff.X) + Math.PI;
            if (IsBetween(angle, minRotation, maxRotation) && (closest == null || closest.YalmDistanceX > o.YalmDistanceX) || ReActionEx.Config.IgnoreCamera)
                closest = o;
        }

        if (closest != null)
            DalamudApi.TargetManager.Target = closest;
    }

    // The issue is likely caused by a missing closing brace in the file.  
    // After reviewing the provided code, it appears that the `PostActionStack` method is missing a closing brace.  
    // Adding the missing brace at the end of the method should resolve the issue.  

    private static void PostActionStack(ActionManager* actionManager, uint actionType, uint actionID, uint adjustedActionID, ref ulong targetObjectID, uint param, uint useType, int pvp)
    {
        if (actionType != 1) return;

        var targetObject = DalamudApi.TargetManager.Target is { } t ? (GameObject*)t.Address : null;
        if (!ReActionEx.Config.EnableAutoChangeTarget && targetObject != null
            || targetObjectID != Game.InvalidObjectID && Game.GetGameObjectFromObjectID(targetObjectID) != targetObject
            || ActionManager.CanUseActionOnGameObject(adjustedActionID, targetObject)
            || !ReActionEx.actionSheet.TryGetValue(adjustedActionID, out var a))
            return;

        // NEW: Skip auto-switching for hostile targets  
        if (a.CanTargetHostile)
        {
            DalamudApi.LogDebug($"Skipping auto-switch for hostile target: {adjustedActionID}");
            return; // Do not auto-switch hostile targets  
        }

        // Continue with target switching for non-hostile targets  
        DalamudApi.LogDebug($"Attempting to swap target {adjustedActionID}, {targetObjectID:X}");
        TargetEnemy();
        if (DalamudApi.TargetManager.Target is not { } target) return;

        var prevTargetObjectID = targetObjectID;
        targetObjectID = Game.GetObjectID((GameObject*)target.Address);

        DalamudApi.LogDebug($"Target swapped {prevTargetObjectID:X} -> {targetObjectID:X}");
    } // <-- Missing closing brace added here  
}