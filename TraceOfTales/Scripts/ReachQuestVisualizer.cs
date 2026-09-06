using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 도달 퀘스트의 목표 위치를 씬에 시각적으로 표시하는 컴포넌트
/// </summary>
public class ReachQuestVisualizer : MonoBehaviour
{
    [Header("Reach Quest References")]
    [SerializeField] private ReachQuest[] reachQuests;

    [Header("Visual Settings")]
    [SerializeField] private bool showAllQuestLocations = true;
    [SerializeField] private float markerSize = 1f;
    [SerializeField] private Color markerColor = new Color(0.3f, 0.7f, 0.3f, 0.8f);

    // 런타임 시각적 표현용 게임오브젝트들
    private GameObject[] locationMarkers;

    private void Start()
    {
        // 퀘스트 목표 위치 시각화
        if (reachQuests != null && reachQuests.Length > 0)
        {
            CreateLocationMarkers();
        }
        else
        {
            Debug.LogWarning("[ReachQuestVisualizer] 표시할 도달 퀘스트가 없습니다.");
        }
    }

    private void CreateLocationMarkers()
    {
        // 모든 마커 제거
        DestroyAllMarkers();

        // 새 마커 생성
        locationMarkers = new GameObject[reachQuests.Length];

        for (int i = 0; i < reachQuests.Length; i++)
        {
            if (reachQuests[i] == null) continue;

            // 마커 게임오브젝트 생성
            GameObject marker = new GameObject($"QuestMarker_{reachQuests[i].QuestName}");
            marker.transform.position = reachQuests[i].TargetLocation;

            // 위치 표시 파티클 시스템 또는 간단한 표식 추가
            CreateVisualMarker(marker, reachQuests[i]);

            locationMarkers[i] = marker;
        }
    }

    private void CreateVisualMarker(GameObject marker, ReachQuest quest)
    {
        // 빌보드 스타일 UI 텍스트 생성 (간단한 표시)
        GameObject uiHolder = new GameObject("UI_Holder");
        uiHolder.transform.SetParent(marker.transform);
        uiHolder.transform.localPosition = Vector3.up * 2; // 지면보다 위에 표시

        // Always look at camera
        uiHolder.AddComponent<Billboard>();

        // 텍스트 메시로 퀘스트 위치 표시 (Unity 2018 이상)
        TMPro.TextMeshPro tmpText = uiHolder.AddComponent<TMPro.TextMeshPro>();
        tmpText.text = quest.LocationName;
        tmpText.fontSize = 5;
        tmpText.alignment = TMPro.TextAlignmentOptions.Center;
        tmpText.color = markerColor;

        // 마커 표시 (간단한 스피어)
        GameObject markerIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        markerIndicator.transform.SetParent(marker.transform);
        markerIndicator.transform.localPosition = Vector3.zero;
        markerIndicator.transform.localScale = Vector3.one * markerSize;

        // 마커 인디케이터 material 설정
        Renderer renderer = markerIndicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material markerMaterial = new Material(Shader.Find("Standard"));
            markerMaterial.SetColor("_Color", markerColor);
            markerMaterial.EnableKeyword("_EMISSION");
            markerMaterial.SetColor("_EmissionColor", markerColor * 0.5f);
            renderer.material = markerMaterial;
        }

        // 콜라이더 비활성화 (충돌 방지)
        Collider collider = markerIndicator.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }

    private void DestroyAllMarkers()
    {
        if (locationMarkers != null)
        {
            foreach (GameObject marker in locationMarkers)
            {
                if (marker != null)
                {
                    Destroy(marker);
                }
            }
        }
    }

    private void OnDestroy()
    {
        // 마커 정리
        DestroyAllMarkers();
    }

    // 퀘스트 목록 업데이트 (런타임 중 목록이 변경될 경우)
    public void UpdateQuestList(ReachQuest[] newQuests)
    {
        reachQuests = newQuests;
        CreateLocationMarkers();
    }

    // 에디터에서만 Refresh 함수
    public void RefreshVisualizers()
    {
        CreateLocationMarkers();
    }
}

// 빌보드 동작 구현 (항상 카메라를 향하도록)
[System.Serializable]
public class Billboard : MonoBehaviour
{
    private Transform cameraTransform;

    private void Start()
    {
        cameraTransform = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (cameraTransform != null)
        {
            transform.LookAt(transform.position + cameraTransform.rotation * Vector3.forward,
                           cameraTransform.rotation * Vector3.up);
        }
    }
}

#if UNITY_EDITOR
// 에디터 내 시각화를 위한 커스텀 인스펙터
[CustomEditor(typeof(ReachQuestVisualizer))]
public class ReachQuestVisualizerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ReachQuestVisualizer visualizer = (ReachQuestVisualizer)target;

        EditorGUILayout.Space();

        if (GUILayout.Button("위치 마커 새로고침", GUILayout.Height(30)))
        {
            visualizer.RefreshVisualizers();
        }

        // 에디터 내 각 ReachQuest의 디버그 모드 표시
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("퀘스트 정보", EditorStyles.boldLabel);

        SerializedProperty questsProperty = serializedObject.FindProperty("reachQuests");
        if (questsProperty != null && questsProperty.arraySize > 0)
        {
            for (int i = 0; i < questsProperty.arraySize; i++)
            {
                SerializedProperty questProperty = questsProperty.GetArrayElementAtIndex(i);
                if (questProperty.objectReferenceValue is ReachQuest quest)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"퀘스트: {quest.QuestName}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"위치: {quest.LocationName} (ID: {quest.locationId})");
                    EditorGUILayout.LabelField($"좌표: {quest.TargetLocation}");
                    EditorGUILayout.LabelField($"현재 상태: {quest.State}");
                    EditorGUILayout.EndVertical();
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("도달 퀘스트가 설정되지 않았습니다.", MessageType.Info);
        }
    }

    // 씬 뷰에 퀘스트 위치 시각화
    private void OnSceneGUI()
    {
        ReachQuestVisualizer visualizer = (ReachQuestVisualizer)target;
        SerializedProperty questsProperty = serializedObject.FindProperty("reachQuests");

        if (questsProperty != null && questsProperty.arraySize > 0)
        {
            for (int i = 0; i < questsProperty.arraySize; i++)
            {
                SerializedProperty questProperty = questsProperty.GetArrayElementAtIndex(i);
                if (questProperty.objectReferenceValue is ReachQuest quest)
                {
                    // 퀘스트 위치 표시
                    Vector3 targetPos = quest.targetZone != null ? quest.targetZone.transform.position : quest.TargetLocation;

                    // Gizmo 색상 설정
                    Handles.color = new Color(0.3f, 0.7f, 0.3f, 0.8f);

                    // 위치 표시 구체
                    Handles.SphereHandleCap(0, targetPos, Quaternion.identity, 0.5f, EventType.Repaint);

                    // 위치 이름 표시
                    Handles.Label(targetPos + Vector3.up, quest.LocationName);
                }
            }
        }
    }

}
#endif