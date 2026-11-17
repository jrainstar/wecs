using GameFrame;
using GameLogic.Gameplay.Common;
using GameLogic.Gameplay.Director;
using GameLogic.Gameplay.Logic;
using GameLogic.Gameplay.Replay;
using GameLogic.Gameplay.View;
using GameLogic.Gameplay.X;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gameplay 测试入口
/// 进入场景先显示主界面, 不直接启动战斗:
/// 1. 直接启动: 正常战斗, 结算时生成房间号并写出录像文件
/// 2. 以房间号启动: 播放该房间号的录像, 输入全部来自录像, 玩家无法操控
/// 战斗中可点击"结算退出"立刻结算并回到主界面
/// </summary>
public class WECS_0 : MonoBehaviour
{
    public World world { get; private set; }
    public LocalDirector director { get; private set; }

    [Tooltip("场景碰撞数据名称, 对应 Assets/AssetRaws/SceneDatas/{name}.json")]
    public string scenedata = "Battle_0";

    [Tooltip("结算后返回的场景名, 留空则回到本场景的主界面")]
    public string mainscene = "";

    /// <summary>
    /// 当前房间号
    /// </summary>
    public string room { get; private set; }
    /// <summary>
    /// 录像录制器 (录像回放时为 null)
    /// </summary>
    private ReplayRecorder recorder { get; set; }
    /// <summary>
    /// 录像播放器 (正常战斗时为 null)
    /// </summary>
    private ReplayPlayer player { get; set; }
    /// <summary>
    /// 界面
    /// </summary>
    private WECS_0UI ui { get; set; }
    /// <summary>
    /// 是否已有快照
    /// </summary>
    private bool hassnapshot { get; set; }
    /// <summary>
    /// 快照所在的逻辑帧
    /// </summary>
    private uint snapshotframe { get; set; }
    /// <summary>
    /// 上一次刷新战斗信息时的帧号
    /// </summary>
    private uint infoframe { get; set; } = uint.MaxValue;

    void Start()
    {
        world = World.CreateWorld();

        ui = new WECS_0UI();
        ui.onlaunch = Launch;
        ui.onreplay = LaunchReplay;
        ui.onsettle = Settle;
        ui.onsnapshot = TakeSnapshot;
        ui.onrestore = RestoreSnapshot;
        ui.Build();
        ui.ShowLaunch();
    }

    // Update is called once per frame
    void Update()
    {
        // 渲染帧驱动, 同时必须驱动 FixedTick, 否则 FixedTickEvent 永远不会派发,
        // BaseDirector.OnFixedTick -> OnStep -> Stage.Step 也就永远不会执行
        world.ticker.Tick(Time.deltaTime);

        // 录像播完, 自动结算退出
        if (null != player && player.finished)
        {
            Settle();
            return;
        }

        if (null == director) return;

        // 帧号变化时才刷新界面 (逻辑帧 25/s), 顺带避免每渲染帧生成字符串
        var frame = null != player ? (uint)player.playedframes : director.frame;
        if (frame == infoframe) return;

        infoframe = frame;
        RefreshBattleInfo();
    }

    private void FixedUpdate()
    {
        world.ticker.FixedTick(Time.fixedDeltaTime);
    }

    /// <summary>
    /// 刷新战斗界面信息 (房间号 / 当前帧 / 快照帧)
    /// </summary>
    private void RefreshBattleInfo()
    {
        if (null == director) return;

        if (null != player)
        {
            ui.SetBattleInfo($"录像回放  房间号: {room}  帧: {player.playedframes} / {player.lastframe}");
            return;
        }

        var info = $"战斗中  房间号: {room}  帧: {director.frame}";
        if (hassnapshot) info += $"  快照: 第 {snapshotframe} 帧";

        ui.SetBattleInfo(info);
    }

    /// <summary>
    /// 快照当前逻辑状态
    /// </summary>
    private void TakeSnapshot()
    {
        if (null == director || null != player) return;

        director.Snapshot();
        hassnapshot = true;
        snapshotframe = director.frame;

        ui.SetSnapshotButtons(true, true);
        infoframe = uint.MaxValue;
        Debug.Log($"[WECS_0] 快照完成, 帧 {snapshotframe}");
    }

    /// <summary>
    /// 恢复到快照
    /// </summary>
    private void RestoreSnapshot()
    {
        if (null == director || null != player || false == hassnapshot) return;

        // 快照恢复后逻辑会从该帧重新推演, 录像从该帧开始重新录制
        if (null != recorder) recorder.Truncate(snapshotframe);

        director.Restore();
        // 恢复在下一个逻辑帧才真正执行, 强制下一帧刷新显示
        infoframe = uint.MaxValue;
        Debug.Log($"[WECS_0] 恢复快照, 回到帧 {snapshotframe}");
    }

    /// <summary>
    /// 直接启动: 正常战斗, 同时录制录像
    /// </summary>
    private void Launch()
    {
        room = ReplayData.GenerateRoom();
        player = null;

        var data = CreateBuildData(LoadScene(scenedata));
        recorder = new ReplayRecorder(data, scenedata);

        StartBattle(data);
        ui.ShowBattle(room, false);
    }

    /// <summary>
    /// 以房间号启动: 播放录像, 玩家无法操控
    /// </summary>
    /// <param name="roomid">房间号</param>
    private void LaunchReplay(string roomid)
    {
        roomid = null == roomid ? string.Empty : roomid.Trim();
        if (0 == roomid.Length)
        {
            ui.ShowLaunch(null, "请输入房间号");
            return;
        }

        if (false == ReplayData.Exists(roomid))
        {
            ui.ShowLaunch(null, $"录像不存在: {roomid}");
            return;
        }

        var replay = ReplayData.Load(roomid);
        if (null == replay || null == replay.frames || 0 == replay.frames.Length)
        {
            ui.ShowLaunch(null, $"录像读取失败: {roomid}");
            return;
        }

        var data = replay.ToBuildData();
        var sdata = data.sdata;
        sdata.scene = LoadScene(replay.scenedata);
        data.sdata = sdata;

        recorder = null;
        room = replay.room;
        player = new ReplayPlayer(replay);

        StartBattle(data);
        ui.ShowBattle(room, true);
    }

    /// <summary>
    /// 结算并退出到主界面
    /// </summary>
    private void Settle()
    {
        var result = "战斗结束";
        var tip = string.Empty;

        if (null != player)
        {
            result = $"录像播放结束  房间号: {room}";
            tip = $"已播放 {player.playedframes} 帧";
        }
        else if (null != recorder)
        {
            result = $"战斗结算  房间号: {room}";
            try
            {
                var path = recorder.Save(room);
                tip = $"录像已生成: {path}";
                Debug.Log($"[WECS_0] 战斗结算, 房间号 {room}, 录像: {path}");
            }
            catch (System.Exception e)
            {
                tip = $"录像保存失败: {e.Message}";
                Debug.LogError($"[WECS_0] 录像保存失败: {e}");
            }
        }

        DestroyBattle();
        recorder = null;
        player = null;

        // 配置了主界面场景则直接切场景, 否则回到本场景的主界面
        if (false == string.IsNullOrEmpty(mainscene))
        {
            SceneManager.LoadScene(mainscene);
            return;
        }

        ui.SetRoom(room);
        ui.ShowLaunch(result, tip);
    }

    /// <summary>
    /// 创建并启动战斗
    /// </summary>
    /// <param name="data">构建数据</param>
    private void StartBattle(BuildData data)
    {
        DestroyBattle();

        Time.fixedDeltaTime = GAME_DEFINE.LOGIC_TICK.AsFloat();

        var local = new LocalDirector();
        local.world = world;
        local.recorder = recorder;
        local.player = player;
        local.Create();
        local.CreateGame(data, false);
        local.StartGame();

        director = local;
        hassnapshot = false;
        snapshotframe = 0;
        infoframe = uint.MaxValue;
    }

    /// <summary>
    /// 停止并销毁当前战斗
    /// </summary>
    private void DestroyBattle()
    {
        if (null == director) return;

        // World 是全局唯一的, 只销毁导演, 世界本身留着给下一局复用
        director.StopGame();
        director.Destroy();

        director = null;
    }

    /// <summary>
    /// 构建测试用的战斗数据
    /// </summary>
    /// <param name="scene">场景碰撞数据</param>
    private BuildData CreateBuildData(SceneData scene)
    {
        List<PlayerData> players = new()
        {
            new PlayerData
            {
                seat = 1,
                hero = 10002,
                position = new IntVector3(-2000, 0, 5000),
                euler = new IntVector3(0, 0, 0),
                scale = 1000,
            },
            new PlayerData
            {
                seat = 2,
                hero = 10002,
                position = new IntVector3(2000, 0, 5000),
                euler = new IntVector3(0, 0, 0),
                scale = 1000,
            },
        };

        return new BuildData
        {
            id = 10086,
            seat = 1,
            sdata = new StageData
            {
                seed = 19491001,
                players = players.ToArray(),
                scene = scene,
            },
        };
    }

    /// <summary>
    /// 加载场景碰撞数据 (Assets/AssetRaws/SceneDatas), 不存在时按空场景处理
    /// </summary>
    /// <param name="name">场景数据名称</param>
    /// <returns>场景数据</returns>
    private SceneData LoadScene(string name)
    {
        if (string.IsNullOrEmpty(name)) return default;

        TextAsset sceneasset = ResourceModule.Instance.LoadAssetSync<TextAsset>(ResourceUtil.scenedatas + name);
        if (null != sceneasset) return SceneData.FromJson(sceneasset);

        Debug.LogWarning($"[WECS_0] 场景数据加载失败: {ResourceUtil.scenedatas + name}");
        return default;
    }
}
