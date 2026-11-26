using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RamenTycoon.EditorTools
{
    /// <summary>
    /// 에디터에서 Play를 누르면 지금 열린 씬과 상관없이 항상 메인화면(Title 씬)부터 시작한다.
    /// (빌드한 게임은 원래 Build Profiles 씬 목록 0번인 Title부터 시작)
    /// 끄고 싶으면 상단 메뉴 Ramen Tycoon > Play Starts From Title 체크 해제.
    /// </summary>
    [InitializeOnLoad]
    static class PlayFromTitle
    {
        const string PrefKey = "LastOrder.PlayFromTitle";
        const string MenuPath = "Ramen Tycoon/Play Starts From Title";
        const string TitlePath = "Assets/_Project/Scenes/Title.unity";

        static PlayFromTitle()
        {
            EditorApplication.delayCall += Apply;
        }

        static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        [MenuItem(MenuPath, false, 200)]
        static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
            Debug.Log(Enabled
                ? "[PlayFromTitle] 이제 Play를 누르면 메인화면(Title)부터 시작해요."
                : "[PlayFromTitle] 이제 Play를 누르면 지금 열린 씬에서 바로 시작해요.");
        }

        [MenuItem(MenuPath, true)]
        static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        static void Apply()
        {
            if (!Enabled)
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            var title = AssetDatabase.LoadAssetAtPath<SceneAsset>(TitlePath);
            if (title == null)
            {
                foreach (var guid in AssetDatabase.FindAssets("Title t:Scene"))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.StartsWith("Assets/_Project")) { title = AssetDatabase.LoadAssetAtPath<SceneAsset>(path); break; }
                }
            }
            if (title == null)
                Debug.LogWarning("[PlayFromTitle] Title 씬을 못 찾았어요. Assets/_Project/Scenes/Title.unity가 있는지 확인하세요.");
            EditorSceneManager.playModeStartScene = title;
        }
    }
}
