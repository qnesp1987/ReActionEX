using Hypostasis.Game.Structures;

namespace ReActionEx.Modules;

public class ActionStacks : PluginModule
{
    protected override bool Validate() => Common.getGameObjectFromPronounID.IsValid && ActionManager.canUseActionOnGameObject.IsValid && ActionManager.canQueueAction.IsValid;
}