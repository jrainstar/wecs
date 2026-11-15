using GameLogic.Gameplay.Common;
using GameLogic.Gameplay.Logic;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Replay
{
    /// <summary>
    /// 录像录制器
    /// 由 LocalDirector 在每个逻辑帧把下发给逻辑层的输入登记进来, 结算时写成录像文件
    /// </summary>
    public class ReplayRecorder
    {
        /// <summary>
        /// 场景碰撞数据名称
        /// </summary>
        public string scenedata { get; private set; }
        /// <summary>
        /// 已录制的帧数
        /// </summary>
        public int framecount => frames.Count;
        /// <summary>
        /// 构建数据 (种子 / 玩家 / GameID 等, 回放需要原样复现)
        /// </summary>
        private BuildData data { get; set; }
        /// <summary>
        /// 已录制的输入
        /// </summary>
        private readonly List<ReplayFrame> frames = new();

        public ReplayRecorder(BuildData data, string scenedata)
        {
            this.data = data;
            this.scenedata = scenedata;
        }

        /// <summary>
        /// 记录一帧输入
        /// </summary>
        /// <param name="frame">逻辑帧号 (Stage.Step 之前)</param>
        /// <param name="type">输入类型</param>
        /// <param name="press">是否按下</param>
        /// <param name="dire">世界方向 (已转换)</param>
        public void Record(uint frame, ushort type, bool press, IntVector2 dire)
        {
            if (0 == frames.Count || frames[frames.Count - 1].frame != frame)
            {
                frames.Add(new ReplayFrame
                {
                    frame = frame,
                    inputs = new List<ReplayInput>(INPUT_DEFINE.SYNC_INPUTS.Length),
                });
            }

            frames[frames.Count - 1].inputs.Add(new ReplayInput
            {
                type = type,
                press = press,
                x = dire.x,
                y = dire.y,
            });
        }

        /// <summary>
        /// 把录像写入文件
        /// </summary>
        /// <param name="room">房间号</param>
        /// <returns>录像文件路径</returns>
        public string Save(string room)
        {
            var replay = new ReplayData
            {
                room = room,
                gameid = data.id,
                seat = data.seat,
                seed = data.sdata.seed,
                scenedata = scenedata,
                players = data.sdata.players,
                frames = frames.ToArray(),
            };

            return replay.Save();
        }
    }
}
