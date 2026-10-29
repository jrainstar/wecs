using Animancer;
using GameLogic.Gameplay.Common;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameLogic.Gameplay.View
{
    /// <summary>
    /// Odin 定义值下拉列表
    /// </summary>
    public class AnimationDropdown
    {
        /// <summary>
        /// 获取状态定义下拉列表
        /// </summary>
        /// <returns>ValueDropdownList(状态)</returns>
        public static ValueDropdownList<byte> GetStateDefine()
        {
            return new()
            {
                { "无", STATE_DEFINE.NONE },
                { "出生", STATE_DEFINE.BORN },
                { "死亡", STATE_DEFINE.DEATH },
                { "待机", STATE_DEFINE.IDLE },
                { "移动", STATE_DEFINE.MOVE },
                { "跳跃", STATE_DEFINE.JUMP },
                { "下坠", STATE_DEFINE.FALL },
                { "技能", STATE_DEFINE.CASTING },
                { "受击", STATE_DEFINE.BEHIT },
            };
        }

        /// <summary>
        /// 获取模型下拉列表
        /// </summary>
        /// <returns>模型下拉列表</returns>
        public static ValueDropdownList<int> ModelValueDropdown()
        {
            var result = new ValueDropdownList<int>();
            result.Add(new ValueDropdownItem<int>("None", 0));
            foreach (var modelcfg in EditorConfig.location.TbRoleData.DataList) result.Add(new ValueDropdownItem<int>(modelcfg.Name, modelcfg.ModelId));

            return result;
        }

        /// <summary>
        /// 获取模型动画名称列表
        /// </summary>
        /// <param name="model">模型</param>
        /// <returns>模型动画名称列表</returns>
        public static ValueDropdownList<string> GetModelAnimNames(int model)
        {
            var result = new ValueDropdownList<string>();
            if (false == EditorConfig.location.TbRoleModel.TryGetValue(model, out var rolemodel)) return result;
            var go = EditorRes.LoadModel(rolemodel.Model);
            var animancer = go.GetComponent<NamedAnimancerComponent>();
            if (null == animancer)
            {
                GameObject.DestroyImmediate(go);
                return result;
            }

            foreach (var clip in animancer.Animations) result.Add(clip.name, clip.name);

            GameObject.DestroyImmediate(go);

            return result;
        }
    }
}