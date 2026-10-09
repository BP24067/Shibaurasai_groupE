using System;
using UnityEngine;

/// <summary>
/// ベストタイム（いちばん速いクリアタイム）を PlayerPrefs に保存する。
/// resetEveryDay がオンなら、日付が変わると記録をリセットする（学祭の「本日のベスト」用）。
///
/// 使い方（ResultUI から）:
///   bool isNew = bestRecord.Submit(gameManager.ElapsedTime);  // 新記録なら true
///   bestRecord.HasRecord / bestRecord.BestTime で表示する
///
/// GameManager と同じオブジェクトに付ける。
/// </summary>
public class BestRecord : MonoBehaviour
{
    [Tooltip("日付が変わったら記録をリセットする")]
    [SerializeField] bool resetEveryDay = true;

    const string KeyTime = "BestRecord.Time";
    const string KeyDate = "BestRecord.Date";

    /// <summary>記録があるかどうか。</summary>
    public bool HasRecord => !float.IsPositiveInfinity(BestTime);

    /// <summary>ベストタイム（秒）。記録が無いときは PositiveInfinity。</summary>
    public float BestTime { get; private set; } = float.PositiveInfinity;

    /// <summary>直前の Submit が新記録だったか。</summary>
    public bool LastWasNewRecord { get; private set; }

    static string Today => DateTime.Now.ToString("yyyy-MM-dd");

    void Awake()
    {
        if (!PlayerPrefs.HasKey(KeyTime)) return;

        if (resetEveryDay && PlayerPrefs.GetString(KeyDate, "") != Today)
        {
            Debug.Log("[BestRecord] 日付が変わったため、ベスト記録をリセットしました。");
            ClearRecord();
            return;
        }
        BestTime = PlayerPrefs.GetFloat(KeyTime);
    }

    /// <summary>クリアタイムを登録する。ベストを更新したら保存して true を返す。</summary>
    public bool Submit(float clearTime)
    {
        LastWasNewRecord = clearTime > 0f && clearTime < BestTime;
        if (LastWasNewRecord)
        {
            BestTime = clearTime;
            PlayerPrefs.SetFloat(KeyTime, clearTime);
            PlayerPrefs.SetString(KeyDate, Today);
            PlayerPrefs.Save();
            Debug.Log($"[BestRecord] 新記録: {clearTime:F2} 秒");
        }
        return LastWasNewRecord;
    }

    /// <summary>記録を削除する（Inspector の ⋮ メニューからも実行できる）。</summary>
    [ContextMenu("ベスト記録をリセット")]
    public void ClearRecord()
    {
        BestTime = float.PositiveInfinity;
        LastWasNewRecord = false;
        PlayerPrefs.DeleteKey(KeyTime);
        PlayerPrefs.DeleteKey(KeyDate);
        PlayerPrefs.Save();
    }
}
