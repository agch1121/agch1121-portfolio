using UnityEngine;

public class NPCController : MonoBehaviour
{
    [SerializeField] private NPC npcBase;
    private GameObject interactionBubble;
    private DialogueManager dialogueManager;

    private NPCMovement npcMove;
    private NPCInteraction npcInteract;
    private Animator animator;
    private Rigidbody2D rb;

    private void Awake()
    {
        // Awake에서 모든 초기화를 처리하여 Start() 순서 문제 해결
        InitializeAllComponents();
    }

    private void InitializeAllComponents()
    {
        // 1. 컴포넌트 참조 설정
        animator = GetComponentInChildren<Animator>();
        npcMove = GetComponent<NPCMovement>();
        npcInteract = GetComponent<NPCInteraction>();
        rb = GetComponent<Rigidbody2D>();

        // 2. 필수 컴포넌트 검증
        if (!ValidateComponents())
        {
            return; // 필수 컴포넌트가 없으면 초기화 중단
        }

        // 3. NPCMovement 초기화 (DialogueManager가 필요 없음)
        if (npcMove != null && npcBase != null && animator != null && rb != null)
        {
            npcMove.Initialize(npcBase, animator, rb);
        }
    }

    private void Start()
    {
        // DialogueManager가 준비될 때까지 대기 후 NPCInteraction 초기화
        InitializeNPCInteraction();
    }

    private void InitializeNPCInteraction()
    {
        // DialogueManager가 초기화될 때까지 대기
        if (DialogueManager.Instance == null)
        {
            Invoke(nameof(InitializeNPCInteraction), 0.1f);
            return;
        }

        // NPCInteraction 초기화
        if (npcInteract != null)
        {
            npcInteract.Initialize(npcBase, interactionBubble, DialogueManager.Instance);
        }
        else
        {
            Debug.LogError($"{gameObject.name}: NPCInteraction이 null입니다!");
        }
    }

    private bool ValidateComponents()
    {
        bool isValid = true;

        if (npcBase == null)
        {
            Debug.LogError($"{gameObject.name}: NPC ScriptableObject가 할당되지 않았습니다!");
            isValid = false;
        }

        if (npcMove == null)
        {
            Debug.LogError($"{gameObject.name}: NPCMovement 컴포넌트를 찾을 수 없습니다!");
            isValid = false;
        }

        if (npcInteract == null)
        {
            Debug.LogError($"{gameObject.name}: NPCInteraction 컴포넌트를 찾을 수 없습니다!");
            isValid = false;
        }

        if (animator == null)
        {
            Debug.LogWarning($"{gameObject.name}: Animator를 찾을 수 없습니다!");
        }

        if (rb == null)
        {
            Debug.LogError($"{gameObject.name}: Rigidbody2D 컴포넌트를 찾을 수 없습니다!");
            isValid = false;
        }

        return isValid;
    }

    // NPC Base 정보 반환 (다른 스크립트에서 사용 가능)
    public NPC GetNPCBase()
    {
        return npcBase;
    }

    // 특정 상호작용 타입이 있는지 확인
    public bool HasInteractionType(InteractionType type)
    {
        if (npcBase != null && npcBase.Interactions != null)
        {
            foreach (var interaction in npcBase.Interactions)
            {
                if (interaction.Type == type)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // 특정 상호작용 객체 가져오기
    public ScriptableObject GetInteractionObject(InteractionType type)
    {
        if (npcBase != null && npcBase.Interactions != null)
        {
            foreach (var interaction in npcBase.Interactions)
            {
                if (interaction.Type == type)
                {
                    return interaction.GetInteractionObject();
                }
            }
        }

        return null;
    }
}