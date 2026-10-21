using System;
using UnityEditor;
using UnityEngine;
using YooAsset.Editor;
using YooAsset;

namespace GameLaunch
{
    public class AssetBuilder
    {
        public static void BuildInternal(bool copyToStreamingAsset, BuildTarget buildTarget, string buildVersion)
        {
            Debug.Log($"开始构建 : {buildTarget}");
            ScriptableBuildParameters scriptableBuildParameters = new ScriptableBuildParameters();
            // 执行构建
            IBuildPipeline pipeline = new ScriptableBuildPipeline();
            BuildParameters buildParameters = scriptableBuildParameters;

            scriptableBuildParameters.CompressOption = ECompressOption.LZ4;
            buildParameters.BuildOutputRoot = BundleBuilderHelper.GetDefaultBuildOutputRoot();
            buildParameters.BundledFileRoot = BundleBuilderHelper.GetStreamingAssetsRoot();
            buildParameters.BuildPipeline = EBuildPipeline.ScriptableBuildPipeline.ToString();
            buildParameters.BuildTarget = buildTarget;
            buildParameters.BuildBundleType = (int)EBundleType.AssetBundle;
            buildParameters.PackageName = GameApp.PackageName;
            buildParameters.PackageVersion = buildVersion;
            buildParameters.VerifyBuildingResult = true;
            // 启用共享资源打包
            buildParameters.EnableSharePackRule = false;
            buildParameters.FileNameStyle = EFileNameStyle.BundleName;
            buildParameters.BundledCopyOption = copyToStreamingAsset ? EBundledCopyOption.ClearAndCopyAll : EBundledCopyOption.None;
            buildParameters.BundledCopyParams = string.Empty;
            buildParameters.BundleEncryptor = CreateBundleEncryptorInstance(GameApp.PackageName, EBuildPipeline.ScriptableBuildPipeline);
            buildParameters.ClearBuildCacheFiles = false; //不清理构建缓存，启用增量构建，可以提高打包速度！
            buildParameters.UseAssetDependencyDB = true; //使用资源依赖关系数据库，可以提高打包速度！


            var buildResult = pipeline.Run(buildParameters, true);
            if (buildResult.Success)
            {
                Debug.Log($"构建成功 : {buildResult.OutputPackageDirectory}");
            }
            else
            {
                Debug.LogError($"构建失败 : {buildResult.ErrorInfo}");
            }
        }

        /// <summary>
        /// 创建资源包加密器实例
        /// </summary>
        private static IBundleEncryptor CreateBundleEncryptorInstance(string packageName, EBuildPipeline buildPipeline)
        {
            var encryptorClassName = BundleBuilderSetting.GetPackageBundleEncryptorClassName(packageName, buildPipeline.ToString());
            if (string.IsNullOrEmpty(encryptorClassName))
                return null;

            var encryptorClassTypes = EditorAssemblyUtility.GetAssignableTypes(typeof(IBundleEncryptor));
            var classType = encryptorClassTypes.Find(x => x.FullName != null && x.FullName.Equals(encryptorClassName));
            if (classType != null)
            {
                Debug.Log($"Use BundleEncryptor {classType}");
                return (IBundleEncryptor)Activator.CreateInstance(classType);
            }

            Debug.LogWarning($"Bundle encryptor class type not found: '{encryptorClassName}'.");
            return null;
        }
    }
}