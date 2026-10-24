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
                        position = new IntVector3(21000, 0, 21000),
                        euler = new IntVector3(0, 0, 0),
                        scale = 1000,
                    },
                    new PlayerData
                    {
                        seat = 2,
                        hero = 100001,
                        position = new IntVector3(20000, 0, 20000),
                        euler = new IntVector3(0, 0, 0),
                        scale = 1000,
                    },
                };
        ulong seat = 3;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                players.Add(new PlayerData
                {
                    seat = seat,
                    hero = 100001,
                    position = new IntVector3(1100 * i, 0, 1100 * j),
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
        engine.ticker.Tick(Time.deltaTime);
    }
}
