using GameLogic.Gameplay.Common;
using GameLogic.Gameplay.Director;
using GameLogic.Gameplay.Logic;
using GameLogic.Gameplay.Replay;
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
    public Engine engine { get; private set; }
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
    /// 战斗信息刷新计时
    /// </summary>
    private float infotimer { get; set; }

    void Start()
    {
        engine = Engine.CreateEngine();

        ui = new WECS_0UI();
        ui.onlaunch = Launch;
        ui.onreplay = LaunchReplay;
        ui.onsettle = Settle;
        ui.Build();
        ui.ShowLaunch();
    }

    // Update is called once per frame
    void Update()
    {
        // 渲染帧驱动, 同时必须驱动 FixedTick, 否则 FixedTickEvent 永远不会派发,
        // BaseDirector.OnFixedTick -> OnStep -> Stage.Step 也就永远不会执行
        engine.ticker.Tick(Time.deltaTime);

        // 录像播完, 自动结算退出
        if (null != player && player.finished)
        {
            Settle();
            return;
        }

        if (null == director) return;

        infotimer += Time.deltaTime;
        if (infotimer < 0.25f) return;

        infotimer = 0;
        ui.SetBattleInfo(null != player
            ? $"录像回放  房间号: {room}  帧: {player.playedframes} / {player.lastframe}"
            : $"战斗中  房间号: {room}  帧: {director.frame}");
    }

    private void FixedUpdate()
    {
        engine.ticker.FixedTick(Time.fixedDeltaTime);
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
        local.engine = engine;
        local.recorder = recorder;
        local.player = player;
        local.Create();
        local.CreateGame(data, false);
        local.StartGame();

        director = local;
        infotimer = 0;
    }

    /// <summary>
    /// 停止并销毁当前战斗
    /// </summary>
    private void DestroyBattle()
    {
        if (null == director) return;

        var root = director.world?.root;
        director.StopGame();
        director.Destroy();
        if (null != root) Destroy(root);

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

        TextAsset sceneasset = engine.gameres.LoadAssetSync<TextAsset>(Location.scenedatas + name);
        if (null != sceneasset) return SceneData.FromJson(sceneasset);

        Debug.LogWarning($"[WECS_0] 场景数据加载失败: {Location.scenedatas + name}");
        return default;
    }
}
