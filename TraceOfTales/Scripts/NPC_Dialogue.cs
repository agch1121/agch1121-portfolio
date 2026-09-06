using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "NPC/Dialogue")]
public class NPC_Dialogue : ScriptableObject
{
    [Header("Dialogue Data")]
    public DialogueData[] Dialogues; // 대화 데이터 배열
}

[System.Serializable]
public class DialogueData
{
    public string DialogueId; // 대화 ID (대화 구분을 위해 사용)
    [TextArea(3, 10)]
    public string[] Sentences; // 대화 문장 목록
    public DialogueChoice[] Choices; // 대화 선택지 목록
}

[System.Serializable]
public class DialogueChoice
{
    public string ChoiceText; // 선택지 텍스트 (ex. "네", "아니요", "도와줄게요")
    public string NextDialogueId; // 이 선택지를 선택하면 진행될 다음 대화 ID (null이면 종료)
    public bool isQuest; // 이 선택지가 퀘스트인지를 구분하는 플래그 - 추후에 퀘스트 개발 시 참조용으로 사용
    public string questId; // 퀘스트 ID (isQuest가 true일 경우만 사용) - 추후에 퀘스트 개발 시 참조용으로 사용
    // 상점 관련 추가
    public bool isShop; // 이 선택지가 상점 이용인지 구분하는 플래그
    public string shopId; // 상점 ID (isShop이 true일 경우만 사용)
}