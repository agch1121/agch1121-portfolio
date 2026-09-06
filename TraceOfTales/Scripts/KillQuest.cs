using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewKillQuest", menuName = "Quest/KillQuest")]
public class KillQuest : Quest
{
    [System.Serializable]
    public class KillTarget
    {
        [Tooltip("사냥할 적 프리팹")]
        public Enemy enemyPrefab;          // Enemy 프리팹 참조
        public int requiredCount;           // 사냥해야 할 적 수
        [HideInInspector]
        public int currentCount;            // 현재 사냥한 적 수

        // 몬스터 정보를 프리팹에서 가져옴
        public string GetMonsterId() => enemyPrefab != null ? enemyPrefab.MonsterId : "";
        public string GetMonsterName() => enemyPrefab != null ? enemyPrefab.MonsterName : "알 수 없음";

        // 이전 하위 호환을 위한 필드들 - 나중에 제거 가능
        [HideInInspector]
        public string MonsterId;            // 이전 버전에서 사용하던 몬스터 ID
        [HideInInspector]
        public string MonsterName;          // 이전 버전에서 사용하던 몬스터 이름
    }

    [Header("Kill Quest Info")]
    public List<KillTarget> KillTargets = new List<KillTarget>();

    private void OnEnable()
    {
        QuestType = QuestType.Kill; // 퀘스트 타입 설정

        // 전체 진행도 초기화
        UpdateTotalProgress();
    }

    public override string GetProgressText()
    {
        if (KillTargets.Count == 1)
        {
            return $"{KillTargets[0].GetMonsterName()} 사냥:{KillTargets[0].currentCount} / {KillTargets[0].requiredCount}";
        }
        else
        {
            System.Text.StringBuilder result = new System.Text.StringBuilder();
            foreach (var target in KillTargets)
            {
                result.AppendLine($"{target.GetMonsterName()} 사냥:{target.currentCount} / {target.requiredCount}");
            }
            return result.ToString().TrimEnd('\n');
        }
    }

    public override bool CheckCondition(string conditionType, string conditionId)
    {
        if (conditionType == "Kill" && State == QuestState.InProgress)
        {
            foreach (KillTarget target in KillTargets)
            {
                // 프리팹 기반 ID 확인
                if (target.GetMonsterId() == conditionId)
                {
                    return true;
                }

                // 하위 호환을 위한 확인 (이전 방식)
                if (!string.IsNullOrEmpty(target.MonsterId) && target.MonsterId == conditionId)
                {
                    return true;
                }
            }
        }
        return false;
    }

    // 몬스터 사냥 시 호출될 메서드
    public void ProcessMonsterKill(string monsterId)
    {
        if (State != QuestState.InProgress) return;

        bool updated = false;

        foreach (KillTarget target in KillTargets)
        {
            // 프리팹 기반 ID 또는 이전 방식의 ID 확인
            bool isMatchingTarget = target.GetMonsterId() == monsterId ||
                                  (!string.IsNullOrEmpty(target.MonsterId) && target.MonsterId == monsterId);

            if (isMatchingTarget && target.currentCount < target.requiredCount)
            {
                target.currentCount++;
                updated = true;

                string monsterName = !string.IsNullOrEmpty(target.GetMonsterName()) ?
                                    target.GetMonsterName() :
                                    (!string.IsNullOrEmpty(target.MonsterName) ? target.MonsterName : monsterId);

                // 몬스터 사냥 퀘스트인 경우 사냥한 몬스터마다 진행 가능하게 처리
                if (State == QuestState.InProgress)
                {
                    break; // 같은 ID의 첫 번째 타겟만 업데이트
                }
            }
        }

        if (updated)
        {
            UpdateTotalProgress();
        }
    }

    // 전체 진행도 계산
    private void UpdateTotalProgress()
    {
        int totalRequired = 0;
        int totalCurrent = 0;

        foreach (KillTarget target in KillTargets)
        {
            totalRequired += target.requiredCount;
            totalCurrent += target.currentCount;
        }

        // 필요한 진행도와 현재 진행도 설정
        requiredProgress = totalRequired;
        currentProgress = totalCurrent;

        // 부모 클래스의 UpdateProgress 메서드 호출하여 이벤트 발생시키기
        UpdateProgress(totalCurrent);

        // *** 아래 4줄의 코드를 삭제하거나 주석 처리 ***
        // if (IsCompleted() && State == QuestState.InProgress)
        // {
        //     CompleteQuest();
        // }
    }

    // 에디터에서 검증
    private void OnValidate()
    {
        // 필요 수량 검증
        foreach (var target in KillTargets)
        {
            if (target.requiredCount <= 0)
            {
                target.requiredCount = 1;
                Debug.LogWarning($"KillQuest: 사냥 대상의 필요 수량이 0 이하로 설정되어 1로 조정되었습니다.");
            }

            // 프리팹이 있는 경우 이름과 ID 업데이트 (에디터에서만 실행)
#if UNITY_EDITOR
            if (target.enemyPrefab != null)
            {
                // 에디터 내 참조를 위해 이전 필드들도 업데이트
                target.MonsterId = target.GetMonsterId();
                target.MonsterName = target.GetMonsterName();
            }
#endif
        }
    }
}