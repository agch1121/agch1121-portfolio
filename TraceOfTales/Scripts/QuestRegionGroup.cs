// QuestRegionGroup.cs 수정

using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class QuestRegionGroup : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI regionNameText;
    [SerializeField] private TextMeshProUGUI questCountText;
    [SerializeField] private Transform contentContainer; // 퀘스트 아이템들이 들어갈 컨테이너
    [SerializeField] private Toggle toggleButton;
    [SerializeField] private GameObject questItemContainer; // 전체 컨테이너

    [Header("Layout Settings")]
    [SerializeField] private float topPadding = 10f;
    [SerializeField] private float bottomPadding = 10f;
    [SerializeField] private float questItemSpacing = 10f; // 간격 증가
    [SerializeField] private float questItemHeight = 40f; // 명시적 높이 지정

    // 퀘스트 개수에 따른 추가 공간 설정
    [SerializeField] private float additionalSpacePerQuest = 10f; // 퀘스트 하나당 추가 공간

    private List<GameObject> questItems = new List<GameObject>();
    private QuestRegion region;
    private bool isExpanded = true;
    private int questCount = 0;

    // 레이아웃 컴포넌트 참조
    private LayoutElement layoutElement;
    private VerticalLayoutGroup contentLayoutGroup;
    private ContentSizeFitter contentSizeFitter;

    private void Awake()
    {
        // 컴포넌트 초기화
        layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }

        contentLayoutGroup = contentContainer.GetComponent<VerticalLayoutGroup>();
        if (contentLayoutGroup == null)
        {
            contentLayoutGroup = contentContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        contentSizeFitter = contentContainer.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter == null)
        {
            contentSizeFitter = contentContainer.gameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    public void Initialize(QuestRegion region, string regionName, int questCount)
    {
        this.region = region;
        this.questCount = questCount;
        regionNameText.text = regionName;
        questCountText.text = $"({questCount})";

        // 토글 버튼 이벤트 등록 - 이전 리스너 제거 후 등록
        toggleButton.onValueChanged.RemoveAllListeners();
        toggleButton.onValueChanged.AddListener(OnToggleChanged);
        toggleButton.isOn = true;

        // 레이아웃 설정
        ConfigureLayout();

        // 초기 크기 설정
        UpdateContainerSize();
    }

    private void ConfigureLayout()
    {
        // 콘텐츠 레이아웃 설정
        if (contentLayoutGroup != null)
        {
            contentLayoutGroup.padding = new RectOffset((int)10, (int)10, (int)topPadding, (int)bottomPadding);
            contentLayoutGroup.spacing = questItemSpacing;
            contentLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            contentLayoutGroup.childControlHeight = true; // 중요: 자식 높이 제어
            contentLayoutGroup.childForceExpandHeight = false; // 자식 강제 확대 비활성화
        }

        // ContentSizeFitter 설정
        if (contentSizeFitter != null)
        {
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    private void UpdateContainerSize()
    {
        // 그룹 전체 높이 계산
        if (layoutElement != null)
        {
            float headerHeight = 40f; // 헤더 영역 높이

            if (isExpanded && questItems.Count > 0)
            {
                // 펼친 상태: 헤더 + 모든 퀘스트 아이템 높이 + 퀘스트 개수에 따른 추가 공간
                float totalItemsHeight = questItems.Count * (questItemHeight + questItemSpacing) + topPadding + bottomPadding;

                // 퀘스트 개수에 따른 추가 공간 계산
                float additionalHeight = questCount * additionalSpacePerQuest;

                layoutElement.minHeight = headerHeight + totalItemsHeight + additionalHeight;
            }
            else
            {
                // 접힌 상태: 헤더만
                layoutElement.minHeight = headerHeight;
            }
        }
    }

    public void AddQuestItem(GameObject questItem)
    {
        questItems.Add(questItem);

        // LayoutElement 추가 및 설정
        LayoutElement itemLayout = questItem.GetComponent<LayoutElement>();
        if (itemLayout == null)
        {
            itemLayout = questItem.AddComponent<LayoutElement>();
        }

        // 고정된 높이 설정 (매우 중요)
        itemLayout.minHeight = questItemHeight;
        itemLayout.preferredHeight = questItemHeight;

        // 부모에 추가
        questItem.transform.SetParent(contentContainer, false);
        questItem.transform.localScale = Vector3.one;

        // RectTransform 설정
        RectTransform questRectTransform = questItem.GetComponent<RectTransform>();
        if (questRectTransform != null)
        {
            // 앵커 설정
            questRectTransform.anchorMin = new Vector2(0, 1);
            questRectTransform.anchorMax = new Vector2(1, 1);
            questRectTransform.pivot = new Vector2(0.5f, 1);
            questRectTransform.anchoredPosition = Vector2.zero;

            // 높이 설정
            questRectTransform.sizeDelta = new Vector2(questRectTransform.sizeDelta.x, questItemHeight);
        }

        // 컨테이너 크기 업데이트
        UpdateContainerSize();

        // 레이아웃 즉시 갱신
        Canvas.ForceUpdateCanvases();
        if (contentContainer is RectTransform contentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
        if (transform.parent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform);
        }
    }

    public void ClearQuests()
    {
        foreach (var item in questItems)
        {
            Destroy(item);
        }
        questItems.Clear();

        // 컨테이너 크기 업데이트
        UpdateContainerSize();
    }

    private void OnToggleChanged(bool isOn)
    {
        isExpanded = isOn;
        questItemContainer.SetActive(isOn);

        // 컨테이너 크기 업데이트
        UpdateContainerSize();

        // 레이아웃 즉시 갱신 - 부모부터 갱신
        Canvas.ForceUpdateCanvases();
        if (transform.parent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform);
        }
    }

    public QuestRegion GetRegion()
    {
        return region;
    }

    public void UpdateQuestCount(int count)
    {
        this.questCount = count;
        questCountText.text = $"({count})";
        UpdateContainerSize(); // 퀘스트 개수 변경시 크기 업데이트
    }
}