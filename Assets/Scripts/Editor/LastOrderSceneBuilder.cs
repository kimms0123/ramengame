using System.Collections.Generic;
using System.IO;
using System.Text;
using RamenTycoon.Core;
using RamenTycoon.Data;
using RamenTycoon.Kitchen;
using RamenTycoon.Player;
using RamenTycoon.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RamenTycoon.EditorTools
{
    /// <summary>
    /// 메뉴: Ramen Tycoon > Build LAST ORDER Scene
    /// 맵, 벽, 주방 설비, 테이블 4개, 플레이어, 손님/하루 진행, UI까지 한 번에 만든다. (Scenes/LastOrder.unity)
    ///
    /// 이미지는 파일 이름으로 자동 연결한다 (README의 이름표 참고). 못 찾은 이미지는 네모로 표시되고,
    /// Data/GameArtConfig 에셋에 직접 끌어 넣은 뒤 다시 빌드하면 적용된다.
    /// 씬을 새로 만들기 때문에, 씬에서 손으로 고친 내용은 다시 빌드하면 사라진다.
    /// (위치를 바꾸고 싶으면 이 파일의 Layout 값을 고치는 것을 추천)
    /// </summary>
    public static class LastOrderSceneBuilder
    {
        const string Root = "Assets/_Project";
        const string DataFolder = Root + "/Data";
        const string IngredientFolder = Root + "/Data/Ingredients";
        const string SquarePath = Root + "/Art/Placeholder/square.png";
        const string ScenePath = Root + "/Scenes/LastOrder.unity";

        // 맵 (1920x1080 → 32x18 유닛) 안쪽 바닥 경계
        const float MapWidth = 32f;
        const float Left = -14.2f, Right = 14.2f, Top = 7.0f, Bottom = -7.3f;
        const float GapMinX = 10.25f, GapMaxX = 12.87f; // 아래쪽 출입구
        const float WallThickness = 0.6f;

        // ───────── 배치 (월드 좌표, 디자이너 시안에서 측정) ─────────
        // 값은 모두 "바닥 가운데" 위치 (x, 아래쪽 y). 크기는 폭(유닛). 맵 타일 1칸 ≈ 0.97유닛
        // 윗줄: 왼쪽 벽부터 화구 3개 → 면 상자 → 싱크대가 틈 없이 붙고, 윗변이 벽에 닿도록
        static readonly Vector2[] StovePositions = { new(-13.475f, 5.3f), new(-12.115f, 5.3f), new(-10.755f, 5.3f) };
        static readonly Vector2 NoodleBoxPos = new(-9.155f, 5.38f);
        static readonly Vector2 SinkPos = new(-6.365f, 5.15f);
        static readonly Vector2 SinkBowlStackPos = new(-7.3f, 6.65f);
        static readonly Vector2 UrnTonkotsuPos = new(-13.35f, 1.16f);
        static readonly Vector2 UrnSpicyPos = new(-13.35f, -1.08f);
        static readonly Vector2 ToppingCounterPos = new(-6.75f, -0.74f);
        static readonly Vector2[] TablePositions = { new(1.03f, 2.06f), new(10.06f, 2.06f), new(1.03f, -3.7f), new(10.06f, -3.7f) };
        const float SeatBehindTable = 0.9f; // 손님 발 위치 = 테이블 아래끝 + 이 값 (테이블 뒤에 선 모습)
        const float AisleY = 1.2f;          // 손님이 지나다니는 홀 가로 통로
        static readonly Vector2 DoorPos = new(11.61f, -7.6f);
        static readonly Vector2 SpawnPos = new(11.61f, -9.8f);
        static readonly Vector2 PlayerStart = new(-6.21f, 1.57f);

        // 크기 (폭, 유닛)
        const float StoveWidth = 1.45f, NoodleBoxWidth = 1.75f, SinkWidth = 3.83f, SinkBowlStackWidth = 1.0f;
        const float UrnWidth = 1.53f, ToppingCounterWidth = 4.49f, TableWidth = 1.74f;
        const float PlayerHeight = 3.4f, CustomerHeight = 3.2f;
        const float PlayerSpeed = 7f, PlayerColliderRadius = 0.45f;

        static Sprite square;

        [MenuItem("Ramen Tycoon/Build LAST ORDER Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            contentCache.Clear();

            EnsureFolder(Root + "/Art/Placeholder");
            EnsureFolder(IngredientFolder);
            EnsureFolder(Root + "/Scenes");

            square = GetOrCreateSquare();
            var ing = CreateIngredients();
            var art = GetOrCreateAsset<GameArtConfig>(DataFolder + "/GameArtConfig.asset");
            var days = GetOrCreateAsset<DayConfig>(DataFolder + "/DayConfig.asset");
            AutoAssignArt(art, ing);
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateMap(art);
            CreateWalls();

            // 시스템
            var systems = new GameObject("_Systems");
            systems.AddComponent<GameClock>();
            systems.AddComponent<GameFlow>();
            var db = systems.AddComponent<GameDatabase>();
            db.art = art;
            db.dayConfig = days;
            db.tonkotsu = ing.tonkotsu;
            db.spicy = ing.spicy;
            db.noodle = ing.noodle;
            db.toppings = new[] { ing.chashu, ing.nori, ing.scallion, ing.egg };
            var factory = systems.AddComponent<ItemFactory>();
            SetRef(factory, "placeholderSprite", square);
            systems.AddComponent<Economy>();
            systems.AddComponent<OrderBoard>();

            var door = new GameObject("DoorPoint").transform;
            door.position = DoorPos;
            var spawn = new GameObject("SpawnPoint").transform;
            spawn.position = SpawnPos;
            var dayManager = systems.AddComponent<DayManager>();
            SetRef(dayManager, "spawnPoint", spawn);
            SetRef(dayManager, "doorPoint", door);
            SetFloat(dayManager, "aisleY", AisleY);
            SetFloat(dayManager, "customerHeight", CustomerHeight);

            new GameObject("_UI").AddComponent<UIRoot>();

            CreatePlayer(art);
            CreateKitchen(art, ing, days);
            CreateHall(art, days);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[LastOrderSceneBuilder] 완료: {ScenePath}\n{MissingArtReport(art, ing)}");
            Selection.activeObject = art;
        }

        // ───────── 맵 / 카메라 / 벽 ─────────

        static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.06f, 0.04f);
            go.transform.position = new Vector3(0f, 0f, -10f);
        }

        static void CreateMap(GameArtConfig art)
        {
            var room = new GameObject("Room");
            if (art.map != null)
            {
                var sr = MakeSprite(room.transform, "Map", art.map, Color.white, Vector2.zero, -1000);
                SpriteFit.FitWidth(sr, MapWidth);
            }
            else
            {
                MakeSprite(room.transform, "Floor_Kitchen", square, new Color(0.6f, 0.66f, 0.72f), new Vector2(-9.4f, -0.15f), -1000).transform.localScale = new Vector3(9.6f, 14.3f, 1f);
                MakeSprite(room.transform, "Floor_Hall", square, new Color(0.45f, 0.28f, 0.17f), new Vector2(4.95f, -0.15f), -1000).transform.localScale = new Vector3(18.5f, 14.3f, 1f);
            }
        }

        static void CreateWalls()
        {
            var walls = new GameObject("Walls").transform;
            float midY = (Top + Bottom) * 0.5f;
            float height = (Top - Bottom) + WallThickness * 2f;
            float width = (Right - Left) + WallThickness * 2f;
            float bottomY = Bottom - WallThickness * 0.5f;
            float leftMin = Left - WallThickness, rightMax = Right + WallThickness;

            Wall(walls, "Wall_Top", new Vector2(0f, Top + WallThickness * 0.5f), new Vector2(width, WallThickness));
            Wall(walls, "Wall_Left", new Vector2(Left - WallThickness * 0.5f, midY), new Vector2(WallThickness, height));
            Wall(walls, "Wall_Right", new Vector2(Right + WallThickness * 0.5f, midY), new Vector2(WallThickness, height));
            Wall(walls, "Wall_Bottom_L", new Vector2((leftMin + GapMinX) * 0.5f, bottomY), new Vector2(GapMinX - leftMin, WallThickness));
            Wall(walls, "Wall_Bottom_R", new Vector2((GapMaxX + rightMax) * 0.5f, bottomY), new Vector2(rightMax - GapMaxX, WallThickness));
            // 출입구: 손님은 충돌이 없어 지나가고, 플레이어만 막힌다
            Wall(walls, "Wall_Entrance", new Vector2((GapMinX + GapMaxX) * 0.5f, Bottom - 0.9f), new Vector2(GapMaxX - GapMinX, 0.4f));
        }

        static void Wall(Transform parent, string wallName, Vector2 center, Vector2 size)
        {
            var go = new GameObject(wallName);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        // ───────── 플레이어 ─────────

        static void CreatePlayer(GameArtConfig art)
        {
            var go = new GameObject("Player");
            go.transform.position = PlayerStart;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = PlayerColliderRadius;

            SpriteRenderer body;
            if (art.playerSprite != null)
            {
                body = MakeSprite(go.transform, "Body", art.playerSprite, Color.white, Vector2.zero, 0, local: true);
                FitContentHeight(body, PlayerHeight);
            }
            else
            {
                body = MakeSprite(go.transform, "Body", square, new Color(0.3f, 0.5f, 0.9f), Vector2.zero, 0, local: true);
                body.transform.localScale = new Vector3(0.75f, PlayerHeight * 0.8f, 1f);
            }
            float h = art.playerSprite != null ? ContentSize(body).y : SpriteFit.WorldSize(body).y;
            // 루트 = 발 위치: 그림의 실제 아래끝이 루트에 오도록
            body.transform.localPosition = art.playerSprite != null
                ? new Vector3(-ContentCenter(body).x, h * 0.5f - ContentCenter(body).y, 0f)
                : new Vector3(0f, h * 0.5f, 0f);

            var anchor = new GameObject("HoldAnchor").transform;
            anchor.SetParent(go.transform, false);
            anchor.localPosition = new Vector3(0f, h * 0.45f, 0f);

            go.AddComponent<YSort>().Body = body;
            var controller = go.AddComponent<PlayerController>();
            SetFloat(controller, "moveSpeed", PlayerSpeed);
            var hand = go.AddComponent<PlayerHand>();
            SetRef(hand, "holdAnchor", anchor);
            var interactor = go.AddComponent<PlayerInteractor>();
            SetFloat(interactor, "probeDistance", 0.9f);
            SetFloat(interactor, "probeRadius", 0.85f);

            if (art.playerAnimator != null)
            {
                var anim = body.gameObject.AddComponent<Animator>();
                anim.runtimeAnimatorController = art.playerAnimator;
                var visual = go.AddComponent<PlayerVisual>();
                SetRef(visual, "animator", anim);
                SetRef(visual, "spriteRenderer", body);
            }
        }

        // ───────── 주방 ─────────

        static void CreateKitchen(GameArtConfig art, Ingredients ing, DayConfig days)
        {
            var kitchen = new GameObject("Kitchen").transform;

            // 화구 3자리: 시안처럼 3개 다 보이고, 아직 안 산 화구는 어둡게 잠김 (업그레이드로 해제)
            var stoveSlots = new GameObject("StoveSlots");
            stoveSlots.transform.SetParent(kitchen, false);
            var stoves = new List<GameObject>();
            for (int i = 0; i < StovePositions.Length; i++)
            {
                var stove = CreateStation<Stove>(stoveSlots.transform, $"Stove_{i + 1}", StovePositions[i], art.stove, StoveWidth,
                    new Vector2(1.3f, 1.3f), new Color(0.35f, 0.35f, 0.38f), out Vector2 size);
                var pot = MakeSprite(stove.transform, "Content", art.potCooking != null ? art.potCooking : square, Color.white, new Vector2(0f, size.y * 0.18f), 0, local: true);
                if (art.potCooking != null) FitContentWidth(pot, size.x * 0.55f);
                else pot.transform.localScale = new Vector3(size.x * 0.45f, size.x * 0.32f, 1f);
                pot.sortingOrder = BodyOrder(stove) + 1;
                pot.enabled = false;
                var bar = CreateProgressBar(stove.transform, new Vector2(0f, size.y * 0.5f + 0.3f), size.x * 0.8f, BodyOrder(stove) + 20);
                SetRef(stove, "contentRenderer", pot);
                SetRef(stove, "progressBar", bar);
                stoves.Add(stove.gameObject);
            }
            var slots = stoveSlots.AddComponent<UpgradeSlots>();
            SetRefArray(slots, "slots", stoves.ToArray());
            SetInt(slots, "startLevel", Mathf.Clamp(days.activeStoves, 1, stoves.Count));
            SetInt(slots, "lockedMode", (int)UpgradeSlots.LockedMode.ShowLocked);
            SetInt(slots, "debugUpgradeKey", (int)Key.F3);

            // 면 상자
            var noodle = CreateStation<IngredientCrate>(kitchen, "NoodleBox", NoodleBoxPos, art.noodleBox, NoodleBoxWidth,
                new Vector2(1.6f, 1.5f), new Color(0.75f, 0.4f, 0.25f), out _);
            SetRef(noodle, "ingredient", ing.noodle);

            // 싱크대 (무한 그릇 + 설거지)
            var sink = CreateStation<Sink>(kitchen, "Sink", SinkPos, art.sink, SinkWidth,
                new Vector2(3.4f, 2.2f), new Color(0.65f, 0.68f, 0.72f), out Vector2 sinkSize);
            var sinkBar = CreateProgressBar(sink.transform, new Vector2(sinkSize.x * 0.22f, sinkSize.y * 0.5f + 0.3f), 1.2f, BodyOrder(sink) + 20);
            SetRef(sink, "progressBar", sinkBar);
            if (art.sinkBowlStack != null)
            {
                var stack = MakeSprite(sink.transform, "BowlStack", art.sinkBowlStack, Color.white, Vector2.zero, BodyOrder(sink) + 1);
                var stackSize = FitContentWidth(stack, SinkBowlStackWidth);
                stack.transform.position = (Vector3)(SinkBowlStackPos + Vector2.up * (stackSize.y * 0.5f)) - (Vector3)ContentCenter(stack);
            }

            // 육수 통 (왼쪽 벽에 세로로)
            var urnT = CreateStation<BrothUrn>(kitchen, "BrothUrn_Tonkotsu", UrnTonkotsuPos, art.brothUrnTonkotsu, UrnWidth,
                new Vector2(1.4f, 2.3f), new Color(0.85f, 0.78f, 0.6f), out _);
            SetRef(urnT, "broth", ing.tonkotsu);
            var urnS = CreateStation<BrothUrn>(kitchen, "BrothUrn_Spicy", UrnSpicyPos, art.brothUrnSpicy, UrnWidth,
                new Vector2(1.4f, 2.3f), new Color(0.8f, 0.3f, 0.22f), out _);
            SetRef(urnS, "broth", ing.spicy);

            // 고명 작업대 (긴 회색 조리대)
            CreateStation<ToppingStation>(kitchen, "ToppingCounter", ToppingCounterPos, art.toppingCounter, ToppingCounterWidth,
                new Vector2(4.3f, 1.4f), new Color(0.55f, 0.55f, 0.58f), out _);
        }

        // ───────── 홀 ─────────

        static void CreateHall(GameArtConfig art, DayConfig days)
        {
            var hall = new GameObject("Hall").transform;
            var tableSlots = new GameObject("TableSlots");
            tableSlots.transform.SetParent(hall, false);

            var tables = new List<GameObject>();
            for (int i = 0; i < TablePositions.Length; i++)
            {
                var table = CreateStation<Table>(tableSlots.transform, $"Table_{i + 1}", TablePositions[i], art.table, TableWidth,
                    new Vector2(1.7f, 1.2f), new Color(0.68f, 0.47f, 0.32f), out Vector2 size);
                SetInt(table, "tableNo", i + 1);

                // 손님 자리: 테이블 뒤쪽 (손님 발이 테이블 아래끝보다 조금 위 → 테이블이 손님 앞에 그려짐)
                var seat = new GameObject("Seat").transform;
                seat.SetParent(table.transform, false);
                seat.position = new Vector3(TablePositions[i].x, TablePositions[i].y + SeatBehindTable, 0f);
                SetRef(table, "seatPoint", seat);

                if (art.chair != null)
                {
                    var chair = MakeSprite(table.transform, "Chair", art.chair, Color.white, Vector2.zero, 0);
                    var chairSize = FitContentWidth(chair, TableWidth * 0.6f);
                    chair.transform.position = seat.position + Vector3.up * (chairSize.y * 0.5f) - (Vector3)ContentCenter(chair);
                    chair.sortingOrder = SpriteFit.SortOrderForY(seat.position.y) - 1;
                }

                tables.Add(table.gameObject);
            }

            var slots = tableSlots.AddComponent<UpgradeSlots>();
            SetRefArray(slots, "slots", tables.ToArray());
            SetInt(slots, "startLevel", Mathf.Clamp(days.activeTables, 1, tables.Count));
        }

        // ───────── 스테이션 공통 ─────────

        /// <summary>
        /// 루트(콜라이더 + 컴포넌트) / Body(스프라이트) / ItemAnchor.
        /// bottom = 바닥 가운데 위치. 이미지가 있으면 폭을 width로 맞추고, 없으면 fallbackSize 네모.
        /// 콜라이더는 그림의 아래쪽(바닥에 닿는 부분)만 막아서 플레이어가 앞에 바짝 설 수 있게 한다.
        /// </summary>
        static T CreateStation<T>(Transform parent, string stationName, Vector2 bottom, Sprite sprite, float width,
            Vector2 fallbackSize, Color fallbackColor, out Vector2 size) where T : Station
        {
            var go = new GameObject(stationName);
            go.transform.SetParent(parent, false);

            SpriteRenderer body;
            if (sprite != null)
            {
                body = MakeSprite(go.transform, "Body", sprite, Color.white, Vector2.zero, 0, local: true);
                size = FitContentWidth(body, width); // 투명 여백을 뺀 실제 그림 폭으로 맞춤
            }
            else
            {
                body = MakeSprite(go.transform, "Body", square, fallbackColor, Vector2.zero, 0, local: true);
                body.transform.localScale = new Vector3(fallbackSize.x, fallbackSize.y, 1f);
                size = fallbackSize;
            }

            go.transform.position = new Vector3(bottom.x, bottom.y + size.y * 0.5f, 0f);
            if (sprite != null) body.transform.localPosition = -(Vector3)ContentCenter(body); // 그림 가운데 = 루트
            body.sortingOrder = SpriteFit.SortOrderForY(bottom.y + 0.1f);

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(size.x * 0.92f, size.y * 0.6f);
            col.offset = new Vector2(0f, -size.y * 0.2f);

            var anchor = new GameObject("ItemAnchor").transform;
            anchor.SetParent(go.transform, false);
            anchor.localPosition = new Vector3(0f, size.y * 0.15f, 0f);

            var station = go.AddComponent<T>();
            SetRef(station, "bodyRenderer", body);
            SetRef(station, "itemAnchor", anchor);
            return station;
        }

        static int BodyOrder(Station s)
        {
            var sr = s.GetComponentInChildren<SpriteRenderer>();
            return sr != null ? sr.sortingOrder : 0;
        }

        static ProgressBar CreateProgressBar(Transform parent, Vector2 localPos, float width, int order)
        {
            var bar = new GameObject("ProgressBar");
            bar.transform.SetParent(parent, false);
            bar.transform.localPosition = localPos;
            bar.transform.localScale = new Vector3(width, width, 1f); // 바 안쪽은 폭 1 기준
            MakeSprite(bar.transform, "Background", square, new Color(0.1f, 0.1f, 0.1f), Vector2.zero, order, local: true)
                .transform.localScale = new Vector3(1f, 0.15f, 1f);
            var fill = MakeSprite(bar.transform, "Fill", square, new Color(0.45f, 0.85f, 0.35f), Vector2.zero, order + 1, local: true);
            fill.transform.localScale = new Vector3(1f, 0.15f, 1f);
            var progress = bar.AddComponent<ProgressBar>();
            SetRef(progress, "fill", fill.transform);
            return progress;
        }

        static SpriteRenderer MakeSprite(Transform parent, string objName, Sprite sprite, Color color, Vector2 pos, int order, bool local = false)
        {
            var go = new GameObject(objName);
            if (parent != null) go.transform.SetParent(parent, false);
            if (local) go.transform.localPosition = pos;
            else go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }


        // ───────── 투명 여백을 뺀 실제 그림 크기 ─────────
        // Unity는 스프라이트 크기를 투명 여백까지 포함해서 계산한다. 그래픽마다 여백이 달라서
        // (테이블은 그림이 전체 폭의 43%뿐) 원본 PNG를 직접 읽어 실제 그림 영역을 구한다.

        static readonly Dictionary<Sprite, Rect> contentCache = new Dictionary<Sprite, Rect>();

        /// <summary>스프라이트 로컬 좌표(유닛, 스케일 적용 전)의 실제 그림 영역</summary>
        static Rect ContentRectUnits(Sprite sprite)
        {
            if (contentCache.TryGetValue(sprite, out var cached)) return cached;

            Rect full = new Rect(sprite.bounds.min, sprite.bounds.size);
            Rect result = full;
            string path = AssetDatabase.GetAssetPath(sprite);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(File.ReadAllBytes(path)))
                {
                    // 임포트 최대 크기 때문에 줄어든 경우를 보정
                    float k = sprite.texture != null && sprite.texture.width > 0 ? (float)tex.width / sprite.texture.width : 1f;
                    Rect r = sprite.rect;
                    int x0 = Mathf.FloorToInt(r.x * k), y0 = Mathf.FloorToInt(r.y * k);
                    int x1 = Mathf.Min(tex.width, Mathf.CeilToInt(r.xMax * k)), y1 = Mathf.Min(tex.height, Mathf.CeilToInt(r.yMax * k));
                    var px = tex.GetPixels32();
                    int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
                    for (int y = y0; y < y1; y++)
                    {
                        for (int x = x0; x < x1; x++)
                        {
                            if (px[y * tex.width + x].a < 40) continue;
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                    if (maxX >= 0)
                    {
                        float ppu = sprite.pixelsPerUnit * k;
                        float px0 = r.x * k + sprite.pivot.x * k; // 피벗의 텍스처 좌표
                        float py0 = r.y * k + sprite.pivot.y * k;
                        result = Rect.MinMaxRect((minX - px0) / ppu, (minY - py0) / ppu, (maxX + 1 - px0) / ppu, (maxY + 1 - py0) / ppu);
                    }
                }
                Object.DestroyImmediate(tex);
            }

            contentCache[sprite] = result;
            float ratio = full.width > 0f ? result.width / full.width : 1f;
            if (ratio > 0.999f) Debug.Log($"[LastOrderSceneBuilder] {sprite.name}: 투명 여백 없음 (원본 크기로 배치)");
            else Debug.Log($"[LastOrderSceneBuilder] {sprite.name}: 실제 그림 폭 = 이미지의 {ratio:P0} → 여백을 빼고 배치");
            return result;
        }

        static Vector2 FitContentWidth(SpriteRenderer sr, float width)
        {
            Rect c = ContentRectUnits(sr.sprite);
            float k = c.width > 0f ? width / c.width : 1f;
            sr.transform.localScale = new Vector3(k, k, 1f);
            return c.size * k;
        }

        static Vector2 FitContentHeight(SpriteRenderer sr, float height)
        {
            Rect c = ContentRectUnits(sr.sprite);
            float k = c.height > 0f ? height / c.height : 1f;
            sr.transform.localScale = new Vector3(k, k, 1f);
            return c.size * k;
        }

        static Vector2 ContentSize(SpriteRenderer sr)
        {
            Rect c = ContentRectUnits(sr.sprite);
            return Vector2.Scale(c.size, sr.transform.localScale);
        }

        /// <summary>실제 그림 가운데가 스프라이트 원점에서 얼마나 떨어져 있는지 (스케일 적용)</summary>
        static Vector2 ContentCenter(SpriteRenderer sr)
        {
            Rect c = ContentRectUnits(sr.sprite);
            return Vector2.Scale(c.center, sr.transform.localScale);
        }

        // ───────── 재료 ─────────

        class Ingredients
        {
            public IngredientData tonkotsu, spicy, noodle, chashu, nori, scallion, egg;
        }

        static Ingredients CreateIngredients()
        {
            return new Ingredients
            {
                tonkotsu = Ingredient("ING_Broth_Tonkotsu", "tonkotsu", "돈코츠", IngredientCategory.Broth, new Color(0.93f, 0.85f, 0.68f), 11, 40, "돈코츠 육수"),
                spicy = Ingredient("ING_Broth_Spicy", "spicy", "매운", IngredientCategory.Broth, new Color(0.85f, 0.3f, 0.2f), 13, 44, "매운 육수"),
                noodle = Ingredient("ING_Noodle", "noodle", "면", IngredientCategory.Noodle, new Color(1f, 0.9f, 0.45f), 0, 0, null),
                chashu = Ingredient("ING_Top_Chashu", "chashu", "차슈", IngredientCategory.Topping, new Color(0.94f, 0.6f, 0.48f), 0, 0, null),
                nori = Ingredient("ING_Top_Nori", "nori", "김", IngredientCategory.Topping, new Color(0.25f, 0.32f, 0.18f), 0, 0, null),
                scallion = Ingredient("ING_Top_Scallion", "scallion", "파", IngredientCategory.Topping, new Color(0.5f, 0.78f, 0.3f), 0, 0, null),
                egg = Ingredient("ING_Top_Egg", "egg", "계란", IngredientCategory.Topping, new Color(1f, 0.85f, 0.4f), 0, 0, null),
            };
        }

        static IngredientData Ingredient(string file, string id, string display, IngredientCategory cat, Color color, int cost, int price, string oldDisplayName)
        {
            string path = $"{IngredientFolder}/{file}.asset";
            var data = AssetDatabase.LoadAssetAtPath<IngredientData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<IngredientData>();
                data.displayName = display;
                data.placeholderColor = color;
                AssetDatabase.CreateAsset(data, path);
            }
            else if (oldDisplayName != null && data.displayName == oldDisplayName)
            {
                data.displayName = display; // 예전 그레이박스 이름 → 짧은 이름 (UI에서 "○○ 라멘"으로 표시)
            }

            data.id = id;
            data.category = cat;
            if (data.cost == 0) data.cost = cost;
            if (data.menuPrice == 0) data.menuPrice = price;
            EditorUtility.SetDirty(data);
            return data;
        }

        // ───────── 이미지 자동 연결 ─────────

        static void AutoAssignArt(GameArtConfig art, Ingredients ing)
        {
            if (art.map == null) art.map = FindSprite(true, "map");
            if (art.stove == null) art.stove = FindSprite(true, "stove_1", "stove");
            if (art.sink == null) art.sink = FindSprite(true, "sink");
            if (art.sinkBowlStack == null) art.sinkBowlStack = FindSprite(true, "bowl_stack");
            if (art.potCooking == null) art.potCooking = FindSprite(true, "pot_cooking");
            if (art.potDone == null) art.potDone = FindSprite(true, "pot_done");
            if (art.brothUrnTonkotsu == null) art.brothUrnTonkotsu = FindSprite(true, "broth_tonkotsu", "urn_tonkotsu");
            if (art.brothUrnSpicy == null) art.brothUrnSpicy = FindSprite(true, "broth_spicy", "urn_spicy");
            if (art.noodleBox == null) art.noodleBox = FindSprite(true, "noodle_box");
            if (art.toppingCounter == null) art.toppingCounter = FindSprite(true, "topping_counter", "counter");
            if (art.table == null) art.table = FindSprite(true, "table");
            if (art.chair == null) art.chair = FindSprite(true, "chair");
            if (art.toppingBackground == null) art.toppingBackground = FindSprite(true, "topping_bg");
            if (art.decoBowlTonkotsu == null) art.decoBowlTonkotsu = FindSprite(true, "deco_bowl_tonkotsu");
            if (art.decoBowlSpicy == null) art.decoBowlSpicy = FindSprite(true, "deco_bowl_spicy");
            if (art.bowlClean == null) art.bowlClean = FindSprite(true, "bowl_clean");
            if (art.bowlDirty == null) art.bowlDirty = FindSprite(true, "bowl_dirty");
            if (art.bowlRamenTonkotsu == null) art.bowlRamenTonkotsu = FindSprite(true, "bowl_ramen_tonkotsu");
            if (art.bowlRamenSpicy == null) art.bowlRamenSpicy = FindSprite(true, "bowl_ramen_spicy");
            if (art.speechBubble == null) art.speechBubble = FindSprite(true, "speech_bubble");
            if (art.memoPaper == null) art.memoPaper = FindSprite(true, "memo_paper");
            if (art.bellSprite == null) art.bellSprite = FindSprite(true, "bell");

            // 플레이어 (임시 펭귄) — 원본 임포트 설정은 건드리지 않는다
            if (art.playerSprite == null) art.playerSprite = FindSprite(false, "penguin_idle_01");
            if (art.playerAnimator == null) art.playerAnimator = FindController("PlayerAnimator");

            // 고명 이미지 → 재료 아이콘 + 데코 이미지
            AssignIngredientArt(ing.chashu, "topping_chashu");
            AssignIngredientArt(ing.nori, "topping_nori");
            AssignIngredientArt(ing.scallion, "topping_scallion");
            AssignIngredientArt(ing.egg, "topping_egg");
            AssignIngredientArt(ing.noodle, "icon_noodle");
            AssignIngredientArt(ing.tonkotsu, "icon_tonkotsu");
            AssignIngredientArt(ing.spicy, "icon_spicy");
        }

        static void AssignIngredientArt(IngredientData data, string fileName)
        {
            if (data == null) return;
            var s = FindSprite(true, fileName);
            if (s == null) return;
            if (data.icon == null) data.icon = s;
            if (data.decoSprite == null && data.category == IngredientCategory.Topping) data.decoSprite = s;
            EditorUtility.SetDirty(data);
        }

        /// <summary>파일 이름(확장자 제외)이 정확히 같은 스프라이트를 찾는다. configure면 픽셀아트용 임포트 설정 적용.</summary>
        static Sprite FindSprite(bool configure, params string[] names)
        {
            foreach (var n in names)
            {
                foreach (var guid in AssetDatabase.FindAssets(n + " t:Texture2D"))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.Equals(Path.GetFileNameWithoutExtension(path), n, System.StringComparison.OrdinalIgnoreCase)) continue;
                    if (configure) ConfigureImporter(path);
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite != null) return sprite;
                }

                // 시트 안의 개별 스프라이트 (penguin_idle_01 등)
                foreach (var guid in AssetDatabase.FindAssets(n + " t:Sprite"))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                    {
                        if (obj is Sprite sp && string.Equals(sp.name, n, System.StringComparison.OrdinalIgnoreCase)) return sp;
                    }
                }
            }
            return null;
        }

        static RuntimeAnimatorController FindController(string controllerName)
        {
            foreach (var guid in AssetDatabase.FindAssets(controllerName + " t:RuntimeAnimatorController"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.Equals(Path.GetFileNameWithoutExtension(path), controllerName, System.StringComparison.OrdinalIgnoreCase)) continue;
                var c = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
                if (c != null) return c;
            }
            return null;
        }

        static void ConfigureImporter(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
            if (importer.spriteImportMode == SpriteImportMode.None) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; changed = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
            if (importer.maxTextureSize < 2048) { importer.maxTextureSize = 2048; changed = true; }
            if (changed) importer.SaveAndReimport();
        }

        static string MissingArtReport(GameArtConfig art, Ingredients ing)
        {
            var sb = new StringBuilder("아직 이미지가 없는 칸 (네모로 표시됨): ");
            var missing = new List<string>();
            void Check(Object o, string label) { if (o == null) missing.Add(label); }

            Check(art.map, "맵");
            Check(art.stove, "화구");
            Check(art.sink, "싱크대");
            Check(art.brothUrnTonkotsu, "돈코츠 육수 통");
            Check(art.brothUrnSpicy, "매운 육수 통");
            Check(art.noodleBox, "면 상자");
            Check(art.toppingCounter, "고명 작업대");
            Check(art.table, "테이블");
            Check(art.toppingBackground, "고명 화면 배경");
            Check(art.decoBowlTonkotsu, "데코 그릇(돈코츠)");
            Check(art.decoBowlSpicy, "데코 그릇(매운)");
            Check(art.playerSprite, "플레이어");
            Check(ing.chashu.icon, "차슈");
            Check(ing.nori.icon, "김");
            Check(ing.scallion.icon, "파");
            Check(ing.egg.icon, "계란");

            sb.Append(missing.Count == 0 ? "없음" : string.Join(", ", missing));
            return sb.ToString();
        }

        // ───────── 에셋 / 직렬화 도우미 ─────────

        static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static Sprite GetOrCreateSquare()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
            if (existing != null) return existing;

            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var pixels = new Color32[16 * 16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            File.WriteAllBytes(SquarePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(SquarePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SquarePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void AddToBuildSettings(string path)
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (list.Exists(s => s.path == path)) return;
            list.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        static SerializedProperty Prop(Object target, string field, out SerializedObject so)
        {
            so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) Debug.LogError($"[LastOrderSceneBuilder] {target.GetType().Name}.{field} 필드를 찾을 수 없습니다.");
            return prop;
        }

        static void SetRef(Object target, string field, Object value)
        {
            var prop = Prop(target, field, out var so);
            if (prop == null) return;
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetRefArray(Object target, string field, Object[] values)
        {
            var prop = Prop(target, field, out var so);
            if (prop == null) return;
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetInt(Object target, string field, int value)
        {
            var prop = Prop(target, field, out var so);
            if (prop == null) return;
            prop.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(Object target, string field, float value)
        {
            var prop = Prop(target, field, out var so);
            if (prop == null) return;
            prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}