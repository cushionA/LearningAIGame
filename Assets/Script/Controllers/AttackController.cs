using Cysharp.Threading.Tasks;
using UnityEngine;
using System;
using UnityEngine.InputSystem;
using System.Threading;

/// <summary>
/// Playerの攻撃を制御するクラス
/// TODO:クールダウンの処理が明らかにリファクタリングできそうなので時間があればぜひ行ってください
/// </summary>
public class AttackController : MonoBehaviour
{
    [SerializeField]
    private float _attackCoolSecondTime = 1f;

    [SerializeField]
    private bool _isAttackAble = true;

    [SerializeField]
    private GameObject _attackBodyPrefab;

    [SerializeField] private InputAction _attackAction;

    private CancellationTokenSource _cts;

    private void OnEnable()
    {
        _attackAction.Enable();
    }

    private void OnDisable()
    {
        _attackAction.Disable();
    }

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
        if (_attackAction.WasPerformedThisFrame())
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
        try
        {
            _cts = new CancellationTokenSource();
            await UniTask.Delay(TimeSpan.FromSeconds(_attackCoolSecondTime), cancellationToken: _cts.Token);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Attack Cool Down Canceled");
        }
        catch (Exception e)
        {
            Debug.LogError($"Attack Cool Down Error: {e}");
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
        }

        _isAttackAble = true;
    }
}
