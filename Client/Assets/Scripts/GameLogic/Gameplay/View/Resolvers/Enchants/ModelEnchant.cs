using GameLogic.Gameplay.Logic;

namespace GameLogic.Gameplay.View
{
    /// <summary>
    /// 模型代理的赋能
    /// </summary>
    public class ModelEnchant : AgentEnchant<RIL_FACADE_MODEL>
    {
        protected override void OnRIL(RIL_FACADE_MODEL ril)
        {
            // 如果没有模型定义, 则回收模型代理, 有则创建模型代理
            // 模型有两种来源: 角色模型表的 model, 以及直接给资源名的 res (AOE 这类对象)
            if (ril.model > 0 || false == string.IsNullOrEmpty(ril.res))
            {
                rilbucket.world.EnsureAgent<ModelAgent>(ril.actor);
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

        private void RecycleAgent(ulong actor)
        {
            var agent = rilbucket.world.GetAgent<ModelAgent>(actor);
            if (null == agent) return;
            rilbucket.world.RmvAgent(agent);
        }
    }

}