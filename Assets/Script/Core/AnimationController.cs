using UnityEngine;
using R3;
using System;

//==============================================ファイルヘッダ===========================================================
// AnimationController
// 
// 概要: StateSystemの状態をAnimatorに反映するコンポーネント
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// StateSystemが管理する以下のReactivePropertyを購読し、Animatorパラメータに自動反映する：
// 
// [購読対象のReactiveProperty]
// - CurrentState (ActionState): 現在の行動状態 → log₂でビット位置に変換してAnimatorに設定
// - MoveVector (Vector3): 移動方向ベクトル → MoveX, MoveZパラメータに設定
// - CurrentStance (StanceType): 構え方向 → Stanceパラメータに設定。上、左、右の三種類
// 
// [Animatorパラメータ]
// - ActionState (Int): 行動状態のビット位置（0-17）
// - MoveX (Float): X方向の移動量
// - MoveZ (Float): Z方向の移動量
// - Stance (Int): 構え方向（0=Up, 1=Left, 2=Right）
// 
// [動作]
// StateSystemの状態変化を検知するとAnimatorパラメータを更新。
// ビットフラグ形式のActionStateはlog₂計算でビット位置（0-17）に変換される。
// 例: ActionState.ガード (1<<0=1) → log₂(1) = 0
//     ActionState.弱攻撃 (1<<7=128) → log₂(128) = 7
// 
// 入力元クラス: StateSystem
// 出力先クラス: Animator
// 
// その他:
// - R3のReactivePropertyを使用したリアクティブな状態同期
// - AddTo(this)によりGameObject破棄時に自動的に購読解除
//=====================================================================================================================
namespace LearningAIGame.CombatSystem.Core
{
    public class AnimationController : MonoBehaviour
    {
        #region フィールド

        /// <summary>
        /// 状態管理システムへの参照を持つ
        /// </summary>
        [SerializeField]
        private StateSystem _stateSystem;

        /// <summary>
        /// アニメーター
        /// </summary>
        [SerializeField]
        private Animator _animator;

        #endregion

        #region Animatorパラメータ名の定数定義

        // Animatorのパラメータ名
        private const string k_PARAM_ACTION_STATE = "ActionState";
        private const string k_PARAM_MOVE_X = "MoveX";
        private const string k_PARAM_MOVE_Z = "MoveZ";
        private const string k_PARAM_STANCE = "Stance";

        #endregion

        #region 初期化

        private void Awake()
        {
            // nullチェック
            if (_stateSystem == null)
            {
                Debug.LogError($"[{nameof(AnimationController)}] StateSystemが設定されていません！");
                return;
            }

            if (_animator == null)
            {
                Debug.LogError($"[{nameof(AnimationController)}] Animatorが設定されていません！");
                return;
            }

            // ReactivePropertyの購読設定
            SubscribeStateSystem();
        }

        #endregion

        #region 購読処理

        /// <summary>
        /// StateSystemのReactivePropertyを購読してAnimatorに反映
        /// </summary>
        private void SubscribeStateSystem()
        {
            // CurrentStateの購読
            _stateSystem.CurrentState
                .Subscribe(state =>
                {
                    // ActionStateをint値としてAnimatorに設定
                    _animator.SetInteger(k_PARAM_ACTION_STATE, (int)Math.Log((int)state, 2));
                })
                .AddTo(this);

            // MoveVectorの購読
            _stateSystem.MoveVector
                .Subscribe(moveVector =>
                {
                    // 移動ベクトルのX,Z成分をAnimatorに設定
                    _animator.SetFloat(k_PARAM_MOVE_X, moveVector.x);
                    _animator.SetFloat(k_PARAM_MOVE_Z, moveVector.z);
                })
                .AddTo(this);

            // CurrentStanceの購読
            _stateSystem.CurrentStance
                .Subscribe(stance =>
                {
                    // StanceTypeをint値としてAnimatorに設定
                    _animator.SetInteger(k_PARAM_STANCE, (int)stance);
                })
                .AddTo(this);
        }

        #endregion

        #region デバッグ用

#if UNITY_EDITOR
        [ContextMenu("現在のアニメーション状態を表示")]
        private void DebugPrintAnimationState()
        {
            if (_animator == null)
                return;

            Debug.Log($"[{nameof(AnimationController)}] " +
                $"ActionState: {_animator.GetInteger(k_PARAM_ACTION_STATE)}, " +
                $"MoveX: {_animator.GetFloat(k_PARAM_MOVE_X)}, " +
                $"MoveZ: {_animator.GetFloat(k_PARAM_MOVE_Z)}, " +
                $"Stance: {_animator.GetInteger(k_PARAM_STANCE)}");
        }
#endif

        #endregion
    }
}