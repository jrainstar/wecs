using GameLogic.Gameplay.Common;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 碰撞结果
    /// </summary>
    public struct HitResult
    {
        /// <summary>
        /// 是否碰撞
        /// </summary>
        public bool hit { get; set; }
        /// <summary>
        /// 碰撞列表
        /// </summary>
        public List<(ulong actor, FPVector3 point, FPVector3 normal, FP penetration)> colliders { get; set; }
    }

    /// <summary>
    /// 碰撞检测
    /// </summary>
    public class Detection : Behavior
    {
        public object @lock { get; private set; } = new();
        public List<(FPVector3 center, FPVector3 dire, FP dis)> raycasts { get; private set; }
        public List<(FPVector3 position, FPQuaternion rotation, FPVector3 size)> overlapboxes { get; private set; }
        public List<(FPVector3 position, FP radius)> overlapspheres { get; private set; }
        public List<(FPVector3 position, FPQuaternion rotation, FP radius, FP height)> overlapcapsules { get; private set; }
        public List<(FPVector3 position, FPQuaternion rotation, FP radius, FP height)> overlapcylinders { get; private set; }

        protected override void OnAssemble()
        {
            base.OnAssemble();
            lock (@lock)
            {
                raycasts = ObjectCache.Ensure<List<(FPVector3 center, FPVector3 dire, FP dis)>>();
                overlapboxes = ObjectCache.Ensure<List<(FPVector3 position, FPQuaternion rotation, FPVector3 size)>>();
                overlapspheres = ObjectCache.Ensure<List<(FPVector3 position, FP radius)>>();
                overlapcapsules = ObjectCache.Ensure<List<(FPVector3 position, FPQuaternion rotation, FP radius, FP height)>>();
                overlapcylinders = ObjectCache.Ensure<List<(FPVector3 position, FPQuaternion rotation, FP radius, FP height)>>();
            }
        }

        protected override void OnDisassemble()
        {
            base.OnDisassemble();
            lock (@lock)
            {
                raycasts.Clear();
                ObjectCache.Set(raycasts);
                overlapboxes.Clear();
                ObjectCache.Set(overlapboxes);
                overlapspheres.Clear();
                ObjectCache.Set(overlapspheres);
                overlapcapsules.Clear();
                ObjectCache.Set(overlapcapsules);
                overlapcylinders.Clear();
                ObjectCache.Set(overlapcylinders);
            }
        }

        protected override void OnTick(FP tick)
        {
            base.OnTick(tick);
            lock (@lock)
            {
                raycasts.Clear();
                overlapboxes.Clear();
                overlapspheres.Clear();
                overlapcapsules.Clear();
                overlapcylinders.Clear();
            }
        }

        /// <summary>
        /// 判断 AABB 是否包含另一个 AABB
        /// </summary>
        /// <param name="container">AABB (容纳区域)</param>
        /// <param name="item">AABB (容纳单位)</param>
        /// <returns>YES/NO</returns>
        public bool Inside(AABB container, AABB item)
        {
            FPVector3 min1 = container.position - container.size * FP.Half;
            FPVector3 max1 = container.position + container.size * FP.Half;
            FPVector3 min2 = item.position - item.size * FP.Half;
            FPVector3 max2 = item.position + item.size * FP.Half;

            return
            (
                min1.x <= min2.x && min1.y <= min2.y && min1.z <= min2.z
                &&
                max1.x >= max2.x && max1.y >= max2.y && max1.z >= max2.z
            );
        }

        /// <summary>
        /// 检测两个碰撞盒之间的重叠
        /// </summary>
        /// <param name="a">碰撞盒 A</param>
        /// <param name="b">碰撞盒 B</param>
        /// <param name="point">碰撞点</param>
        /// <param name="normal">从 BoxB 指向 BoxA 的法线</param>
        /// <param name="penetration">穿透深度</param>
        /// <returns>YES/NO</returns>
        public bool Overlap(ColliderInfo a, ColliderInfo b, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            point = FPVector3.zero;
            normal = FPVector3.zero;
            penetration = FP.MaxValue;

            // 如果两个碰撞盒的层不匹配，则不进行碰撞检测
            if (false == COLLISION_DEFINE.QUERY(a.layer, b.layer)) return false;

            // 获取空间信息
            if (false == stage.SeekBehaviorInfo(a.actor, out SpatialInfo aspatial) || false == stage.SeekBehaviorInfo(b.actor, out SpatialInfo bspatial)) return false;

            return Detect(a, a.actor, aspatial, b, b.actor, bspatial, out point, out normal, out penetration);
        }

        /// <summary>
        /// ColliderInfo 检测
        /// </summary>
        /// <param name="collider">碰撞盒</param>
        /// <returns>结果</returns>
        public HitResult Overlap(ColliderInfo collider)
        {
            if (false == stage.SeekBehaviorInfo(collider.actor, out SpatialInfo spatial)) return default;

            HitResult result = default;
            switch (collider.shape)
            {
                case COLLISION_DEFINE.COLLIDER_BOX:
                    result = OverlapBox(spatial.position + collider.box.offset, FPQuaternion.Euler(spatial.euler), collider.box.size, collider.layer);
                    break;
                case COLLISION_DEFINE.COLLIDER_SPHERE:
                    result = OverlapSphere(spatial.position + collider.sphere.offset, collider.sphere.radius, collider.layer);
                    break;
                case COLLISION_DEFINE.COLLIDER_CAPSULE:
                    // 胶囊/圆柱保留 offset: 直接把自身碰撞盒当作检测体
                    result = OverlapShape(spatial, collider, collider.layer);
                    break;
                case COLLISION_DEFINE.COLLIDER_CYLINDER:
                    result = OverlapShape(spatial, collider, collider.layer);
                    break;
            }

            // 如果发生碰撞，清除当前碰撞体
            if (result.hit)
            {
                List<(ulong actor, FPVector3 point, FPVector3 normal, FP penetration)> colliders = ObjectCache.Ensure<List<(ulong actor, FPVector3 point, FPVector3 normal, FP penetration)>>();
                foreach (var c in result.colliders)
                {
                    if (collider.actor == c.actor) continue;
                    colliders.Add(c);
                }
                result.colliders = colliders;

                stage.cache.AutoRecycle(colliders);
            }

            return result;
        }

        /// <summary>
        /// Box 检测
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="rotation">旋转</param>
        /// <param name="size">尺寸</param>
        /// <param name="layer">层级 (-1, 默认全检测)</param>
        /// <returns>结果</returns>
        public HitResult OverlapBox(FPVector3 position, FPQuaternion rotation, FPVector3 size, int layer = -1)
        {
            lock (@lock)
            {
                overlapboxes.Add((position, rotation, size));
            }

            if (false == stage.SeekBehaviorInfos(out List<ColliderInfo> colliders)) return default;

            ColliderInfo probe = ObjectCache.Ensure<ColliderInfo>();
            probe.Ready(0);
            probe.shape = COLLISION_DEFINE.COLLIDER_BOX;
            probe.box = new BoxShape { offset = FPVector3.zero, size = size };

            Transform transform = new Transform(position, rotation);
            HitResult result = OverlapShape(probe, transform, colliders, layer);
            probe.Reset();
            ObjectCache.Set(probe);
            return result;
        }

        /// <summary>
        /// Sphere 检测
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="radius">半径</param>
        /// <param name="layer">层级 (-1, 默认全检测)</param>
        /// <returns>结果</returns>
        public HitResult OverlapSphere(FPVector3 position, FP radius, int layer = -1)
        {
            lock (@lock)
            {
                overlapspheres.Add((position, radius));
            }

            if (false == stage.SeekBehaviorInfos(out List<ColliderInfo> colliders)) return default;

            ColliderInfo probe = ObjectCache.Ensure<ColliderInfo>();
            probe.Ready(0);
            probe.shape = COLLISION_DEFINE.COLLIDER_SPHERE;
            probe.sphere = new SphereShape { offset = FPVector3.zero, radius = radius };

            Transform transform = new Transform(position, FPQuaternion.identity);
            HitResult result = OverlapShape(probe, transform, colliders, layer);
            probe.Reset();
            ObjectCache.Set(probe);
            return result;
        }

        /// <summary>
        /// Capsule 检测
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="rotation">旋转 (长轴为局部 Y)</param>
        /// <param name="radius">半径</param>
        /// <param name="height">全高, 包含两端半球</param>
        /// <param name="layer">层级 (-1, 默认全检测)</param>
        /// <returns>结果</returns>
        public HitResult OverlapCapsule(FPVector3 position, FPQuaternion rotation, FP radius, FP height, int layer = -1)
        {
            lock (@lock)
            {
                overlapcapsules.Add((position, rotation, radius, height));
            }

            if (false == stage.SeekBehaviorInfos(out List<ColliderInfo> colliders)) return default;

            ColliderInfo probe = ObjectCache.Ensure<ColliderInfo>();
            probe.Ready(0);
            probe.shape = COLLISION_DEFINE.COLLIDER_CAPSULE;
            probe.capsule = new CapsuleShape { offset = FPVector3.zero, radius = radius, height = height };

            Transform transform = new Transform(position, rotation);
            HitResult result = OverlapShape(probe, transform, colliders, layer);
            probe.Reset();
            ObjectCache.Set(probe);
            return result;
        }

        /// <summary>
        /// Cylinder 检测
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="rotation">旋转 (长轴为局部 Y)</param>
        /// <param name="radius">半径</param>
        /// <param name="height">沿轴全高</param>
        /// <param name="layer">层级 (-1, 默认全检测)</param>
        /// <returns>结果</returns>
        public HitResult OverlapCylinder(FPVector3 position, FPQuaternion rotation, FP radius, FP height, int layer = -1)
        {
            lock (@lock)
            {
                overlapcylinders.Add((position, rotation, radius, height));
            }

            if (false == stage.SeekBehaviorInfos(out List<ColliderInfo> colliders)) return default;

            ColliderInfo probe = ObjectCache.Ensure<ColliderInfo>();
            probe.Ready(0);
            probe.shape = COLLISION_DEFINE.COLLIDER_CYLINDER;
            probe.cylinder = new CylinderShape { offset = FPVector3.zero, radius = radius, height = height };

            Transform transform = new Transform(position, rotation);
            HitResult result = OverlapShape(probe, transform, colliders, layer);
            probe.Reset();
            ObjectCache.Set(probe);
            return result;
        }

        /// <summary>
        /// 以碰撞盒自身为检测体, 检测其与场景中其它碰撞盒的重叠
        /// </summary>
        /// <param name="spatial">自身空间信息</param>
        /// <param name="collider">自身碰撞盒</param>
        /// <param name="layer">层级 (-1, 默认全检测)</param>
        /// <returns>结果</returns>
        private HitResult OverlapShape(SpatialInfo spatial, ColliderInfo collider, int layer)
        {
            if (false == stage.SeekBehaviorInfos(out List<ColliderInfo> colliders)) return default;

            Transform transform = MakeTransform(spatial);
            return OverlapShape(collider, transform, colliders, layer);
        }

        /// <summary>
        /// 碰撞盒检测通用实现
        /// </summary>
        /// <param name="shape">检测体碰撞盒</param>
        /// <param name="transform">检测体变换</param>
        /// <param name="colliders">待检测碰撞盒集合</param>
        /// <param name="layer">层级 (-1, 默认全检测)</param>
        /// <returns>结果</returns>
        private HitResult OverlapShape(ColliderInfo shape, in Transform transform, List<ColliderInfo> colliders, int layer)
        {
            HitResult result = new();
            if (null == shape) return result;

            foreach (var collider in colliders)
            {
                if (false == stage.SeekBehaviorInfo(collider.actor, out SpatialInfo spatial)) continue;
                if (-1 != layer && false == COLLISION_DEFINE.QUERY(layer, collider.layer)) continue;

                Transform targetTransform = MakeTransform(spatial);
                if (false == DetectionShape.Overlaps(shape, transform, collider, targetTransform)) continue;
                if (false == DetectionShape.DetectShapeShape(shape, transform, collider, targetTransform,
                    out FPVector3 point, out FPVector3 normal, out FP penetration)) continue;

                result.hit = true;
                if (null == result.colliders)
                {
                    result.colliders = ObjectCache.Ensure<List<(ulong id, FPVector3 point, FPVector3 normal, FP penetration)>>();
                    stage.cache.AutoRecycle(result.colliders);
                }
                result.colliders.Add((collider.actor, point, normal, penetration));
            }

            return result;
        }

        /// <summary>
        /// 射线检测
        /// </summary>
        /// <param name="origin">原点</param>
        /// <param name="dire">方向</param>
        /// <param name="distance">距离</param>
        /// <param name="layer">层级 (-1, 默认全检测)</param>
        /// <returns>结果</returns>
        public HitResult Raycast(FPVector3 origin, FPVector3 dire, FP distance, int layer = -1)
        {
            lock (@lock)
            {
                raycasts.Add((origin, dire, distance));
            }

            if (false == stage.SeekBehaviorInfos(out List<ColliderInfo> colliders)) return default;

            FPVector3 end = origin + dire.normalized * distance;
            HitResult result = new();
            foreach (var collider in colliders)
            {
                if (false == stage.SeekBehaviorInfo(collider.actor, out SpatialInfo spatial)) continue;
                if (-1 != layer && false == COLLISION_DEFINE.QUERY(layer, collider.layer)) continue;

                Transform transform = MakeTransform(spatial);
                if (false == DetectionShape.DetectLineShape(origin, end, collider, transform,
                    out FPVector3 point, out FPVector3 normal, out FP hitDistance)) continue;

                result.hit = true;
                if (null == result.colliders)
                {
                    result.colliders = ObjectCache.Ensure<List<(ulong id, FPVector3 point, FPVector3 normal, FP penetration)>>();
                    stage.cache.AutoRecycle(result.colliders);
                }
                result.colliders.Add((collider.actor, point, normal, hitDistance));
            }

            return result;
        }

        /// <summary>
        /// 线段检测
        /// </summary>
        /// <param name="start">起点</param>
        /// <param name="end">终点</param>
        /// <param name="layer">层级 (-1, 默认全检测)</param>
        /// <returns>结果</returns>
        public HitResult Linecast(FPVector3 start, FPVector3 end, int layer = -1)
        {
            // 计算射线方向和长度
            FPVector3 dire = end - start;
            return Raycast(start, dire.normalized, dire.magnitude, layer);
        }

        /// <summary>
        /// AABB 碰撞检测
        /// </summary>
        /// <param name="a">AABB-A</param>
        /// <param name="b">AABB-B</param>
        /// <returns>YES/NO</returns>
        public bool Detect(AABB a, AABB b)
        {
            FPVector3 min1 = a.position - a.size * FP.Half;
            FPVector3 max1 = a.position + a.size * FP.Half;
            FPVector3 min2 = b.position - b.size * FP.Half;
            FPVector3 max2 = b.position + b.size * FP.Half;

            return
            (
                min1.x < max2.x && max1.x > min2.x
                                &&
                min1.y < max2.y && max1.y > min2.y
                                &&
                min1.z < max2.z && max1.z > min2.z
            );
        }

        /// <summary>
        /// 检测两个碰撞盒之间的碰撞 (统一入口, 支持盒/球/胶囊/圆柱全组合)
        /// </summary>
        /// <param name="a">碰撞盒 A</param>
        /// <param name="aspatial">A 的空间信息</param>
        /// <param name="b">碰撞盒 B</param>
        /// <param name="bspatial">B 的空间信息</param>
        /// <param name="point">碰撞点</param>
        /// <param name="normal">从 B 指向 A 的法线</param>
        /// <param name="penetration">穿透深度</param>
        /// <returns>YES/NO</returns>
        public bool Detect(ColliderInfo a, SpatialInfo aspatial, ColliderInfo b, SpatialInfo bspatial,
            out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            return Detect(a, a.actor, aspatial, b, b.actor, bspatial, out point, out normal, out penetration);
        }

        /// <summary>
        /// 检测两个碰撞盒之间的碰撞, 支持指定各自的缩放
        /// </summary>
        /// <param name="a">碰撞盒 A</param>
        /// <param name="aactor">A 的 Actor (用于取默认缩放)</param>
        /// <param name="aspatial">A 的空间信息</param>
        /// <param name="b">碰撞盒 B</param>
        /// <param name="bactor">B 的 Actor (用于取默认缩放)</param>
        /// <param name="bspatial">B 的空间信息</param>
        /// <param name="point">碰撞点</param>
        /// <param name="normal">从 B 指向 A 的法线</param>
        /// <param name="penetration">穿透深度</param>
        /// <returns>YES/NO</returns>
        private bool Detect(ColliderInfo a, ulong aactor, SpatialInfo aspatial,
            ColliderInfo b, ulong bactor, SpatialInfo bspatial,
            out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            point = FPVector3.zero;
            normal = FPVector3.zero;
            penetration = FP.MaxValue;

            Transform transformA = MakeTransform(aspatial);
            Transform transformB = MakeTransform(bspatial);

            // 缩放由空间信息承载, 按 Actor 缩放复制一份碰撞盒后再检测
            FP scaleA = ScaleOf(aactor);
            FP scaleB = ScaleOf(bactor);
            ColliderInfo shapeA = scaleA != FP.One ? Scaled(a, scaleA) : a;
            ColliderInfo shapeB = scaleB != FP.One ? Scaled(b, scaleB) : b;

            return DetectionShape.DetectShapeShape(shapeA, transformA, shapeB, transformB,
                out point, out normal, out penetration);
        }

        /// <summary>
        /// 检测两个 Box 之间的碰撞
        /// </summary>
        /// <param name="a">BoxA</param>
        /// <param name="b">BoxB</param>
        /// <param name="apos">BoxA 位置</param>
        /// <param name="bpos">BoxB 位置</param>
        /// <param name="arot">BoxA 旋转</param>
        /// <param name="brot">BoxB 旋转</param>
        /// <param name="point">碰撞点</param>
        /// <param name="normal">从 BoxB 指向 BoxA 的法线</param>
        /// <param name="penetration">穿透深度</param>
        /// <returns>YES/NO</returns>
        public bool Detect(BoxShape a, BoxShape b, FPVector3 apos, FPVector3 bpos, FPQuaternion arot, FPQuaternion brot, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            return DetectionShape.DetectBoxBox(a, new Transform(apos, arot),
                b, new Transform(bpos, brot),
                out point, out normal, out penetration);
        }

        /// <summary>
        /// 检测两个球体之间的碰撞
        /// </summary>
        /// <param name="a">SphereA</param>
        /// <param name="b">SphereB</param>
        /// <param name="apos">SphereA 位置</param>
        /// <param name="bpos">SphereB 位置</param>
        /// <param name="point">碰撞点</param>
        /// <param name="normal">从 SphereB 指向 SphereA 的法线</param>
        /// <param name="penetration">穿透深度</param>
        /// <returns>YES/NO</returns>
        public bool Detect(SphereShape a, SphereShape b, FPVector3 apos, FPVector3 bpos, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            return DetectionShape.DetectSphereSphere(a, new Transform(apos, FPQuaternion.identity),
                b, new Transform(bpos, FPQuaternion.identity),
                out point, out normal, out penetration);
        }

        /// <summary>
        /// 检测 Box 和 Sphere 之间的碰撞
        /// </summary>
        /// <param name="a">Box</param>
        /// <param name="b">Sphere</param>
        /// <param name="apos">Box 位置</param>
        /// <param name="bpos">Sphere 位置</param>
        /// <param name="arot">Box 旋转</param>
        /// <param name="point">碰撞点</param>
        /// <param name="normal">从 Box 指向 Sphere 的法线</param>
        /// <param name="penetration">穿透深度</param>
        /// <returns>YES/NO</returns>
        public bool Detect(BoxShape a, SphereShape b, FPVector3 apos, FPVector3 bpos, FPQuaternion arot, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            // 参考实现约定: 球在 A 位, 盒在 B 位时法线由盒指向球
            bool hit = DetectionShape.DetectSphereBox(b, new Transform(bpos, FPQuaternion.identity),
                a, new Transform(apos, arot),
                out point, out normal, out penetration);
            if (hit) normal = -normal;
            return hit;
        }

        /// <summary>
        /// 检测 Box 和 Capsule 之间的碰撞
        /// </summary>
        public bool Detect(BoxShape a, CapsuleShape b, FPVector3 apos, FPVector3 bpos, FPQuaternion arot, FPQuaternion brot, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            return DetectionShape.DetectBoxCapsule(a, new Transform(apos, arot),
                b, new Transform(bpos, brot),
                out point, out normal, out penetration);
        }

        /// <summary>
        /// 检测 Box 和 Cylinder 之间的碰撞
        /// </summary>
        public bool Detect(BoxShape a, CylinderShape b, FPVector3 apos, FPVector3 bpos, FPQuaternion arot, FPQuaternion brot, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            return DetectionShape.DetectBoxCylinder(a, new Transform(apos, arot),
                b, new Transform(bpos, brot),
                out point, out normal, out penetration);
        }

        /// <summary>
        /// 检测 Sphere 和 Capsule 之间的碰撞
        /// </summary>
        public bool Detect(SphereShape a, CapsuleShape b, FPVector3 apos, FPVector3 bpos, FPQuaternion brot, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            return DetectionShape.DetectSphereCapsule(a, new Transform(apos, FPQuaternion.identity),
                b, new Transform(bpos, brot),
                out point, out normal, out penetration);
        }

        /// <summary>
        /// 检测 Sphere 和 Cylinder 之间的碰撞
        /// </summary>
        public bool Detect(SphereShape a, CylinderShape b, FPVector3 apos, FPVector3 bpos, FPQuaternion brot, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            return DetectionShape.DetectSphereCylinder(a, new Transform(apos, FPQuaternion.identity),
                b, new Transform(bpos, brot),
                out point, out normal, out penetration);
        }

        /// <summary>
        /// 检测 Capsule 和 Capsule 之间的碰撞
        /// </summary>
        public bool Detect(CapsuleShape a, CapsuleShape b, FPVector3 apos, FPVector3 bpos, FPQuaternion arot, FPQuaternion brot, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            return DetectionShape.DetectCapsuleCapsule(a, new Transform(apos, arot),
                b, new Transform(bpos, brot),
                out point, out normal, out penetration);
        }

        /// <summary>
        /// 检测 Capsule 和 Cylinder 之间的碰撞
        /// </summary>
        public bool Detect(CapsuleShape a, CylinderShape b, FPVector3 apos, FPVector3 bpos, FPQuaternion arot, FPQuaternion brot, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            return DetectionShape.DetectCapsuleCylinder(a, new Transform(apos, arot),
                b, new Transform(bpos, brot),
                out point, out normal, out penetration);
        }

        /// <summary>
        /// 检测 Cylinder 和 Cylinder 之间的碰撞
        /// </summary>
        public bool Detect(CylinderShape a, CylinderShape b, FPVector3 apos, FPVector3 bpos, FPQuaternion arot, FPQuaternion brot, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            return DetectionShape.DetectCylinderCylinder(a, new Transform(apos, arot),
                b, new Transform(bpos, brot),
                out point, out normal, out penetration);
        }

        /// <summary>
        /// Box 线段检测
        /// </summary>
        /// <param name="start">线段起点</param>
        /// <param name="end">线段终点</param>
        /// <param name="box">Box</param>
        /// <param name="position">Box 位置</param>
        /// <param name="rotation">Box 旋转</param>
        /// <param name="point">碰撞点</param>
        /// <param name="normal">线段接触 Box 的法线</param>
        /// <param name="penetration">起点到碰撞点的距离</param>
        /// <returns>YES/NO</returns>
        public bool Linecast(FPVector3 start, FPVector3 end, BoxShape box, FPVector3 position, FPQuaternion rotation, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            return DetectionShape.DetectLineBox(start, end, box, new Transform(position, rotation),
                out point, out normal, out penetration);
        }

        /// <summary>
        /// Sphere 线段检测
        /// </summary>
        /// <param name="start">线段起点</param>
        /// <param name="end">线段终点</param>
        /// <param name="sphere">Sphere</param>
        /// <param name="position">Sphere 位置</param>
        /// <param name="point">碰撞点</param>
        /// <param name="normal">线段接触 Sphere 的法线</param>
        /// <param name="penetration">起点到碰撞点的距离</param>
        /// <returns>YES/NO</returns>
        public bool Linecast(FPVector3 start, FPVector3 end, SphereShape sphere, FPVector3 position, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            return DetectionShape.DetectLineSphere(start, end, sphere, new Transform(position, FPQuaternion.identity),
                out point, out normal, out penetration);
        }

        /// <summary>
        /// Capsule 线段检测
        /// </summary>
        public bool Linecast(FPVector3 start, FPVector3 end, CapsuleShape capsule, FPVector3 position, FPQuaternion rotation, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            return DetectionShape.DetectLineCapsule(start, end, capsule, new Transform(position, rotation),
                out point, out normal, out penetration);
        }

        /// <summary>
        /// Cylinder 线段检测
        /// </summary>
        public bool Linecast(FPVector3 start, FPVector3 end, CylinderShape cylinder, FPVector3 position, FPQuaternion rotation, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            return DetectionShape.DetectLineCylinder(start, end, cylinder, new Transform(position, rotation),
                out point, out normal, out penetration);
        }

        /// <summary>
        /// Box 射线检测
        /// </summary>
        /// <param name="origin">射线起点</param>
        /// <param name="dire">射线方向</param>
        /// <param name="distance">射线距离</param>
        /// <param name="box">Box</param>
        /// <param name="position">Box 位置</param>
        /// <param name="rotation">Box 旋转</param>
        /// <param name="point">碰撞点</param>
        /// <param name="normal">射中 Box 的法线</param>
        /// <param name="penetration">起点到碰撞点的距离</param>
        /// <returns>YES/NO</returns>
        public bool Raycast(FPVector3 origin, FPVector3 dire, FP distance, BoxShape box, FPVector3 position, FPQuaternion rotation, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            return Linecast(origin, origin + dire * distance, box, position, rotation, out point, out normal, out penetration);
        }

        /// <summary>
        /// Sphere 射线检测
        /// </summary>
        /// <param name="origin">射线起点</param>
        /// <param name="dire">射线方向</param>
        /// <param name="distance">射线距离</param>
        /// <param name="sphere">Sphere</param>
        /// <param name="position">Sphere 位置</param>
        /// <param name="point">碰撞点</param>
        /// <param name="normal">射中 Sphere 的法线</param>
        /// <param name="penetration">起点到碰撞点的距离</param>
        /// <returns>YES/NO</returns>
        public bool Raycast(FPVector3 origin, FPVector3 dire, FP distance, SphereShape sphere, FPVector3 position, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            return Linecast(origin, origin + dire * distance, sphere, position, out point, out normal, out penetration);
        }

        /// <summary>
        /// 设置碰撞盒信息
        /// </summary>
        /// <param name="collider">碰撞盒</param>
        /// <param name="id">碰撞盒配置 ID</param>
        public void SetColliderInfo(ColliderInfo collider, int id)
        {
            if (false == stage.cfg.location.TbColliderData.TryGetValue(id, out var collidercfg)) return;
            if (null == collidercfg) return;

            FPVector3 offset = new FPVector3
            (
                collidercfg.Offset[0] * stage.cfg.int2fp,
                collidercfg.Offset[1] * stage.cfg.int2fp,
                collidercfg.Offset[2] * stage.cfg.int2fp
            );

            switch (collidercfg.Type)
            {
                case COLLISION_DEFINE.COLLIDER_BOX:
                    collider.shape = COLLISION_DEFINE.COLLIDER_BOX;
                    collider.box = new BoxShape
                    {
                        offset = offset,
                        size = new FPVector3(
                            collidercfg.ShapeData[0] * stage.cfg.int2fp,
                            collidercfg.ShapeData[1] * stage.cfg.int2fp,
                            collidercfg.ShapeData[2] * stage.cfg.int2fp
                        )
                    };
                    break;
                case COLLISION_DEFINE.COLLIDER_SPHERE:
                    collider.shape = COLLISION_DEFINE.COLLIDER_SPHERE;
                    collider.sphere = new SphereShape
                    {
                        offset = offset,
                        radius = collidercfg.ShapeData[0] * stage.cfg.int2fp
                    };
                    break;
                case COLLISION_DEFINE.COLLIDER_CAPSULE:
                    collider.shape = COLLISION_DEFINE.COLLIDER_CAPSULE;
                    collider.capsule = new CapsuleShape
                    {
                        offset = offset,
                        radius = collidercfg.ShapeData[0] * stage.cfg.int2fp,
                        height = collidercfg.ShapeData[1] * stage.cfg.int2fp
                    };
                    break;
                case COLLISION_DEFINE.COLLIDER_CYLINDER:
                    collider.shape = COLLISION_DEFINE.COLLIDER_CYLINDER;
                    collider.cylinder = new CylinderShape
                    {
                        offset = offset,
                        radius = collidercfg.ShapeData[0] * stage.cfg.int2fp,
                        height = collidercfg.ShapeData[1] * stage.cfg.int2fp
                    };
                    break;
                default:
                    return;
            }
        }

        /// <summary>
        /// 计算 AABB 包围盒
        /// </summary>
        /// <param name="id">ActorID</param>
        /// <param name="aabb">AABB 包围盒</param>
        /// <returns>YES/NO</returns>
        public bool CalcAABB(ulong id, out AABB aabb)
        {
            aabb = default;
            if (false == stage.SeekBehaviorInfo(id, out ColliderInfo collider)) return false;

            return CalcAABB(collider, out aabb);
        }

        /// <summary>
        /// 计算 AABB 包围盒
        /// </summary>
        /// <param name="collider">碰撞盒信息</param>
        /// <param name="aabb">AABB 包围盒</param>
        /// <returns>YES/NO</returns>
        public bool CalcAABB(ColliderInfo collider, out AABB aabb)
        {
            aabb = default;
            if (false == stage.SeekBehaviorInfo(collider.actor, out SpatialInfo spatial)) return false;

            FP scale = ScaleOf(collider.actor);
            ColliderInfo shape = scale != FP.One ? Scaled(collider, scale) : collider;

            Transform transform = MakeTransform(spatial);
            DetectionShape.ComputeBounds(shape, transform, out FPVector3 lower, out FPVector3 upper);

            aabb = new AABB
            {
                position = (lower + upper) * FP.Half,
                size = upper - lower
            };

            return true;
        }

        /// <summary>
        /// 由空间信息构造检测用变换
        /// </summary>
        private static Transform MakeTransform(SpatialInfo spatial) =>
            new Transform(spatial.position, FPQuaternion.Euler(spatial.euler));

        /// <summary>
        /// 按缩放复制一份碰撞盒 (脱离 Actor, 仅用于检测, 不进入对象池)
        /// </summary>
        /// <param name="collider">源碰撞盒</param>
        /// <param name="scale">缩放</param>
        /// <returns>缩放后的碰撞盒</returns>
        private static ColliderInfo Scaled(ColliderInfo collider, FP scale)
        {
            ColliderInfo copy = new ColliderInfo();
            copy.Ready(collider.actor);
            copy.layer = collider.layer;
            copy.shape = collider.shape;
            copy.box = new BoxShape
            {
                offset = collider.box.offset * scale,
                size = collider.box.size * scale
            };
            copy.sphere = new SphereShape
            {
                offset = collider.sphere.offset * scale,
                radius = collider.sphere.radius * scale
            };
            copy.capsule = new CapsuleShape
            {
                offset = collider.capsule.offset * scale,
                radius = collider.capsule.radius * scale,
                height = collider.capsule.height * scale
            };
            copy.cylinder = new CylinderShape
            {
                offset = collider.cylinder.offset * scale,
                radius = collider.cylinder.radius * scale,
                height = collider.cylinder.height * scale
            };
            return copy;
        }

        /// <summary>
        /// 取 Actor 的缩放, 无空间信息时按 1 处理
        /// </summary>
        private FP ScaleOf(ulong actor)
        {
            if (0 == actor) return FP.One;
            return stage.SeekBehaviorInfo(actor, out SpatialInfo spatial) ? spatial.scale : FP.One;
        }
    }
}
