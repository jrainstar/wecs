using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 运动行为
    /// </summary>
    public class Movement : Behavior<MovementInfo>
    {
        /// <summary>
        /// 移动
        /// </summary>
        /// <param name="dire">方向</param>
        /// <param name="tick">步长</param>
        public void Move(FPVector3 dire, FP tick)
        {
            if (false == stage.SeekBehaviorInfo(actor, out SpatialInfo spatial)) return;

            // 先算出本帧位移, 状态切换失败 (例如死亡/受击) 就不移动
            // 注意不能把位移写在状态切换之后: 切换成功时状态已经是 MOVE, 再次切换会直接返回 true
            if (stage.SeekBehavior(actor, out StateMachine machine))
            {
                if (false == machine.TryChangeState(STATE_DEFINE.MOVE)) return;
            }

            dire.Normalize();
            var speed = 5;
            var motion = dire * speed * tick;
            spatial.position += motion;

            // 位移后求解与其它碰撞盒的重叠, 把对方按质量分摊推着走
            Resolve();

            FP angle = FPMath.Atan2(dire.x, dire.z) * FPMath.Rad2Deg;
            spatial.euler = FPVector3.up * angle;

            info.turnmotion = true;
        }

        /// <summary>
        /// 求解重叠: 按质量倒数分摊推开量。质量为负(无限大)的一方不移动,
        /// 因此撞到墙体时只有自身被顶回, 撞到轻的对手则能推着走。
        /// </summary>
        private void Resolve()
        {
            if (false == stage.SeekBehaviorInfo(actor, out ColliderInfo self)) return;
            if (false == stage.SeekBehaviorInfo(actor, out SpatialInfo selfspatial)) return;
            // 自身不可推动, 位置不会被推开量影响
            if (self.immovable) return;

            FP selfinv = self.invmass;
            if (selfinv <= FP.Zero) return;

            HitResult hitresult = stage.detection.Overlap(self);
            if (false == hitresult.hit || null == hitresult.colliders) return;

            foreach (var c in hitresult.colliders)
            {
                if (c.penetration <= FP.Zero) continue;
                if (false == stage.SeekBehaviorInfo(c.actor, out ColliderInfo other)) continue;
                if (false == stage.SeekBehaviorInfo(c.actor, out SpatialInfo otherspatial)) continue;

                FP otherinv = other.invmass;
                FP sum = selfinv + otherinv;
                if (sum <= FP.Zero) continue;

                // 法线由对方指向自身, 沿法线方向分离两者
                FPVector3 correction = c.normal * c.penetration;
                selfspatial.position += correction * (selfinv / sum);
                if (otherinv > FP.Zero)
                {
                    otherspatial.position -= correction * (otherinv / sum);
                }
            }
        }

        protected override void OnTick(FP tick)
        {
            base.OnTick(tick);
            if (false == stage.SeekBehavior(actor, out Gamepad gamepad)) return;

            var joystick = gamepad.GetInput(INPUT_DEFINE.JOYSTICK);
            if (false == joystick.press) return;

            // 时间轴锁定移动 (如施法动作 Move=false): 不位移也不切 MOVE 状态, 动画由时间轴全权接管
            if (stage.SeekBehaviorInfo(actor, out TimelineInfo timelineinfo) && timelineinfo.lockmove) return;

            Move(new FPVector3(joystick.dire.x, 0, joystick.dire.y), tick);
        }

        protected override void OnEndTick()
        {
            base.OnEndTick();
            // 如果没有在移动状态, 则尝试切换到 Idle 状态
            if (false == info.turnmotion && stage.SeekBehavior(actor, out StateMachine machine))
            {
                machine.TryChangeState(STATE_DEFINE.IDLE);
            }
            info.turnmotion = false;
        }
    }
}