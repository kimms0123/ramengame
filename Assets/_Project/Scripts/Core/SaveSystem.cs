using System;
using System.Collections.Generic;
using System.IO;
using RamenTycoon.Kitchen;
using UnityEngine;

namespace RamenTycoon.Core
{
    /// <summary>저장 파일 내용. 하루가 끝난 시점(정산)의 상태를 담는다.</summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        [Tooltip("이어하기 했을 때 시작할 일차 (마지막 날 정산 뒤에 저장했으면 총 일수 + 1 → 바로 엔딩)")]
        public int day = 1;
        public int coins;
        public float satisfaction;
        public List<UpgradeLevel> upgrades = new();
        public string savedAt;

        public int GetLevel(string id, int fallback)
        {
            if (upgrades != null)
                foreach (var u in upgrades)
                    if (u != null && u.id == id) return u.level;
            return fallback;
        }
    }

    [Serializable]
    public class UpgradeLevel
    {
        public string id;
        public int level;
    }

    /// <summary>
    /// 자동 저장. 하루가 시작될 때(DAY N 화면)와 정산 화면이 뜰 때 저장한다.
    /// 영업 도중에 나가거나 창을 닫으면, 이어하기 했을 때 그날 아침 상태부터 다시 한다.
    /// 엔딩을 보면 저장 파일을 지운다.
    ///
    /// 파일 위치: Application.persistentDataPath/lastorder_save.json
    ///  (Windows: C:\Users\이름\AppData\LocalLow\회사이름\제품이름\)
    /// 테스트할 때 지우려면 메뉴 Ramen Tycoon > Delete Save File.
    /// </summary>
    public static class SaveSystem
    {
        const string FileName = "lastorder_save.json";

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>타이틀에서 [이어하기]를 누르면 채워짐. 게임 씬의 DayManager·Economy·UpgradeSlots가 시작할 때 읽는다.</summary>
        public static SaveData Pending { get; private set; }

        /// <summary>마지막 자동 저장이 성공했는지 (정산 화면 표시용)</summary>
        public static bool LastSaveOk { get; private set; }

        public static bool HasSave => Peek() != null;

        /// <summary>저장 파일을 읽기만 함 (없거나 깨졌으면 null)</summary>
        public static SaveData Peek()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                return data != null && data.day >= 1 ? data : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] 저장 파일을 읽지 못했어요: {e.Message}");
                return null;
            }
        }

        public static bool Save(SaveData data)
        {
            LastSaveOk = false;
            if (data == null) return false;
            try
            {
                data.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath); // 쓰는 도중 꺼져도 기존 파일이 깨지지 않게 임시 파일에 먼저 쓴다
                LastSaveOk = true;
                Debug.Log($"[SaveSystem] 자동 저장 완료 (DAY {data.day}부터 이어하기, {data.coins} C) → {FilePath}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] 저장하지 못했어요: {e.Message}");
            }
            return LastSaveOk;
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] 저장 파일을 지우지 못했어요: {e.Message}");
            }
        }

        /// <summary>[이어하기]: 저장 파일을 읽어 다음 씬에서 쓰도록 준비. 파일이 없으면 false</summary>
        public static bool PrepareContinue()
        {
            Pending = Peek();
            return Pending != null;
        }

        /// <summary>[시작하기]: 새 게임</summary>
        public static void PrepareNewGame() => Pending = null;

        public static void ClearPending() => Pending = null;

        /// <summary>지금 게임 상태를 저장 데이터로 (nextDay = 이어하기 했을 때 시작할 날)</summary>
        public static SaveData Capture(int nextDay)
        {
            var data = new SaveData { day = nextDay };
            var eco = Economy.Instance;
            if (eco != null)
            {
                data.coins = eco.Coins;
                data.satisfaction = eco.Satisfaction;
            }
            foreach (var slots in UnityEngine.Object.FindObjectsByType<UpgradeSlots>(FindObjectsInactive.Include))
            {
                if (slots.MaxLevel <= 0) continue;
                data.upgrades.Add(new UpgradeLevel { id = slots.UpgradeId, level = slots.Level });
            }
            return data;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Pending = null;
            LastSaveOk = false;
        }
    }
}