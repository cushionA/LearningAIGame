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
    public class AnimationController : MonoBehaviour, IGameHelper
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

        private int _lastStateHash;

        private HashSet<int> _neutralStateHashes = new HashSet<int>();


        /// <summary>
        /// アニメーションステートのハッシュ値から名前へのマッピング
        /// </summary>
        private Dictionary<int, string> _stateHashToName = new Dictionary<int, string>
{
    { Animator.StringToHash("BlockedWeakAttack"), "BlockedWeakAttack" },
    { Animator.StringToHash("BlockingSuccess_Left"), "BlockingSuccess_Left" },
    { Animator.StringToHash("BlockingSuccess_Right"), "BlockingSuccess_Right" },
    { Animator.StringToHash("BlockingSuccess_Up"), "BlockingSuccess_Up" },
    { Animator.StringToHash("Blocking_Left"), "Blocking_Left" },
    { Animator.StringToHash("Blocking_Right"), "Blocking_Right" },
    { Animator.StringToHash("Blocking_Up"), "Blocking_Up" },
    { Animator.StringToHash("Death"), "Death" },
    { Animator.StringToHash("FrontAvoid"), "FrontAvoid" },
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

        #region Animatorパラメータ名の定数定義

        // Animatorのパラメータ名
        private const string k_PARAM_ACTION_STATE = "ActionState";
        private const string k_PARAM_MOVE_X = "MoveX";
        private const string k_PARAM_MOVE_Z = "MoveZ";
        private const string k_PARAM_STANCE = "Stance";

        #endregion

        #region 初期化

        private void Start()
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

            _neutralStateHashes.Add(Animator.StringToHash("Guard_Right"));
            _neutralStateHashes.Add(Animator.StringToHash("Guard_Left"));
            _neutralStateHashes.Add(Animator.StringToHash("Guard_Up"));

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
                    // 即座にAnimatorを更新して遷移を反映
                    _animator.Update(0f);


                    //if (gameObject.name == "NPC")
                    //{
                    //    _actionList.Add($"State：{state}");
                    //    Debug.Log($"[AnimationController] 行動状態遷移: {string.Join(",", _actionList)}");
                    //}
                })
                .AddTo(this);

            // MoveVectorの購読
            _stateSystem.MoveVector
                .Subscribe(moveVector =>
                {
                    // 移動ベクトルのX,Z成分をAnimatorに設定
                    _animator.SetFloat(k_PARAM_MOVE_X, moveVector.x);
                    _animator.SetFloat(k_PARAM_MOVE_Z, moveVector.z);
                    // 即座にAnimatorを更新して遷移を反映
                    _animator.Update(0f);
                })
                .AddTo(this);

            // CurrentStanceの購読
            _stateSystem.CurrentStance
                .Subscribe(stance =>
                {
                    // StanceTypeをint値としてAnimatorに設定
                    _animator.SetInteger(k_PARAM_STANCE, (int)stance);
                    // 即座にAnimatorを更新して遷移を反映
                    _animator.Update(0f);
                })
                .AddTo(this);
        }

        List<String> _actionList = new List<string>();
        List<String> _stateList = new List<string>();

        private void LateUpdate()
        {
            if (_animator == null)
                return;

            int currentHash = _animator.GetCurrentAnimatorStateInfo(0).shortNameHash;

            if (currentHash == _lastStateHash)
                return;

            if (gameObject.name == "Player")
            {
                _stateList.Add($"State：{GetStateName(currentHash)} + 状態：{_stateSystem.CurrentState.CurrentValue}");
                Debug.Log($"[AnimationController] アニメーションステート遷移: {string.Join(",", _stateList)}");
            }

            // ステートが変わった && Neutralに戻った
            if (IsNeutralState(currentHash))
            {
                Debug.Log($"[AnimationController] Neutralステートに遷移 {_stateSystem.CurrentState.CurrentValue}");
                _stateSystem.SetNeutral();
            }

            _lastStateHash = currentHash;
        }

        private bool IsNeutralState(int stateHash)
        {
            Debug.Log($"[AnimationController] 状態確認: current={GetStateName(stateHash)}, last={GetStateName(_lastStateHash)}");
            // Neutralステートのハッシュ値を取得
            return _neutralStateHashes.Contains(stateHash) && !_neutralStateHashes.Contains(_lastStateHash);
        }


        #endregion

        #region ユーティリティ

        /// <summary>
        /// アニメーションステートのハッシュ値から名前を取得
        /// </summary>
        /// <param name="hash">アニメーションステートのハッシュ値</param>
        /// <returns>ステート名。見つからない場合は"Unknown"</returns>
        public string GetStateName(int hash)
        {
            return _stateHashToName.TryGetValue(hash, out string name) ? name : "Unknown";
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

        public void Lock()
        {
        }

        public void Unlock()
        {

        }

        public void SetUp()
        {

        }

        public void RoundStart()
        {
            // アニメをニュートラルに戻す。
            _animator.Play("Guard_Up");
        }

        public void RoundEnd()
        {

        }

        public void GameEnd()
        {

        }
#endif

        #endregion
    }
}