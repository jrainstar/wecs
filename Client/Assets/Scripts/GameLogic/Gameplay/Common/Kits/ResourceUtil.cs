using GameConf;

namespace GameLogic.Gameplay.Common
{
    /// <summary>
    /// 资源定位工具
    /// 配置表里的模型/资源字段存的都是 ResourceData 的编号, 实际资源路径统一在这里解析,
    /// 避免逻辑层与渲染层各写一份 角色 -> 角色模型 -> 资源 的查表链
    /// </summary>
    public static class ResourceUtil
    {
        /// <summary>
        /// 按资源编号取资源路径
        /// </summary>
        /// <param name="tables">配置表</param>
        /// <param name="resource">资源编号 (TbResourceData)</param>
        /// <param name="path">资源路径</param>
        /// <returns>YES/NO</returns>
        public static bool TryGetPath(Tables tables, int resource, out string path)
        {
            path = null;
            if (null == tables) return false;
            if (0 >= resource) return false;
            if (false == tables.TbResourceData.TryGetValue(resource, out var data)) return false;
            if (null == data || string.IsNullOrEmpty(data.Path)) return false;

            path = data.Path;

            return true;
        }

        /// <summary>
        /// 按角色编号取模型资源路径 (角色 -> 角色模型 -> 资源)
        /// </summary>
        /// <param name="tables">配置表</param>
        /// <param name="role">角色编号 (TbRoleData)</param>
        /// <param name="path">资源路径</param>
        /// <returns>YES/NO</returns>
        public static bool TryGetRoleModelPath(Tables tables, int role, out string path)
        {
            path = null;
            if (null == tables) return false;
            if (0 >= role) return false;
            if (false == tables.TbRoleData.TryGetValue(role, out var roledata)) return false;
            if (null == roledata) return false;
            if (false == tables.TbRoleModel.TryGetValue(roledata.ModelId, out var rolemodel)) return false;
            if (null == rolemodel) return false;

            return TryGetPath(tables, rolemodel.Model, out path);
        }
    }
}
