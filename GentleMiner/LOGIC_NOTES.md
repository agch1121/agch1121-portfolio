\# 타격력 계산 로직 상세



Gentle Miner의 핵심인 "손 움직임 → 타격력" 변환 과정을 정리한 문서입니다.



\## 1. 배경 — 왜 커스텀 계산이 필요했나



립모션 센서가 기본 제공하는 `Hand.GrabStrength` 값을 그대로 썼을 때,

손에 40\~50% 정도의 힘만 줘도 인식값이 바로 1.0(최대치)으로 튀어버리는

조기 포화 현상이 있었다. 재활 훈련 목적상 사용자의 미세한 힘 조절이

게임 내 타격력에 그대로 반영돼야 하는데, 이 상태로는 절반 이상의

힘 구간이 전부 "최대 세기"로 뭉뚱그려져서 의미가 없었다.



기본 API 값을 보정하는 옵션은 따로 없어서, 손가락 관절 데이터를

직접 가공해 강도를 계산하는 방향으로 접근했다.



```csharp

// GripCalculator.cs

if (useCustomGrabStrength \&\& gripCalculator != null)

&#x20;   RightHandGrabStrength = gripCalculator.CustomGrabStrength;

else

&#x20;   RightHandGrabStrength = hand.GrabStrength; // 여기서 포화 문제 발생

```



\## 2. 손가락 굽힘 계산



엄지는 나머지 손가락과 관절 구조가 달라서 따로 계산했다.



\*\*엄지\*\*: 손가락 방향 벡터와 손바닥 법선 벡터의 내적을 구하고,

`InverseLerp`로 유의미한 구간(-0.7\~0.3)만 0\~1로 재매핑했다.

이 구간을 실측해서 잡은 이유는, 이 범위 밖에서는 물리적으로

거의 움직임이 없었기 때문이다.



```csharp

float CalcThumbCurl(Finger thumb, Hand hand)

{

&#x20;   Vector3 thumbDirection = new Vector3(thumb.Direction.x, thumb.Direction.y, thumb.Direction.z);

&#x20;   Vector3 palmNormal = new Vector3(hand.PalmNormal.x, hand.PalmNormal.y, hand.PalmNormal.z);

&#x20;   float dot = Vector3.Dot(thumbDirection, palmNormal);

&#x20;   return Mathf.Clamp01(Mathf.InverseLerp(-0.7f, 0.3f, dot));

}

```



\*\*나머지 손가락\*\*: 중간 마디와 끝 마디, 두 뼈(bone) 방향 벡터의

내적으로 굽힘 정도를 구했다. 펴져 있으면 두 벡터가 거의 같은 방향(내적≈1),

굽으면 벌어지는(내적↓) 원리를 이용했다.



```csharp

float CalcFingerCurl(Finger finger)

{

&#x20;   Vector3 intermediateDir = ...;

&#x20;   Vector3 distalDir = ...;

&#x20;   float dot = Vector3.Dot(intermediateDir, distalDir);

&#x20;   return 1.0f - Mathf.Clamp01(dot);

}

```



다섯 손가락 값을 손가락별 가중치(엄지는 별도 가중치 적용)로

평균 내고, 최종적으로 `sensitivity` 값으로 민감도를 조절해

최종 악력 값을 만든다.



\## 3. 양손 협응 정확도



왼손(끌)의 목표 지점과 오른손(망치)의 실제 타격 지점 사이

3차원 거리를 재서 정확도를 계산한다. 0.05m 이내면 100%,

0.30m 이상 벗어나면 0%로 처리했다.



```csharp

float accuracy = 1.0f - Mathf.InverseLerp(perfectRadius, maxEvaluationRadius, minDistance);

```



\## 4. 최종 타격력 및 파괴 연동



악력·속도로 만든 기본 힘에, 위에서 구한 정확도를 0.5\~1.2배

가중치로 곱해서 최종 타격력을 정한다. 정확하게 칠수록 더 큰

힘이 들어가게 만들어서, 정밀한 동작을 자연스럽게 유도하고 싶었다.



```csharp

float accuracyModifier = Mathf.Lerp(0.5f, 1.2f, accuracy);

float finalForce = baseForce \* accuracyModifier;

```



이 값은 그대로 LibreFracture 기반 파괴 로직(`MineralBlock.cs`)에

전달되어, 힘이 클수록 파괴 반경과 제거되는 조각 수가 늘어나게 했다.



```csharp

float adjustedRadius = miningRadius \* (0.7f + (strikeForce \* 0.6f));

```



\## 관련 파일

\- `GripCalculator.cs` — 악력 계산

\- `AimSystem.cs` — 정확도 판정

\- `ToolSystem.cs` — 최종 타격력 결정

\- `MineralBlock.cs` — 파괴 처리

\- `HandController.cs` — 손 데이터 수신

