using UnityEngine;
using Unity.Cinemachine;

public class CameraDebug : MonoBehaviour
{
    void Update()
    {
        var brain = GetComponent<CinemachineBrain>();
        if (brain != null && brain.ActiveVirtualCamera != null)
        {
            Debug.Log($"🎥 Active VCam: {brain.ActiveVirtualCamera.Name}");
        }
    }
}
