using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 三维窄相检测。接触法线由形状 B 指向形状 A, 相切视为接触。
    /// 算法移植自已验证的 Phys3D Detection, 适配本项目定点数与形状结构。
    /// </summary>
    public static class DetectionShape
    {
        private static readonly FP Tolerance = FP.EN6;
        private static readonly FP AxisTolerance = FP.EN8;

        /// <summary>搜索最小重叠时的初始轴深度, 不能用 FP.MaxValue 作为哨兵 (浮点模式下其值为正无穷)。</summary>
        private static readonly FP Unbounded = (FP)1000;

        /// <summary>轴对齐包围盒, 用于宽相粗筛。</summary>
        private struct AABB
        {
            public FPVector3 lower;
            public FPVector3 upper;

            public AABB(FPVector3 lower, FPVector3 upper)
            {
                this.lower = lower;
                this.upper = upper;
            }

            public bool Overlaps(in AABB other) =>
                other.upper.X >= lower.X && other.upper.Y >= lower.Y && other.upper.Z >= lower.Z &&
                other.lower.X <= upper.X && other.lower.Y <= upper.Y && other.lower.Z <= upper.Z;
        }

        /// <summary>世界空间中的包围盒, 用于分离轴与支撑点计算。</summary>
        private struct OBB
        {
            public FPVector3 center;
            public FPVector3 half;
            public FPVector3 x;
            public FPVector3 y;
            public FPVector3 z;

            public OBB(BoxShape shape, in Transform transform)
            {
                center = transform.LocalToWorld(shape.Center);
                half = shape.HalfSize;
                x = Unit(transform.right, FPVector3.right);
                y = Unit(transform.up, FPVector3.up);
                z = Unit(transform.forward, FPVector3.forward);
            }

            public FPVector3 Axis(int index) => index == 0 ? x : index == 1 ? y : z;

            public FP ProjectRadius(FPVector3 axis) =>
                half.X * FP.Abs(FPVector3.Dot(x, axis))
                + half.Y * FP.Abs(FPVector3.Dot(y, axis))
                + half.Z * FP.Abs(FPVector3.Dot(z, axis));

            public FPVector3 SupportNear(FPVector3 direction, FPVector3 target)
            {
                FPVector3 offset = target - center;
                return center
                    + x * SupportCoordinate(FPVector3.Dot(x, direction), half.X, FPVector3.Dot(offset, x))
                    + y * SupportCoordinate(FPVector3.Dot(y, direction), half.Y, FPVector3.Dot(offset, y))
                    + z * SupportCoordinate(FPVector3.Dot(z, direction), half.Z, FPVector3.Dot(offset, z));
            }
        }

        /// <summary>世界空间中的圆柱, 局部长轴为 Y, 用于分离轴与支撑点计算。</summary>
        private struct Cylinder
        {
            public FPVector3 center;
            public FPVector3 axis;
            public FP halfHeight;
            public FP radius;

            public Cylinder(CylinderShape shape, in Transform transform)
            {
                center = transform.LocalToWorld(shape.Center);
                axis = Unit(transform.up, FPVector3.up);
                halfHeight = shape.HalfHeight;
                radius = shape.Radius;
            }

            /// <summary>圆柱在单位轴上的投影半径: 轴向半高投影加圆盘半径投影。</summary>
            public FP ProjectRadius(FPVector3 axis)
            {
                FP axial = FP.Abs(FPVector3.Dot(this.axis, axis));
                FP radial = SafeSqrt(FP.One - axial * axial);
                return halfHeight * axial + radius * radial;
            }

            public FPVector3 SupportNear(FPVector3 direction, FPVector3 target)
            {
                FPVector3 offset = target - center;
                FP axial = FPVector3.Dot(direction, axis);
                FP axialOffset = SupportCoordinate(axial, halfHeight, FPVector3.Dot(offset, axis));
                FPVector3 radial = direction - axis * axial;
                FPVector3 radialOffset = offset - axis * FPVector3.Dot(offset, axis);
                FPVector3 radialDirection = Length(radial) > Tolerance ? Unit(radial, FPVector3.right)
                    : Length(radialOffset) > Tolerance ? Unit(radialOffset, FPVector3.right)
                    : Perpendicular(axis);
                return center + axis * axialOffset + radialDirection * radius;
            }
        }

        #region 形状对检测

        /// <summary>
        /// 形状与形状的窄相检测
        /// </summary>
        /// <param name="a">形状 A</param>
        /// <param name="transformA">形状 A 的变换</param>
        /// <param name="b">形状 B</param>
        /// <param name="transformB">形状 B 的变换</param>
        /// <param name="point">碰撞点</param>
        /// <param name="normal">由 B 指向 A 的法线</param>
        /// <param name="penetration">穿透深度</param>
        /// <returns>是否碰撞</returns>
        public static bool DetectShapeShape(BaseShape a, in Transform transformA,
            BaseShape b, in Transform transformB, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            point = normal = FPVector3.zero;
            penetration = FP.Zero;
            if (null == a || null == b) return false;

            bool hit;
            switch (a.Type)
            {
                case ShapeType.Sphere:
                    switch (b.Type)
                    {
                        case ShapeType.Sphere:
                            return DetectSphereSphere((SphereShape)a, transformA, (SphereShape)b, transformB, out point, out normal, out penetration);
                        case ShapeType.Box:
                            return DetectSphereBox((SphereShape)a, transformA, (BoxShape)b, transformB, out point, out normal, out penetration);
                        case ShapeType.Capsule:
                            return DetectSphereCapsule((SphereShape)a, transformA, (CapsuleShape)b, transformB, out point, out normal, out penetration);
                        case ShapeType.Cylinder:
                            return DetectSphereCylinder((SphereShape)a, transformA, (CylinderShape)b, transformB, out point, out normal, out penetration);
                        default:
                            return false;
                    }
                case ShapeType.Box:
                    switch (b.Type)
                    {
                        case ShapeType.Sphere:
                            hit = DetectSphereBox((SphereShape)b, transformB, (BoxShape)a, transformA, out point, out normal, out penetration);
                            if (hit) normal = -normal;
                            return hit;
                        case ShapeType.Box:
                            return DetectBoxBox((BoxShape)a, transformA, (BoxShape)b, transformB, out point, out normal, out penetration);
                        case ShapeType.Capsule:
                            return DetectBoxCapsule((BoxShape)a, transformA, (CapsuleShape)b, transformB, out point, out normal, out penetration);
                        case ShapeType.Cylinder:
                            return DetectBoxCylinder((BoxShape)a, transformA, (CylinderShape)b, transformB, out point, out normal, out penetration);
                        default:
                            return false;
                    }
                case ShapeType.Capsule:
                    switch (b.Type)
                    {
                        case ShapeType.Sphere:
                            hit = DetectSphereCapsule((SphereShape)b, transformB, (CapsuleShape)a, transformA, out point, out normal, out penetration);
                            if (hit) normal = -normal;
                            return hit;
                        case ShapeType.Box:
                            hit = DetectBoxCapsule((BoxShape)b, transformB, (CapsuleShape)a, transformA, out point, out normal, out penetration);
                            if (hit) normal = -normal;
                            return hit;
                        case ShapeType.Capsule:
                            return DetectCapsuleCapsule((CapsuleShape)a, transformA, (CapsuleShape)b, transformB, out point, out normal, out penetration);
                        case ShapeType.Cylinder:
                            return DetectCapsuleCylinder((CapsuleShape)a, transformA, (CylinderShape)b, transformB, out point, out normal, out penetration);
                        default:
                            return false;
                    }
                case ShapeType.Cylinder:
                    switch (b.Type)
                    {
                        case ShapeType.Sphere:
                            hit = DetectSphereCylinder((SphereShape)b, transformB, (CylinderShape)a, transformA, out point, out normal, out penetration);
                            if (hit) normal = -normal;
                            return hit;
                        case ShapeType.Box:
                            hit = DetectBoxCylinder((BoxShape)b, transformB, (CylinderShape)a, transformA, out point, out normal, out penetration);
                            if (hit) normal = -normal;
                            return hit;
                        case ShapeType.Capsule:
                            hit = DetectCapsuleCylinder((CapsuleShape)b, transformB, (CylinderShape)a, transformA, out point, out normal, out penetration);
                            if (hit) normal = -normal;
                            return hit;
                        case ShapeType.Cylinder:
                            return DetectCylinderCylinder((CylinderShape)a, transformA, (CylinderShape)b, transformB, out point, out normal, out penetration);
                        default:
                            return false;
                    }
                default:
                    return false;
            }
        }

        public static bool DetectSphereSphere(SphereShape a, in Transform transformA,
            SphereShape b, in Transform transformB, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            FPVector3 centerA = transformA.LocalToWorld(a.Center);
            FPVector3 centerB = transformB.LocalToWorld(b.Center);
            return RoundedContact(centerA, a.Radius, centerB, b.Radius,
                Unit(centerA - centerB, FPVector3.up), out point, out normal, out penetration);
        }

        public static bool DetectSphereBox(SphereShape a, in Transform transformA,
            BoxShape b, in Transform transformB, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            point = normal = FPVector3.zero;
            penetration = FP.Zero;

            FPVector3 center = transformA.LocalToWorld(a.Center);
            FPVector3 local = transformB.WorldToLocal(center) - b.Center;
            FPVector3 half = b.HalfSize;
            FPVector3 closest = ClampToBox(local, half);
            FPVector3 delta = local - closest;
            FP distance = Length(delta);
            if (distance > a.Radius + Tolerance) return false;

            FPVector3 localNormal;
            if (distance > FP.Zero)
            {
                localNormal = Unit(delta, FPVector3.right);
                penetration = Max(FP.Zero, a.Radius - distance);
            }
            else
            {
                int axis = 0;
                FP faceDistance = half.X - FP.Abs(local.X);
                for (int i = 1; i < 3; ++i)
                {
                    FP candidate = half[i] - FP.Abs(local[i]);
                    if (candidate < faceDistance)
                    {
                        faceDistance = candidate;
                        axis = i;
                    }
                }
                localNormal = Basis(axis) * NonzeroSign(local[axis]);
                closest = local;
                closest[axis] = localNormal[axis] * half[axis];
                penetration = Max(FP.Zero, a.Radius + faceDistance);
            }

            normal = Unit(transformB.TransformDirection(localNormal), FPVector3.right);
            FPVector3 boxPoint = transformB.LocalToWorld(closest + b.Center);
            point = (boxPoint + center - normal * a.Radius) * FP.Half;
            return true;
        }

        public static bool DetectBoxBox(BoxShape a, in Transform transformA,
            BoxShape b, in Transform transformB, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            point = normal = FPVector3.zero;
            penetration = FP.Zero;

            OBB obbA = new OBB(a, transformA);
            OBB obbB = new OBB(b, transformB);
            FPVector3 centerDelta = obbA.center - obbB.center;
            FP bestDepth = Unbounded;
            FPVector3 bestNormal = FPVector3.right;

            for (int i = 0; i < 15; ++i)
            {
                FPVector3 axis = i < 3 ? obbA.Axis(i)
                    : i < 6 ? obbB.Axis(i - 3)
                    : FPVector3.Cross(obbA.Axis((i - 6) / 3), obbB.Axis((i - 6) % 3));
                if (MaxAbs(axis) <= AxisTolerance) continue;
                axis = Unit(axis, FPVector3.right);
                FP centerProjection = FPVector3.Dot(centerDelta, axis);
                FP depth = obbA.ProjectRadius(axis) + obbB.ProjectRadius(axis) - FP.Abs(centerProjection);
                if (depth < -Tolerance) return false;
                if (depth < bestDepth)
                {
                    bestDepth = depth;
                    bestNormal = centerProjection < FP.Zero ? -axis : axis;
                }
            }

            normal = bestNormal;
            penetration = Max(FP.Zero, bestDepth);

            FPVector3 pointA = obbA.SupportNear(-normal, obbB.center);
            FPVector3 pointB = obbB.SupportNear(normal, pointA);
            pointA = obbA.SupportNear(-normal, pointB);
            point = (pointA + pointB) * FP.Half;
            return true;
        }

        public static bool DetectSphereCapsule(SphereShape a, in Transform transformA,
            CapsuleShape b, in Transform transformB, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            FPVector3 center = transformA.LocalToWorld(a.Center);
            GetWorldEndpoints(b, transformB, out FPVector3 p0, out FPVector3 p1);
            FPVector3 closest = ClosestOnSegment(center, p0, p1);
            return RoundedContact(center, a.Radius, closest, b.Radius,
                Perpendicular(p1 - p0), out point, out normal, out penetration);
        }

        public static bool DetectCapsuleCapsule(CapsuleShape a, in Transform transformA,
            CapsuleShape b, in Transform transformB, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            GetWorldEndpoints(a, transformA, out FPVector3 a0, out FPVector3 a1);
            GetWorldEndpoints(b, transformB, out FPVector3 b0, out FPVector3 b1);
            ClosestSegments(a0, a1, b0, b1, out FPVector3 closestA, out FPVector3 closestB);

            FPVector3 axisA = Unit(a1 - a0, FPVector3.zero);
            FPVector3 axisB = Unit(b1 - b0, FPVector3.zero);
            FPVector3 fallback = FPVector3.Cross(axisA, axisB);
            if (MaxAbs(fallback) <= AxisTolerance)
            {
                FPVector3 axis = MaxAbs(axisA) > AxisTolerance ? axisA : axisB;
                FPVector3 centers = (a0 + a1 - b0 - b1) * FP.Half;
                fallback = centers - axis * FPVector3.Dot(centers, axis);
                fallback = Length(fallback) > Tolerance ? Unit(fallback, FPVector3.up) : Perpendicular(axis);
            }
            else
            {
                fallback = Unit(fallback, FPVector3.up);
                if (FPVector3.Dot(fallback, a0 + a1 - b0 - b1) < FP.Zero) fallback = -fallback;
            }

            return RoundedContact(closestA, a.Radius, closestB, b.Radius,
                fallback, out point, out normal, out penetration);
        }

        public static bool DetectBoxCapsule(BoxShape a, in Transform transformA,
            CapsuleShape b, in Transform transformB, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            point = normal = FPVector3.zero;
            penetration = FP.Zero;

            GetWorldEndpoints(b, transformB, out FPVector3 world0, out FPVector3 world1);
            FPVector3 p0 = transformA.WorldToLocal(world0) - a.Center;
            FPVector3 p1 = transformA.WorldToLocal(world1) - a.Center;
            FPVector3 half = a.HalfSize;
            ClosestSegmentBox(p0, p1, half, out FPVector3 segmentPoint, out FPVector3 boxPoint);
            FPVector3 delta = boxPoint - segmentPoint;
            FP distance = Length(delta);
            if (distance > b.Radius + Tolerance) return false;

            FPVector3 localNormal;
            FPVector3 capsulePoint;
            if (distance > Tolerance)
            {
                localNormal = Unit(delta, FPVector3.right);
                penetration = Max(FP.Zero, b.Radius - distance);
                capsulePoint = segmentPoint + localNormal * b.Radius;
            }
            else
            {
                FP bestDepth = Unbounded;
                localNormal = FPVector3.right;
                FPVector3 segmentAxis = Unit(p1 - p0, FPVector3.zero);
                for (int i = 0; i < 6; ++i)
                {
                    FPVector3 axis = i < 3 ? Basis(i) : FPVector3.Cross(segmentAxis, Basis(i - 3));
                    if (MaxAbs(axis) <= AxisTolerance) continue;
                    axis = Unit(axis, FPVector3.right);
                    FP radius = FPVector3.Dot(half, FPVector3.Abs(axis));
                    FP projection0 = FPVector3.Dot(p0, axis);
                    FP projection1 = FPVector3.Dot(p1, axis);
                    FP positive = radius + Max(projection0, projection1) + b.Radius;
                    FP negative = radius - Min(projection0, projection1) + b.Radius;
                    FP depth = Min(positive, negative);
                    if (depth < bestDepth)
                    {
                        bestDepth = depth;
                        localNormal = positive <= negative ? axis : -axis;
                    }
                }
                penetration = Max(FP.Zero, bestDepth);
                FP along = FPVector3.Dot(p1 - p0, localNormal);
                segmentPoint = FP.Abs(along) <= Tolerance ? ClosestOnSegment(FPVector3.zero, p0, p1)
                    : along > FP.Zero ? p1 : p0;
                capsulePoint = segmentPoint + localNormal * b.Radius;
                boxPoint = new FPVector3(SupportCoordinate(-localNormal.X, half.X, segmentPoint.X),
                    SupportCoordinate(-localNormal.Y, half.Y, segmentPoint.Y),
                    SupportCoordinate(-localNormal.Z, half.Z, segmentPoint.Z));
            }

            normal = Unit(transformA.TransformDirection(localNormal), FPVector3.right);
            point = transformA.LocalToWorld((boxPoint + capsulePoint) * FP.Half + a.Center);
            return true;
        }

        /// <summary>球与圆柱: 解析最近点。圆柱内部按端面/侧面的最小平移方向处理深度穿透。</summary>
        public static bool DetectSphereCylinder(SphereShape a, in Transform transformA,
            CylinderShape b, in Transform transformB, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            point = normal = FPVector3.zero;
            penetration = FP.Zero;

            FPVector3 center = transformA.LocalToWorld(a.Center);
            FPVector3 local = transformB.WorldToLocal(center) - b.Center;
            FPVector3 radial = new FPVector3(local.X, FP.Zero, local.Z);
            FP radialDistance = Length(radial);
            FP radius = b.Radius;
            FP halfHeight = b.HalfHeight;

            FPVector3 localNormal;
            FPVector3 closest;
            if (radialDistance > radius)
            {
                FPVector3 clamped = radial * (radius / radialDistance);
                closest = new FPVector3(clamped.X, FP.Clamp(local.Y, -halfHeight, halfHeight), clamped.Z);
                FPVector3 delta = local - closest;
                FP distance = Length(delta);
                if (distance > a.Radius + Tolerance) return false;
                localNormal = distance > FP.Zero ? Unit(delta, FPVector3.up) : FPVector3.up;
                penetration = Max(FP.Zero, a.Radius - distance);
            }
            else if (FP.Abs(local.Y) > halfHeight)
            {
                FP sign = local.Y < FP.Zero ? -FP.One : FP.One;
                closest = new FPVector3(local.X, halfHeight * sign, local.Z);
                FP distance = FP.Abs(local.Y) - halfHeight;
                if (distance > a.Radius + Tolerance) return false;
                localNormal = FPVector3.up * sign;
                penetration = Max(FP.Zero, a.Radius - distance);
            }
            else
            {
                FP capDepth = halfHeight - FP.Abs(local.Y);
                FP radialDepth = radius - radialDistance;
                if (capDepth < radialDepth)
                {
                    FP sign = local.Y < FP.Zero ? -FP.One : FP.One;
                    localNormal = FPVector3.up * sign;
                    closest = new FPVector3(local.X, halfHeight * sign, local.Z);
                    penetration = a.Radius + capDepth;
                }
                else
                {
                    localNormal = radialDistance > Tolerance ? Unit(radial, FPVector3.right) : FPVector3.right;
                    closest = new FPVector3(localNormal.X * radius, local.Y, localNormal.Z * radius);
                    penetration = a.Radius + radialDepth;
                }
            }

            normal = Unit(transformB.TransformDirection(localNormal), FPVector3.up);
            FPVector3 cylinderPoint = transformB.LocalToWorld(closest + b.Center);
            point = (cylinderPoint + center - normal * a.Radius) * FP.Half;
            return true;
        }

        /// <summary>
        /// 盒与圆柱: 在盒三轴、圆柱轴、轴叉积及径向候选轴上做分离轴测试。
        /// 圆柱投影半径解析精确, 接触点用双向支撑点近似。
        /// </summary>
        public static bool DetectBoxCylinder(BoxShape a, in Transform transformA,
            CylinderShape b, in Transform transformB, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            point = normal = FPVector3.zero;
            penetration = FP.Zero;

            OBB box = new OBB(a, transformA);
            Cylinder cylinder = new Cylinder(b, transformB);
            FPVector3 delta = box.center - cylinder.center;
            FP bestDepth = Unbounded;
            FPVector3 bestNormal = FPVector3.right;

            for (int i = 0; i < 3; ++i)
            {
                FPVector3 axis = box.Axis(i);
                if (!TestAxis(axis, delta, box.ProjectRadius(axis) + cylinder.ProjectRadius(axis),
                    ref bestDepth, ref bestNormal)) return false;
            }

            FPVector3 cylinderAxis = cylinder.axis;
            if (!TestAxis(cylinderAxis, delta, box.ProjectRadius(cylinderAxis) + cylinder.ProjectRadius(cylinderAxis),
                ref bestDepth, ref bestNormal)) return false;

            for (int i = 0; i < 3; ++i)
            {
                FPVector3 cross = FPVector3.Cross(cylinderAxis, box.Axis(i));
                if (MaxAbs(cross) <= AxisTolerance) continue;
                cross = Unit(cross, FPVector3.right);
                if (!TestAxis(cross, delta, box.ProjectRadius(cross) + cylinder.ProjectRadius(cross),
                    ref bestDepth, ref bestNormal)) return false;
            }

            FPVector3 radial = delta - cylinderAxis * FPVector3.Dot(delta, cylinderAxis);
            if (Length(radial) > Tolerance)
            {
                radial = Unit(radial, FPVector3.right);
                if (!TestAxis(radial, delta, box.ProjectRadius(radial) + cylinder.ProjectRadius(radial),
                    ref bestDepth, ref bestNormal)) return false;
            }

            FPVector3[] seeds = s_AxisSeeds ?? (s_AxisSeeds = new FPVector3[8]);
            int seedCount = 0;
            seeds[seedCount++] = bestNormal;
            seeds[seedCount++] = box.Axis(0);
            seeds[seedCount++] = box.Axis(1);
            seeds[seedCount++] = box.Axis(2);
            seeds[seedCount++] = cylinderAxis;
            for (int i = 0; i < 3; ++i)
            {
                FPVector3 cross = FPVector3.Cross(cylinderAxis, box.Axis(i));
                if (MaxAbs(cross) > AxisTolerance) seeds[seedCount++] = Unit(cross, FPVector3.right);
            }

            if (!RefineSeparatingAxis(axis => box.ProjectRadius(axis) + cylinder.ProjectRadius(axis)
                    - FP.Abs(FPVector3.Dot(delta, axis)),
                seeds, seedCount, ref bestDepth, ref bestNormal)) return false;

            return ResolveConvexContact(a, transformA, b, transformB, bestNormal, bestDepth,
                out point, out normal, out penetration);
        }

        /// <summary>胶囊与圆柱: 候选轴快速排除分离, 剩余情形由轴精化精确处理。</summary>
        public static bool DetectCapsuleCylinder(CapsuleShape a, in Transform transformA,
            CylinderShape b, in Transform transformB, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            point = normal = FPVector3.zero;
            penetration = FP.Zero;

            GetWorldEndpoints(a, transformA, out FPVector3 a0, out FPVector3 a1);
            FPVector3 capsuleCenter = (a0 + a1) * FP.Half;
            FPVector3 capsuleAxis = Unit(a1 - a0, FPVector3.zero);
            FP capsuleHalf = Length(a1 - a0) * FP.Half;
            Cylinder cylinder = new Cylinder(b, transformB);
            FPVector3 delta = capsuleCenter - cylinder.center;
            FP bestDepth = Unbounded;
            FPVector3 bestNormal = FPVector3.right;

            FP capsuleRadius = capsuleHalf * FP.Abs(FPVector3.Dot(capsuleAxis, cylinder.axis)) + a.Radius;
            if (!TestAxis(cylinder.axis, delta, cylinder.ProjectRadius(cylinder.axis) + capsuleRadius,
                ref bestDepth, ref bestNormal)) return false;

            if (MaxAbs(capsuleAxis) > AxisTolerance)
            {
                capsuleRadius = capsuleHalf + a.Radius;
                if (!TestAxis(capsuleAxis, delta, cylinder.ProjectRadius(capsuleAxis) + capsuleRadius,
                    ref bestDepth, ref bestNormal)) return false;

                FPVector3 cross = FPVector3.Cross(capsuleAxis, cylinder.axis);
                if (MaxAbs(cross) > AxisTolerance)
                {
                    cross = Unit(cross, FPVector3.right);
                    capsuleRadius = capsuleHalf * FP.Abs(FPVector3.Dot(capsuleAxis, cross)) + a.Radius;
                    if (!TestAxis(cross, delta, cylinder.ProjectRadius(cross) + capsuleRadius,
                        ref bestDepth, ref bestNormal)) return false;
                }

                FPVector3 radial = delta - capsuleAxis * FPVector3.Dot(delta, capsuleAxis);
                if (Length(radial) > Tolerance)
                {
                    radial = Unit(radial, FPVector3.right);
                    capsuleRadius = capsuleHalf * FP.Abs(FPVector3.Dot(capsuleAxis, radial)) + a.Radius;
                    if (!TestAxis(radial, delta, cylinder.ProjectRadius(radial) + capsuleRadius,
                        ref bestDepth, ref bestNormal)) return false;
                }
            }

            FPVector3 cylinderRadial = delta - cylinder.axis * FPVector3.Dot(delta, cylinder.axis);
            if (Length(cylinderRadial) > Tolerance)
            {
                cylinderRadial = Unit(cylinderRadial, FPVector3.right);
                capsuleRadius = capsuleHalf * FP.Abs(FPVector3.Dot(capsuleAxis, cylinderRadial)) + a.Radius;
                if (!TestAxis(cylinderRadial, delta, cylinder.ProjectRadius(cylinderRadial) + capsuleRadius,
                    ref bestDepth, ref bestNormal)) return false;
            }

            FPVector3[] seeds = s_AxisSeeds ?? (s_AxisSeeds = new FPVector3[8]);
            int seedCount = 0;
            seeds[seedCount++] = bestNormal;
            seeds[seedCount++] = capsuleAxis;
            seeds[seedCount++] = cylinder.axis;
            FPVector3 capsuleCross = FPVector3.Cross(capsuleAxis, cylinder.axis);
            if (MaxAbs(capsuleCross) > AxisTolerance)
                seeds[seedCount++] = Unit(capsuleCross, FPVector3.right);

            if (!RefineSeparatingAxis(axis => cylinder.ProjectRadius(axis)
                    + capsuleHalf * FP.Abs(FPVector3.Dot(capsuleAxis, axis)) + a.Radius
                    - FP.Abs(FPVector3.Dot(delta, axis)),
                seeds, seedCount, ref bestDepth, ref bestNormal)) return false;

            return ResolveConvexContact(a, transformA, b, transformB, bestNormal, bestDepth,
                out point, out normal, out penetration);
        }

        /// <summary>圆柱与圆柱: 候选轴快速排除分离, 剩余情形由轴精化精确处理。</summary>
        public static bool DetectCylinderCylinder(CylinderShape a, in Transform transformA,
            CylinderShape b, in Transform transformB, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            point = normal = FPVector3.zero;
            penetration = FP.Zero;

            Cylinder ca = new Cylinder(a, transformA);
            Cylinder cb = new Cylinder(b, transformB);
            FPVector3 delta = ca.center - cb.center;
            FP bestDepth = Unbounded;
            FPVector3 bestNormal = FPVector3.right;

            if (!TestAxis(ca.axis, delta, ca.ProjectRadius(ca.axis) + cb.ProjectRadius(ca.axis),
                ref bestDepth, ref bestNormal)) return false;
            if (!TestAxis(cb.axis, delta, ca.ProjectRadius(cb.axis) + cb.ProjectRadius(cb.axis),
                ref bestDepth, ref bestNormal)) return false;

            FPVector3 cross = FPVector3.Cross(ca.axis, cb.axis);
            if (MaxAbs(cross) > AxisTolerance)
            {
                cross = Unit(cross, FPVector3.right);
                if (!TestAxis(cross, delta, ca.ProjectRadius(cross) + cb.ProjectRadius(cross),
                    ref bestDepth, ref bestNormal)) return false;
            }

            FPVector3 radialA = delta - ca.axis * FPVector3.Dot(delta, ca.axis);
            if (Length(radialA) > Tolerance)
            {
                radialA = Unit(radialA, FPVector3.right);
                if (!TestAxis(radialA, delta, ca.ProjectRadius(radialA) + cb.ProjectRadius(radialA),
                    ref bestDepth, ref bestNormal)) return false;
            }

            FPVector3 radialB = delta - cb.axis * FPVector3.Dot(delta, cb.axis);
            if (Length(radialB) > Tolerance)
            {
                radialB = Unit(radialB, FPVector3.right);
                if (!TestAxis(radialB, delta, ca.ProjectRadius(radialB) + cb.ProjectRadius(radialB),
                    ref bestDepth, ref bestNormal)) return false;
            }

            FPVector3[] seeds = s_AxisSeeds ?? (s_AxisSeeds = new FPVector3[8]);
            int seedCount = 0;
            seeds[seedCount++] = bestNormal;
            seeds[seedCount++] = ca.axis;
            seeds[seedCount++] = cb.axis;
            if (MaxAbs(cross) > AxisTolerance) seeds[seedCount++] = Unit(cross, FPVector3.right);

            if (!RefineSeparatingAxis(axis => ca.ProjectRadius(axis) + cb.ProjectRadius(axis)
                    - FP.Abs(FPVector3.Dot(delta, axis)),
                seeds, seedCount, ref bestDepth, ref bestNormal)) return false;

            return ResolveConvexContact(a, transformA, b, transformB, bestNormal, bestDepth,
                out point, out normal, out penetration);
        }

        #endregion

        #region 线与形状检测

        /// <summary>
        /// 线段与形状检测
        /// </summary>
        /// <param name="start">起点</param>
        /// <param name="end">终点</param>
        /// <param name="shape">形状</param>
        /// <param name="transform">形状变换</param>
        /// <param name="point">碰撞点</param>
        /// <param name="normal">由形状指向线段外侧的法线</param>
        /// <param name="distance">起点到碰撞点的距离</param>
        /// <returns>是否碰撞</returns>
        public static bool DetectLineShape(FPVector3 start, FPVector3 end, BaseShape shape,
            in Transform transform, out FPVector3 point, out FPVector3 normal, out FP distance)
        {
            point = normal = FPVector3.zero;
            distance = FP.Zero;
            if (null == shape) return false;

            switch (shape.Type)
            {
                case ShapeType.Sphere:
                    return DetectLineSphere(start, end, (SphereShape)shape, transform, out point, out normal, out distance);
                case ShapeType.Box:
                    return DetectLineBox(start, end, (BoxShape)shape, transform, out point, out normal, out distance);
                case ShapeType.Capsule:
                    return DetectLineCapsule(start, end, (CapsuleShape)shape, transform, out point, out normal, out distance);
                case ShapeType.Cylinder:
                    return DetectLineCylinder(start, end, (CylinderShape)shape, transform, out point, out normal, out distance);
                default:
                    return false;
            }
        }

        public static bool DetectLineSphere(FPVector3 start, FPVector3 end, SphereShape shape,
            in Transform transform, out FPVector3 point, out FPVector3 normal, out FP distance)
        {
            point = normal = FPVector3.zero;
            distance = FP.Zero;

            FPVector3 delta = end - start;
            FP length = Length(delta);
            if (length == FP.Zero) return false;
            FPVector3 direction = Unit(delta, FPVector3.forward);
            FPVector3 center = transform.LocalToWorld(shape.Center);
            FP startDistance = Length(start - center);
            if (startDistance == shape.Radius)
            {
                point = start;
                normal = Unit(start - center, -direction);
                return true;
            }
            if (!SphereRoots(start - center, direction, shape.Radius, out FP entry, out FP exit)) return false;
            FP hit = startDistance < shape.Radius ? exit : entry >= FP.Zero ? entry : exit;
            if (hit < FP.Zero || hit > length + Tolerance) return false;
            distance = FP.Clamp(hit, FP.Zero, length);
            point = start + direction * distance;
            normal = Unit(point - center, -direction);
            return true;
        }

        public static bool DetectLineBox(FPVector3 start, FPVector3 end, BoxShape shape,
            in Transform transform, out FPVector3 point, out FPVector3 normal, out FP distance)
        {
            point = normal = FPVector3.zero;
            distance = FP.Zero;

            FP length = Length(end - start);
            if (length == FP.Zero) return false;
            FPVector3 origin = transform.WorldToLocal(start) - shape.Center;
            FPVector3 delta = transform.InverseTransformDirection(end - start);
            FPVector3 half = shape.HalfSize;
            bool inside = IsInsideBox(origin, half);
            FP enter = FP.Zero;
            FP exit = FP.One;
            FPVector3 enterNormal = FPVector3.zero;
            FPVector3 exitNormal = FPVector3.zero;
            FPVector3 boundaryNormal = FPVector3.zero;

            for (int i = 0; i < 3; ++i)
            {
                FP o = origin[i];
                FP d = delta[i];
                FP h = half[i];
                if (inside && FP.Abs(o) == h && boundaryNormal == FPVector3.zero)
                    boundaryNormal = Basis(i) * NonzeroSign(o);
                if (d == FP.Zero)
                {
                    if (o < -h || o > h) return false;
                    continue;
                }
                FP sign = NonzeroSign(d);
                FP speed = FP.Abs(d);
                FP nearNumerator = -h - o * sign;
                FP farNumerator = h - o * sign;
                if (nearNumerator > speed || farNumerator < FP.Zero) return false;
                if (nearNumerator >= FP.Zero)
                {
                    FP value = nearNumerator / speed;
                    if (value >= enter)
                    {
                        enter = value;
                        enterNormal = -Basis(i) * sign;
                    }
                }
                if (farNumerator <= speed)
                {
                    FP value = farNumerator / speed;
                    if (value <= exit)
                    {
                        exit = value;
                        exitNormal = Basis(i) * sign;
                    }
                }
                if (enter > exit + Tolerance) return false;
            }

            FP fraction;
            FPVector3 localNormal;
            if (inside && boundaryNormal != FPVector3.zero)
            {
                fraction = FP.Zero;
                localNormal = boundaryNormal;
            }
            else if (inside)
            {
                if (exitNormal == FPVector3.zero) return false;
                fraction = exit;
                localNormal = exitNormal;
            }
            else
            {
                if (enterNormal == FPVector3.zero) return false;
                fraction = enter;
                localNormal = enterNormal;
            }

            distance = length * fraction;
            point = start + (end - start) * fraction;
            normal = Unit(transform.TransformDirection(localNormal), FPVector3.right);
            return true;
        }

        public static bool DetectLineCapsule(FPVector3 start, FPVector3 end, CapsuleShape shape,
            in Transform transform, out FPVector3 point, out FPVector3 normal, out FP distance)
        {
            point = normal = FPVector3.zero;
            distance = FP.Zero;

            FP length = Length(end - start);
            if (length == FP.Zero) return false;
            FPVector3 origin = transform.WorldToLocal(start) - shape.Center;
            FPVector3 direction = Unit(transform.InverseTransformDirection(end - start), FPVector3.forward);
            FP best = Unbounded;
            FPVector3 bestNormal = FPVector3.zero;
            FP half = shape.HalfLength;
            FP radius = shape.Radius;

            FPVector3 startOffset = origin - FPVector3.up * FP.Clamp(origin.Y, -half, half);
            if (Length(startOffset) == radius)
            {
                point = start;
                normal = Unit(transform.TransformDirection(Unit(startOffset, -direction)), FPVector3.up);
                return true;
            }

            FPVector3 radialDirection = new FPVector3(direction.X, FP.Zero, direction.Z);
            FP radialSpeed = Length(radialDirection);
            if (radialSpeed > FP.Zero)
            {
                FPVector3 radialOrigin = new FPVector3(origin.X, FP.Zero, origin.Z);
                if (SphereRoots(radialOrigin, Unit(radialDirection, FPVector3.right), radius, out FP entry, out FP exit))
                {
                    ConsiderCylinderRoot(entry, radialSpeed, origin, direction, half, length, ref best, ref bestNormal);
                    ConsiderCylinderRoot(exit, radialSpeed, origin, direction, half, length, ref best, ref bestNormal);
                }
            }

            for (int i = 0; i < 2; ++i)
            {
                FP sign = i == 0 ? -FP.One : FP.One;
                FPVector3 center = FPVector3.up * (half * sign);
                if (!SphereRoots(origin - center, direction, radius, out FP entry, out FP exit)) continue;
                ConsiderCapRoot(entry, origin, direction, center, sign, length, ref best, ref bestNormal);
                ConsiderCapRoot(exit, origin, direction, center, sign, length, ref best, ref bestNormal);
            }

            if (best == Unbounded) return false;
            distance = best;
            point = start + Unit(end - start, FPVector3.forward) * distance;
            normal = Unit(transform.TransformDirection(bestNormal), FPVector3.up);
            return true;
        }

        /// <summary>线段与有限圆柱: 侧面二次方程加两个端面, 取最近的正向交点; 起点在内部时自然得到出射点。</summary>
        public static bool DetectLineCylinder(FPVector3 start, FPVector3 end, CylinderShape shape,
            in Transform transform, out FPVector3 point, out FPVector3 normal, out FP distance)
        {
            point = normal = FPVector3.zero;
            distance = FP.Zero;

            FP length = Length(end - start);
            if (length == FP.Zero) return false;
            FPVector3 origin = transform.WorldToLocal(start) - shape.Center;
            FPVector3 delta = transform.InverseTransformDirection(end - start);
            FP radius = shape.Radius;
            FP halfHeight = shape.HalfHeight;
            FP best = Unbounded;
            FPVector3 bestNormal = FPVector3.zero;

            FP a = delta.X * delta.X + delta.Z * delta.Z;
            if (a > AxisTolerance)
            {
                FP b = FP.Two * (origin.X * delta.X + origin.Z * delta.Z);
                FP c = origin.X * origin.X + origin.Z * origin.Z - radius * radius;
                FP discriminant = b * b - FP.Two * FP.Two * a * c;
                if (discriminant >= FP.Zero)
                {
                    FP root = SafeSqrt(discriminant);
                    FP inverse = FP.Half / a;
                    ConsiderCylinderSide((-b - root) * inverse, origin, delta, halfHeight, ref best, ref bestNormal);
                    ConsiderCylinderSide((-b + root) * inverse, origin, delta, halfHeight, ref best, ref bestNormal);
                }
            }

            if (FP.Abs(delta.Y) > AxisTolerance)
            {
                for (int i = 0; i < 2; ++i)
                {
                    FP sign = i == 0 ? -FP.One : FP.One;
                    FP t = (halfHeight * sign - origin.Y) / delta.Y;
                    FP x = origin.X + delta.X * t;
                    FP z = origin.Z + delta.Z * t;
                    if (x * x + z * z > radius * radius + Tolerance) continue;
                    ConsiderCylinderCap(t, sign, ref best, ref bestNormal);
                }
            }

            if (best == Unbounded) return false;
            distance = length * best;
            point = start + (end - start) * best;
            normal = Unit(transform.TransformDirection(bestNormal), FPVector3.up);
            return true;
        }

        private static void ConsiderCylinderSide(FP t, FPVector3 origin, FPVector3 delta, FP halfHeight,
            ref FP best, ref FPVector3 bestNormal)
        {
            if (t < FP.Zero || t > FP.One || t >= best) return;
            FP y = origin.Y + delta.Y * t;
            if (y < -halfHeight - Tolerance || y > halfHeight + Tolerance) return;
            FPVector3 radial = new FPVector3(origin.X + delta.X * t, FP.Zero, origin.Z + delta.Z * t);
            if (Length(radial) <= Tolerance) return;
            best = t;
            bestNormal = Unit(radial, FPVector3.right);
        }

        private static void ConsiderCylinderCap(FP t, FP sign, ref FP best, ref FPVector3 bestNormal)
        {
            if (t < FP.Zero || t > FP.One || t >= best) return;
            best = t;
            bestNormal = FPVector3.up * sign;
        }

        private static void ConsiderCylinderRoot(FP radialDistance, FP radialSpeed, FPVector3 origin,
            FPVector3 direction, FP halfLength, FP length, ref FP best, ref FPVector3 bestNormal)
        {
            FP radialLimit = radialSpeed * length;
            if (radialDistance < FP.Zero || radialDistance > radialLimit + radialSpeed * Tolerance) return;
            FP t = radialDistance >= radialLimit ? length : radialDistance / radialSpeed;
            if (t >= best) return;
            FPVector3 hit = origin + direction * t;
            if (hit.Y < -halfLength - Tolerance || hit.Y > halfLength + Tolerance) return;
            best = t;
            bestNormal = Unit(new FPVector3(hit.X, FP.Zero, hit.Z), -direction);
        }

        private static void ConsiderCapRoot(FP t, FPVector3 origin, FPVector3 direction,
            FPVector3 center, FP sign, FP length, ref FP best, ref FPVector3 bestNormal)
        {
            if (t < FP.Zero || t > length + Tolerance || t >= best) return;
            t = Min(t, length);
            FPVector3 delta = origin + direction * t - center;
            if (delta.Y * sign < -Tolerance) return;
            best = t;
            bestNormal = Unit(delta, -direction);
        }

        #endregion

        #region 宽相与包围盒

        /// <summary>
        /// 计算形状在世界空间中的轴对齐包围盒
        /// </summary>
        /// <param name="shape">形状</param>
        /// <param name="transform">变换</param>
        /// <returns>包围盒最小/最大点</returns>
        public static void ComputeBounds(BaseShape shape, in Transform transform,
            out FPVector3 lower, out FPVector3 upper)
        {
            lower = upper = transform.LocalToWorld(shape.Center);
            switch (shape.Type)
            {
                case ShapeType.Sphere:
                {
                    FPVector3 radius = new FPVector3(((SphereShape)shape).Radius);
                    lower -= radius;
                    upper += radius;
                    break;
                }
                case ShapeType.Box:
                {
                    BoxShape box = (BoxShape)shape;
                    FPVector3 half = box.HalfSize;
                    FPVector3 x = transform.right * half.X;
                    FPVector3 y = transform.up * half.Y;
                    FPVector3 z = transform.forward * half.Z;
                    FPVector3 extents = new FPVector3(
                        FP.Abs(x.X) + FP.Abs(y.X) + FP.Abs(z.X),
                        FP.Abs(x.Y) + FP.Abs(y.Y) + FP.Abs(z.Y),
                        FP.Abs(x.Z) + FP.Abs(y.Z) + FP.Abs(z.Z));
                    lower -= extents;
                    upper += extents;
                    break;
                }
                case ShapeType.Capsule:
                {
                    CapsuleShape capsule = (CapsuleShape)shape;
                    GetWorldEndpoints(capsule, transform, out FPVector3 p0, out FPVector3 p1);
                    FPVector3 radius = new FPVector3(capsule.Radius);
                    lower = FPVector3.Min(p0, p1) - radius;
                    upper = FPVector3.Max(p0, p1) + radius;
                    break;
                }
                case ShapeType.Cylinder:
                {
                    CylinderShape cylinder = (CylinderShape)shape;
                    FPVector3 axis = transform.up;
                    FPVector3 extents = new FPVector3(
                        CylinderExtent(cylinder, axis.X),
                        CylinderExtent(cylinder, axis.Y),
                        CylinderExtent(cylinder, axis.Z));
                    lower -= extents;
                    upper += extents;
                    break;
                }
            }
        }

        /// <summary>
        /// 两个形状在世界空间中是否可能重叠, 用于窄相之前的 AABB 粗筛
        /// </summary>
        public static bool Overlaps(BaseShape a, in Transform transformA, BaseShape b, in Transform transformB)
        {
            if (null == a || null == b) return false;
            ComputeBounds(a, transformA, out FPVector3 al, out FPVector3 au);
            ComputeBounds(b, transformB, out FPVector3 bl, out FPVector3 bu);
            return new AABB(al, au).Overlaps(new AABB(bl, bu));
        }

        /// <summary>轴向半高在该轴上的投影与圆盘在该轴上的投影之和, 旋转时精确包围。</summary>
        private static FP CylinderExtent(CylinderShape shape, FP component)
        {
            FP radial = SafeSqrt(FP.One - component * component);
            return shape.HalfHeight * FP.Abs(component) + shape.Radius * radial;
        }

        #endregion

        #region 内部实现

        /// <summary>凸体投影重叠函数: 给定单位轴返回两体投影重叠量(负值为间隙)。</summary>
        private delegate FP OverlapOnAxis(FPVector3 axis);

        /// <summary>切平面内的 8 个搜索方向, 避免依赖三角函数查表。</summary>
        private static readonly FP s_Diagonal = (FP)0.70710678f;

        /// <summary>候选轴种子缓冲, 避免每次检测分配数组。</summary>
        [System.ThreadStatic] private static FPVector3[] s_AxisSeeds;

        /// <summary>
        /// 候选轴只覆盖少数特征方向, 对曲边凸体可能漏掉真实分离轴。
        /// 这里以候选轴为种子, 在球面上做模式搜索求最小重叠量, 把假阳性收敛为真实间隙。
        /// 搜索一旦得到负重叠即确认分离; 否则返回精化后的最小穿透轴。
        /// </summary>
        private static bool RefineSeparatingAxis(OverlapOnAxis overlap, FPVector3[] seeds, int seedCount,
            ref FP bestDepth, ref FPVector3 bestNormal)
        {
            for (int i = 0; i < seedCount; ++i)
            {
                FPVector3 axis = Unit(seeds[i], FPVector3.right);
                if (overlap(axis) < -Tolerance) return false;
            }

            FPVector3 current = Unit(bestNormal, FPVector3.up);
            FP currentDepth = overlap(current);
            if (currentDepth < -Tolerance) return false;
            FPVector3 tangent = Perpendicular(current);
            FPVector3 bitangent = FPVector3.Cross(current, tangent);
            FP step = FP.Half;
            for (int iteration = 0; iteration < 32; ++iteration)
            {
                bool improved = false;
                for (int i = 0; i < 8; ++i)
                {
                    FPVector3 offset = i switch
                    {
                        0 => tangent,
                        1 => tangent * s_Diagonal + bitangent * s_Diagonal,
                        2 => bitangent,
                        3 => -tangent * s_Diagonal + bitangent * s_Diagonal,
                        4 => -tangent,
                        5 => -tangent * s_Diagonal - bitangent * s_Diagonal,
                        6 => -bitangent,
                        _ => tangent * s_Diagonal - bitangent * s_Diagonal
                    };
                    FPVector3 candidate = Unit(current + offset * step, current);
                    FP depth = overlap(candidate);
                    if (depth < -Tolerance) return false;
                    if (depth >= currentDepth) continue;
                    currentDepth = depth;
                    current = candidate;
                    tangent = Perpendicular(current);
                    bitangent = FPVector3.Cross(current, tangent);
                    improved = true;
                }
                if (!improved)
                {
                    step *= FP.Half;
                    if (step <= (FP)0.0001f) break;
                }
            }

            if (currentDepth < bestDepth)
            {
                bestDepth = currentDepth;
                bestNormal = current;
            }
            return true;
        }

        private static bool ResolveConvexContact(BaseShape a, in Transform transformA,
            BaseShape b, in Transform transformB, FPVector3 fallbackNormal, FP fallbackDepth,
            out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            normal = Unit(fallbackNormal, FPVector3.up);
            penetration = Max(FP.Zero, fallbackDepth);
            point = (ConvexSupport(a, transformA, normal) + ConvexSupport(b, transformB, -normal)) * FP.Half;
            return true;
        }

        /// <summary>凸体在给定方向上的支撑点(世界空间), 用于构造接触点。</summary>
        private static FPVector3 ConvexSupport(BaseShape shape, in Transform transform, FPVector3 direction)
        {
            switch (shape.Type)
            {
                case ShapeType.Box:
                {
                    BoxShape box = (BoxShape)shape;
                    FPVector3 half = box.HalfSize;
                    FPVector3 local = transform.InverseTransformDirection(direction);
                    FPVector3 corner = new FPVector3(
                        local.X < FP.Zero ? -half.X : half.X,
                        local.Y < FP.Zero ? -half.Y : half.Y,
                        local.Z < FP.Zero ? -half.Z : half.Z);
                    return transform.LocalToWorld(corner + box.Center);
                }
                case ShapeType.Capsule:
                {
                    CapsuleShape capsule = (CapsuleShape)shape;
                    GetWorldEndpoints(capsule, transform, out FPVector3 start, out FPVector3 end);
                    FPVector3 center = FPVector3.Dot(end - start, direction) < FP.Zero ? start : end;
                    return center + Unit(direction, FPVector3.up) * capsule.Radius;
                }
                case ShapeType.Cylinder:
                {
                    CylinderShape cylinder = (CylinderShape)shape;
                    FPVector3 axis = Unit(transform.up, FPVector3.up);
                    FPVector3 center = transform.LocalToWorld(cylinder.Center);
                    FP axial = FPVector3.Dot(axis, direction);
                    center += axis * (axial < FP.Zero ? -cylinder.HalfHeight : cylinder.HalfHeight);
                    FPVector3 radial = direction - axis * axial;
                    return center + Unit(radial, Perpendicular(axis)) * cylinder.Radius;
                }
                case ShapeType.Sphere:
                {
                    SphereShape sphere = (SphereShape)shape;
                    return transform.LocalToWorld(sphere.Center) + Unit(direction, FPVector3.up) * sphere.Radius;
                }
                default:
                    return transform.LocalToWorld(shape.Center);
            }
        }

        private static bool TestAxis(FPVector3 axis, FPVector3 delta, FP radiusSum,
            ref FP bestDepth, ref FPVector3 bestNormal)
        {
            FP projection = FPVector3.Dot(delta, axis);
            FP depth = radiusSum - FP.Abs(projection);
            if (depth < -Tolerance) return false;
            if (depth < bestDepth)
            {
                bestDepth = depth;
                bestNormal = projection < FP.Zero ? -axis : axis;
            }
            return true;
        }

        private static bool RoundedContact(FPVector3 centerA, FP radiusA, FPVector3 centerB, FP radiusB,
            FPVector3 fallback, out FPVector3 point, out FPVector3 normal, out FP penetration)
        {
            point = normal = FPVector3.zero;
            penetration = FP.Zero;
            FPVector3 delta = centerA - centerB;
            FP distance = Length(delta);
            FP radius = radiusA + radiusB;
            if (distance > radius + Tolerance) return false;
            normal = distance > Tolerance ? Unit(delta, FPVector3.up) : Unit(fallback, FPVector3.up);
            penetration = Max(FP.Zero, radius - distance);
            point = (centerA - normal * radiusA + centerB + normal * radiusB) * FP.Half;
            return true;
        }

        /// <summary>胶囊两端球心(世界空间)。</summary>
        private static void GetWorldEndpoints(CapsuleShape shape, in Transform transform,
            out FPVector3 p0, out FPVector3 p1)
        {
            FPVector3 center = transform.LocalToWorld(shape.Center);
            FPVector3 axis = transform.up * shape.HalfLength;
            p0 = center - axis;
            p1 = center + axis;
        }

        private static FPVector3 ClosestOnSegment(FPVector3 point, FPVector3 start, FPVector3 end)
        {
            FPVector3 delta = end - start;
            FP length = Length(delta);
            if (length == FP.Zero) return start;
            FPVector3 direction = Unit(delta, FPVector3.up);
            return start + direction * FP.Clamp(FPVector3.Dot(point - start, direction), FP.Zero, length);
        }

        private static void ClosestSegments(FPVector3 a0, FPVector3 a1, FPVector3 b0, FPVector3 b1,
            out FPVector3 closestA, out FPVector3 closestB)
        {
            closestA = a0;
            closestB = ClosestOnSegment(a0, b0, b1);
            FP best = Length(closestA - closestB);
            ConsiderSegmentPair(a1, ClosestOnSegment(a1, b0, b1), ref best, ref closestA, ref closestB);
            ConsiderSegmentPair(ClosestOnSegment(b0, a0, a1), b0, ref best, ref closestA, ref closestB);
            ConsiderSegmentPair(ClosestOnSegment(b1, a0, a1), b1, ref best, ref closestA, ref closestB);

            FP lengthA = Length(a1 - a0);
            FP lengthB = Length(b1 - b0);
            if (lengthA == FP.Zero || lengthB == FP.Zero) return;
            FPVector3 u = Unit(a1 - a0, FPVector3.up);
            FPVector3 v = Unit(b1 - b0, FPVector3.up);
            FPVector3 cross = FPVector3.Cross(u, v);
            FP crossLength = Length(cross);
            if (crossLength <= AxisTolerance) return;
            FPVector3 n = Unit(cross, FPVector3.up);
            FPVector3 offset = b0 - a0;
            FP numeratorA = FPVector3.Dot(FPVector3.Cross(offset, v), n);
            FP numeratorB = FPVector3.Dot(FPVector3.Cross(offset, u), n);
            if (numeratorA < FP.Zero || numeratorA > lengthA * crossLength
                || numeratorB < FP.Zero || numeratorB > lengthB * crossLength) return;
            FP s = FP.Clamp(numeratorA / crossLength, FP.Zero, lengthA);
            FP t = FP.Clamp(numeratorB / crossLength, FP.Zero, lengthB);
            ConsiderSegmentPair(a0 + u * s, b0 + v * t, ref best, ref closestA, ref closestB);
        }

        private static void ConsiderSegmentPair(FPVector3 a, FPVector3 b, ref FP best,
            ref FPVector3 closestA, ref FPVector3 closestB)
        {
            FP distance = Length(a - b);
            if (distance >= best) return;
            best = distance;
            closestA = a;
            closestB = b;
        }

        private static void ClosestSegmentBox(FPVector3 start, FPVector3 end, FPVector3 half,
            out FPVector3 segmentPoint, out FPVector3 boxPoint)
        {
            segmentPoint = start;
            boxPoint = ClampToBox(start, half);
            FP best = Length(start - boxPoint);
            FPVector3 delta = end - start;
            if (MaxAbs(delta) == FP.Zero || best == FP.Zero) return;
            FP lower = FP.Zero;
            for (int interval = 0; interval < 7; ++interval)
            {
                FP upper = FP.One;
                for (int axis = 0; axis < 3; ++axis)
                {
                    if (delta[axis] == FP.Zero) continue;
                    for (int side = -1; side <= 1; side += 2)
                    {
                        FP numerator = half[axis] * side - start[axis];
                        FP component = delta[axis];
                        if (component < FP.Zero)
                        {
                            numerator = -numerator;
                            component = -component;
                        }
                        if (numerator <= FP.Zero || numerator >= component) continue;
                        FP breakpoint = numerator / component;
                        if (breakpoint > lower && breakpoint < upper) upper = breakpoint;
                    }
                }

                FP midpoint = (lower + upper) * FP.Half;
                FPVector3 sample = start + delta * midpoint;
                FPVector3 activeDelta = FPVector3.zero;
                FPVector3 activeOffset = FPVector3.zero;
                for (int axis = 0; axis < 3; ++axis)
                {
                    FP bound;
                    if (sample[axis] < -half[axis]) bound = -half[axis];
                    else if (sample[axis] > half[axis]) bound = half[axis];
                    else continue;
                    activeDelta[axis] = delta[axis];
                    activeOffset[axis] = start[axis] - bound;
                }

                FP candidate = lower;
                FP activeScale = MaxAbs(activeDelta);
                if (activeScale > FP.Zero)
                {
                    FPVector3 direction = activeDelta / activeScale;
                    FP minimizer = -FPVector3.Dot(direction, activeOffset) / FPVector3.Dot(direction, direction);
                    candidate = minimizer <= activeScale * lower ? lower
                        : minimizer >= activeScale * upper ? upper
                        : minimizer / activeScale;
                }

                ConsiderBoxPoint(start + delta * candidate, half, ref best, ref segmentPoint, ref boxPoint);
                ConsiderBoxPoint(start + delta * upper, half, ref best, ref segmentPoint, ref boxPoint);
                if (best == FP.Zero || upper == FP.One) return;
                lower = upper;
            }
        }

        private static void ConsiderBoxPoint(FPVector3 candidate, FPVector3 half, ref FP best,
            ref FPVector3 segmentPoint, ref FPVector3 boxPoint)
        {
            FPVector3 closest = ClampToBox(candidate, half);
            FP distance = Length(candidate - closest);
            if (distance >= best) return;
            best = distance;
            segmentPoint = candidate;
            boxPoint = closest;
        }

        private static bool SphereRoots(FPVector3 origin, FPVector3 direction, FP radius, out FP entry, out FP exit)
        {
            entry = exit = FP.Zero;
            FP a = FPVector3.Dot(direction, direction);
            if (a == FP.Zero) return false;
            FP middle = -FPVector3.Dot(origin, direction) / a;
            FP perpendicular = Length(origin + direction * middle);
            if (perpendicular > radius + Tolerance) return false;
            FP span = FP.Zero;
            if (radius > FP.Zero)
            {
                FP ratio = Min(FP.One, perpendicular / radius);
                span = radius * SafeSqrt((FP.One - ratio) * (FP.One + ratio) / a);
            }
            entry = middle - span;
            exit = middle + span;
            return true;
        }

        private static bool IsInsideBox(FPVector3 point, FPVector3 half) =>
            FP.Abs(point.X) <= half.X && FP.Abs(point.Y) <= half.Y && FP.Abs(point.Z) <= half.Z;

        private static FPVector3 ClampToBox(FPVector3 point, FPVector3 half) =>
            new FPVector3(FP.Clamp(point.X, -half.X, half.X),
                FP.Clamp(point.Y, -half.Y, half.Y), FP.Clamp(point.Z, -half.Z, half.Z));

        private static FPVector3 Perpendicular(FPVector3 vector)
        {
            if (MaxAbs(vector) == FP.Zero) return FPVector3.up;
            FPVector3 unit = Unit(vector, FPVector3.up);
            FPVector3 abs = FPVector3.Abs(unit);
            FPVector3 basis = abs.X <= abs.Y && abs.X <= abs.Z ? FPVector3.right
                : abs.Y <= abs.Z ? FPVector3.up : FPVector3.forward;
            return Unit(FPVector3.Cross(unit, basis), FPVector3.right);
        }

        private static FPVector3 Unit(FPVector3 vector, FPVector3 fallback)
        {
            FP scale = MaxAbs(vector);
            if (scale == FP.Zero) return fallback;
            FPVector3 scaled = vector / scale;
            FP length = FP.Sqrt(FPVector3.Dot(scaled, scaled));
            return length > FP.Zero ? scaled / length : fallback;
        }

        private static FP Length(FPVector3 vector)
        {
            FP scale = MaxAbs(vector);
            if (scale == FP.Zero) return FP.Zero;
            FPVector3 scaled = vector / scale;
            return scale * FP.Sqrt(FPVector3.Dot(scaled, scaled));
        }

        private static FP MaxAbs(FPVector3 vector) =>
            Max(FP.Abs(vector.X), Max(FP.Abs(vector.Y), FP.Abs(vector.Z)));

        private static FPVector3 Basis(int axis) =>
            axis == 0 ? FPVector3.right : axis == 1 ? FPVector3.up : FPVector3.forward;

        private static FP SupportCoordinate(FP direction, FP half, FP target) =>
            direction > Tolerance ? half : direction < -Tolerance ? -half : FP.Clamp(target, -half, half);

        private static FP NonzeroSign(FP value) => value < FP.Zero ? -FP.One : FP.One;

        private static FP Min(FP a, FP b) => a < b ? a : b;

        private static FP Max(FP a, FP b) => a > b ? a : b;

        /// <summary>
        /// 非负开方。定点模式下 FP.Sqrt 遇到负数会抛异常, 浮点模式下会产生 NaN,
        /// 这里统一夹到 0 避免退化几何体污染接触求解。
        /// </summary>
        private static FP SafeSqrt(FP value) => value > FP.Zero ? FP.Sqrt(value) : FP.Zero;

        #endregion
    }
}
