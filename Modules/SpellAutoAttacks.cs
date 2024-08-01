using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;

namespace ReActionEx.Modules;

public class SpellAutoAttacks : PluginModule
{
    public override bool ShouldEnable => ReActionEx.Config.EnableSpellAutoAttacks && !ReActionEx.Config.EnableSpellAutoAttacksOutOfCombat;

    protected override bool Validate() => Game.spellAutoAttackPatch.IsValid;
    protected override void Enable() => DalamudApi.Framework.Update += Update;
    protected override void Disable() => DalamudApi.Framework.Update -= Update;

    private static void Update(IFramework framework)
    {
        if (ReActionEx.Config.EnableSpellAutoAttacks)
        {
            if (Game.spellAutoAttackPatch.IsEnabled != DalamudApi.Condition[ConditionFlag.InCombat])
                Game.spellAutoAttackPatch.Toggle();
        }
        else
        {
            Game.spellAutoAttackPatch.Disable();
        }
    }
}