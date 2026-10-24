using Codice.CM.Common;
using GameLogic.Gameplay.Common;
using System.Collections.Generic;
using UnityEditorInternal;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 英雄预制信息
    /// </summary>
    public class HeroPrefabInfo : PrefabInfo
    {
        /// <summary>
        /// 英雄 ID
        /// </summary>
        public int hero { get; set; }
        /// <summary>
        /// 空间信息
        /// </summary>
        public SpatialData spatial { get; set; }
    }

    /// <summary>
    /// 英雄预制创建器
    /// </summary>
    public class HeroPrefab : Prefab<HeroPrefabInfo>
    {
        public override byte type => ACTOR_DEFINE.HERO;

        protected override void OnProcessing(ulong actor, HeroPrefabInfo info)
        {
            var facade = stage.AddBehavior<Facade>(actor);
            facade.SetModel(0);

            stage.AddBehaviorInfo<TickerInfo>(actor);

            var spatial = stage.AddBehaviorInfo<SpatialInfo>(actor);
            spatial.position = info.spatial.position;
            spatial.euler = info.spatial.euler;
            spatial.scale = info.spatial.scale;

        }
    }
}