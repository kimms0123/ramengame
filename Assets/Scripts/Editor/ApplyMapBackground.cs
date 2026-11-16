using System.Collections.Generic;
using RamenTycoon.Kitchen;
using RamenTycoon.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RamenTycoon.EditorTools
{
    /// <summary>
    /// 메뉴: Ramen Tycoon > Apply Map Background
    /// 맵 이미지(1920x1080)를 배경으로 깔고, 카메라/벽 충돌/스테이션 위치를 맵에 맞춘다.
    /// 씬을 새로 만들지 않으므로 플레이어(펭귄) 등 직접 수정한 내용은 유지된다.
    /// 되돌리려면 Ctrl+Z.
    ///
    /// 기준: PPU 60 → 맵 = 32 x 18 유닛, 카메라 Orthographic Size 9, 타일 1칸 ≈ 0.97유닛
    /// </summary>
    public static class ApplyMapBackground
    {
        const string MapPath = "Assets/_Project/Art/map.png";
        const float Ppu = 60f;
        const float CameraSize = 9f;

        // 맵 이미지에서 잰 바닥 안쪽 경계 (월드 좌표)
        const float Left = -14.2f, Right = 14.2f, Top = 7.0f, Bottom = -7.3f;
        // 하단 오른쪽 출입구 (손님 입장 통로)
        const float GapMinX = 10.25f, GapMaxX = 12.87f;
        const float WallThickness = 0.6f;

        // 스테이션 이름 → 위치. 주방은 왼쪽(타일 10x15), 홀은 오른쪽(타일 19x15)
        static readonly Dictionary<string, Vector2> Layout = new Dictionary<string, Vector2>
        {
            // 윗줄: 재료 통 + 그릇 스택
            { "Crate_Tonkotsu", new Vector2(-13.3f, 6.3f) },
            { "Crate_Spicy",    new Vector2(-12.3f, 6.3f) },
            { "Crate_Noodle",   new Vector2(-11.3f, 6.3f) },
            { "PlateStack",     new Vector2(-9.0f, 6.3f) },
            // 화구 줄: 재료 통 아래, 사이 통로(y≈5.3)에서 양쪽을 다 쓸 수 있다
            { "Stove_1", new Vector2(-13.3f, 4.2f) },
            { "Stove_2", new Vector2(-12.3f, 4.2f) },
            { "Stove_3", new Vector2(-11.3f, 4.2f) },
            // 왼쪽 아래: 조리대
            { "Counter_1", new Vector2(-13.2f, -6.4f) },
            { "Counter_2", new Vector2(-12.2f, -6.4f) },
            // 홀: 테이블
            { "Table_1", new Vector2(1.5f, 3.5f) },
            { "Table_2", new Vector2(6.5f, 3.5f) },
            { "Table_3", new Vector2(1.5f, -2.0f) },
            { "Table_4", new Vector2(6.5f, -2.0f) },
        };

        [MenuItem("Ramen Tycoon/Apply Map Background")]
        public static void Run()
        {
            var sprite = LoadMapSprite();
            if (sprite == null) return;

            SetupCamera();
            SetupBackground(sprite);
            HideOldFloors();
            SetupWalls();
            MoveStations();
            MovePlayer();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[ApplyMapBackground] 완료. Game 뷰 비율을 16:9로 맞추고 씬을 저장하세요(Ctrl+S).");
        }

        // ───────── 단계별 처리 ─────────

        static Sprite LoadMapSprite()
        {
            // Project 창에서 이미지를 선택하고 실행하면 그 파일을 쓴다. 선택이 없으면 기본 경로(MapPath)를 쓴다.
            string path = MapPath;
            var selected = Selection.activeObject;
            if (selected is Texture2D || selected is Sprite)
            {
                string selectedPath = AssetDatabase.GetAssetPath(selected);
                if (!string.IsNullOrEmpty(selectedPath)) path = selectedPath;
            }

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[ApplyMapBackground] 맵 이미지를 찾을 수 없습니다. Project 창에서 맵 이미지를 클릭해 선택한 뒤 다시 실행하세요.");
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Ppu;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();

            Debug.Log($"[ApplyMapBackground] 맵 이미지: {path}");
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[ApplyMapBackground] MainCamera 태그가 붙은 카메라를 찾지 못했습니다.");
                return;
            }

            Undo.RecordObject(cam, "Map camera");
            Undo.RecordObject(cam.transform, "Map camera");
            cam.orthographic = true;
            cam.orthographicSize = CameraSize;
            cam.transform.position = new Vector3(0f, 0f, -10f);
        }

        static void SetupBackground(Sprite sprite)
        {
            var go = GameObject.Find("MapBackground");
            if (go == null)
            {
                go = new GameObject("MapBackground");
                Undo.RegisterCreatedObjectUndo(go, "Create MapBackground");
            }

            go.transform.position = Vector3.zero;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = Undo.AddComponent<SpriteRenderer>(go);
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingOrder = -20;
        }

        static void HideOldFloors()
        {
            var room = GameObject.Find("Room");
            if (room == null) return;

            foreach (var name in new[] { "Floor_Kitchen", "Floor_Hall" })
            {
                var t = room.transform.Find(name);
                if (t == null) continue;
                var sr = t.GetComponent<SpriteRenderer>();
                if (sr == null) continue;
                Undo.RecordObject(sr, "Hide floor");
                sr.enabled = false;
            }
        }

        static void SetupWalls()
        {
            var room = GameObject.Find("Room");
            if (room == null)
            {
                room = new GameObject("Room");
                Undo.RegisterCreatedObjectUndo(room, "Create Room");
            }

            // 주방/홀 경계는 맵에서 장식 테두리라 충돌을 두지 않는다 (플레이어가 그릇을 들고 자유롭게 오감)
            foreach (var name in new[] { "Divider_Top", "Divider_Bottom" })
            {
                var d = room.transform.Find(name);
                if (d != null) Undo.DestroyObjectImmediate(d.gameObject);
            }

            float midY = (Top + Bottom) * 0.5f;
            float height = (Top - Bottom) + WallThickness * 2f;
            float width = (Right - Left) + WallThickness * 2f;
            float bottomY = Bottom - WallThickness * 0.5f;

            // 위, 왼쪽, 오른쪽
            SetWall(room.transform, "Wall_Top", new Vector2(0f, Top + WallThickness * 0.5f), new Vector2(width, WallThickness));
            SetWall(room.transform, "Wall_Left", new Vector2(Left - WallThickness * 0.5f, midY), new Vector2(WallThickness, height));
            SetWall(room.transform, "Wall_Right", new Vector2(Right + WallThickness * 0.5f, midY), new Vector2(WallThickness, height));

            // 아래: 출입구를 사이에 두고 둘로 나눔
            float leftMin = Left - WallThickness;
            SetWall(room.transform, "Wall_Bottom",
                new Vector2((leftMin + GapMinX) * 0.5f, bottomY), new Vector2(GapMinX - leftMin, WallThickness));

            float rightMax = Right + WallThickness;
            SetWall(room.transform, "Wall_Bottom_R",
                new Vector2((GapMaxX + rightMax) * 0.5f, bottomY), new Vector2(rightMax - GapMaxX, WallThickness));

            // 출입구 바깥쪽은 플레이어가 나가지 못하게 막는다 (손님은 물리 충돌 없이 이동)
            SetWall(room.transform, "Wall_EntranceBlock",
                new Vector2((GapMinX + GapMaxX) * 0.5f, Bottom - 1.0f), new Vector2(GapMaxX - GapMinX, 0.4f));
        }

        static void SetWall(Transform parent, string wallName, Vector2 center, Vector2 size)
        {
            var t = parent.Find(wallName);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(wallName);
                Undo.RegisterCreatedObjectUndo(go, "Create wall");
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = t.gameObject;
            }

            Undo.RecordObject(go.transform, "Move wall");
            go.transform.position = center;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = Undo.AddComponent<BoxCollider2D>(go);
            col.size = Vector2.one; // 크기는 transform scale로 조절

            // 기존 네모 벽 그림은 숨긴다 (맵 이미지의 테두리를 사용)
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Undo.RecordObject(sr, "Hide wall");
                sr.enabled = false;
            }
        }

        static void MoveStations()
        {
            int moved = 0;
            foreach (var s in Object.FindObjectsByType<Station>(FindObjectsInactive.Include))
            {
                if (!Layout.TryGetValue(s.name, out var pos)) continue;
                Undo.RecordObject(s.transform, "Move station");
                s.transform.position = new Vector3(pos.x, pos.y, s.transform.position.z);
                moved++;
            }
            Debug.Log($"[ApplyMapBackground] 스테이션 {moved}개 이동");
        }

        static void MovePlayer()
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player == null) return;

            Undo.RecordObject(player.transform, "Move player");
            player.transform.position = new Vector3(-9.0f, 2.0f, player.transform.position.z);
        }
    }
}