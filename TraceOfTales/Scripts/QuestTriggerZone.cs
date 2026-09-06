using UnityEngine;
using System.Collections.Generic;
using System;

public class QuestTriggerZone : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private string locationId;       // 위치 ID (ReachQuest의 locationId와 일치해야 함)
    [SerializeField] private string locationName;     // 위치 이름 (표시용)

    [Header("Associated Quests")]
    [Tooltip("이 트리거 존과 연관된 ReachQuest 목록")]
    [SerializeField] private List<ReachQuest> linkedQuests = new List<ReachQuest>();

    [Header("Gizmo Settings")]
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);
    [SerializeField] private bool showGizmo = true;

    // 추가적인 이벤트
    public event Action<QuestTriggerZone> OnPlayerEnter;
    public event Action<QuestTriggerZone> OnPlayerExit;

    private bool playerInTrigger = false;
    private Transform playerTransform;

    // 위치 ID getter
    public string GetLocationId()
    {
        return locationId;
    }

    // 위치 이름 getter
    public string GetLocationName()
    {
        return locationName;
    }

    private void OnValidate()
    {
        // 콜라이더가 없으면 자동으로 추가
        if (GetComponent<Collider2D>() == null)
        {
            Debug.LogWarning($"QuestTriggerZone '{locationName}'에 Collider2D가 없습니다. 자동으로 추가됩니다.");

            // 기본 BoxCollider2D 추가
            BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector2(5f, 5f);
        }
        else
        {
            // 이미 있는 콜라이더가 트리거로 설정되어 있는지 확인
            Collider2D[] colliders = GetComponents<Collider2D>();
            foreach (Collider2D collider in colliders)
            {
                if (!collider.isTrigger)
                {
                    Debug.LogWarning($"QuestTriggerZone '{locationName}'의 콜라이더가 트리거로 설정되지 않았습니다.");
                    collider.isTrigger = true;
                }
            }
        }

        // 연결된 퀘스트 업데이트
        UpdateLinkedQuests();
    }

    // 연결된 퀘스트들의 정보를 동기화
    private void UpdateLinkedQuests()
    {
        if (linkedQuests == null) return;

        for (int i = linkedQuests.Count - 1; i >= 0; i--)
        {
            ReachQuest quest = linkedQuests[i];
            if (quest == null)
            {
                linkedQuests.RemoveAt(i);
                continue;
            }

            // 퀘스트에 현재 트리거 존 정보 설정
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(quest, "Update Linked Quest");
            quest.targetZone = this;
            quest.locationId = locationId;
            quest.LocationName = locationName;
            quest.TargetLocation = transform.position;
            UnityEditor.EditorUtility.SetDirty(quest);
#endif
        }
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(locationId))
        {
            Debug.LogError($"QuestTriggerZone '{locationName}'의 locationId가 설정되지 않았습니다!");
        }

        // 연결된 퀘스트 설정 (런타임)
        foreach (ReachQuest quest in linkedQuests)
        {
            if (quest != null)
            {
                quest.SetTargetZone(this);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerTransform = other.transform;
        playerInTrigger = true;

        GameEvents.LocationReached(locationId, playerTransform.position);

        // 연결된 퀘스트 진행 처리
        ProcessLinkedQuests();

        // 이벤트 발생
        OnPlayerEnter?.Invoke(this);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!playerInTrigger || !other.CompareTag("Player")) return;

        // 시간 체크가 필요한 퀘스트만 처리
        UpdateLinkedQuestsStayTime();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInTrigger = false;

        // 머무름 시간 초기화
        ResetLinkedQuestsStayTime();

        // 이벤트 발생
        OnPlayerExit?.Invoke(this);
    }

    // 연결된 퀘스트들 직접 처리
    private void ProcessLinkedQuests()
    {
        foreach (ReachQuest quest in linkedQuests)
        {
            if (quest != null && quest.State == QuestState.InProgress)
            {
                quest.TriggerEntered();
            }
        }

        // 퀘스트 매니저 통해 처리 (추가적인 퀘스트 대응)
        if (QuestManager.Instance != null)
        {
            foreach (Quest quest in QuestManager.Instance.GetActiveQuests())
            {
                if (quest is ReachQuest reachQuest &&
                    reachQuest.State == QuestState.InProgress &&
                    reachQuest.locationId == locationId &&
                    !reachQuest.requiresTriggerStay &&
                    !linkedQuests.Contains(reachQuest))
                {
                    // 시간 체크가 필요없는 퀘스트는 즉시 완료 처리
                    reachQuest.LocationReached();
                }
            }
        }
    }

    // 체류 시간이 필요한 퀘스트 업데이트
    private void UpdateLinkedQuestsStayTime()
    {
        // 연결된 퀘스트 처리
        foreach (ReachQuest quest in linkedQuests)
        {
            if (quest != null && quest.State == QuestState.InProgress && quest.requiresTriggerStay)
            {
                quest.UpdateTriggerStay(Time.deltaTime);
            }
        }

        // 퀘스트 매니저 통해 추가 퀘스트 처리
        if (QuestManager.Instance != null)
        {
            foreach (Quest quest in QuestManager.Instance.GetActiveQuests())
            {
                if (quest is ReachQuest reachQuest &&
                    reachQuest.State == QuestState.InProgress &&
                    reachQuest.locationId == locationId &&
                    reachQuest.requiresTriggerStay &&
                    !linkedQuests.Contains(reachQuest))
                {
                    reachQuest.UpdateTriggerStay(Time.deltaTime);
                }
            }
        }
    }

    // 퀘스트 매니저를 통해 이 위치를 목표로 하는 머무름 퀘스트 시간 초기화
    private void ResetLinkedQuestsStayTime()
    {
        // 연결된 퀘스트 처리
        foreach (ReachQuest quest in linkedQuests)
        {
            if (quest != null && quest.State == QuestState.InProgress && quest.requiresTriggerStay)
            {
                quest.ResetTriggerStay();
            }
        }

        // 퀘스트 매니저 통해 추가 퀘스트 처리
        if (QuestManager.Instance != null)
        {
            foreach (Quest quest in QuestManager.Instance.GetActiveQuests())
            {
                if (quest is ReachQuest reachQuest &&
                    reachQuest.State == QuestState.InProgress &&
                    reachQuest.locationId == locationId &&
                    reachQuest.requiresTriggerStay &&
                    !linkedQuests.Contains(reachQuest))
                {
                    reachQuest.ResetTriggerStay();
                }
            }
        }
    }

    // 퀘스트 연결/해제 메서드
    public void LinkQuest(ReachQuest quest)
    {
        if (quest != null && !linkedQuests.Contains(quest))
        {
            linkedQuests.Add(quest);
            quest.SetTargetZone(this);
        }
    }

    public void UnlinkQuest(ReachQuest quest)
    {
        if (quest != null && linkedQuests.Contains(quest))
        {
            linkedQuests.Remove(quest);
        }
    }

    // Scene에서 시각적으로 표시하기 위한 Gizmo
    private void OnDrawGizmos()
    {
        if (!showGizmo) return;

        Gizmos.color = gizmoColor;

        // 콜라이더 형태에 따라 다른 모양으로 시각화
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null) return;

        if (collider is BoxCollider2D boxCollider)
        {
            // 박스 콜라이더 시각화
            Vector3 size = new Vector3(boxCollider.size.x, boxCollider.size.y, 1f) * transform.lossyScale.x;
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawCube(boxCollider.offset, size);
            Gizmos.matrix = oldMatrix;
        }
        else if (collider is CircleCollider2D circleCollider)
        {
            // 원형 콜라이더 시각화
            Gizmos.DrawSphere(transform.position + new Vector3(circleCollider.offset.x, circleCollider.offset.y, 0),
                circleCollider.radius * transform.lossyScale.x);
        }

#if UNITY_EDITOR
        // 위치 이름 표시
        Gizmos.color = Color.white;
        Vector3 labelPosition = transform.position + Vector3.up * 2f;
        UnityEditor.Handles.Label(labelPosition, locationName + " (ID: " + locationId + ")");

        // 연결된 퀘스트 개수 표시
        if (linkedQuests != null && linkedQuests.Count > 0)
        {
            int validCount = 0;
            foreach (var quest in linkedQuests)
            {
                if (quest != null) validCount++;
            }

            if (validCount > 0)
            {
                UnityEditor.Handles.Label(labelPosition + Vector3.up * 0.5f, $"연결된 퀘스트: {validCount}개");
            }
        }
#endif
    }

#if UNITY_EDITOR
    // 에디터 전용 메서드 - 연결된 퀘스트 새로고침
    public void RefreshLinkedQuests()
    {
        UpdateLinkedQuests();
    }

    // 에디터 전용 - 연결된 모든 퀘스트 찾기
    public void FindAllLinkedQuests()
    {
        // 프로젝트의 모든 ReachQuest 에셋 검색
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ReachQuest");
        List<ReachQuest> newLinkedQuests = new List<ReachQuest>();

        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            ReachQuest quest = UnityEditor.AssetDatabase.LoadAssetAtPath<ReachQuest>(path);

            if (quest != null && quest.locationId == locationId)
            {
                newLinkedQuests.Add(quest);
                Debug.Log($"QuestTriggerZone '{locationName}': 연관된 퀘스트 찾음 - '{quest.QuestName}'");
            }
        }

        if (newLinkedQuests.Count > 0)
        {
            UnityEditor.Undo.RecordObject(this, "Find Linked Quests");
            linkedQuests = newLinkedQuests;
            UnityEditor.EditorUtility.SetDirty(this);

            // 연결된 퀘스트 업데이트
            UpdateLinkedQuests();
        }
        else
        {
            Debug.Log($"QuestTriggerZone '{locationName}': 위치 ID '{locationId}'와 연관된 퀘스트를 찾을 수 없습니다.");
        }
    }
#endif
}