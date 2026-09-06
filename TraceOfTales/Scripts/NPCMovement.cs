using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NPCMovement : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waypointRadius = 0.1f;
    private NPC npcBase;
    private Animator animator;

    private bool isMoving;
    private bool isInteract;
    private float waitTimer;
    private Rigidbody2D rb;
    private Transform currentWaypoint;

    private readonly string isMovingParam = "1_Move";
    private readonly string isIdleParam = "0_Idle";

    public bool IsInteract { get => isInteract; set => isInteract = value; }

    // 기본 정보 설정
    public void Initialize(NPC npcBase, Animator animator, Rigidbody2D rb)
    {
        this.npcBase = npcBase;
        this.animator = animator;
        this.rb = rb;
        SetRandomWaypoint();
    }

    private void FixedUpdate()
    {
        if(waypoints.Length <= 0) StopMoving();

        // 상점이 열려 있거나 대화 중이면 움직임 중지
        if (isInteract || (ShopManager.Instance != null && ShopManager.Instance.IsShopOpen))
        {
            StopMoving();
            return;
        }

        if (isMoving)
        {
            MoveToWaypoint();
        }
        // 대기 시간이 끝나면 다음 웨이포인트로 이동
        else if (waitTimer <= 0)
        {
            SetRandomWaypoint();
            StartMoving();
        }
        else
        {
            waitTimer -= Time.fixedDeltaTime;
        }
    }

    private void MoveToWaypoint()
    {
        if (currentWaypoint == null || rb == null || npcBase == null) return;

        Vector3 direction = currentWaypoint.position - transform.position;

        // 만약 웨이포인트에 도착했다면 이동 중지
        if (direction.magnitude <= waypointRadius)
        {
            StopMoving();
            return;
        }

        // NPC 이동
        direction.Normalize();
        rb.MovePosition(rb.position + (Vector2)(direction * npcBase.MoveSpeed * Time.fixedDeltaTime));

        // 이동 방향에 따라 회전 (오른쪽이면 y축 180도 회전)
        float originalScaleX = Mathf.Abs(transform.localScale.x); // 원래 크기 유지
        if (direction.x > 0)
            transform.localScale = new Vector3(-originalScaleX, transform.localScale.y, transform.localScale.z); // 우측 이동 → 반전
        else if (direction.x < 0)
            transform.localScale = new Vector3(originalScaleX, transform.localScale.y, transform.localScale.z); // 좌측 이동 → 원래대로
    }

    private void SetRandomWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        // 유효한 waypoint 목록 생성(null과 이미 방문한 waypoint 제외)
        List<Transform> validWaypoints = waypoints.Where(wp => wp != null && wp != currentWaypoint).ToList();

        if (validWaypoints.Count == 0)
        {
            return;
        }
        // 유효한 waypoint 중 랜덤으로 선택
        currentWaypoint = validWaypoints[Random.Range(0, validWaypoints.Count)];
    }


    private void StartMoving()
    {
        // 상점이 열려 있으면 움직이지 않음
        if (ShopManager.Instance != null && ShopManager.Instance.IsShopOpen)
        {
            return;
        }

        isMoving = true;
        animator?.SetBool(isIdleParam, false);
        animator?.SetBool(isMovingParam, true);
    }

    private void StopMoving()
    {
        isMoving = false;
        animator?.SetBool(isMovingParam, false);
        animator?.SetBool(isIdleParam, true);

        if (npcBase == null)
        {
            Debug.LogError("npcBase가 설정되지 않아 대기 시간을 설정할 수 없습니다.");
            return;
        }

        waitTimer = Random.Range(npcBase.MinWaitTime, npcBase.MaxWaitTime);
    }
}