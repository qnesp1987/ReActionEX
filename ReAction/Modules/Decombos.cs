using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace ReActionEx.Modules;

public unsafe class Decombos : PluginModule
{
    private enum ActionID : uint
    {
        Liturgy_of_the_Bell = 25862,

        Earthly_Star = 7439,

        Fire_in_Red = 34650,
        Blizzard_in_Cyan = 34653,
        Fire_II_in_Red = 34656,
        Blizzard_II_in_Cyan = 34659
    }

    public override bool ShouldEnable => ReActionEx.Config.EnableDecomboLiturgy
    || ReActionEx.Config.EnableDecomboEarthlyStar
    || ReActionEx.Config.EnableDecomboMinorArcana
    || ReActionEx.Config.EnableDecomboFireInRed
    || ReActionEx.Config.EnableDecomboFire2InRed
    || ReActionEx.Config.EnableDecomboBlizzardInCyan
    || ReActionEx.Config.EnableDecomboBlizzard2InCyan;


    protected override void Enable() => GetAdjustedActionIdHook.Enable();
    protected override void Disable() => GetAdjustedActionIdHook.Disable();

    private delegate ActionID GetAdjustedActionIdDelegate(ActionManager* actionManager, ActionID actionID);
    [HypostasisClientStructsInjection(typeof(ActionManager.MemberFunctionPointers), Required = true, EnableHook = false)]
    private static Hook<GetAdjustedActionIdDelegate> GetAdjustedActionIdHook;
    private static ActionID GetAdjustedActionIdDetour(ActionManager* actionManager, ActionID actionID)
    {
        var ret = GetAdjustedActionIdHook.Original(actionManager, actionID);

        switch (actionID)
        {
case ActionID.Meditation when ReActionEx.Config.EnableDecomboMeditation:
    return actionID;
case ActionID.The_Forbidden_Chakra when ReActionEx.Config.EnableDecomboMeditation:
case ActionID.Steel_Peak when ReActionEx.Config.EnableDecomboMeditation:
    return ret != ActionID.Meditation ? ret : actionID;

case ActionID.Bunshin when ReActionEx.Config.EnableDecomboBunshin:
case ActionID.Phantom_Kamaitachi when ReActionEx.Config.EnableDecomboBunshin:
    return actionID;

case ActionID.The_Wanderers_Minuet when ReActionEx.Config.EnableDecomboWanderersMinuet:
    return actionID;

case ActionID.Liturgy_of_the_Bell when ReActionEx.Config.EnableDecomboLiturgy:
    return actionID;


            case ActionID.Earthly_Star when ReActionEx.Config.EnableDecomboEarthlyStar:
                return actionID;

case ActionID.Minor_Arcana when ReActionEx.Config.EnableDecomboMinorArcana:
    return actionID;
case ActionID.Lord_of_Crowns when ReActionEx.Config.EnableDecomboMinorArcana:
case ActionID.Lady_of_Crowns when ReActionEx.Config.EnableDecomboMinorArcana:
    var minorArcanaAdjustment = GetAdjustedActionIdHook.Original(actionManager, ActionID.Minor_Arcana);
    return minorArcanaAdjustment != ActionID.Minor_Arcana ? minorArcanaAdjustment : actionID;

case ActionID.Geirskogul when ReActionEx.Config.EnableDecomboGeirskogul:
    return actionID;
case ActionID.Fire_in_Red when ReActionEx.Config.EnableDecomboFireInRed:
    return actionID;
case ActionID.Fire_II_in_Red when ReActionEx.Config.EnableDecomboFire2InRed:
    return actionID;
case ActionID.Blizzard_in_Cyan when ReActionEx.Config.EnableDecomboBlizzardInCyan:
    return actionID;
case ActionID.Blizzard_II_in_Cyan when ReActionEx.Config.EnableDecomboBlizzard2InCyan:
    return actionID;


            default:
                return ret;
        }
    }
}