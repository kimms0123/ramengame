using System;
using System.Collections.Generic;
using RamenTycoon.Customers;
using RamenTycoon.Data;
using RamenTycoon.Kitchen;
using RamenTycoon.Logic;
using RamenTycoon.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RamenTycoon.Core
{
    public enum DayPhase { Prep, Business, Closing, Settlement, Ending }

    /// <summary>
    /// 하루 진행 (기획서 05): 영업 준비 → 영업(4분 30초) → 정산 → 다음 날. 5일차 정산 후 엔딩.
    /// 손님 스폰, 주문 생성도 여기서 한다.
    /// </summary>
    public class DayManager : MonoBehaviour
    {
        public struct SettlementData
        {
            public int day;
            public int revenue;
            public int ingredientCost;
            public int rent;
            public int served;
            public int lost;
            public int mistakes;
            public int net;
            public int coinsAfter;
            public float satisfaction;
        }

        public static DayManager Instance { get; private set; }

        [Header("손님 경로")]
        [SerializeField] Transform spawnPoint;
        [SerializeField] Transform doorPoint;
        [Tooltip("홀 가운데 가로 통로의 y 좌표")]
        [SerializeField] float aisleY = 0.5f;

        [Header("손님 외형 (이미지 없을 때)")]
        [SerializeField] float customerHeight = 1.1f;

        public DayPhase Phase { get; private set; } = DayPhase.Prep;
        public int Day { get; private set; } = 1;
        public float TimeLeft { get; private set; }
        public int WaitingOutside => pendingSince.Count;
        public SettlementData LastSettlement { get; private set; }
        public EndingType Ending { get; private set; }
        public bool EndingByLowSatisfaction { get; private set; }

        public event Action PhaseChanged;

        DayConfig cfg;
        DaySetting setting;
        OrderGenerator generator;
        readonly List<float> spawnTimes = new();
        readonly Queue<float> pendingSince = new();
        int spawnIndex;
        float timer;
        float closingTimer;
        static Sprite placeholderSprite;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            cfg = GameDatabase.Days != null ? GameDatabase.Days : ScriptableObject.CreateInstance<DayConfig>();
            generator = new OrderGenerator(Environment.TickCount);
            BeginPrep(1);
        }

        // ───────── 단계 전환 ─────────

        public void BeginPrep(int day)
        {
            Day = day;
            setting = cfg.Get(day);
            TimeLeft = cfg.businessSeconds;
            if (GameFlow.Instance != null)
            {
                GameFlow.Instance.SetDay(day);
                GameFlow.Instance.ChangeState(GameState.DayPrep);
            }
            SetPhase(DayPhase.Prep, blockInput: true);
        }

        public void StartBusiness()
        {
            if (Phase != DayPhase.Prep) return;

            if (Economy.Instance != null) Economy.Instance.ResetDayStats();
            BuildSchedule();
            timer = 0f;
            closingTimer = 0f;
            pendingSince.Clear();

            if (GameFlow.Instance != null) GameFlow.Instance.ChangeState(GameState.Business);
            SetPhase(DayPhase.Business, blockInput: false);
        }

        public void NextDay()
        {
            if (Phase != DayPhase.Settlement) return;
            if (Day >= cfg.TotalDays) ShowEnding();
            else BeginPrep(Day + 1);
        }

        public void Restart()
        {
            InputGate.GameplayBlocked = false;
            if (GameClock.Instance != null) GameClock.Instance.Paused = false;
            // 씬이 Build Settings에 있어야 한다 (씬 빌더가 자동으로 추가함)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        void SetPhase(DayPhase phase, bool blockInput)
        {
            Phase = phase;
            InputGate.GameplayBlocked = blockInput;
            if (GameClock.Instance != null) GameClock.Instance.Paused = blockInput;
            PhaseChanged?.Invoke();
        }

        // ───────── 영업 ─────────

        void Update()
        {
            if (Phase == DayPhase.Business) UpdateBusiness();
            else if (Phase == DayPhase.Closing) UpdateClosing();
        }

        void UpdateBusiness()
        {
            float dt = GameClock.DeltaTime;
            timer += dt;
            TimeLeft = Mathf.Max(0f, cfg.businessSeconds - timer);

            while (spawnIndex < spawnTimes.Count && timer >= spawnTimes[spawnIndex])
            {
                pendingSince.Enqueue(timer);
                spawnIndex++;
            }

            // 밖에서 너무 오래 기다린 손님은 돌아간다
            while (pendingSince.Count > 0 && timer - pendingSince.Peek() > setting.patience)
            {
                pendingSince.Dequeue();
                if (Economy.Instance != null) Economy.Instance.RecordLost();
            }

            TrySpawnPending();

            if (timer >= cfg.businessSeconds)
            {
                // 아직 못 들어온 손님은 놓친 손님
                while (pendingSince.Count > 0)
                {
                    pendingSince.Dequeue();
                    if (Economy.Instance != null) Economy.Instance.RecordLost();
                }
                if (GameFlow.Instance != null) GameFlow.Instance.ChangeState(GameState.DaySettle);
                SetPhase(DayPhase.Closing, blockInput: false);
            }
        }

        void UpdateClosing()
        {
            closingTimer += GameClock.DeltaTime;

            bool anyBusy = false;
            foreach (var t in ActiveTables())
            {
                if (t.State == Table.TableState.Reserved || t.State == Table.TableState.WaitingFood || t.State == Table.TableState.Eating)
                {
                    anyBusy = true;
                    break;
                }
            }

            if (!anyBusy || closingTimer >= cfg.closingGraceSeconds) EndDay();
        }

        void EndDay()
        {
            foreach (var t in FindObjectsByType<Table>(FindObjectsInactive.Include))
            {
                // 영업 시간 안에 서빙 못 한 손님은 놓친 손님으로 처리
                if (t.State == Table.TableState.WaitingFood && Economy.Instance != null) Economy.Instance.RecordLost();
                t.ForceClear();
            }
            foreach (var c in new List<Customer>(Customer.Active)) Destroy(c.gameObject);
            if (OrderBoard.Instance != null) OrderBoard.Instance.Clear();

            // 들고 있던 그릇/재료는 하루가 끝나면 정리
            var hand = FindAnyObjectByType<PlayerHand>();
            if (hand != null && !hand.IsEmpty) Destroy(hand.Release().gameObject);

            var eco = Economy.Instance;
            int rent = setting.rent;
            if (eco != null) eco.Pay(rent);

            LastSettlement = new SettlementData
            {
                day = Day,
                revenue = eco != null ? eco.DayRevenue : 0,
                ingredientCost = eco != null ? eco.DayIngredientCost : 0,
                rent = rent,
                served = eco != null ? eco.DayServed : 0,
                lost = eco != null ? eco.DayLost : 0,
                mistakes = eco != null ? eco.DayMistakes : 0,
                net = (eco != null ? eco.DayRevenue - eco.DayIngredientCost : 0) - rent,
                coinsAfter = eco != null ? eco.Coins : 0,
                satisfaction = eco != null ? eco.Satisfaction : 0f
            };

            SetPhase(DayPhase.Settlement, blockInput: true);
        }

        void ShowEnding()
        {
            var eco = Economy.Instance;
            int coins = eco != null ? eco.Coins : 0;
            float sat = eco != null ? eco.Satisfaction : 0f;

            Ending = EndingEvaluator.Evaluate(coins, cfg.debt, cfg.gourmetRemain, sat, cfg.closeSatisfaction);
            EndingByLowSatisfaction = sat < cfg.closeSatisfaction;
            if (Ending != EndingType.Closed && eco != null) eco.Pay(cfg.debt); // 빚 상환

            if (GameFlow.Instance != null) GameFlow.Instance.ChangeState(GameState.Ending);
            SetPhase(DayPhase.Ending, blockInput: true);
        }

        // ───────── 손님 스폰 ─────────

        void BuildSchedule()
        {
            spawnTimes.Clear();
            spawnIndex = 0;

            int n = Mathf.Max(0, setting.customers);
            float end = cfg.businessSeconds * 0.88f;
            float rush = Mathf.Clamp(setting.rushSeconds, 0f, end);
            int rushCount = rush > 0f ? Mathf.RoundToInt(n * 0.4f) : 0;
            int normalCount = n - rushCount;

            AddEvenly(spawnTimes, normalCount, 2f, end - rush);
            AddEvenly(spawnTimes, rushCount, end - rush, end);
            spawnTimes.Sort();
        }

        static void AddEvenly(List<float> list, int count, float from, float to)
        {
            if (count <= 0 || to <= from) return;
            float gap = (to - from) / count;
            for (int i = 0; i < count; i++)
            {
                float jitter = UnityEngine.Random.Range(-0.3f, 0.3f) * gap;
                list.Add(Mathf.Clamp(from + gap * (i + 0.5f) + jitter, from, to));
            }
        }

        void TrySpawnPending()
        {
            while (pendingSince.Count > 0)
            {
                var free = new List<Table>();
                foreach (var t in ActiveTables())
                {
                    if (t.IsFree) free.Add(t);
                }
                if (free.Count == 0) return;

                pendingSince.Dequeue();
                SpawnCustomer(free[UnityEngine.Random.Range(0, free.Count)]);
            }
        }

        void SpawnCustomer(Table table)
        {
            var go = new GameObject("Customer");
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(go.transform, false);
            var sr = bodyGo.AddComponent<SpriteRenderer>();

            var art = GameDatabase.Art;
            if (art != null && art.customerSprites != null && art.customerSprites.Length > 0)
            {
                sr.sprite = art.customerSprites[UnityEngine.Random.Range(0, art.customerSprites.Length)];
                sr.color = Color.white;
            }
            else
            {
                sr.sprite = PlaceholderSprite();
                sr.color = Color.HSVToRGB(UnityEngine.Random.value, 0.35f, 0.95f);
            }
            SpriteFit.FitHeight(sr, customerHeight);
            bodyGo.transform.localPosition = new Vector3(0f, customerHeight * 0.5f, 0f);

            if (art != null && art.customerAnimator != null)
            {
                var anim = bodyGo.AddComponent<Animator>();
                anim.runtimeAnimatorController = art.customerAnimator;
            }

            go.AddComponent<YSort>().Body = sr;
            var customer = go.AddComponent<Customer>();

            Vector3 spawn = spawnPoint != null ? spawnPoint.position : new Vector3(11.5f, -9.5f, 0f);
            Vector3 door = doorPoint != null ? doorPoint.position : new Vector3(11.5f, -6.8f, 0f);
            customer.Init(table, spawn, door, aisleY, OnCustomerArrived);
        }

        void OnCustomerArrived(Customer customer, Table table)
        {
            var db = GameDatabase.Instance;
            if (db == null || table == null) return;
            var order = generator.Generate(db.tonkotsu, db.spicy, db.toppings, setting.spicyChance, setting.variationChance);
            table.OnCustomerArrived(order, setting.patience);
        }

        static IEnumerable<Table> ActiveTables()
        {
            return FindObjectsByType<Table>(FindObjectsInactive.Exclude);
        }

        static Sprite PlaceholderSprite()
        {
            if (placeholderSprite == null)
            {
                var tex = Texture2D.whiteTexture;
                placeholderSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            }
            return placeholderSprite;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}