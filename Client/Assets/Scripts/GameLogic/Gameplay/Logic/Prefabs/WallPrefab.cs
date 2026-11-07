using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 墙体预制信息
    /// </summary>
    public class WallPrefabInfo : PrefabInfo
    {
        /// <summary>
        /// 碰撞物件数据 (位置/旋转/四种形状)
        /// </summary>
        public CollisionData collision { get; set; }
    }

    /// <summary>
    /// 墙体预制创建器
    /// 场景静态碰撞体: 只有空间信息与碰撞盒, 没有行为与模型, 质量取负值 (不可推动)
    /// </summary>
    public class WallPrefab : Prefab<WallPrefabInfo>
    {
        public override byte type => ACTOR_DEFINE.WALL;

        protected override void OnProcessing(ulong actor, WallPrefabInfo info)
        {
            var spatial = stage.AddBehaviorInfo<SpatialInfo>(actor);
            info.collision.Apply(spatial);

            // 四种形状 (Box/Sphere/Capsule/Cylinder) 由数据里的 shape 分派
            var collider = stage.AddBehaviorInfo<ColliderInfo>(actor);
            info.collision.Apply(collider);

            // 墙体不可推动, 负数表示质量无限大; 角色移动时只会被顶回
            collider.mass = -FP.One;
        }
    }
}
