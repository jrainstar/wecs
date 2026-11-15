using GameLogic.Gameplay.Common;
using GameLogic.Gameplay.Logic;

namespace GameLogic.Gameplay.Replay
{
    /// <summary>
    /// 录像播放器
    /// 回放时由 LocalDirector 在每个逻辑帧把录像里的输入下发给逻辑层, 不读取本地设备输入
    /// </summary>
    public class ReplayPlayer
    {
        /// <summary>
        /// 回放数据
        /// </summary>
        public ReplayData replay { get; private set; }
        /// <summary>
        /// 录像的房间号
        /// </summary>
        public string room => replay.room;
        /// <summary>
        /// 录像的座位号
        /// </summary>
        public ulong seat => replay.seat;
        /// <summary>
        /// 最后一帧的帧号
        /// </summary>
        public uint lastframe { get; private set; }
        /// <summary>
        /// 已播放的帧数
        /// </summary>
        public int playedframes => cursor;
        /// <summary>
        /// 录像是否已播放完毕
        /// </summary>
        public bool finished { get; private set; }
        /// <summary>
        /// 播放游标
        /// </summary>
        private int cursor { get; set; }

        public ReplayPlayer(ReplayData replay)
        {
            this.replay = replay;
            lastframe = null != replay.frames && replay.frames.Length > 0 ? replay.frames[replay.frames.Length - 1].frame : 0;
        }

        /// <summary>
        /// 下发该逻辑帧的输入
        /// </summary>
        /// <param name="frame">逻辑帧号 (Stage.Step 之前)</param>
        /// <param name="stage">逻辑场景</param>
        /// <returns>false 表示录像已播放完毕, 不应再推进逻辑帧</returns>
        public bool Sync(uint frame, Stage stage)
        {
            var frames = replay.frames;
            if (null == frames || cursor >= frames.Length)
            {
                finished = true;
                return false;
            }

            // 跳过漏录的帧 (正常情况下逐帧连续)
            while (cursor < frames.Length && frames[cursor].frame < frame) cursor++;

            if (cursor >= frames.Length)
            {
                finished = true;
                return false;
            }

            var recorded = frames[cursor];
            // 该帧没有记录: 本帧不下发, 等下一帧
            if (recorded.frame != frame) return true;

            var inputs = recorded.inputs;
            if (null != inputs)
            {
                for (int i = 0; i < inputs.Count; i++)
                {
                    var input = inputs[i];
                    stage.SetInput(seat, input.type, input.press, new IntVector2(input.x, input.y));
                }
            }

            cursor++;
            return true;
        }
    }
}
