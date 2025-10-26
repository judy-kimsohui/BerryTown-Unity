using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using System;

namespace TestCamera
{
    public class CameraManager : MonoBehaviour
    {
        [Header("카메라 목록")]
        [SerializeField] private CinemachineCamera[] virtualCameras;
        [SerializeField] private PlayerInputHandler inputHandler;

        private int currentIndex = 0;
        private bool isCinematicActive = false;

        private void Awake()
        {
            // inputHandler = GetComponent<PlayerInputHandler>();
        }
        
        private void Start()
        {
            ActivateCamera(0); // Player 카메라 시작
        }

        private void Update()
        {
            // if (isCinematicActive || inputHandler == null) return;

            // 핸들러 입력 기반으로 전환
            if (inputHandler.Camera_PlayerPressed)
                ActivateCamera(0);
            else if (inputHandler.Camera_NPC1Pressed)
                ActivateCamera(1);
            else if (inputHandler.Camera_NPC2Pressed)
                ActivateCamera(2);
            else if (inputHandler.Camera_NPC3Pressed)
                ActivateCamera(3);

            // 컷신 트리거
            // if (inputHandler.Camera_CutScenePressed)
            //     StartCoroutine(PlayCinematic(3, 3f));
        }

        private void ActivateCamera(int index)
        {
            if (index < 0 || index >= virtualCameras.Length) return;

            // 대상 카메라만 높임
            virtualCameras[index].Priority = 10;
            virtualCameras[currentIndex].Priority = 5;
            
            Debug.Log("index : " + index + "currentIndex : " + currentIndex);

            currentIndex = index;
            Debug.Log($"[CameraManager] Switched to: {virtualCameras[index].name}");
        }


        private IEnumerator PlayCinematic(int index, float duration)
        {
            isCinematicActive = true;

            // 현재 카메라 기억
            int prev = currentIndex;

            // 컷신 카메라 활성화
            ActivateCamera(index);
            Debug.Log("🎬 컷신 시작!");

            yield return new WaitForSeconds(duration);

            // 항상 Player 카메라(0)로 복귀
            ActivateCamera(0);
            Debug.Log("🎬 컷신 종료 → 플레이어 카메라 복귀");

            isCinematicActive = false;
        }

    }
}
