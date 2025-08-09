using Hypostasis.Game.Structures;
using Lumina.Excel.Sheets;

namespace ReActionEx.Modules;

public unsafe class EnhancedAutoFaceTarget : PluginModule
{
    // jmp 1Ch
    private static readonly AsmPatch removeAutoFaceTargetPatch = new("80 7E 34 06 75 1E 48 8D 0D", [0x90, 0x90, 0x90, 0x90, 0xEB, 0x1C]);
    // jz -> jmp ??
    private static readonly AsmPatch removeAutoFaceGroundTargetPatch = new("80 7E 34 06 74 21 48 8D 8F", [0x90, 0x90, 0x90, 0x90, 0xEB]);

    public override bool ShouldEnable => ReActionEx.Config.EnableEnhancedAutoFaceTarget;

    protected override bool Validate() => removeAutoFaceTargetPatch.IsValid;

    protected override void Enable()
    {
        removeAutoFaceTargetPatch.Disable();
        removeAutoFaceGroundTargetPatch.Enable();
        ActionStackManager.PostActionStack += PostActionStack;
    }

    protected override void Disable()
    {
        removeAutoFaceTargetPatch.Disable();
        removeAutoFaceGroundTargetPatch.Disable();
        ActionStackManager.PostActionStack -= PostActionStack;
    }

    private static void PostActionStack(ActionManager* actionManager, uint actionType, uint actionID, uint adjustedActionID, ref ulong targetObjectID, uint param, uint useType, int pvp)
    {
        if (DalamudApi.DataManager.GetExcelSheet<Action>()?.GetRowOrDefault(adjustedActionID) is { NeedToFaceTarget: false }) // This is checked by Client::Game::ActionManager_GetActionInRangeOrLoS
            removeAutoFaceTargetPatch.Enable();
        else
            removeAutoFaceTargetPatch.Disable();
    }
}
