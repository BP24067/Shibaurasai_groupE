using UnityEngine;
using TMPro;

public class MessageUI : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("GameManager参照")]
    [SerializeField] private GameManger gameManager;

    private void OnEnable()
    {
        //gamemanagerのイベントに状態が変わったら通知
        if(gameManeger != null)
        {
            gameManeger.OnStateChanged += OnStateChangedHandler;
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


    /// <summary>
    /// GameManegerの状態(State)が変わった時に自動的に呼び出されるメソッド
    /// </summary>
    /// <param name="newState">変更後の新しい状態</param>
    
    //↓状態が変わった時に実行する処理名
    private void OnStateChangedHandler(GameManager.State newState)
    {
        if(messagetext == null) return;

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
