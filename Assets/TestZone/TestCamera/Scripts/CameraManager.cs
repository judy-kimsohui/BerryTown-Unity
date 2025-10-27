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

        private int currentIndex = 4;

        private void Start()
        {
            ActivateCamera(4); // Player 카메라 시작
        }

        private void Update()
        {
            // 핸들러 입력 기반으로 전환
            if (inputHandler.Camera_PlayerPressed)
                ActivateCamera(0);
            else if (inputHandler.Camera_NPC1Pressed)
                ActivateCamera(1);
            else if (inputHandler.Camera_NPC2Pressed)
                ActivateCamera(2);
            else if (inputHandler.Camera_NPC3Pressed)
                ActivateCamera(3);
            else if (inputHandler.Camera_CutScenePressed)
                ActivateCamera(4);
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

        // 외부 참조용 카메라
        public void ActivateCamera(string name)
        {
            if (name == "Player")
            {
                ActivateCamera(0);
            }
            if (name == "Dolly")
            {
                ActivateCamera(4);
            }
        }

        // 컷신용 카메라 참조 반환
        public CinemachineCamera GetCinematicCamera()
        {
            if (virtualCameras.Length > 4)
                return virtualCameras[4]; // 시네마틱 카메라 인덱스
            else
            {
                Debug.LogError("Cinematic Camera가 virtualCameras에 없습니다!");
                return null;
            }
        }

    }
}
