using UnityEngine;
using TMPro;

public class MessageUI : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("GameManager参照")]
    [SerializeField] private GameManager gameManager;

    private void OnEnable()
    {
        //gameManagerのイベントに状態が変わったら通知
        if(gameManager != null)
        {
            gameManager.OnStateChanged += OnStateChangedHandler;
        }
    }

    private void OnDisable()
    {
        //不要になったら登録解除(メモリリーク・エラー防止)
        if(gameManager != null)
        {
            gameManager.OnStateChanged -= OnStateChangedHandler;
        }
    }

    private void Start()
    {
        // 登録より前に状態が決まっていた場合に備え、今の状態で一度表示する
        if (gameManager != null) OnStateChangedHandler(gameManager.State);
    }

    /// <summary>他のスクリプト（SilentZone など）から一時的にメッセージを出す</summary>
    public void Show(string message)
    {
        if (messageText != null) messageText.text = message;
    }


    /// <summary>
    /// GameManagerの状態(State)が変わった時に自動的に呼び出されるメソッド
    /// </summary>
    /// <param name="newState">変更後の新しい状態</param>
    
    //↓状態が変わった時に実行する処理名
    private void OnStateChangedHandler(GameManager.State newState)
    {
        if(messageText == null) return;

        switch(newState)
        {
            case GameManager.State.Ready:
                messageText.text = "声を出してスタート!!";
                break;

            case GameManager.State.Playing:
                messageText.text = ""; //プレイ中はメッセージを消す
                break;

            case GameManager.State.Goal:
                messageText.text = "GOAL!! クリアおめでとう!";
                break;

            default:
                messageText.text = "";
                break;

            //とりあえずスタート時とゴール時を書きました
            //ほかにもあれば追加をお願いします
            
        }
    }
}
