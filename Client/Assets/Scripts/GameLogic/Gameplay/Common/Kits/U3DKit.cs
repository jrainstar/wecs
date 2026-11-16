using GameLogic.Gameplay.Common;
using GameLogic.Gameplay.Logic;
using UnityEngine;

namespace GameLogic.Gameplay.Common
{
    /// <summary>
    /// Unity3D 工具组件
    /// </summary>
    public class U3DKit : Node
    {
        public InputKit input { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();

            input = AddNode<InputKit>();
            input.Create();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        /// <summary>
        /// 获取 Unity3D 实体/组件
        /// </summary>
        /// <typeparam name="T">Unity 的类型，GameObject 或 Component</typeparam>
        /// <param name="go">实体</param>
        /// <returns>GameObject/Component</returns>
        public T GetEntity<T>(GameObject go) where T : UnityEngine.Object
        {
            if (go is T) return go as T;

            return go.GetComponent<T>();
        }

        /// <summary>
        /// 精准查找 Unity3D 实体/组件
        /// </summary>
        /// <typeparam name="T">Unity 的类型，GameObject 或 Component</typeparam>
        /// <param name="go">根实体</param>
        /// <param name="path">实体路径</param>
        /// <returns>GameObject/Component</returns>
        public T GetEntity<T>(GameObject go, string path) where T : UnityEngine.Object
        {
            var entity = go.transform.Find(path)?.gameObject;

            if (null == entity) return null;

            if (entity is T) return entity as T;

            return entity.GetComponent<T>();
        }

        /// <summary>
        /// 模糊查找 Unity3D 实体/组件
        /// </summary>
        /// <typeparam name="T">Unity 的类型，GameObject 或 Component</typeparam>
        /// <param name="go">预制体根实体</param>
        /// <param name="entityName">匹配实体名</param>
        /// <returns>GameObject/Component</returns>
        public T SeekEntity<T>(GameObject go, string entityName) where T : UnityEngine.Object
        {
            for (int i = 0; i < go.transform.childCount; i++)
            {
                var entity = go.transform.GetChild(i).gameObject;
                if (entity.name.Equals(entityName))
                {
                    if (entity is T) return entity as T;

                    return entity.GetComponent<T>();
                }

                var child = SeekEntity<T>(entity, entityName);
                if (null == child) continue;

                if (child is T) return child;
            }

            return null;
        }

        ///// <summary>
        ///// 世界坐标转 UI 本地坐标
        ///// </summary>
        ///// <param name="parentRect">父节点 RectTransform</param>
        ///// <param name="worldPoint">世界坐标</param>
        ///// <returns>UI 本地坐标</returns>
        //public Vector2 WorldToUILoaclPoint(RectTransform parentRect, Vector3 worldPoint)
        //{
        //    Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPoint);
        //    RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, engine.gameui.uicamera, out Vector2 uiPoint);

        //    return uiPoint;
        //}
    }
}