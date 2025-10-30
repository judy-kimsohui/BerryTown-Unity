using UnityEngine;

public class BasicRigidBodyPush : MonoBehaviour
{
	public LayerMask pushLayers;
	public bool canPush;
	[Range(0.5f, 5f)] public float strength = 1.1f;

	// OnControllerColliderHit : 플레이어가 상대 오브젝트의 콜라이더와 충돌했을 때 자동으로 호출됨
	// ControllerColliderHit : 충돌의 세부 데이터
	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		if (canPush) PushRigidBodies(hit);
	}

	// Rigidbody : 물리엔진이 오브젝트를 실제로 움직이게 해주는 컴포넌트
		// 중력(gravity)의 영향을 받음
		// 다른 물체와 충돌하면 반발하거나 멈춤
		// AddForce(), AddTorque() 같은 물리 힘을 받을 수 있음
		// 충돌 처리 OnCollisionEnter(), 트리거 감지 OnTriggerEnter()
	private void PushRigidBodies(ControllerColliderHit hit)
	{
		// https://docs.unity3d.com/ScriptReference/CharacterController.OnControllerColliderHit.html

		// make sure we hit a non kinematic rigidbody
		Rigidbody body = hit.collider.attachedRigidbody;

		// Is Kinematic : 물리 영향 무시 (직접 스크립트로 움직일 때 켬)
		if (body == null || body.isKinematic) return;

		// Layer : 오브젝트들을 그룹으로 나눠서, 렌더링이나 충돌 판정 같은 걸 구분할 수 있게 해주는 분류 체계
		// make sure we only push desired(원하는) layer(s)
		var bodyLayerMask = 1 << body.gameObject.layer;
		if ((bodyLayerMask & pushLayers.value) == 0) return;

		// We dont want to push objects below us
		if (hit.moveDirection.y < -0.3f) return;

		// Calculate push direction from move direction, horizontal motion only
		Vector3 pushDir = new Vector3(hit.moveDirection.x, 0.0f, hit.moveDirection.z);

		// Apply the push and take strength into account
		body.AddForce(pushDir * strength, ForceMode.Impulse);
	}
}