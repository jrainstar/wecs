using Codice.CM.Client.Differences;
using GameLogic.Gameplay.Common;
using GameLogic.Gameplay.Logic;

namespace GameLogic.Gameplay.View
{
    /// <summary>
    /// 动画代理赋能
    /// </summary>
    public class AnimationEnchant : AgentEnchant<RIL_FACADE_ANIMATION>
    {
        protected override void OnRIL(RIL_FACADE_ANIMATION ril)
        {
            // 如果没有模型定义, 则回收动画代理
            if (false == rilbucket.SeekRIL(ril.actor, out RIL_FACADE_MODEL facademodel) || 0 >= facademodel.model)
            {
                RecycleAgent(ril.actor);
                return;
            }

            // 如果模型定义没有动画配置, 则回收动画代理

            if (false == engine.config.location.TbRoleData.TryGetValue(facademodel.model, out var roledata)) return;
            if (false == engine.config.location.TbRoleModel.TryGetValue(roledata.ModelId, out var rolemodel)) return;

            if (string.IsNullOrEmpty(rolemodel.Animation))
            {
                RecycleAgent(ril.actor);
                return;
            }

            // 如果模型定义有动画配置, 则创建动画代理
            rilbucket.world.EnsureAgent<AnimationAgent>(ril.actor);
        }

        protected override void OnLossRIL(RIL_LOSS ril)
        {
            base.OnLossRIL(ril);
            RecycleAgent(ril.actor);
        }

        /// <summary>
        /// 回收动画代理
        /// </summary>
        /// <param name="actor">ActorID</param>
        private void RecycleAgent(ulong actor)
        {
            var agent = rilbucket.world.GetAgent<AnimationAgent>(actor);
            if (null == agent) return;
            rilbucket.world.RmvAgent(agent);
        }
    }
}