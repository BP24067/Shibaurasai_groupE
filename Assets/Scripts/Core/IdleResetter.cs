using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 一定時間プレイヤーの入力（発生やキー入力）がない場合、
/// 自動的にGameMagerをReady状態へリセットするスクリプト。
/// </summary>
public class IdleResetter : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] GameManager gameManager;
    [SerializeField] MicInput mic;

    [Header("設定")]
    [Tooltip("無操作状態がこの秒数続くとReadyに戻す")]
    [SerializeField] float idleTimeout = 30f;

    [Tooltip("「入力があった」と判定する声量のしきい値(MicInput.Level)")]
    [SerializeField, Range(0f, 1f)] float voiceThreshold = 0.05f;

    [Tooltip("キーボード入力やマウスクリックでも放置タイマーをリセットするか")]
    [SerializeField] bool includeAnyInput = true;

    float idleTimer = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(gameManager == null)
        {
            Debug.LogError("[Idleresetter] GameManagerが設定されていません。Inspectorで割り当ててください。");
        }

        if(mic == null)
        {
            Debug.LogWarning("[IdleResetter] MicInputが設定されていません。声による無操作判定が行われません。");
        } 
    }

    // Update is called once per frame
    void Update()
    {
        if(gameManager == null) return;
        
        if(gameManager.State == GameState.Ready)
        {
            idleTimer = 0f;
            return;
        }

        //入力検知
        bool hasInput = false;

        //1.マイク入力の検知（voiceThreshold以上の声が出ていれば入力ありとみなす）
        if(mic != null && mic.Level >= voiceThreshold)
        {
            hasInput = true;
        }

        //2.キーボード・マウス操作の検知（デバッグや汎用入力用）
        if(includeanyInput)
        {
            //Input System（キーボード・マウス）の入力チェック
            if(Keyboard.current != null && Keyboard.current.anyKey.isPressed)
            {
                hasInput = true;
            }
            if(Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                hasInput = true;
            }
        }  

        //タイマー処理
        if(hasInput)
        {
            //操作や発声を検知したらタイマーをリセット
            idleTimer = 0f;
        } 
        else{
            //無操作状態なら時間を進める
            idleTimer += Time.deltaTime;

            //指定時間を超えたらReady状態へ初期化
            if(idleTimer >= idleTimeout)
            {
                Debug.Log($"[Idleressetter] {idleTimeout}秒間入力がなかったため、Readyへリセットします。");
                idleTimer = 0f;
                gameManager.ResetToReady();
            }
        }
    }
}
