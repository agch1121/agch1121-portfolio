using System.Collections;
using UnityEngine;

public class DialogueNoticeManager : MonoBehaviour
{
    [SerializeField] private GameObject continueNoticeObject; // F키 안내 이미지 오브젝트
    private Coroutine blinkCoroutine; // 깜빡임 효과용 코루틴

    public void Initialize()
    {
        if (continueNoticeObject != null)
        {
            continueNoticeObject.SetActive(false);
        }
    }

    // 대화 계속하기 안내 이미지 업데이트
    public void UpdateContinueNotice(bool isTyping, bool hasMoreSentences, bool hasChoices)
    {
        if (continueNoticeObject != null)
        {
            // 타이핑이 완료된 상태일 때 (문장이 다 출력됨)
            if (!isTyping)
            {
                // 다음 문장이 있거나 선택지가 있는 경우 또는 현재가 마지막 문장인 경우에도 계속하기 표시
                continueNoticeObject.SetActive(true);

                // 이미 깜빡임 코루틴이 실행 중이 아니라면 시작
                if (blinkCoroutine == null)
                {
                    blinkCoroutine = StartCoroutine(BlinkContinueNotice());
                }
            }
            else
            {
                // 타이핑 중에는 안내 이미지 숨기기
                StopBlinkEffect();
            }
        }
    }

    private IEnumerator BlinkContinueNotice()
    {
        // 처음에 무조건 보이게 설정
        continueNoticeObject.SetActive(true);

        while (true)
        {
            // 깜빡임 효과
            continueNoticeObject.SetActive(!continueNoticeObject.activeSelf);
            yield return new WaitForSeconds(0.5f); // 깜빡임 속도 조절
        }
    }

    public void StopBlinkEffect()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        if (continueNoticeObject != null)
        {
            continueNoticeObject.SetActive(false);
        }
    }
}