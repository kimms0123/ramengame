using UnityEngine;

namespace RamenTycoon.Data
{
    /// <summary>
    /// 게임에 쓰이는 모든 이미지 슬롯. 비어 있는 칸은 코드가 네모/색으로 대신 그린다.
    /// 그래픽이 나오면 이 에셋의 칸에 끌어다 넣고 씬을 다시 빌드하면 바로 적용된다.
    /// (UI 이미지는 Play할 때 읽으므로 씬을 다시 빌드할 필요도 없다)
    /// </summary>
    [CreateAssetMenu(menuName = "Ramen Tycoon/Game Art Config", fileName = "GameArtConfig")]
    public class GameArtConfig : ScriptableObject
    {
        [Header("맵")]
        public Sprite map;

        [Header("주방 설비")]
        public Sprite stove;
        public Sprite sink;
        [Tooltip("싱크대 왼쪽 빈 공간에 쌓인 그릇 (무한 그릇 표시)")]
        public Sprite sinkBowlStack;
        public Sprite brothUrnTonkotsu;
        public Sprite brothUrnSpicy;
        public Sprite noodleBox;
        [Tooltip("고명 작업대 (긴 회색 조리대)")]
        public Sprite toppingCounter;

        [Header("홀")]
        public Sprite table;
        public Sprite chair;

        [Header("월드 그릇 (플레이어가 들고 다니는 작은 그릇)")]
        public Sprite bowlClean;
        public Sprite bowlBrothTonkotsu;
        public Sprite bowlBrothSpicy;
        public Sprite bowlRamenTonkotsu;
        public Sprite bowlRamenSpicy;
        public Sprite bowlDirty;
        [Tooltip("화구 위 냄비 (면 삶는 중)")]
        public Sprite potCooking;
        public Sprite potDone;

        [Header("캐릭터")]
        public Sprite playerSprite;
        public RuntimeAnimatorController playerAnimator;
        [Tooltip("손님 외형. 여러 장 넣으면 랜덤")]
        public Sprite[] customerSprites;
        public RuntimeAnimatorController customerAnimator;

        [Header("고명 데코 화면")]
        [Tooltip("1920x1080 배경 (위쪽 고명 통 4개 포함)")]
        public Sprite toppingBackground;
        public Sprite decoBowlTonkotsu;
        public Sprite decoBowlSpicy;
        public Sprite bellSprite;
        public Sprite trashSprite;

        [Tooltip("배경 이미지 안의 고명 통 위치 (왼쪽 위 기준 0~1). 순서 = 차슈, 김, 파, 계란")]
        public Rect[] toppingBinRects =
        {
            new Rect(0.207f, 0.188f, 0.135f, 0.227f),
            new Rect(0.357f, 0.188f, 0.138f, 0.227f),
            new Rect(0.508f, 0.188f, 0.139f, 0.227f),
            new Rect(0.659f, 0.188f, 0.141f, 0.227f),
        };

        [Tooltip("데코 그릇 위치 (왼쪽 위 기준 0~1)")]
        public Rect decoBowlRect = new Rect(0.40f, 0.47f, 0.29f, 0.50f);

        [Tooltip("고명 화면의 빌지(주문서) 위치: x, y = 왼쪽 위(0~1), width = 폭(화면 비율). 높이는 내용에 맞춰 자동")]
        public Rect memoRect = new Rect(0.06f, 0.48f, 0.2f, 0.46f);

        [Header("UI 공통")]
        [Tooltip("머리 위 말풍선 배경 (9-slice 권장)")]
        public Sprite speechBubble;
        [Tooltip("빌지 종이")]
        public Sprite memoPaper;
        public Sprite hudPanel;
        [Tooltip("정산/시작/엔딩 패널 배경 (9-slice 권장)")]
        public Sprite panelBackground;
        public Sprite buttonSprite;
        public Sprite coinIcon;
        public Sprite clockIcon;
        public Sprite heartIcon;
        public Sprite patienceBarBack;
        public Sprite patienceBarFill;
        public Sprite dirtyIcon;
        public Sprite eatingIcon;

        [Header("폰트 (비우면 기본 폰트)")]
        [Tooltip("한글 픽셀 폰트(예: Galmuri, DungGeunMo)의 .ttf를 넣으면 전체 UI에 적용")]
        public Font uiFont;
    }
}