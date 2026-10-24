namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 座位系统
    /// </summary>
    public class Seat : Behavior<SeatInfo>
    {
        protected override void OnAssemble()
        {
            base.OnAssemble();
            stage.eventor.Listen<ActorRmvEvent>(this, OnEntityRmv);
        }

        protected override void OnDisassemble()
        {
            base.OnDisassemble();
            stage.eventor.UnListen<ActorRmvEvent>(this, OnEntityRmv);
        }

        /// <summary>
        /// 根据座位 ID 获取 EntityID
        /// </summary>
        /// <param name="seat">座位 ID</param>
        /// <returns>EntityID</returns>
        public ulong GetEntity(ulong seat)
        {
            if (behaviorinfo.seatdict.TryGetValue(seat, out var entity)) return entity;

            return 0;
        }

        /// <summary>
        /// 根据 EntityID 获取座位 ID
        /// </summary>
        /// <param name="entity">EntityID</param>
        /// <returns>座位 ID</returns>
        public ulong GetSeat(ulong entity)
        {
            if (behaviorinfo.actordict.TryGetValue(entity, out var seat)) return seat;

            return 0;
        }

        /// <summary>
        /// 坐下
        /// </summary>
        /// <param name="seat">座位 ID</param>
        /// <param name="entity">EntityID</param>
        public void Sitdown(ulong seat, ulong entity)
        {
            if (behaviorinfo.seatdict.ContainsKey(seat)) behaviorinfo.seatdict.Remove(seat);
            if (behaviorinfo.actordict.ContainsKey(entity)) behaviorinfo.actordict.Remove(entity);

            behaviorinfo.seatdict.Add(seat, entity);
            behaviorinfo.actordict.Add(entity, seat);
        }

        /// <summary>
        /// 站起
        /// </summary>
        /// <param name="entity">EntityID</param>
        public void Standup(ulong entity)
        {
            if (false == behaviorinfo.actordict.TryGetValue(entity, out var seat)) return;

            behaviorinfo.actordict.Remove(entity);
            behaviorinfo.seatdict.Remove(seat);
        }

        private void OnEntityRmv(ActorRmvEvent e)
        {
            // 站起
            Standup(e.actor);
        }
    }
}
