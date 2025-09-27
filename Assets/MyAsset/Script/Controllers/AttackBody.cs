using Cysharp.Threading.Tasks;
using UnityEngine;
using System;
using System.Threading;

/// <summary>
/// 攻撃の本体の制御を行うクラス
/// </summary>
public class AttackBody : MonoBehaviour
{
    [SerializeField]
    private float _deadTime = 0.8f;

    CancellationTokenSource _cts;

    private void Start()
    {
        _cts = new CancellationTokenSource();
        DestroyMe().Forget();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Debug.Log("Enemy Hit");
            //ここに敵にダメージを与える処理を追加

            Destroy(other.gameObject);
            _cts.Cancel(); // 衝突したら自己破壊の遅延をキャンセルして即座に破壊
        }
    }

    private async UniTask DestroyMe()
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_deadTime), cancellationToken: _cts.Token);
        }
        catch (OperationCanceledException)
        {
            // キャンセルされた場合の処理（必要に応じて）
            return;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unexpected error: {ex}");
            return;
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            Destroy(gameObject);
        }
    }
}
