using UnityEngine;

namespace RamenTycoon.Data
{
    /// <summary>
    /// 캐릭터 한 명의 4방향 스프라이트. 각 배열의 0번 = 서 있는 모습, 1번부터 = 걷기 프레임.
    /// 옆모습은 한쪽 방향만 그리고, 반대쪽은 좌우 뒤집어서 쓴다.
    /// 씬 빌더가 chef_front_0, chef_side_1 같은 이름의 이미지로 자동으로 만든다.
    /// </summary>
    [CreateAssetMenu(menuName = "Ramen Tycoon/Character Sprite Set", fileName = "CharacterSet")]
    public class CharacterSpriteSet : ScriptableObject
    {
        [Tooltip("앞모습 (아래로 걸을 때). 0 = 서 있기, 1~ = 걷기")]
        public Sprite[] front;

        [Tooltip("뒷모습 (위로 걸을 때). 0 = 서 있기, 1~ = 걷기")]
        public Sprite[] back;

        [Tooltip("옆모습. 0 = 서 있기, 1~ = 걷기")]
        public Sprite[] side;

        [Tooltip("옆모습 그림이 왼쪽을 보고 있으면 체크 (오른쪽으로 갈 때 뒤집음)")]
        public bool sideFacesLeft = true;

        [Tooltip("걷기 애니메이션 속도 (초당 프레임)")]
        public float walkFps = 8f;

        public bool IsValid => front != null && front.Length > 0;
    }
}
