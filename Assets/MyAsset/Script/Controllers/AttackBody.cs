using Cysharp.Threading.Tasks;
using UnityEngine;
using System;

/// <summary>
/// 攻撃の本体の制御を行うクラス
/// </summary>
public class AttackBody : MonoBehaviour
{
    [SerializeField]
    private float _deadTime = 0.8f;

    private void Start()
    {
        DestroyMe().Forget();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Debug.Log("Enemyに当たった");
        }
    }

    private async UniTask DestroyMe()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(_deadTime));
        Destroy(gameObject);
    }
}
