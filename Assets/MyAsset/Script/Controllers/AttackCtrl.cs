using Cysharp.Threading.Tasks;
using UnityEngine;
using System;

/// <summary>
/// Playerの攻撃を制御するクラス
/// TODO:クールダウンの処理が明らかにリファクタリングできそうなので時間があればぜひ行ってください
/// </summary>
public class AttackCtrl : MonoBehaviour
{
    [SerializeField]
    private float _attackCoolSecondTime = 1f;

    [SerializeField]
    private bool _isAttackAble = true;

    [SerializeField]
    private GameObject _attackBodyPrefab;

    private void Start()
    {
        _isAttackAble = true;
    }

    private void Update()
    {
        //攻撃できないならしない
        if (!_isAttackAble)
            return;

        //攻撃のキー受付
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Attack();
        }
    }

    private void Attack()
    {
        Debug.Log("Attack");
        Instantiate(_attackBodyPrefab, transform.position + transform.forward, transform.rotation);
        AttackCoolDown().Forget();
    }

    private async UniTask AttackCoolDown()
    {
        _isAttackAble = false;

        // 攻撃のクールダウン処理
        await UniTask.Delay(TimeSpan.FromSeconds(_attackCoolSecondTime));

        _isAttackAble = true;
    }
}
