using UnityEngine;

/// <summary>
/// 위치 도달 퀘스트의 시각적 인디케이터를 관리하는 컴포넌트
/// QuestTriggerZone 프리팹의 자식으로 배치되어 플레이어의 도달 상태를 표시
/// </summary>
public class QuestMarker : MonoBehaviour
{
    [Header("Indicator Sprites")]
    [SerializeField] private GameObject exclamationSprite; // 느낌표 (밟기 전)
    [SerializeField] private GameObject questionSprite;    // 물음표 (밟은 후)

    [Header("Animation Settings")]
    [SerializeField] private bool enableFloatingAnimation = true;
    [SerializeField] private float floatingSpeed = 2f;
    [SerializeField] private float floatingHeight = 0.5f;

    private QuestTriggerZone parentTriggerZone;
    private bool isPlayerInside = false;
    private Vector3 originalPosition;

    private void Awake()
    {
        // 부모의 QuestTriggerZone 컴포넌트 찾기
        parentTriggerZone = GetComponentInParent<QuestTriggerZone>();
        if (parentTriggerZone == null)
        {
            Debug.LogError($"{gameObject.name}: 부모에서 QuestTriggerZone을 찾을 수 없습니다!");
            return;
        }

        // 초기 상태 설정 (느낌표 활성화, 물음표 비활성화)
        SetIndicatorState(false);

        // 애니메이션을 위한 원래 위치 저장
        originalPosition = transform.localPosition;
    }

    private void Start()
    {
        // QuestTriggerZone 이벤트 구독
        if (parentTriggerZone != null)
        {
            parentTriggerZone.OnPlayerEnter += OnPlayerEnterZone;
            parentTriggerZone.OnPlayerExit += OnPlayerExitZone;
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (parentTriggerZone != null)
        {
            parentTriggerZone.OnPlayerEnter -= OnPlayerEnterZone;
            parentTriggerZone.OnPlayerExit -= OnPlayerExitZone;
        }
    }

    private void Update()
    {
        // 부유 애니메이션 처리
        if (enableFloatingAnimation)
        {
            HandleFloatingAnimation();
        }
    }

    /// <summary>
    /// 플레이어가 트리거 존에 진입했을 때 호출
    /// </summary>
    private void OnPlayerEnterZone(QuestTriggerZone zone)
    {
        isPlayerInside = true;
        SetIndicatorState(true); // 물음표로 변경
    }

    /// <summary>
    /// 플레이어가 트리거 존에서 나갔을 때 호출
    /// </summary>
    private void OnPlayerExitZone(QuestTriggerZone zone)
    {
        isPlayerInside = false;

        // 퀘스트가 완료되었다면 인디케이터 숨기기
        if (IsQuestCompleted())
        {
            HideIndicator();
        }
        else
        {
            // 퀘스트가 완료되지 않았다면 다시 느낌표로 변경
            SetIndicatorState(false); // 느낌표로 변경
        }
    }

    /// <summary>
    /// 인디케이터 상태 설정
    /// </summary>
    /// <param name="playerVisited">true: 물음표 표시, false: 느낌표 표시</param>
    private void SetIndicatorState(bool playerVisited)
    {
        if (exclamationSprite != null)
        {
            exclamationSprite.SetActive(!playerVisited);
        }

        if (questionSprite != null)
        {
            questionSprite.SetActive(playerVisited);
        }
    }

    /// <summary>
    /// 관련된 위치 도달 퀘스트가 완료되었는지 확인
    /// </summary>
    private bool IsQuestCompleted()
    {
        if (parentTriggerZone == null || QuestManager.Instance == null)
            return false;

        string locationId = parentTriggerZone.GetLocationId();
        var activeQuests = QuestManager.Instance.GetActiveQuests();

        foreach (var quest in activeQuests)
        {
            if (quest is ReachQuest reachQuest &&
                reachQuest.locationId == locationId &&
                reachQuest.State == QuestState.InProgress &&
                reachQuest.IsCompleted())
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 부유 애니메이션 처리
    /// </summary>
    private void HandleFloatingAnimation()
    {
        float newY = originalPosition.y + Mathf.Sin(Time.time * floatingSpeed) * floatingHeight;
        transform.localPosition = new Vector3(originalPosition.x, newY, originalPosition.z);
    }

    /// <summary>
    /// 퀘스트 완료 시 인디케이터를 완전히 숨기는 메서드
    /// </summary>
    public void HideIndicator()
    {
        if (exclamationSprite != null)
            exclamationSprite.SetActive(false);

        if (questionSprite != null)
            questionSprite.SetActive(false);
    }
}