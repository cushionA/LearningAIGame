using R3;
using System;
using System.Collections.Generic;
using UnityEngine;
using static LearningAIGame.CombatSystem.Core.StateSystem;

//==============================================ファイルヘッダ===========================================================
// AnimationController
// 
// 概要: StateSystemの状態をAnimatorに反映するコンポーネント
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// StateSystemが管理するReactivePropertyを購読し、Animatorパラメータに自動反映する。
// 基本的にはAnimatorのステートマシンで遷移を制御するが、
// 被弾・被防御系の状態は即座に割り込み再生する。
// 
// [強制割り込み対象]
// - 被弾系: 小怯み、大怯み、死亡
// - 被防御系: 弱攻撃ブロッキング、強攻撃ブロッキング、弱攻撃ガード
// 
// 入力元クラス: StateSystem
// 出力先クラス: Animator
//=====================================================================================================================
namespace LearningAIGame.CombatSystem.Core
{
    public class AnimationController : MonoBehaviour, IGameHelper
    {
        #region フィールド

        [SerializeField]
        private StateSystem _stateSystem;

        [SerializeField]
        private Animator _animator;

        private int _lastStateHash;

        private readonly HashSet<int> _neutralStateHashes = new HashSet<int>();

        /// <summary>
        /// 強制割り込みが必要な状態とアニメーション名のマッピング
        /// </summary>
        private static readonly Dictionary<ActionState, string> _forceInterruptAnimations = new Dictionary<ActionState, string>
        {
            // 被弾系
            { ActionState.小怯み, "SmallStagger" },
            { ActionState.大怯み, "LargeStagger" },
            { ActionState.死亡, "Death" },
            // 被防御系（自分の攻撃が防がれた）
            { ActionState.弱攻撃ブロッキング, "BlockedWeakAttack" },
            { ActionState.強攻撃ブロッキング, "BlockedHeavyAttack" },
            { ActionState.弱攻撃ガード, "GuardedWeakAttack" }
        };

        /// <summary>
        /// 強制割り込みが必要な状態（構え方向に依存するもの）
        /// アニメーション名は {ベース名}_{構え方向} の形式
        /// </summary>
        private static readonly Dictionary<ActionState, string> _forceInterruptAnimationsWithStance = new Dictionary<ActionState, string>
        {
            // 防御成功系（自分が防御に成功した）
            { ActionState.ガード成功, "GuardSuccess" },
            { ActionState.ブロッキング成功, "BlockingSuccess" }
        };

        /// <summary>
        /// アニメーションステートのハッシュ値から名前へのマッピング
        /// </summary>
        private readonly Dictionary<int, string> _stateHashToName = new Dictionary<int, string>
        {
            { Animator.StringToHash("BlockedWeakAttack"), "BlockedWeakAttack" },
            { Animator.StringToHash("BlockedHeavyAttack"), "BlockedHeavyAttack" },
            { Animator.StringToHash("BlockingSuccess_Left"), "BlockingSuccess_Left" },
            { Animator.StringToHash("BlockingSuccess_Right"), "BlockingSuccess_Right" },
            { Animator.StringToHash("BlockingSuccess_Up"), "BlockingSuccess_Up" },
            { Animator.StringToHash("Blocking_Left"), "Blocking_Left" },
            { Animator.StringToHash("Blocking_Right"), "Blocking_Right" },
            { Animator.StringToHash("Blocking_Up"), "Blocking_Up" },
            { Animator.StringToHash("Death"), "Death" },
            { Animator.StringToHash("FrontAvoid"), "FrontAvoid" },
            { Animator.StringToHash("BackAvoid"), "BackAvoid" },
            { Animator.StringToHash("GuardSuccess_Left"), "GuardSuccess_Left" },
            { Animator.StringToHash("GuardSuccess_Right"), "GuardSuccess_Right" },
            { Animator.StringToHash("GuardSuccess_Up"), "GuardSuccess_Up" },
            { Animator.StringToHash("Guard_Left"), "Guard_Left" },
            { Animator.StringToHash("Guard_Right"), "Guard_Right" },
            { Animator.StringToHash("Guard_Up"), "Guard_Up" },
            { Animator.StringToHash("GuardedWeakAttack"), "GuardedWeakAttack" },
            { Animator.StringToHash("HeavyAttackCancel_Left"), "HeavyAttackCancel_Left" },
            { Animator.StringToHash("HeavyAttackCancel_Right"), "HeavyAttackCancel_Right" },
            { Animator.StringToHash("HeavyAttackCancel_Up"), "HeavyAttackCancel_Up" },
            { Animator.StringToHash("HeavyAttack_Left"), "HeavyAttack_Left" },
            { Animator.StringToHash("HeavyAttack_Right"), "HeavyAttack_Right" },
            { Animator.StringToHash("HeavyAttack_Up"), "HeavyAttack_Up" },
            { Animator.StringToHash("LargeStagger"), "LargeStagger" },
            { Animator.StringToHash("SideAvoid"), "SideAvoid" },
            { Animator.StringToHash("SmallStagger"), "SmallStagger" },
            { Animator.StringToHash("WeakAttack_Left"), "WeakAttack_Left" },
            { Animator.StringToHash("WeakAttack_Right"), "WeakAttack_Right" },
            { Animator.StringToHash("WeakAttack_Up"), "WeakAttack_Up" }
        };

        #endregion

        #region Animatorパラメータ名

        private const string k_PARAM_ACTION_STATE = "ActionState";
        private const string k_PARAM_MOVE_X = "MoveX";
        private const string k_PARAM_MOVE_Z = "MoveZ";
        private const string k_PARAM_STANCE = "Stance";

        #endregion

        #region 初期化

        private void Start()
        {
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

            _neutralStateHashes.Add(Animator.StringToHash("Guard_Right"));
            _neutralStateHashes.Add(Animator.StringToHash("Guard_Left"));
            _neutralStateHashes.Add(Animator.StringToHash("Guard_Up"));

            SubscribeStateSystem();
        }

        #endregion

        #region 購読処理

        private void SubscribeStateSystem()
        {
            // CurrentStateの購読
            _stateSystem.CurrentState
                .Subscribe(OnStateChanged)
                .AddTo(this);

            // MoveVectorの購読
            _stateSystem.MoveVector
                .Subscribe(moveVector =>
                {
                    _animator.SetFloat(k_PARAM_MOVE_X, moveVector.x);
                    _animator.SetFloat(k_PARAM_MOVE_Z, moveVector.z);
                })
                .AddTo(this);

            // CurrentStanceの購読
            _stateSystem.CurrentStance
                .Subscribe(stance =>
                {
                    _animator.SetInteger(k_PARAM_STANCE, (int)stance);
                })
                .AddTo(this);
        }

        /// <summary>
        /// 行動状態変更時のハンドラ
        /// </summary>
        private void OnStateChanged(ActionState state)
        {
            // Animatorパラメータを更新（ステートマシン用）
            int stateValue = (int)Math.Log((int)state, 2);
            _animator.SetInteger(k_PARAM_ACTION_STATE, stateValue);

            // 強制割り込み対象かチェック（構え方向に依存しない）
            if (_forceInterruptAnimations.TryGetValue(state, out string animName))
            {
                _animator.Play(animName, 0, 0f);
                Debug.Log($"[{nameof(AnimationController)}] 強制割り込み: {animName}");
            }
            // 強制割り込み対象かチェック（構え方向に依存する）
            else if (_forceInterruptAnimationsWithStance.TryGetValue(state, out string baseAnimName))
            {
                string stanceSuffix = GetStanceSuffix(_stateSystem.CurrentStance.CurrentValue);
                string fullAnimName = $"{baseAnimName}_{stanceSuffix}";
                _animator.Play(fullAnimName, 0, 0f);
                Debug.Log($"[{nameof(AnimationController)}] 強制割り込み（構え依存）: {fullAnimName}");
            }
            // それ以外はステートマシンに任せる
        }

        /// <summary>
        /// StanceTypeから文字列サフィックスを取得
        /// </summary>
        private string GetStanceSuffix(StanceType stance)
        {
            return stance switch
            {
                StanceType.Up => "Up",
                StanceType.Left => "Left",
                StanceType.Right => "Right",
                _ => "Up"
            };
        }

        private void LateUpdate()
        {
            if (_animator == null)
                return;

            int currentHash = _animator.GetCurrentAnimatorStateInfo(0).shortNameHash;

            if (currentHash == _lastStateHash)
                return;

            // Neutralステートに遷移した場合
            if (_neutralStateHashes.Contains(currentHash) && !_neutralStateHashes.Contains(_lastStateHash))
            {
                var currentState = _stateSystem.CurrentState.CurrentValue;

                Debug.Log($"[{nameof(AnimationController)}] SetNeutral()呼び出し: {currentState} → ガード");
                _stateSystem.SetNeutral();

            }

            _lastStateHash = currentHash;
        }

        private bool IsNeutralState(int stateHash)
        {
            return _neutralStateHashes.Contains(stateHash) && !_neutralStateHashes.Contains(_lastStateHash);
        }

        #endregion

        #region ユーティリティ

        public string GetStateName(int hash)
        {
            return _stateHashToName.TryGetValue(hash, out string name) ? name : "Unknown";
        }

        #endregion

        #region IGameHelper実装

        public void Lock() { }
        public void Unlock() { }
        public void SetUp() { }

        public void RoundStart()
        {
            _animator.Play("Guard_Up", 0, 0f);
            _lastStateHash = Animator.StringToHash("Guard_Up");
        }

        public void RoundEnd() { }
        public void GameEnd() { }

        #endregion

        #region デバッグ用

#if UNITY_EDITOR
        [ContextMenu("現在のアニメーション状態を表示")]
        private void DebugPrintAnimationState()
        {
            if (_animator == null)
                return;

            var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"[{nameof(AnimationController)}] " +
                $"CurrentAnim: {GetStateName(stateInfo.shortNameHash)}, " +
                $"ActionState: {_animator.GetInteger(k_PARAM_ACTION_STATE)}, " +
                $"Stance: {_animator.GetInteger(k_PARAM_STANCE)}, " +
                $"StateSystem: {_stateSystem.CurrentState.CurrentValue}");
        }
#endif

        #endregion
    }
}