namespace RamenTycoon.Logic
{
    public enum EndingType
    {
        Closed,  // 폐업
        Normal,  // 정상 영업
        Gourmet  // 인기 맛집
    }

    /// <summary>
    /// 5일차 정산 후 엔딩 판정. (기획서 07 ECONOMY + 만족도 폐업 조건)
    /// </summary>
    public static class EndingEvaluator
    {
        public static EndingType Evaluate(int coins, int debt, int gourmetRemain, float satisfaction, float closeSatisfaction)
        {
            if (satisfaction < closeSatisfaction) return EndingType.Closed;
            if (coins < debt) return EndingType.Closed;
            if (coins - debt >= gourmetRemain) return EndingType.Gourmet;
            return EndingType.Normal;
        }

        public static string Title(EndingType type) => type switch
        {
            EndingType.Gourmet => "인기 맛집",
            EndingType.Normal => "정상 영업",
            _ => "폐업"
        };

        public static string Description(EndingType type, bool lowSatisfaction) => type switch
        {
            EndingType.Gourmet => "빚을 모두 갚고도 넉넉히 남았다.\n골목에 줄이 서는 가게가 되었다.",
            EndingType.Normal => "빚을 모두 갚았다.\n내일도 가게 불은 켜진다.",
            _ => lowSatisfaction
                ? "손님들의 발길이 끊겼다.\n결국 가게 문을 닫게 되었다."
                : "빚을 갚지 못했다.\n결국 가게 문을 닫게 되었다."
        };
    }
}
