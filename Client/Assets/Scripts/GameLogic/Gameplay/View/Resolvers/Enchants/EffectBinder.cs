using GameLogic.Gameplay.Logic;

namespace GameLogic.Gameplay.View
{
    /// <summary>
    /// 特效代理赋能
    /// </summary>
    public class EffectBinder : AgentBinder<RIL_FACADE_EFFECT>
    {
        protected override void OnRIL(RIL_FACADE_EFFECT ril)
        {
            // 有存活的特效才需要特效代理, 全部播完则回收
            if (null != ril.effectdict && 0 < ril.effectdict.Count)
            {
                rilbucket.world.EnsureAgent<EffectAgent>(ril.actor);
            }
            else
            {
                RecycleAgent(ril.actor);
            }
        }

        protected override void OnLossRIL(RIL_LOSS ril)
        {
            base.OnLossRIL(ril);
            RecycleAgent(ril.actor);
        }

        /// <summary>
        /// 回收特效代理
        /// </summary>
        /// <param name="actor">ActorID</param>
        private void RecycleAgent(ulong actor)
        {
            var agent = rilbucket.world.GetAgent<EffectAgent>(actor);
            if (null == agent) return;
            rilbucket.world.RmvAgent(agent);
        }
    }
}
