using System.Collections.Generic;
using UnityEngine;

// 상호작용 타입 열거형
public enum InteractionType
{
    Dialogue, // 대화
    Quest,    // 퀘스트
    Shop,     // 상점
}

[CreateAssetMenu(fileName = "New NPC", menuName = "NPC/NPC")]
public class NPC : ScriptableObject
{
    [Header("Basic Info")]
    public string NpcId;
    public string NpcName;
    public Sprite NpcImage;
    public bool CanInteract; // 상호작용 가능 여부

    [Header("Default Dialogue")]
    [Tooltip("NPC의 기본 인사 대사입니다. 대화 선택지 없이 표시됩니다.")]
    [TextArea(2, 5)]
    public string[] DefaultDialogue; // NPC의 기본 인사말 배열

    [Header("Movement")]
    public float MoveSpeed = 3f;
    public float MinWaitTime = 2f;  // 최소 대기(Idle) 시간
    public float MaxWaitTime = 5f;  // 최대 대기(Idle) 시간

    [Header("Interactions")]
    public List<Interaction> Interactions; // 대화, 퀘스트, 상점 등의 상호작용 목록
}

[System.Serializable]
public class Interaction
{
    public string InteractionName; // UI에 표시될 상호작용 이름 (예: "대화하기", "퀘스트 받기")
    public InteractionType Type;   // 상호작용 타입
    public GameObject ButtonPrefab; // 상호작용 버튼 프리팹

    // 각 상호작용 타입별 ScriptableObject 참조
    [SerializeField] private NPC_Dialogue dialogue; // 대화 데이터
    [SerializeField] private NPC_QuestProvider questProvider; // 퀘스트 제공자 데이터
    [SerializeField] private ShopDefinition shop;      // 상점 데이터

    // 타입에 맞는 상호작용 객체 반환
    public ScriptableObject GetInteractionObject()
    {
        switch (Type)
        {
            case InteractionType.Dialogue:
                return dialogue;
            case InteractionType.Quest:
                return questProvider;
            case InteractionType.Shop:
                return shop;
            default:
                return null;
        }
    }
}