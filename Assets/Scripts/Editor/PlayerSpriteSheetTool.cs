using System.Collections.Generic;
using System.Linq;
using RamenTycoon.Core;
using RamenTycoon.Data;
using RamenTycoon.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RamenTycoon.EditorTools
{
    /// <summary>
    /// 메뉴: Ramen Tycoon > Create Player Animation From Selected Sheet
    /// Project 창에서 잘라 둔(Multiple) 플레이어 시트를 선택하고 실행하면:
    ///  1. 대기(Idle) / 걷기(Walk) 애니메이션 클립 생성 (첫 장 = 대기, 나머지 = 걷기, 2-3-4-3 순서로 반복)
    ///  2. IsMoving 파라미터로 전환되는 Animator Controller 생성
    ///  3. GameArtConfig의 Player Sprite / Player Animator 칸 교체
    ///  4. 지금 열린 씬의 플레이어에도 바로 적용 (씬을 다시 빌드하지 않아도 됨)
    /// </summary>
    public static class PlayerSpriteSheetTool
    {
        const string ArtConfigPath = "Assets/_Project/Data/GameArtConfig.asset";
        const float WalkFps = 8f;
        const float PlayerHeight = 3.4f; // 시안 기준 (LastOrderSceneBuilder와 같은 값)

        [MenuItem("Ramen Tycoon/Create Player Animation From Selected Sheet")]
        public static void Run()
        {
            var tex = Selection.activeObject as Texture2D;
            if (tex == null && Selection.activeObject is Sprite selectedSprite) tex = selectedSprite.texture;
            if (tex == null)
            {
                Debug.LogError("[PlayerSpriteSheetTool] Project 창에서 플레이어 시트 이미지를 선택한 뒤 실행하세요.");
                return;
            }

            string texPath = AssetDatabase.GetAssetPath(tex);
            var frames = AssetDatabase.LoadAllAssetsAtPath(texPath).OfType<Sprite>().OrderBy(s => s.rect.x).ToList();
            if (frames.Count < 2)
            {
                Debug.LogError($"[PlayerSpriteSheetTool] 잘린 스프라이트가 {frames.Count}장입니다. Sprite Mode를 Multiple로 바꾸고 Sprite Editor에서 Slice 했는지 확인하세요.");
                return;
            }

            string folder = System.IO.Path.GetDirectoryName(texPath).Replace('\\', '/');
            string baseName = tex.name;

            // 1. 클립
            var idleClip = CreateClip($"{folder}/{baseName}_idle.anim", new List<Sprite> { frames[0] }, 1f);
            var walkFrames = BuildWalkCycle(frames);
            var walkClip = CreateClip($"{folder}/{baseName}_walk.anim", walkFrames, WalkFps);

            // 2. 컨트롤러
            var controller = CreateController($"{folder}/{baseName}_Animator.controller", idleClip, walkClip);

            // 3. GameArtConfig
            var art = AssetDatabase.LoadAssetAtPath<GameArtConfig>(ArtConfigPath);
            if (art != null)
            {
                art.playerSprite = frames[0];
                art.playerAnimator = controller;
                EditorUtility.SetDirty(art);
            }
            else
            {
                Debug.LogWarning($"[PlayerSpriteSheetTool] {ArtConfigPath} 가 없습니다. Build LAST ORDER Scene을 한 번 실행하면 생깁니다.");
            }

            // 4. 현재 씬의 플레이어
            bool applied = ApplyToScenePlayer(frames[0], controller);

            AssetDatabase.SaveAssets();
            Debug.Log($"[PlayerSpriteSheetTool] 완료: 프레임 {frames.Count}장, 걷기 {walkFrames.Count}프레임 반복. " +
                      (applied ? "현재 씬 플레이어에 적용했어요. 씬을 저장하세요(Ctrl+S)." : "열린 씬에 플레이어가 없어 GameArtConfig만 바꿨어요."));
        }

        /// <summary>걷기: 첫 장(대기)을 뺀 나머지를 왕복 (2-3-4-3)</summary>
        static List<Sprite> BuildWalkCycle(List<Sprite> frames)
        {
            var walk = frames.Skip(1).ToList();
            for (int i = walk.Count - 2; i >= 1; i--) walk.Add(walk[i]);
            return walk;
        }

        static AnimationClip CreateClip(string path, List<Sprite> sprites, float fps)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }
            clip.frameRate = fps;

            // 마지막에 첫 장을 한 번 더 넣어 반복할 때 마지막 프레임도 같은 시간만큼 보이게 한다
            var keys = new ObjectReferenceKeyframe[sprites.Count + 1];
            for (int i = 0; i < sprites.Count; i++) keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };
            keys[sprites.Count] = new ObjectReferenceKeyframe { time = sprites.Count / fps, value = sprites[0] };

            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            return clip;
        }

        static AnimatorController CreateController(string path, AnimationClip idle, AnimationClip walk)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null) AssetDatabase.DeleteAsset(path);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

            var sm = controller.layers[0].stateMachine;
            var idleState = sm.AddState("Idle");
            idleState.motion = idle;
            var walkState = sm.AddState("Walk");
            walkState.motion = walk;
            sm.defaultState = idleState;

            var toWalk = idleState.AddTransition(walkState);
            toWalk.hasExitTime = false;
            toWalk.duration = 0f;
            toWalk.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");

            var toIdle = walkState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");

            return controller;
        }

        static bool ApplyToScenePlayer(Sprite firstFrame, RuntimeAnimatorController controller)
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player == null) return false;

            var body = player.transform.Find("Body");
            if (body == null) return false;

            var sr = body.GetComponent<SpriteRenderer>();
            Undo.RecordObject(sr, "Player sprite");
            Undo.RecordObject(body, "Player body");
            sr.sprite = firstFrame;
            sr.color = Color.white;
            SpriteFit.FitHeight(sr, PlayerHeight);

            // 피벗 위치에 맞춰 발이 플레이어 루트(충돌 원)에 오도록 올린다
            float h = SpriteFit.WorldSize(sr).y;
            float pivotRatio = firstFrame.rect.height > 0f ? firstFrame.pivot.y / firstFrame.rect.height : 0.5f;
            body.localPosition = new Vector3(0f, h * pivotRatio, 0f);

            var anim = body.GetComponent<Animator>();
            if (anim == null) anim = Undo.AddComponent<Animator>(body.gameObject);
            Undo.RecordObject(anim, "Player animator");
            anim.runtimeAnimatorController = controller;

            var visual = player.GetComponent<PlayerVisual>();
            if (visual == null) visual = Undo.AddComponent<PlayerVisual>(player.gameObject);
            var so = new SerializedObject(visual);
            so.FindProperty("animator").objectReferenceValue = anim;
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.ApplyModifiedProperties();

            var anchor = player.transform.Find("HoldAnchor");
            if (anchor != null)
            {
                Undo.RecordObject(anchor, "Hold anchor");
                anchor.localPosition = new Vector3(0f, h * 0.45f, 0f);
            }

            EditorUtility.SetDirty(player.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            return true;
        }
    }
}