using RamenTycoon.Kitchen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RamenTycoon.EditorTools
{
    /// <summary>
    /// 메뉴: Ramen Tycoon > Replace ServeCounter With Tables
    /// 현재 열려 있는 씬에서 ServeCounter를 지우고, TableMarker 자리에 Table 스테이션을 만든다.
    /// 씬을 새로 만들지 않기 때문에 플레이어(펭귄) 등 직접 수정한 부분은 그대로 유지된다.
    /// </summary>
    public static class ReplaceServeCounterWithTables
    {
        const string SpritePath = "Assets/_Project/Art/Placeholder/square.png";

        static readonly Vector2[] DefaultPositions =
        {
            new(2.5f, 2f), new(5.5f, 2f), new(2.5f, -1.5f), new(5.5f, -1.5f)
        };

        [MenuItem("Ramen Tycoon/Replace ServeCounter With Tables")]
        public static void Run()
        {
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (square == null)
            {
                Debug.LogError($"[ReplaceServeCounter] {SpritePath} 를 찾을 수 없습니다. Build Graybox Scene을 먼저 한 번 실행했는지 확인하세요.");
                return;
            }

            // 1. 서빙 카운터 제거
            var serve = Object.FindFirstObjectByType<ServeCounter>();
            if (serve != null) Undo.DestroyObjectImmediate(serve.gameObject);

            // 2. 테이블 위치 표시(TableMarker_N) 수집 후 제거
            var positions = new System.Collections.Generic.List<Vector2>();
            foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (!sr.gameObject.name.StartsWith("TableMarker_")) continue;
                positions.Add(sr.transform.position);
                Undo.DestroyObjectImmediate(sr.gameObject);
            }
            if (positions.Count == 0) positions.AddRange(DefaultPositions);

            // 3. 테이블 생성
            var hall = GameObject.Find("Hall");
            if (hall == null)
            {
                hall = new GameObject("Hall");
                Undo.RegisterCreatedObjectUndo(hall, "Create Hall");
            }

            for (int i = 0; i < positions.Count; i++)
                CreateTable($"Table_{i + 1}", positions[i], hall.transform, square);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[ReplaceServeCounter] 완료: 테이블 {positions.Count}개 생성. 씬을 저장하세요(Ctrl+S).");
        }

        static void CreateTable(string tableName, Vector2 pos, Transform parent, Sprite square)
        {
            var go = new GameObject(tableName);
            Undo.RegisterCreatedObjectUndo(go, "Create Table");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 1.2f);

            var body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = new Color(0.68f, 0.47f, 0.32f);
            sr.sortingOrder = 0;

            var anchor = new GameObject("ItemAnchor").transform;
            anchor.SetParent(go.transform, false);
            anchor.localPosition = new Vector3(0f, 0.1f, 0f);

            var table = go.AddComponent<Table>();
            SetField(table, "bodyRenderer", sr);
            SetField(table, "itemAnchor", anchor);
        }

        static void SetField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[ReplaceServeCounter] {target.GetType().Name}.{fieldName} 필드를 찾을 수 없습니다.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
