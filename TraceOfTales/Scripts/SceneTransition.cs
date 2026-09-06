using UnityEngine;

public class SceneTransition : MonoBehaviour
{
    [Header("Scene Transition Settings")]
    [SerializeField] private string targetSceneName = "Golden Plain";
    [SerializeField] private KeyCode transitionKey = KeyCode.M;

    [Header("Current Scene Info")]
    [SerializeField] private string currentSceneName = "Gwionid Forest";

    [Header("UI Restrictions")]
    [Tooltip("다른 UI가 열려있을 때 씬 전환을 막을지 여부")]
    [SerializeField] private bool blockWhenUIOpen = true;

    private void Update()
    {
        // M키 입력 감지
        if (Input.GetKeyDown(transitionKey))
        {
            // UI가 열려있는지 확인 (선택사항)
            if (blockWhenUIOpen && IsAnyUIOpen())
            {
                Debug.Log("UI가 열려있어 씬 전환이 차단되었습니다.");
                return;
            }

            // 씬 전환 실행
            TransitionToScene();
        }
    }

    private void TransitionToScene()
    {
        // SceneLoadingManager를 통해 씬 전환
        if (SceneLoadingManager.Instance != null)
        {
            Debug.Log($"씬 전환 시작: {currentSceneName} → {targetSceneName}");
            SceneLoadingManager.Instance.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError("SceneLoadingManager 인스턴스를 찾을 수 없습니다!");

            // 대안으로 직접 씬 로드
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetSceneName);
        }
    }

    private bool IsAnyUIOpen()
    {
        // 대화창이 열려있는지 확인
        if (DialogueManager.Instance != null &&
            DialogueManager.Instance.DialoguePanel != null &&
            DialogueManager.Instance.DialoguePanel.activeSelf)
        {
            return true;
        }

        // 퀘스트 UI가 열려있는지 확인
        if (QuestUIManager.Instance != null)
        {
            // QuestUIManager에 IsQuestPanelActive 같은 프로퍼티가 있다면 사용
            // 없다면 직접 패널 확인
        }

        // 상점이 열려있는지 확인
        if (ShopManager.Instance != null && ShopManager.Instance.IsShopOpen)
        {
            return true;
        }

        // 인벤토리가 열려있는지 확인
        if (UIManager.Instance != null)
        {
            // UIManager의 인벤토리 패널 상태 확인이 필요하다면 추가
        }

        return false;
    }

    // 에디터에서 테스트용 메서드
    [ContextMenu("Test Scene Transition")]
    private void TestTransition()
    {
        TransitionToScene();
    }

    // 다른 스크립트에서 호출할 수 있는 공개 메서드
    public void TriggerSceneTransition()
    {
        TransitionToScene();
    }

    // 설정 변경을 위한 메서드들
    public void SetTargetScene(string sceneName)
    {
        targetSceneName = sceneName;
    }

    public void SetTransitionKey(KeyCode key)
    {
        transitionKey = key;
    }
}