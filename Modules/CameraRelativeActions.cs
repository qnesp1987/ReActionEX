using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System;
using ActionManager = Hypostasis.Game.Structures.ActionManager;

namespace ReActionEx.Modules;

public unsafe class CameraRelativeActions : PluginModule
{
    public override bool ShouldEnable => ReActionEx.Config.EnableCameraRelativeDirectionals || ReActionEx.Config.EnableCameraRelativeDashes;

    protected override bool Validate() => Common.CameraManager != null;
    protected override void Enable() => ActionStackManager.PostActionStack += PostActionStack;
    protected override void Disable() => ActionStackManager.PostActionStack -= PostActionStack;

    [HypostasisSignatureInjection("E8 ?? ?? ?? ?? 83 FE 4F", Required = true)]
    private static delegate* unmanaged<GameObject*, float, void> fpSetGameObjectRotation;
    private static void SetCharacterRotationToCamera(bool reverseBackwardsDashes)
    {
        var worldCamera = Common.CameraManager->worldCamera;
        if (worldCamera == null) return;

        var rotation = worldCamera->GameObjectHRotation;
        if (reverseBackwardsDashes)
            rotation = rotation > 0 ? rotation - MathF.PI : rotation + MathF.PI;

        fpSetGameObjectRotation((GameObject*)DalamudApi.ClientState.LocalPlayer!.Address, rotation);
    }

    private static void PostActionStack(ActionManager* actionManager, uint actionType, uint actionID, uint adjustedActionID, ref ulong targetObjectID, uint param, uint useType, int pvp)
    {
        if (!CheckAction(actionType, actionID, adjustedActionID)
            || actionManager->CS.GetActionStatus((ActionType)actionType, adjustedActionID) != 0
            || actionManager->animationLock != 0)
            return;

        DalamudApi.LogDebug($"Rotating camera {actionType}, {adjustedActionID}");

        if (ReActionEx.actionSheet.TryGetValue(adjustedActionID, out var a))
        {
            SetCharacterRotationToCamera(a.BehaviourType is 3 or 4 && ReActionEx.Config.EnableReverseBackwardDashes);
        }
    }

    private static bool CheckAction(uint actionType, uint actionID, uint adjustedActionID)
    {
        if (!ReActionEx.actionSheet.TryGetValue(adjustedActionID, out var a)) return false;
        if (ReActionEx.Config.EnableCameraRelativeDirectionals && a.IsPlayerAction && (a.AutoAttackBehaviour == 6 || (a.CastType is 3 or 4 && a.CanTargetSelf))) return true; // Channeled abilities and cones and rectangles
        if (!ReActionEx.Config.EnableCameraRelativeDashes) return false;
        if (!a.AffectsPosition && adjustedActionID != 29494) return false; // Block non movement abilities
        if (!a.CanTargetSelf) return false; // Block non self targeted abilities
        if (ReActionEx.Config.EnableNormalBackwardDashes && a.BehaviourType is 3 or 4) return false; // Block backwards dashes if desired
        return a.BehaviourType > 1; // Block abilities like Loom and Shukuchi
    }
}