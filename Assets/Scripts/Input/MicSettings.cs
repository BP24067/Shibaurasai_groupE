using UnityEngine;

/// <summary>
/// マイクの調整値（最大音量 MaxDb・使用マイク名）を PlayerPrefs に保存し、次回起動時に読み込む。
/// 会場で DebugPanel を使って調整した値を、アプリを再起動しても使えるようにするためのもの。
///
///   読み込み … Start で保存済みの値を MicInput / MicDeviceSelector に反映する
///   保存     … 終了時（OnDisable）に、起動時から値が変わっていれば自動保存。Save() で任意に保存もできる
///
/// しきい値（NoiseFloorDb）は Ready のたびに MicCalibration が測り直すので保存しない。
///
/// 注意: 保存した値は Inspector の値より優先される。Inspector で MaxDb を変えても反映されないときは、
///       このコンポーネントの ⋮ →「保存した設定を削除」を実行するか、loadOnStart をオフにする。
///
/// MicInput と同じオブジェクトに付ける。
/// </summary>
public class MicSettings : MonoBehaviour
{
    [SerializeField] MicInput mic;
    [SerializeField] MicDeviceSelector deviceSelector;
    [Tooltip("起動時に保存済みの値を読み込む")]
    [SerializeField] bool loadOnStart = true;
    [Tooltip("終了時、値が変わっていれば自動で保存する")]
    [SerializeField] bool saveOnQuit = true;

    const string KeyMaxDb = "MicSettings.MaxDb";
    const string KeyDevice = "MicSettings.Device";

    float startMaxDb;
    string startDevice;

    public bool HasSavedSettings => PlayerPrefs.HasKey(KeyMaxDb) || PlayerPrefs.HasKey(KeyDevice);

    string CurrentDevice => deviceSelector != null ? deviceSelector.CurrentDevice : (mic != null ? mic.DeviceName : null);

    void Start()
    {
        if (mic == null)
        {
            Debug.LogError("[MicSettings] Mic が設定されていません。");
            return;
        }
        if (loadOnStart) Load();
        startMaxDb = mic.MaxDb;
        startDevice = CurrentDevice;
    }

    void OnDisable()
    {
        if (!saveOnQuit || mic == null) return;
        bool changed = !Mathf.Approximately(mic.MaxDb, startMaxDb) || CurrentDevice != startDevice;
        if (changed) Save();
    }

    /// <summary>保存済みの値を MicInput / MicDeviceSelector に反映する。</summary>
    public void Load()
    {
        if (mic == null || !HasSavedSettings) return;

        if (PlayerPrefs.HasKey(KeyMaxDb)) mic.MaxDb = PlayerPrefs.GetFloat(KeyMaxDb);

        string device = PlayerPrefs.GetString(KeyDevice, "");
        if (deviceSelector != null && !string.IsNullOrEmpty(device))
        {
            if (System.Array.IndexOf(Microphone.devices, device) >= 0) deviceSelector.Select(device);
            else Debug.LogWarning($"[MicSettings] 保存されたマイク「{device}」が見つからないため、自動で選んだマイクを使います。");
        }

        Debug.Log($"[MicSettings] 読み込み: MaxDb {mic.MaxDb:F1} dB / Device {CurrentDevice}");
    }

    /// <summary>現在の値を保存する。</summary>
    public void Save()
    {
        if (mic == null) return;
        PlayerPrefs.SetFloat(KeyMaxDb, mic.MaxDb);
        PlayerPrefs.SetString(KeyDevice, CurrentDevice ?? "");
        PlayerPrefs.Save();
        Debug.Log($"[MicSettings] 保存: MaxDb {mic.MaxDb:F1} dB / Device {CurrentDevice}");
    }

    /// <summary>保存した値を削除する（Inspector の ⋮ メニューからも実行できる）。</summary>
    [ContextMenu("保存した設定を削除")]
    public void ClearSaved()
    {
        PlayerPrefs.DeleteKey(KeyMaxDb);
        PlayerPrefs.DeleteKey(KeyDevice);
        PlayerPrefs.Save();
        Debug.Log("[MicSettings] 保存した設定を削除しました。");
    }
}
