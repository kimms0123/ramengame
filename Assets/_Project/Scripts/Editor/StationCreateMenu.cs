using RamenTycoon.Kitchen;
using UnityEditor;
using UnityEngine;

namespace RamenTycoon.EditorTools
{
    /// <summary>
    /// Hierarchy 우클릭(또는 상단 GameObject 메뉴) > Ramen Tycoon > … 로 설비를 바로 설치한다.
    /// Scene 화면 가운데에 생기고, 그림·충돌 영역·물건 위치는 자동으로 구성된다.
    /// 씬을 새로 만들 필요 없이 원하는 만큼 추가하고 드래그해서 옮기면 된다.
    /// </summary>
    public static class StationCreateMenu
    {
        const string Menu = "GameObject/Ramen Tycoon/";

        [MenuItem(Menu + "Trash Can (쓰레기통)", false, 10)]
        static void TrashCan() => Create<TrashCan>("TrashCan", "Kitchen");

        [MenuItem(Menu + "Stove (화구)", false, 11)]
        static void Stove() => Create<Stove>("Stove", "Kitchen");

        [MenuItem(Menu + "Noodle Box (면 상자)", false, 12)]
        static void NoodleBox() => Create<IngredientCrate>("NoodleBox", "Kitchen");

        [MenuItem(Menu + "Broth Urn - Tonkotsu (돈코츠 육수 통)", false, 13)]
        static void UrnTonkotsu() => Create<BrothUrn>("BrothUrn_Tonkotsu", "Kitchen");

        [MenuItem(Menu + "Broth Urn - Spicy (매운 육수 통)", false, 14)]
        static void UrnSpicy() => Create<BrothUrn>("BrothUrn_Spicy", "Kitchen");

        [MenuItem(Menu + "Sink (싱크대)", false, 15)]
        static void Sink() => Create<Sink>("Sink", "Kitchen");

        [MenuItem(Menu + "Topping Counter (고명 작업대)", false, 16)]
        static void ToppingCounter() => Create<ToppingStation>("ToppingCounter", "Kitchen");

        [MenuItem(Menu + "Table (손님 테이블)", false, 30)]
        static void Table() => Create<Table>("Table", "Hall");

        static void Create<T>(string objName, string parentName) where T : Station
        {
            var go = new GameObject(objName);

            // 부모: 씬의 Kitchen / Hall 아래로 정리 (없으면 맨 위)
            var parent = GameObject.Find(parentName);
            if (parent != null) go.transform.SetParent(parent.transform, false);

            // Scene 화면 가운데에 놓기
            var view = SceneView.lastActiveSceneView;
            Vector3 pos = view != null ? view.pivot : Vector3.zero;
            pos.z = 0f;
            go.transform.position = pos;

            Station.SuppressAutoSetup = true; // AddComponent 때 자동 구성이 한 번 더 돌지 않게
            T station;
            try { station = go.AddComponent<T>(); }
            finally { Station.SuppressAutoSetup = false; }
            station.EditorSetup();

            Undo.RegisterCreatedObjectUndo(go, "Create " + objName);
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }
    }
}
