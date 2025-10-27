using UnityEngine;

public class CloudPet : MonoBehaviour
{
    [Header("Follow Target (Player)")]
    [SerializeField] private Transform player;

    [Header("Follow Settings")]
    [SerializeField] private float followSpeed = 3f;     // 따라오는 속도
    [SerializeField] private float xOffset = -1.5f;      // 플레이어 뒤에 위치할 거리 (음수면 왼쪽)
    [SerializeField] private float smoothTime = 0.15f;   // 부드럽게 따라올 정도 (낮을수록 빠름)

    private float velocityX; // SmoothDamp용 내부 속도 추적 변수

    void Start()
    {
        player = GameManager.Instance.player.transform;
    }

    void Update()
    {
        if (player == null) return;

        // 목표 위치: 플레이어의 x좌표 + 오프셋
        float targetX = player.position.x + xOffset;

        // 현재 x에서 목표 x로 부드럽게 이동
        float newX = Mathf.SmoothDamp(transform.position.x, targetX, ref velocityX, smoothTime);

        // 실제로 이동 (y, z는 그대로)
        transform.position = new Vector3(newX, -1.8f, transform.position.z);
    }
}
