using System.Collections;
using TMPro;
using UnityEngine;

public class DialogueTypingEffect : MonoBehaviour
{
    [Header("Typing Effect Settings")]
    [SerializeField] private float typingSpeed = 0.05f; // 타이핑 속도 (글자당 시간)

    private TextMeshProUGUI dialogueText;
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private int currentSentenceIndex = 0;

    public bool IsTyping => isTyping;
    public int CurrentSentenceIndex => currentSentenceIndex;

    // 이벤트 추가 - 타이핑 완료 시 호출될 이벤트
    public event System.Action OnTypingCompleted;

    public void Initialize(TextMeshProUGUI dialogueTextRef)
    {
        dialogueText = dialogueTextRef;
    }

    // 현재 문장이 더 있는지 확인 (일반 대화용)
    public bool HasMoreSentences(DialogueData dialogue)
    {
        return currentSentenceIndex < dialogue.Sentences.Length;
    }

    // 현재 문장이 더 있는지 확인 (인사말용)
    public bool HasMoreSentences(string[] sentences)
    {
        return currentSentenceIndex < sentences.Length;
    }

    public void ResetIndex()
    {
        currentSentenceIndex = 0;
    }

    public void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            isTyping = false;
        }
    }

    // 현재 문장을 완료하고 다음 문장으로 넘어감
    public void CompleteCurrentSentence()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // DialogueManager의 isInGreetingMode를 확인하여 현재 모드 결정
        bool isGreetingMode = DialogueManager.Instance.IsInGreetingMode;

        if (dialogueText != null && currentSentenceIndex > 0)
        {
            string fullSentence;

            if (isGreetingMode)
            {
                // 인사말 모드인 경우, DialogueManager에서 인사말 배열 가져오기
                string[] greetings = DialogueManager.Instance.GetGreetingSentences();
                if (greetings != null && currentSentenceIndex <= greetings.Length)
                {
                    fullSentence = greetings[currentSentenceIndex - 1];
                    dialogueText.text = fullSentence;
                }
            }
            else
            {
                // 일반 대화 모드인 경우
                DialogueData currentDialogue = DialogueManager.Instance.GetCurrentDialogue();
                if (currentDialogue != null && currentSentenceIndex <= currentDialogue.Sentences.Length)
                {
                    fullSentence = currentDialogue.Sentences[currentSentenceIndex - 1];
                    dialogueText.text = fullSentence;
                }
            }
        }

        isTyping = false;

        // 타이핑 완료 이벤트 호출
        if (OnTypingCompleted != null)
        {
            OnTypingCompleted.Invoke();
        }
    }

    // 문장 타이핑
    public void TypeSentence(string sentence)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeSentenceCoroutine(sentence));
        currentSentenceIndex++;
    }

    // 타이핑 코루틴
    private IEnumerator TypeSentenceCoroutine(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;

        // 타이핑 완료 이벤트 발생
        if (OnTypingCompleted != null)
        {
            OnTypingCompleted.Invoke();
        }
    }
}