using GameLogic.Gameplay.Logic;

namespace GameLogic.Gameplay.View
{

    /// <summary>
    /// 外观动画指令的赋能
    /// </summary>
    public class AnimationEnchant : AgentEnchant<RIL_FACADE_ANIMATION>
    {
        protected override void OnRIL(RIL_FACADE_ANIMATION ril)
        {
            // 有动画定义时, 确保模型代理存在, 由模型代理解析动画
            if (false == string.IsNullOrEmpty(ril.animname)) rilbucket.world.EnsureAgent<ModelAgent>(ril.actor);
        }
    }
}