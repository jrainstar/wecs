using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 外观指令
    /// </summary>
    public class RIL_FACADE_MODEL : IRIL
    {
        public override ushort id => RIL_DEFINE.FACADE_MODEL;
        
        /// <summary>
        /// 角色id
        /// </summary>
        public int model { get; set; }

        /// <summary>
        /// 模型资源名 (不走角色模型表, 例如 AOE), 为空时用 model 走角色模型表
        /// </summary>
        public string res { get; set; }
        
        protected override void OnReady()
        {
            model = 0;
            res = null;
        }

        protected override void OnReset()
        {
            model = 0;
            res = null;
        }
    }
}