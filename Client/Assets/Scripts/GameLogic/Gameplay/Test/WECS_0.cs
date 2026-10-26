using GameLogic.Gameplay.Common;
using GameLogic.Gameplay.Director;
using GameLogic.Gameplay.Logic;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WECS_0 : MonoBehaviour
{
    public Engine engine { get; private set; }
    public BaseDirector director { get; private set; }

    void Start()
    {
        engine = Engine.CreateEngine();

        List<PlayerData> players = new()
                {
                    new PlayerData
                    {
                        seat = 1,
                        hero = 100001,
                        position = new IntVector3(-2000, 0, 5000),
                        euler = new IntVector3(0, 0, 0),
                        scale = 1000,
                    },
                    new PlayerData
                    {
                        seat = 2,
                        hero = 100001,
                        position = new IntVector3(2000, 0, 5000),
                        euler = new IntVector3(0, 0, 0),
                        scale = 1000,
                    },
                };
        // 位置单位: 1000 = 1 米, 相机在 (0,1,-10), 所以摆放在相机前方 5 米处
        // 间距放大 10 倍: 每格 10000 (10 米)
        ulong seat = 3;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                players.Add(new PlayerData
                {
                    seat = seat,
                    hero = 100001,
                    position = new IntVector3(-15000 + 10000 * i, 0, 5000 + 10000 * j),
                    euler = new IntVector3(0, 0, 0),
                    scale = 1000,
                });
                seat++;
            }
        }

        BuildData data = new BuildData();
        data.id = 10086;
        data.seat = 1;
        data.sdata = new StageData
        {
            seed = 19491001,
            players = players.ToArray(),
        };

        Time.fixedDeltaTime = GAME_DEFINE.LOGIC_TICK.AsFloat();
        if (null != director)
        {
            director.StopGame();
            director.Destroy();
        }

        director = new LocalDirector();
        director.engine = engine;
        director.Create();

        director.CreateGame(data, false);
        director.StartGame();
    }

    // Update is called once per frame
    void Update()
    {
        // 渲染帧驱动, 同时必须驱动 FixedTick, 否则 FixedTickEvent 永远不会派发,
        // BaseDirector.OnFixedTick -> OnStep -> Stage.Step 也就永远不会执行
        engine.ticker.Tick(Time.deltaTime);

    }

    private void FixedUpdate()
    {
        engine.ticker.FixedTick(Time.fixedDeltaTime);
    }
}
