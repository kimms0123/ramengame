using RamenTycoon.Core;
using UnityEditor;
using UnityEngine;

namespace RamenTycoon.EditorTools
{
    /// <summary>자동 저장 파일 테스트용 메뉴</summary>
    public static class SaveFileMenu
    {
        [MenuItem("Ramen Tycoon/Delete Save File")]
        static void DeleteSave()
        {
            if (!System.IO.File.Exists(SaveSystem.FilePath))
            {
                EditorUtility.DisplayDialog("저장 파일", "저장 파일이 없어요.", "확인");
                return;
            }
            if (!EditorUtility.DisplayDialog("저장 파일 지우기", "자동 저장 파일을 지울까요?\n(타이틀의 이어하기가 사라져요)", "지우기", "취소")) return;
            SaveSystem.Delete();
            Debug.Log("[SaveFileMenu] 저장 파일을 지웠어요.");
        }

        [MenuItem("Ramen Tycoon/Open Save Folder")]
        static void OpenFolder() => EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
