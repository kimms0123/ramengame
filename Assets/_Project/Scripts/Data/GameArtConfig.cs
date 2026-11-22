using TMPro;
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

        [Header("캐릭터 (4방향)")]
        [Tooltip("플레이어 4방향 스프라이트. 있으면 아래 Player Sprite / Animator보다 우선")]
        public CharacterSpriteSet playerSet;
        [Tooltip("손님 4방향 스프라이트. 여러 개 넣으면 랜덤")]
        public CharacterSpriteSet[] customerSets;

        [Header("캐릭터 (예전 방식, 4방향 세트가 없을 때만 사용)")]
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
        public Rect memoRect = new Rect(0.8031f, 0.3509f, 0.1708f, 0.3028f);

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

        [Header("HUD")]
        [Tooltip("상단 나무 바 (DAY | 남은 시간, 코인 / 빚)")]
        public Sprite hudBar;
        public Sprite settingsIcon;

        [Header("타이틀 화면 (글자는 TMP로 얹음 → 이미지에는 글자 없이)")]
        [Tooltip("버튼이 없는 가게 배경 1920x1080")]
        public Sprite titleBackground;
        [Tooltip("버튼 틀 (시작하기/나가기 공통, 글자·아이콘 없이)")]
        public Sprite titleButton;
        [Tooltip("시작하기 버튼 왼쪽 아이콘 (재생 삼각형)")]
        public Sprite titleStartIcon;
        [Tooltip("나가기 버튼 왼쪽 아이콘 (전원)")]
        public Sprite titleExitIcon;

        [Header("설정 창 (글자는 TMP로 얹음 → 이미지에는 글자 없이)")]
        [Tooltip("창 배경 (테두리, 제목 띠, 크림색 줄 3개 포함 / 글자·슬라이더·버튼 없이). 1383x778 기준 배치")]
        public Sprite settingsPanel;
        public Sprite settingsTitleIcon;
        public Sprite settingsSoundIcon;
        public Sprite settingsScreenIcon;
        [Tooltip("슬라이더 빈 트랙 (테두리 + 황토색)")]
        public Sprite settingsSliderTrack;
        [Tooltip("슬라이더 채움 (빨간색, 가로로 늘어남)")]
        public Sprite settingsSliderFill;
        public Sprite settingsHandle;
        [Tooltip("빈 체크 상자")]
        public Sprite settingsCheckbox;
        [Tooltip("체크 표시 (상자 위에 겹침)")]
        public Sprite settingsCheck;
        public Sprite settingsButtonDefault;
        public Sprite settingsButtonApply;
        public Sprite settingsButtonClose;
        [Tooltip("오른쪽 위 X 버튼")]
        public Sprite settingsButtonX;

        [Header("엔딩")]
        public Sprite endingGood;
        public Sprite endingNormal;
        public Sprite endingBad;
        [Tooltip("아래쪽 대화 상자 (없으면 회색 네모)")]
        public Sprite endingTextBox;
        [Tooltip("다음 페이지 표시 ▼ (없으면 글자로)")]
        public Sprite endingArrow;

        [Header("폰트")]
        [Tooltip("UI 전체에 쓰는 TMP 폰트 에셋. 한글 폰트(.ttf)를 우클릭 → Create > TextMeshPro > Font Asset > SDF 로 만든 것을 넣는다")]
        public TMP_FontAsset uiFontAsset;
    }
}
