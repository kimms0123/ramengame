using System.Collections.Generic;
using UnityEngine;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 씬에 미리 만들어 두는 UI 화면의 공통 부모.
    ///
    /// - 씬 빌더(Ramen Tycoon 메뉴)가 에디터에서 Build()를 불러 UI 오브젝트를 씬에 저장해 둔다.
    ///   → Scene/Hierarchy에서 위치·크기·이미지·글자를 직접 고칠 수 있다.
    /// - Play 하면 저장된 오브젝트를 그대로 쓰고, 버튼 연결(Bind)만 코드로 한다.
    /// - 씬에 아직 안 만들어져 있거나 필요한 오브젝트가 지워졌으면 Play 때 코드로 다시 만든다.
    /// </summary>
    [DefaultExecutionOrder(110)] // UIRoot(100)가 캔버스를 준비한 뒤
    public abstract class BakedView : MonoBehaviour
    {
        [SerializeField, HideInInspector] List<GameObject> builtObjects = new();

        UIRoot rootCache;

        /// <summary>같은 오브젝트에 붙은 UIRoot (에디터에서도 동작)</summary>
        protected UIRoot Root
        {
            get
            {
                if (rootCache == null) rootCache = UIRoot.Instance != null ? UIRoot.Instance : GetComponent<UIRoot>();
                return rootCache;
            }
        }

        public bool IsBuilt => builtObjects.Count > 0;

        /// <summary>아직 안 만들었거나, 코드가 바뀌어서 필요한 오브젝트가 모자라면 true</summary>
        public bool NeedsBuild => !IsBuilt || !IsIntact();

        protected virtual void Awake()
        {
            EnsureBuilt();
            Bind();
        }

        public void EnsureBuilt()
        {
            if (IsBuilt && IsIntact()) return;
            if (IsBuilt)
                Debug.LogWarning($"[{GetType().Name}] 씬에 만들어 둔 UI 중 꼭 필요한 오브젝트가 지워져서 기본 모양으로 다시 만들어요. " +
                                 "(오브젝트 이름을 바꾸거나 위치를 옮기는 건 괜찮지만, 지우면 안 돼요)");
            Rebuild();
        }

        /// <summary>이 화면이 만든 오브젝트(또는 그 자식)인지</summary>
        public bool Owns(Transform t)
        {
            if (t == null) return false;
            foreach (var go in builtObjects)
                if (go != null && (t == go.transform || t.IsChildOf(go.transform))) return true;
            return false;
        }

        /// <summary>만들어 둔 UI를 지우고 기본 모양으로 다시 만든다 (씬 빌더의 Rebuild UI 메뉴가 사용)</summary>
        public void Rebuild()
        {
            foreach (var go in builtObjects) UIFactory.DestroySafe(go);
            builtObjects.Clear();
            Build();
        }

        /// <summary>새로 만든 최상위 오브젝트를 기록 (다시 만들 때 지우기 위해)</summary>
        protected T Track<T>(T component) where T : Component
        {
            builtObjects.Add(component.gameObject);
            return component;
        }

        /// <summary>UI 오브젝트를 만들고 필요한 참조를 [SerializeField] 칸에 넣는다. 버튼 연결은 하지 않는다.</summary>
        protected abstract void Build();

        /// <summary>코드가 쓰는 참조가 모두 살아 있는지</summary>
        protected abstract bool IsIntact();

        /// <summary>Play 때 버튼·슬라이더에 함수를 연결한다 (씬에 저장되지 않는 부분)</summary>
        protected virtual void Bind() { }
    }
}