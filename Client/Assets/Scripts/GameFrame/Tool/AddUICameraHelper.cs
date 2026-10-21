using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameEngine
{
    [RequireComponent(typeof(Camera))]
    public class AddUICameraHelper : MonoBehaviour
    {
        private void Start()
        {
            if (UIModule.Instance != null)
            {
                AddOverlayCameraAsBase(GetComponent<Camera>(), UIModule.Instance.UICamera);
                AddOverlayCameraAsBase(GetComponent<Camera>(), UIModule.Instance.UIRoleCamera);
            }
        }

        private void AddOverlayCameraAsBase(Camera toBaseCamera, Camera toOverlayCamera)
        {
            UniversalAdditionalCameraData toBaseData = toBaseCamera.GetUniversalAdditionalCameraData();
            UniversalAdditionalCameraData toOverlayData = toOverlayCamera.GetUniversalAdditionalCameraData();

            if (toBaseData.renderType != CameraRenderType.Base)
            {
                toBaseData.renderType = CameraRenderType.Base;
            }

            if (toOverlayData.renderType != CameraRenderType.Overlay)
            {
                toOverlayData.renderType = CameraRenderType.Overlay;
            }

            if (!toBaseData.cameraStack.Contains(toOverlayCamera))
            {
                toBaseData.cameraStack.Add(toOverlayCamera);
            }
        }

    }
}