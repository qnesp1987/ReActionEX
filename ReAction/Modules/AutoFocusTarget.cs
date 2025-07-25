using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace ReActionEx.Modules;

public class AutoFocusTarget : PluginModule
{
    public override bool ShouldEnable => ReActionEx.Config.AutoFocusTargetID != 0;

    protected override bool Validate() => Game.SetFocusTargetByObjectIDHook is { Address: not 0 };
    protected override void Enable() => DalamudApi.Framework.Update += Update;
    protected override void Disable() => DalamudApi.Framework.Update -= Update;

    private static unsafe void Update(IFramework framework)
    {
        var target = ReActionEx.Config.EnableAutoFocusTargetOutOfCombat || DalamudApi.Condition[ConditionFlag.InCombat] ? PronounManager.GetGameObjectFromID(ReActionEx.Config.AutoFocusTargetID) : null;

        if (target == null && Game.FocusTargetInfo.Name != null)
            Game.RefocusTarget();
        else
            Game.SetFocusTargetByObjectIDHook.Original(TargetSystem.Instance(), Game.GetObjectID(target));
    }
}