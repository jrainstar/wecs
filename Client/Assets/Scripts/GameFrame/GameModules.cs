using System;
using System.Collections.Generic;
using UnityEngine;
using GameCore;

namespace GameEngine
{
    public class GameModules : GameModule<GameModules>
    {
        private static readonly GameLinkedList<GameModule> m_Moudles = new();

        protected override void Awake()
        {
            base.Awake();
            GameObject.DontDestroyOnLoad(this.gameObject);
        }

        /// <summary>
        /// 获取游戏框架组件。
        /// </summary>
        /// <typeparam name="T">要获取的游戏框架组件类型。</typeparam>
        /// <returns>要获取的游戏框架组件。</returns>
        public static T Get<T>() where T : GameModule
        {
            return (T)Get(typeof(T));
        }

        /// <summary>
        /// 获取游戏框架组件。
        /// </summary>
        /// <param name="type">要获取的游戏框架组件类型。</param>
        /// <returns>要获取的游戏框架组件。</returns>
        public static GameModule Get(Type type)
        {
            LinkedListNode<GameModule> current = m_Moudles.First;
            while (current != null)
            {
                if (current.Value.GetType() == type)
                {
                    return current.Value;
                }

                current = current.Next;
            }

            return null;
        }

        /// <summary>
        /// 获取游戏框架组件。
        /// </summary>
        /// <param name="typeName">要获取的游戏框架组件类型名称。</param>
        /// <returns>要获取的游戏框架组件。</returns>
        public static GameModule Get(string typeName)
        {
            LinkedListNode<GameModule> current = m_Moudles.First;
            while (current != null)
            {
                Type type = current.Value.GetType();
                if (type.FullName == typeName || type.Name == typeName)
                {
                    return current.Value;
                }

                current = current.Next;
            }

            return null;
        }

        /// <summary>
        /// 注册游戏框架组件。
        /// </summary>
        /// <param name="gameFrameworkComponent">要注册的游戏框架组件。</param>
        internal static void RegisterModule(GameModule gameFrameworkComponent)
        {
            if (gameFrameworkComponent == null)
            {
                Log.Error("Game Framework component is invalid.");
                return;
            }

            Type type = gameFrameworkComponent.GetType();

            LinkedListNode<GameModule> current = m_Moudles.First;
            while (current != null)
            {
                if (current.Value.GetType() == type)
                {
                    Log.Error($"Game Framework component type '{type.FullName}' is already exist.");
                    return;
                }

                current = current.Next;
            }

            m_Moudles.AddLast(gameFrameworkComponent);
        }
    }
}
