using System;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public static class GameEvents
{
    // 몬스터 처치 이벤트 (몬스터ID를 매개변수로 전달)
    public static event Action<Enemy> OnEnemyKilled;

    // 아이템 수집 이벤트 (아이템ID, 수량을 매개변수로 전달)
    public static event Action<string, int> OnItemCollected;

    // 위치 도달 이벤트 (위치ID, 플레이어 위치를 매개변수로 전달)
    public static event Action<string, Vector3> OnLocationReached;

    // 몬스터 처치 이벤트 호출
    public static void EnemyKilled(Enemy enemy)
    {
        Debug.Log($"GameEvents: 몬스터 처치 이벤트 발생 - {enemy}");
        OnEnemyKilled?.Invoke(enemy);
    }

    // 아이템 수집 이벤트 호출
    public static void ItemCollected(string itemId, int amount = 1)
    {
        OnItemCollected?.Invoke(itemId, amount);
    }

    // 위치 도달 이벤트 호출
    public static void LocationReached(string locationId, Vector3 playerPosition)
    {
        OnLocationReached?.Invoke(locationId, playerPosition);
    }
}