using GameLogic.Gameplay.Common;
using GameLogic.Gameplay.Logic;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GameLogic.Gameplay.Replay
{
    /// <summary>
    /// 单个输入的录像数据
    /// dire 是 Director 转换后的世界方向, 回放时不再经过相机转换, 保证与录制时一致
    /// </summary>
    public struct ReplayInput
    {
        /// <summary>
        /// 输入类型 (INPUT_DEFINE)
        /// </summary>
        public ushort type;
        /// <summary>
        /// 是否按下
        /// </summary>
        public bool press;
        /// <summary>
        /// 方向 X
        /// </summary>
        public int x;
        /// <summary>
        /// 方向 Y
        /// </summary>
        public int y;
    }

    /// <summary>
    /// 一个逻辑帧的输入集合
    /// </summary>
    public struct ReplayFrame
    {
        /// <summary>
        /// 逻辑帧号 (Stage.Step 之前的帧号)
        /// </summary>
        public uint frame;
        /// <summary>
        /// 该帧的输入
        /// </summary>
        public List<ReplayInput> inputs;
    }

    /// <summary>
    /// 录像数据 (构建参数 + 逐帧输入)
    /// 逻辑层是确定性帧同步的, 只要构建参数与逐帧输入一致, 回放结果就和录制时完全一致
    /// </summary>
    public class ReplayData
    {
        /// <summary>
        /// 文件头标识
        /// </summary>
        private const int MAGIC = 0x53434557;
        /// <summary>
        /// 录像格式版本
        /// </summary>
        public const int VERSION = 1;

        /// <summary>
        /// 房间号, 同时作为录像文件名
        /// </summary>
        public string room;
        /// <summary>
        /// GameID
        /// </summary>
        public ulong gameid;
        /// <summary>
        /// 我(录制者)的座位号, 回放时把输入下发给该座位
        /// </summary>
        public ulong seat;
        /// <summary>
        /// 随机器种子
        /// </summary>
        public int seed;
        /// <summary>
        /// 场景碰撞数据名称 (Location.scenedatas 下的资源名)
        /// </summary>
        public string scenedata;
        /// <summary>
        /// 玩家数据
        /// </summary>
        public PlayerData[] players;
        /// <summary>
        /// 逐帧输入
        /// </summary>
        public ReplayFrame[] frames;

        /// <summary>
        /// 录像存放目录
        /// </summary>
        public static string root => Path.Combine(Application.persistentDataPath, "Replays");
        /// <summary>
        /// 录像文件后缀
        /// </summary>
        public static string extension => ".wreplay";

        /// <summary>
        /// 房间号对应的录像文件路径
        /// </summary>
        public static string PathOf(string room) => Path.Combine(root, room + extension);

        /// <summary>
        /// 录像文件是否存在
        /// </summary>
        public static bool Exists(string room)
        {
            if (string.IsNullOrEmpty(room)) return false;

            return File.Exists(PathOf(room));
        }

        /// <summary>
        /// 生成一个未被占用的房间号
        /// </summary>
        public static string GenerateRoom()
        {
            var random = new System.Random(Guid.NewGuid().GetHashCode());
            for (int i = 0; i < 100; i++)
            {
                var room = random.Next(100000, 1000000).ToString();
                if (false == Exists(room)) return room;
            }

            return DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        /// <summary>
        /// 转换为构建数据, 场景数据由调用方按 scenedata 加载后填回
        /// </summary>
        public BuildData ToBuildData()
        {
            return new BuildData
            {
                id = gameid,
                seat = seat,
                sdata = new StageData
                {
                    seed = seed,
                    players = players,
                    scene = default,
                },
            };
        }

        /// <summary>
        /// 写入录像文件
        /// </summary>
        /// <returns>录像文件路径</returns>
        public string Save()
        {
            var path = PathOf(room);
            Directory.CreateDirectory(root);

            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(MAGIC);
                writer.Write(VERSION);
                writer.Write(room ?? string.Empty);
                writer.Write(gameid);
                writer.Write(seat);
                writer.Write(seed);
                writer.Write(scenedata ?? string.Empty);

                var players = this.players ?? Array.Empty<PlayerData>();
                writer.Write(players.Length);
                for (int i = 0; i < players.Length; i++)
                {
                    var p = players[i];
                    writer.Write(p.seat);
                    writer.Write(p.hero);
                    writer.Write(p.position.x);
                    writer.Write(p.position.y);
                    writer.Write(p.position.z);
                    writer.Write(p.euler.x);
                    writer.Write(p.euler.y);
                    writer.Write(p.euler.z);
                    writer.Write(p.scale);
                }

                var frames = this.frames ?? Array.Empty<ReplayFrame>();
                writer.Write(frames.Length);
                for (int i = 0; i < frames.Length; i++)
                {
                    var f = frames[i];
                    writer.Write(f.frame);

                    var inputs = f.inputs;
                    writer.Write(null != inputs ? inputs.Count : 0);
                    if (null == inputs) continue;

                    for (int j = 0; j < inputs.Count; j++)
                    {
                        var input = inputs[j];
                        writer.Write(input.type);
                        writer.Write(input.press);
                        writer.Write(input.x);
                        writer.Write(input.y);
                    }
                }
            }

            return path;
        }

        /// <summary>
        /// 读取录像文件
        /// </summary>
        /// <returns>录像数据, 读取失败时返回 null</returns>
        public static ReplayData Load(string room)
        {
            var path = PathOf(room);
            if (false == File.Exists(path))
            {
                Debug.LogWarning($"[Replay] 录像不存在: {path}");
                return null;
            }

            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (var reader = new BinaryReader(stream))
                {
                    if (MAGIC != reader.ReadInt32())
                    {
                        Debug.LogError($"[Replay] 录像格式错误: {path}");
                        return null;
                    }

                    var replay = new ReplayData();
                    var version = reader.ReadInt32();
                    if (VERSION != version)
                    {
                        Debug.LogError($"[Replay] 录像版本不匹配 ({version} != {VERSION}): {path}");
                        return null;
                    }

                    replay.room = reader.ReadString();
                    replay.gameid = reader.ReadUInt64();
                    replay.seat = reader.ReadUInt64();
                    replay.seed = reader.ReadInt32();
                    replay.scenedata = reader.ReadString();

                    var playercount = reader.ReadInt32();
                    replay.players = new PlayerData[playercount];
                    for (int i = 0; i < playercount; i++)
                    {
                        replay.players[i] = new PlayerData
                        {
                            seat = reader.ReadUInt64(),
                            hero = reader.ReadInt32(),
                            position = new IntVector3(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()),
                            euler = new IntVector3(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()),
                            scale = reader.ReadInt32(),
                        };
                    }

                    var framecount = reader.ReadInt32();
                    replay.frames = new ReplayFrame[framecount];
                    for (int i = 0; i < framecount; i++)
                    {
                        var frame = new ReplayFrame
                        {
                            frame = reader.ReadUInt32(),
                        };

                        var inputcount = reader.ReadInt32();
                        frame.inputs = new List<ReplayInput>(inputcount);
                        for (int j = 0; j < inputcount; j++)
                        {
                            frame.inputs.Add(new ReplayInput
                            {
                                type = reader.ReadUInt16(),
                                press = reader.ReadBoolean(),
                                x = reader.ReadInt32(),
                                y = reader.ReadInt32(),
                            });
                        }

                        replay.frames[i] = frame;
                    }

                    return replay;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Replay] 录像读取失败: {path}\n{e}");
                return null;
            }
        }
    }
}
