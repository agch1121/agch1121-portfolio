using UnityEngine;

[CreateAssetMenu(fileName = "NewReachQuest", menuName = "Quest/ReachQuest")]
public class ReachQuest : Quest
{
    [Header("Reach Quest Info")]
    public string LocationName;      // 도달해야 할 위치 이름
    public string locationId;        // 도달해야 할 위치 ID

    [Header("Target Settings")]
    [Tooltip("직접 QuestTriggerZone 프리팹을 참조합니다. 이 값이 설정되면 좌표 및 거리 정보는 이 프리팹에서 가져옵니다.")]
    public QuestTriggerZone targetZone;   // 목표 트리거 존 프리팹

    [Tooltip("런타임에 생성할 목표 위치 프리팹입니다. 이 프리팹은 퀘스트가 활성화될 때 생성되고 비활성화될 때 제거됩니다.")]
    public GameObject targetPrefab;       // 씬에 생성할 목표 위치 프리팹

    [Tooltip("targetZone이 없을 경우에만 사용됩니다.")]
    public Vector3 TargetLocation;   // 도달해야 할 위치 좌표 (targetZone이 없을 경우 사용)

    [Tooltip("targetZone이 없을 경우에만 사용됩니다.")]
    public float reachDistance = 3f; // 도달 인정 거리 (QuestTriggerZone 크기 조절용)

    [Header("Trigger Options")]
    public bool requiresTriggerStay = false;    // 특정 시간동안 트리거에 머물러야 하는지
    public float requiredStayTime = 0f;         // 트리거에 머물러야 하는 시간(초)

    private float currentStayTime = 0f;         // 현재까지 트리거에 머문 시간

    private void OnEnable()
    {
        QuestType = QuestType.Reach; // 퀘스트 타입 설정
        requiredProgress = 1;        // 도달 퀘스트는 항상 1로 설정
    }

    public override string GetProgressText()
    {
        if (currentProgress >= requiredProgress)
        {
            return $"{LocationName}에 도달";
        }

        if (requiresTriggerStay && currentStayTime > 0)
        {
            float remainingTime = requiredStayTime - currentStayTime;
            return $"{LocationName}에서 {remainingTime:F1}초 더 머물기";
        }

        return $"{LocationName}로 이동하기";
    }

    public override bool CheckCondition(string conditionType, string conditionId)
    {
        // 위치 도달 이벤트 확인
        if (conditionType == "Reach" && conditionId == locationId && State == QuestState.InProgress)
        {
            return true;
        }
        return false;
    }

    // 특정 위치 도달 시 호출될 메서드
    public void LocationReached()
    {
        if (State != QuestState.InProgress) return;

        // 머무는 시간이 필요 없으면 바로 진행도 업데이트
        if (!requiresTriggerStay)
        {
            UpdateProgress(1); // 도달 완료
        }
    }

    // 현재 위치가 목표에 도달했는지 확인
    public bool CheckReached(Vector3 playerPosition)
    {
        // targetZone이 설정되어 있다면 그 정보를 사용
        if (targetZone != null)
        {
            // targetZone의 Collider2D 컴포넌트를 통해 플레이어가 영역 내에 있는지 확인
            Collider2D targetCollider = targetZone.GetComponent<Collider2D>();
            if (targetCollider != null)
            {
                // 플레이어 위치가 트리거 존의 콜라이더 영역 내에 있는지 확인
                return targetCollider.OverlapPoint(playerPosition);
            }
        }

        // targetZone이 없거나 콜라이더를 가져올 수 없으면 기존 거리 기반 확인 사용
        float distance = Vector3.Distance(playerPosition, TargetLocation);
        return distance <= reachDistance;
    }

    // 트리거 영역 진입 시 거리 체크 없이 도달 처리
    public void TriggerEntered()
    {
        if (State != QuestState.InProgress) return;

        // 머무는 시간이 필요 없으면 바로 진행도 업데이트
        if (!requiresTriggerStay)
        {
            UpdateProgress(1); // 도달 완료
        }
    }

    // 플레이어가 트리거 영역에 머무는 동안 호출
    public void UpdateTriggerStay(float deltaTime)
    {
        if (State != QuestState.InProgress || !requiresTriggerStay) return;

        currentStayTime += deltaTime;

        // 필요한 시간만큼 머물렀는지 확인
        if (currentStayTime >= requiredStayTime)
        {
            UpdateProgress(1); // 도달 완료
        }
    }

    // 플레이어가 트리거 영역을 벗어났을 때 호출
    public void ResetTriggerStay()
    {
        if (requiresTriggerStay && currentStayTime > 0 && currentProgress < requiredProgress)
        {
            currentStayTime = 0f;
        }
    }

    // 런타임에 targetZone 설정 메서드
    public void SetTargetZone(QuestTriggerZone zone)
    {
        if (zone != null)
        {
            targetZone = zone;
            locationId = zone.GetLocationId();
            LocationName = zone.GetLocationName();
            TargetLocation = zone.transform.position;
        }
    }

#if UNITY_EDITOR
    // 에디터에서 디버그 정보 표시
    public bool showDebugMarker = false;

    // 에디터에서만 사용되는 시각화 메서드
    public void DrawDebugMarker()
    {
        if (!showDebugMarker) return;

        Vector3 targetPos = targetZone != null ? targetZone.transform.position : TargetLocation;

        // Gizmo 색상 설정
        UnityEditor.Handles.color = Color.green;

        // 위치 표시 구체
        UnityEditor.Handles.SphereHandleCap(0, targetPos, Quaternion.identity, 0.5f, EventType.Repaint);

        // 위치 이름 표시
        UnityEditor.Handles.Label(targetPos + Vector3.up, LocationName);
    }

    // 검증 기능
    private void OnValidate()
    {
        if (targetZone != null)
        {
            // targetZone에서 정보 가져오기
            string zoneLocationId = targetZone.GetLocationId();
            string zoneLocationName = targetZone.GetLocationName();

            // ID나 이름이 비어있지 않고 현재 설정과 다르면 동기화
            if (!string.IsNullOrEmpty(zoneLocationId) && locationId != zoneLocationId)
            {
                locationId = zoneLocationId;
            }

            if (!string.IsNullOrEmpty(zoneLocationName) && LocationName != zoneLocationName)
            {
                LocationName = zoneLocationName;
            }
        }
    }
#endif
}