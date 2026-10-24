
namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 座位行为
    /// </summary>
    public class Seat : Behavior<SeatInfo>
    {
        protected override void OnAssemble()
        {
            base.OnAssemble();
            stage.eventor.Listen<ActorRmvEvent>(this, OnActorRmv);
        }

        protected override void OnDisassemble()
        {
            base.OnDisassemble();
            stage.eventor.UnListen<ActorRmvEvent>(this, OnActorRmv);
        }

        /// <summary>
        /// 根据座位 ID 获取 Actor
        /// </summary>
        /// <param name="seat">座位 ID</param>
        /// <returns>ActorID</returns>
        public ulong GetActor(ulong seat)
        {
            if (info.sadict.TryGetValue(seat, out var actor)) return actor;

            return 0;
        }

        /// <summary>
        /// 根据 Actor 获取座位 ID
        /// </summary>
        /// <param name="actor">Actor</param>
        /// <returns>座位 ID</returns>
        public ulong GetSeat(ulong actor)
        {
            if (info.asdict.TryGetValue(actor, out var seat)) return seat;

            return 0;
        }

        /// <summary>
        /// 坐下
        /// </summary>
        /// <param name="seat">座位 ID</param>
        /// <param name="id">Actor</param>
        public void Sitdown(ulong seat, ulong id)
        {
            if (info.sadict.ContainsKey(seat)) info.sadict.Remove(seat);
            if (info.asdict.ContainsKey(id)) info.asdict.Remove(id);

            info.sadict.Add(seat, id);
            info.asdict.Add(id, seat);
        }

        private void OnActorRmv(ActorRmvEvent e)
        {
            // 站起
            if (false == info.asdict.TryGetValue(e.actor, out var seat)) return;

            info.asdict.Remove(e.actor);
            info.sadict.Remove(seat);
        }
    }
}