using System;
using UnityEngine;

/// <summary>
/// 使用するマイクを選ぶ。名前に preferredName を含むマイク（USBマイク）を優先し、
/// 見つからなければ最初のマイクを使う。DebugPanel から切り替えることもできる。
/// </summary>
public class MicDeviceSelector : MonoBehaviour
{
    [SerializeField] string preferredName = "USB";

    /// <summary>現在選ばれているマイク名（マイクが1つも無いときは null）。</summary>
    public string CurrentDevice { get; private set; }

    /// <summary>マイクが切り替わったときに通知する（引数は新しいマイク名）。</summary>
    public event Action<string> DeviceChanged;

    void Awake()
    {
        var devices = Microphone.devices;
        Debug.Log($"[MicDeviceSelector] 接続中のマイク ({devices.Length}): {string.Join(" / ", devices)}");
        CurrentDevice = FindPreferred(devices);
    }

    /// <summary>次のマイクに切り替える（会場で別のマイクが選ばれてしまったとき用）。</summary>
    public void SelectNext()
    {
        var devices = Microphone.devices;
        if (devices.Length == 0) return;
        int index = Array.IndexOf(devices, CurrentDevice);
        Select(devices[(index + 1) % devices.Length]);
    }

    /// <summary>マイク一覧を取り直し、優先マイクを選び直す（USBマイクを挿し直したとき用）。</summary>
    public void Refresh() => Select(FindPreferred(Microphone.devices));

    public void Select(string deviceName)
    {
        if (deviceName == CurrentDevice) return;
        CurrentDevice = deviceName;
        Debug.Log($"[MicDeviceSelector] マイクを切り替え: {deviceName}");
        DeviceChanged?.Invoke(deviceName);
    }

    string FindPreferred(string[] devices)
    {
        if (devices.Length == 0)
        {
            Debug.LogWarning("[MicDeviceSelector] マイクが見つかりません。");
            return null;
        }
        if (!string.IsNullOrEmpty(preferredName))
        {
            foreach (var device in devices)
            {
                if (device.IndexOf(preferredName, StringComparison.OrdinalIgnoreCase) >= 0) return device;
            }
            Debug.LogWarning($"[MicDeviceSelector] 「{preferredName}」を含むマイクが無いため {devices[0]} を使います。");
        }
        return devices[0];
    }
}
