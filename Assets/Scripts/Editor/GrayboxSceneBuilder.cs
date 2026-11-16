using System.IO;
using RamenTycoon.Core;
using RamenTycoon.Data;
using RamenTycoon.Kitchen;
using RamenTycoon.Player;
using RamenTycoon.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RamenTycoon.EditorTools
{
    /// <summary>
    /// 메뉴: Ramen Tycoon > Build Graybox Scene
    /// 플레이스홀더 스프라이트, 재료 SO, 그레이박스 씬을 한 번에 만든다.
    /// 다시 실행하면 씬을 새로 만든다 (기존 Graybox.unity는 덮어씀).
    /// </summary>
    public static class GrayboxSceneBuilder
    {
        const string Root = "Assets/_Project";
        const string SpritePath = Root + "/Art/Placeholder/square.png";
        const string IngredientFolder = Root + "/Data/Ingredients";
        const string ScenePath = Root + "/Scenes/Graybox.unity";

        static Sprite square;

        [MenuItem("Ramen Tycoon/Build Graybox Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder(Root + "/Art/Placeholder");
            EnsureFolder(IngredientFolder);
            EnsureFolder(Root + "/Scenes");

            square = CreateSquareSprite();
            var tonkotsu = CreateIngredient("ING_Broth_Tonkotsu", "broth_tonkotsu", "돈코츠 육수", IngredientCategory.Broth, new Color(0.93f, 0.85f, 0.68f));
            var spicy = CreateIngredient("ING_Broth_Spicy", "broth_spicy", "매운 육수", IngredientCategory.Broth, new Color(0.85f, 0.3f, 0.2f));
            var noodle = CreateIngredient("ING_Noodle", "noodle", "면", IngredientCategory.Noodle, new Color(1f, 0.9f, 0.45f));
            CreateIngredient("ING_Top_Chashu", "chashu", "차슈", IngredientCategory.Topping, new Color(0.94f, 0.6f, 0.48f));
            CreateIngredient("ING_Top_Nori", "nori", "김", IngredientCategory.Topping, new Color(0.15f, 0.17f, 0.15f));
            CreateIngredient("ING_Top_Scallion", "scallion", "파", IngredientCategory.Topping, new Color(0.5f, 0.78f, 0.3f));
            CreateIngredient("ING_Top_Sprout", "sprout", "숙주", IngredientCategory.Topping, new Color(0.95f, 0.95f, 0.88f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateRoom();

            // 시스템
            var systems = new GameObject("_Systems");
            systems.AddComponent<GameClock>();
            systems.AddComponent<GameFlow>();
            var factory = systems.AddComponent<ItemFactory>();
            SetField(factory, "placeholderSprite", square);

            // 플레이어
            var player = CreatePlayer(new Vector2(-3f, -1f));

            // 주방 (왼쪽): 재료 통 → 화구 → 조리대 → 서빙 카운터(경계)
            CreateStation<IngredientCrate>("Crate_Tonkotsu", new Vector2(-7f, 3.5f), new Vector2(0.9f, 0.9f), new Color(0.55f, 0.45f, 0.3f), s => SetField(s, "ingredient", tonkotsu));
            CreateStation<IngredientCrate>("Crate_Spicy", new Vector2(-6f, 3.5f), new Vector2(0.9f, 0.9f), new Color(0.55f, 0.3f, 0.25f), s => SetField(s, "ingredient", spicy));
            CreateStation<IngredientCrate>("Crate_Noodle", new Vector2(-5f, 3.5f), new Vector2(0.9f, 0.9f), new Color(0.6f, 0.55f, 0.3f), s => SetField(s, "ingredient", noodle));

            CreateStove("Stove_1", new Vector2(-3.5f, 3.5f));
            CreateStove("Stove_2", new Vector2(-2.5f, 3.5f));

            var plateStack = CreateStation<PlateStack>("PlateStack", new Vector2(-7f, -2f), new Vector2(0.9f, 0.9f), new Color(0.7f, 0.72f, 0.75f), null);
            CreateStation<Counter>("Counter_1", new Vector2(-7f, 0f), new Vector2(0.9f, 0.9f), new Color(0.5f, 0.5f, 0.52f), null);
            CreateStation<Counter>("Counter_2", new Vector2(-7f, 1f), new Vector2(0.9f, 0.9f), new Color(0.5f, 0.5f, 0.52f), null);

            // 홀 (오른쪽): 플레이어가 라멘을 직접 들고 와서 테이블에 내려놓는다 (서빙 카운터 없음)
            for (int i = 0; i < 4; i++)
            {
                var pos = new Vector2(i % 2 == 0 ? 2.5f : 5.5f, i < 2 ? 2f : -1.5f);
                var table = CreateStation<Table>($"Table_{i + 1}", pos, new Vector2(1.2f, 1.2f), new Color(0.68f, 0.47f, 0.32f), null);
                table.transform.SetParent(GetOrCreate("Hall").transform, true);
            }

            // 디버그 HUD
            var hud = systems.AddComponent<DebugHud>();
            SetField(hud, "playerHand", player.GetComponent<PlayerHand>());
            SetField(hud, "plateStack", plateStack);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GrayboxSceneBuilder] 완료: {ScenePath}");
        }

        // ───────── 생성 헬퍼 ─────────

        static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.1f, 0.07f);
            go.transform.position = new Vector3(0f, 0f, -10f);
        }

        static void CreateRoom()
        {
            var room = new GameObject("Room");

            // 바닥: 주방(타일) / 홀(나무)
            CreateSprite("Floor_Kitchen", room.transform, new Vector2(-4.5f, 0f), new Vector2(7f, 9f), new Color(0.6f, 0.66f, 0.72f), -10);
            CreateSprite("Floor_Hall", room.transform, new Vector2(3.5f, 0f), new Vector2(9f, 9f), new Color(0.45f, 0.28f, 0.17f), -10);

            // 벽 (충돌만)
            CreateWall(room.transform, "Wall_Top", new Vector2(0f, 4.75f), new Vector2(16.5f, 0.5f));
            CreateWall(room.transform, "Wall_Bottom", new Vector2(0f, -4.75f), new Vector2(16.5f, 0.5f));
            CreateWall(room.transform, "Wall_Left", new Vector2(-8.25f, 0f), new Vector2(0.5f, 9f));
            CreateWall(room.transform, "Wall_Right", new Vector2(8.25f, 0f), new Vector2(0.5f, 9f));

            // 주방/홀 경계 (가운데가 뚫려 있는 출입구 → 플레이어가 그릇을 들고 홀로 나간다)
            CreateWall(room.transform, "Divider_Top", new Vector2(-1f, 2.75f), new Vector2(0.3f, 3.5f));
            CreateWall(room.transform, "Divider_Bottom", new Vector2(-1f, -2.75f), new Vector2(0.3f, 3.5f));
        }

        static void CreateWall(Transform parent, string wallName, Vector2 pos, Vector2 size)
        {
            var go = CreateSprite(wallName, parent, pos, size, new Color(0.3f, 0.18f, 0.12f), -1);
            go.AddComponent<BoxCollider2D>(); // 스프라이트가 1유닛이라 스케일과 함께 맞춰진다
        }

        static GameObject CreatePlayer(Vector2 pos)
        {
            var go = new GameObject("Player");
            go.transform.position = pos;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;

            CreateSprite("Body", go.transform, Vector2.zero, new Vector2(0.75f, 0.75f), new Color(0.3f, 0.5f, 0.9f), 2, local: true);

            var anchor = new GameObject("HoldAnchor").transform;
            anchor.SetParent(go.transform, false);
            anchor.localPosition = new Vector3(0f, 0.55f, 0f);

            go.AddComponent<PlayerController>();
            var hand = go.AddComponent<PlayerHand>();
            SetField(hand, "holdAnchor", anchor);
            go.AddComponent<PlayerInteractor>();
            return go;
        }

        static void CreateStove(string stoveName, Vector2 pos)
        {
            CreateStation<Stove>(stoveName, pos, new Vector2(0.9f, 0.9f), new Color(0.35f, 0.35f, 0.38f), stove =>
            {
                var content = CreateSprite("Content", stove.transform, Vector2.zero, new Vector2(0.5f, 0.5f), Color.white, 1, local: true);

                var bar = new GameObject("ProgressBar");
                bar.transform.SetParent(stove.transform, false);
                bar.transform.localPosition = new Vector3(0f, 0.65f, 0f);
                CreateSprite("Background", bar.transform, Vector2.zero, new Vector2(1f, 0.15f), new Color(0.1f, 0.1f, 0.1f), 10, local: true);
                var fill = CreateSprite("Fill", bar.transform, Vector2.zero, new Vector2(1f, 0.15f), new Color(0.45f, 0.85f, 0.35f), 11, local: true);
                var progress = bar.AddComponent<ProgressBar>();
                SetField(progress, "fill", fill.transform);

                SetField(stove, "contentRenderer", content.GetComponent<SpriteRenderer>());
                SetField(stove, "progressBar", progress);
            });
        }

        static T CreateStation<T>(string stationName, Vector2 pos, Vector2 size, Color color, System.Action<T> configure) where T : Station
        {
            var go = new GameObject(stationName);
            go.transform.SetParent(GetOrCreate("Kitchen").transform, false);
            go.transform.position = pos;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;

            var body = CreateSprite("Body", go.transform, Vector2.zero, size, color, 0, local: true);

            var anchor = new GameObject("ItemAnchor").transform;
            anchor.SetParent(go.transform, false);
            anchor.localPosition = new Vector3(0f, 0.1f, 0f);

            var station = go.AddComponent<T>();
            SetField(station, "bodyRenderer", body.GetComponent<SpriteRenderer>());
            SetField(station, "itemAnchor", anchor);
            configure?.Invoke(station);
            return station;
        }

        static GameObject CreateSprite(string spriteName, Transform parent, Vector2 pos, Vector2 size, Color color, int order, bool local = false)
        {
            var go = new GameObject(spriteName);
            if (parent != null) go.transform.SetParent(parent, false);
            if (local) go.transform.localPosition = pos;
            else go.transform.position = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = color;
            sr.sortingOrder = order;
            return go;
        }

        static GameObject GetOrCreate(string objName)
        {
            var go = GameObject.Find(objName);
            return go != null ? go : new GameObject(objName);
        }

        // ───────── 에셋 헬퍼 ─────────

        static Sprite CreateSquareSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (existing != null) return existing;

            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var pixels = new Color32[16 * 16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            File.WriteAllBytes(SpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(SpritePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }

        static IngredientData CreateIngredient(string fileName, string id, string displayName, IngredientCategory category, Color color)
        {
            string path = $"{IngredientFolder}/{fileName}.asset";
            var data = AssetDatabase.LoadAssetAtPath<IngredientData>(path);
            if (data != null) return data;

            data = ScriptableObject.CreateInstance<IngredientData>();
            data.id = id;
            data.displayName = displayName;
            data.category = category;
            data.placeholderColor = color;
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void SetField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[GrayboxSceneBuilder] {target.GetType().Name}.{fieldName} 필드를 찾을 수 없습니다.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}