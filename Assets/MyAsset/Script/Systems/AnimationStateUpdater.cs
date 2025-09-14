using LearningAIGame.CombatSystem;
using UnityEngine;

namespace BattleSystem.Animation
{
    /// <summary>
    /// 被弾情報の構造体
    /// </summary>
    [System.Serializable]
    public struct HitInfo
    {
        public AttackDirection direction;
        public bool isCritical;
        public float intensity;

        public HitInfo(AttackDirection dir, bool critical, float dmgIntensity)
        {
            direction = dir;
            isCritical = critical;
            intensity = dmgIntensity;
        }
    }

    /// <summary>
    /// StateSystemとAnimatorを同期するクラス
    /// バトルシステム仕様書のAnimatorController設計に対応
    /// </summary>
    public class AnimationStateUpdater : MonoBehaviour
    {
        [Header("システム参照")]
        [SerializeField] private Animator animator;
        [SerializeField] private StateSystem stateSystem;

        [Header("デバッグ設定")]
        [SerializeField] private bool enableDebugLog = false;

        // パフォーマンス最適化用
        private int lastFrameUpdate = -1;

        private void Update()
        {
            // 同一フレームでの重複更新を防ぐ
            if ( lastFrameUpdate == Time.frameCount )
                return;
            lastFrameUpdate = Time.frameCount;

            // StateSystemの参照チェック
            if ( stateSystem == null || animator == null )
            {
                if ( enableDebugLog )
                    Debug.LogWarning($"{gameObject.name}: StateSystemまたはAnimatorが設定されていません");
                return;
            }

            UpdateMovementParameters();
            UpdateCombatParameters();
            UpdateSpecialParameters();
            UpdateEffectParameters();

            if ( enableDebugLog )
            {
                LogCurrentState();
            }
        }

        /// <summary>
        /// 移動関連パラメータの更新
        /// </summary>
        private void UpdateMovementParameters()
        {
            // 基本移動状態
            animator.SetBool("isGrounded", stateSystem.IsGrounded);
            animator.SetBool("isMoving", stateSystem.IsMoving);
            animator.SetFloat("moveSpeed", stateSystem.CurrentSpeed);
            animator.SetFloat("verticalVelocity", stateSystem.VerticalVelocity);

            // 移動方向（ブレンドツリー用）
            var moveVector = stateSystem.GetMovementVector();
            animator.SetFloat("moveX", moveVector.x);
            animator.SetFloat("moveY", moveVector.z); // 3D空間のZ軸をYとして使用

            // 特殊移動
            animator.SetBool("isBoosting", stateSystem.IsBoosting);
            animator.SetBool("isJumping", stateSystem.CurrentActionState == ActionState.Jumping);
            animator.SetBool("inAir", !stateSystem.IsGrounded);
        }

        /// <summary>
        /// 戦闘関連パラメータの更新
        /// </summary>
        private void UpdateCombatParameters()
        {
            // 戦闘モード
            animator.SetInteger("combatMode", (int)stateSystem.CurrentActionMode);

            // 攻撃状態
            animator.SetBool("isAttacking", stateSystem.CurrentActionState == ActionState.Attacking);
            animator.SetInteger("attackDirection", (int)stateSystem.CurrentDirection);
            animator.SetInteger("comboCount", stateSystem.GetComboCount());
            animator.SetBool("airAttack", stateSystem.IsAirAttacking);

            // 防御状態
            animator.SetBool("isGuarding", stateSystem.IsGuarding);
            animator.SetBool("isBlocking", stateSystem.IsBlocking);
            animator.SetInteger("guardDirection", (int)stateSystem.GetGuardDirection());
            animator.SetBool("blockSuccess", stateSystem.GetLastBlockResult());
            animator.SetBool("guardBroken", stateSystem.IsGuardBroken);

            // 射撃システム
            if ( stateSystem.CurrentActionMode == ActionMode.Ranged )
            {
                animator.SetBool("isAiming", stateSystem.IsAiming);
                animator.SetBool("isReloading", stateSystem.AnalysisData.isReloading);
                animator.SetFloat("aimAccuracy", stateSystem.GetAimAccuracy());
                animator.SetFloat("weaponPower", stateSystem.GetWeaponPower());
            }
            else
            {
                // 射撃モードでない場合は射撃パラメータをリセット
                animator.SetBool("isAiming", false);
                animator.SetBool("isReloading", false);
                animator.SetFloat("aimAccuracy", 0f);
                animator.SetFloat("weaponPower", 1f);
            }
        }

        /// <summary>
        /// 特殊状態パラメータの更新
        /// </summary>
        private void UpdateSpecialParameters()
        {
            // 回避システム
            animator.SetBool("isDodging", stateSystem.CurrentActionState == ActionState.Dodging);
            animator.SetBool("canDodge", stateSystem.CanDodge);

            // スタン・無敵状態
            animator.SetBool("isStunned", stateSystem.HealthData.isStunned);
            animator.SetBool("isInvincible", stateSystem.HealthData.isInvincible);
            animator.SetFloat("stunGauge", stateSystem.HealthData.stunGauge);

            // エネルギー状態
            animator.SetBool("energyDepleted", stateSystem.IsEnergyDepleted);
            animator.SetFloat("energyPercentage", stateSystem.EnergyPercentage);
            animator.SetBool("energyRecoveryPaused", stateSystem.IsEnergyRecoveryPaused);

            // マニューバ・スキル使用可否
            animator.SetBool("canUseSkills", stateSystem.AnalysisData.canUseSkills);
            animator.SetBool("canUseManeuvers", stateSystem.AnalysisData.canUseManeuvers);

            // アクション状態の詳細設定
            animator.SetBool("isUsingManeuver", stateSystem.CurrentActionState == ActionState.UsingManeuver);
            animator.SetBool("isEnergyShielding", stateSystem.CurrentActionState == ActionState.EnergyShielding);
        }

        /// <summary>
        /// エフェクト関連パラメータの更新
        /// </summary>
        private void UpdateEffectParameters()
        {
            // 被弾情報は一時的なのでStateSystemから取得
            var hitInfo = stateSystem.GetLatestHitInfo();
            if ( hitInfo.HasValue )
            {
                animator.SetInteger("hitDirection", (int)hitInfo.Value.direction);
                animator.SetBool("criticalHit", hitInfo.Value.isCritical);
                animator.SetFloat("damageIntensity", hitInfo.Value.intensity);

                // ヒットトリガーを発火
                animator.SetTrigger("hitTrigger");

                // 被弾情報をクリア
                stateSystem.ClearHitInfo();

                if ( enableDebugLog )
                {
                    Debug.Log($"{gameObject.name}: 被弾エフェクト再生 - 方向:{hitInfo.Value.direction}, クリティカル:{hitInfo.Value.isCritical}");
                }
            }

            // クールダウン情報の設定
            UpdateCooldownParameters();
        }

        /// <summary>
        /// クールダウン関連パラメータの更新
        /// </summary>
        private void UpdateCooldownParameters()
        {
            // スキルクールダウンの設定（最大4つまで対応）
            for ( int i = 0; i < Mathf.Min(4, stateSystem.AnalysisData.skillCooldowns.Length); i++ )
            {
                animator.SetFloat($"skillCooldown{i}", stateSystem.AnalysisData.skillCooldowns[i]);
            }

            // マニューバクールダウンの設定（最大3つまで対応）
            for ( int i = 0; i < Mathf.Min(3, stateSystem.AnalysisData.maneuverCooldowns.Length); i++ )
            {
                animator.SetFloat($"maneuverCooldown{i}", stateSystem.AnalysisData.maneuverCooldowns[i]);
            }

            // 回避関連のタイミング情報
            animator.SetFloat("nextDodgeTime", stateSystem.NextDodgeTime);
            animator.SetFloat("dodgeIntervalRemaining", Mathf.Max(0f, stateSystem.NextDodgeTime - Time.time));
        }

        /// <summary>
        /// 外部からトリガーを発火するためのメソッド
        /// </summary>
        public void TriggerAttack()
        {
            animator.SetTrigger("attackTrigger");

            if ( enableDebugLog )
                Debug.Log($"{gameObject.name}: 攻撃トリガー発火");
        }

        public void TriggerBlock()
        {
            animator.SetTrigger("blockTrigger");

            if ( enableDebugLog )
                Debug.Log($"{gameObject.name}: ブロックトリガー発火");
        }

        public void TriggerDodge()
        {
            animator.SetTrigger("dodgeTrigger");

            if ( enableDebugLog )
                Debug.Log($"{gameObject.name}: 回避トリガー発火");
        }

        public void TriggerQuickTurn()
        {
            animator.SetTrigger("quickTurnTrigger");

            if ( enableDebugLog )
                Debug.Log($"{gameObject.name}: クイックターントリガー発火");
        }

        public void TriggerManeuver()
        {
            animator.SetTrigger("maneuverTrigger");

            if ( enableDebugLog )
                Debug.Log($"{gameObject.name}: マニューバトリガー発火");
        }

        public void TriggerModeSwitch()
        {
            animator.SetTrigger("modeSwitchTrigger");

            if ( enableDebugLog )
                Debug.Log($"{gameObject.name}: モード切替トリガー発火");
        }

        public void TriggerSkill(int skillIndex)
        {
            animator.SetTrigger($"skill{skillIndex}Trigger");
            animator.SetInteger("lastUsedSkill", skillIndex);

            if ( enableDebugLog )
                Debug.Log($"{gameObject.name}: スキル{skillIndex}トリガー発火");
        }

        /// <summary>
        /// アニメーションイベントから呼び出されるコールバック（アニメーションからの通知用）
        /// </summary>
        public void OnAnimationAttackStart()
        {
            // アニメーション側から攻撃開始の通知を受けた場合の処理
            if ( enableDebugLog )
                Debug.Log($"{gameObject.name}: アニメーション攻撃開始イベント受信");
        }

        public void OnAnimationAttackEnd()
        {
            // アニメーション側から攻撃終了の通知を受けた場合の処理
            if ( enableDebugLog )
                Debug.Log($"{gameObject.name}: アニメーション攻撃終了イベント受信");
        }

        public void OnAnimationDodgeStart()
        {
            // アニメーション側から回避開始の通知を受けた場合の処理
            if ( enableDebugLog )
                Debug.Log($"{gameObject.name}: アニメーション回避開始イベント受信");
        }

        public void OnAnimationDodgeEnd()
        {
            // アニメーション側から回避終了の通知を受けた場合の処理
            if ( enableDebugLog )
                Debug.Log($"{gameObject.name}: アニメーション回避終了イベント受信");
        }

        /// <summary>
        /// 特定のパラメータを強制設定する（デバッグ・特殊状況用）
        /// </summary>
        public void ForceSetParameter(string parameterName, bool value)
        {
            animator.SetBool(parameterName, value);

            if ( enableDebugLog )
                Debug.Log($"{gameObject.name}: パラメータ強制設定 {parameterName} = {value}");
        }

        public void ForceSetParameter(string parameterName, float value)
        {
            animator.SetFloat(parameterName, value);

            if ( enableDebugLog )
                Debug.Log($"{gameObject.name}: パラメータ強制設定 {parameterName} = {value:F2}");
        }

        public void ForceSetParameter(string parameterName, int value)
        {
            animator.SetInteger(parameterName, value);

            if ( enableDebugLog )
                Debug.Log($"{gameObject.name}: パラメータ強制設定 {parameterName} = {value}");
        }

        /// <summary>
        /// 緊急時のアニメーション状態リセット
        /// </summary>
        public void ResetAnimationState()
        {
            // 全てのトリガーをリセット
            animator.ResetTrigger("attackTrigger");
            animator.ResetTrigger("blockTrigger");
            animator.ResetTrigger("dodgeTrigger");
            animator.ResetTrigger("quickTurnTrigger");
            animator.ResetTrigger("maneuverTrigger");
            animator.ResetTrigger("modeSwitchTrigger");
            animator.ResetTrigger("hitTrigger");

            // スキルトリガーのリセット
            for ( int i = 0; i < 4; i++ )
            {
                animator.ResetTrigger($"skill{i}Trigger");
            }

            // 基本パラメータのリセット
            animator.SetBool("isAttacking", false);
            animator.SetBool("isGuarding", false);
            animator.SetBool("isBlocking", false);
            animator.SetBool("isDodging", false);
            animator.SetBool("isUsingManeuver", false);

            if ( enableDebugLog )
                Debug.Log($"{gameObject.name}: アニメーション状態をリセットしました");
        }

        /// <summary>
        /// アニメーターパラメータの存在確認（安全な設定のため）
        /// </summary>
        private bool HasParameter(string parameterName)
        {
            foreach ( AnimatorControllerParameter parameter in animator.parameters )
            {
                if ( parameter.name == parameterName )
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 安全なパラメータ設定メソッド
        /// </summary>
        private void SafeSetBool(string parameterName, bool value)
        {
            if ( HasParameter(parameterName) )
            {
                animator.SetBool(parameterName, value);
            }
            else if ( enableDebugLog )
            {
                Debug.LogWarning($"{gameObject.name}: アニメーターパラメータ '{parameterName}' が見つかりません");
            }
        }

        private void SafeSetFloat(string parameterName, float value)
        {
            if ( HasParameter(parameterName) )
            {
                animator.SetFloat(parameterName, value);
            }
            else if ( enableDebugLog )
            {
                Debug.LogWarning($"{gameObject.name}: アニメーターパラメータ '{parameterName}' が見つかりません");
            }
        }

        private void SafeSetInteger(string parameterName, int value)
        {
            if ( HasParameter(parameterName) )
            {
                animator.SetInteger(parameterName, value);
            }
            else if ( enableDebugLog )
            {
                Debug.LogWarning($"{gameObject.name}: アニメーターパラメータ '{parameterName}' が見つかりません");
            }
        }

        /// <summary>
        /// デバッグ用：現在の状態をログ出力
        /// </summary>
        private void LogCurrentState()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== Animation State Debug ({gameObject.name}) ===");
            sb.AppendLine($"Combat Mode: {stateSystem.CurrentActionMode}");
            sb.AppendLine($"Action State: {stateSystem.CurrentActionState}");
            sb.AppendLine($"Direction: {stateSystem.CurrentDirection}");
            sb.AppendLine($"Is Grounded: {stateSystem.IsGrounded}");
            sb.AppendLine($"Move Speed: {stateSystem.CurrentSpeed:F2}");
            sb.AppendLine($"Is Moving: {stateSystem.IsMoving}");
            sb.AppendLine($"Is Boosting: {stateSystem.IsBoosting}");
            sb.AppendLine($"Is Guarding: {stateSystem.IsGuarding}");
            sb.AppendLine($"Is Blocking: {stateSystem.IsBlocking}");
            sb.AppendLine($"Is Aiming: {stateSystem.IsAiming}");
            sb.AppendLine($"Energy Depleted: {stateSystem.IsEnergyDepleted}");
            sb.AppendLine($"Can Dodge: {stateSystem.CanDodge}");
            sb.AppendLine($"Combo Count: {stateSystem.GetComboCount()}");

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// AnimatorのCurrentStateInfo取得（デバッグ・分析用）
        /// </summary>
        public AnimatorStateInfo GetCurrentStateInfo(int layerIndex = 0)
        {
            return animator.GetCurrentAnimatorStateInfo(layerIndex);
        }

        /// <summary>
        /// 特定のステートが再生中かどうかを確認
        /// </summary>
        public bool IsPlayingState(string stateName, int layerIndex = 0)
        {
            var stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
            return stateInfo.IsName(stateName);
        }

        /// <summary>
        /// アニメーション長の取得
        /// </summary>
        public float GetCurrentStateLength(int layerIndex = 0)
        {
            var stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
            return stateInfo.length;
        }

        /// <summary>
        /// アニメーション正規化時間の取得
        /// </summary>
        public float GetCurrentStateNormalizedTime(int layerIndex = 0)
        {
            var stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
            return stateInfo.normalizedTime;
        }

#if UNITY_EDITOR
        /// <summary>
        /// エディタ用：Inspector上でのテスト機能
        /// </summary>
        [Header("エディタテスト")]
        [SerializeField] private bool testMode = false;

        [ContextMenu("Test Attack Trigger")]
        private void TestAttackTrigger()
        {
            if ( Application.isPlaying )
                TriggerAttack();
        }

        [ContextMenu("Test Block Trigger")]
        private void TestBlockTrigger()
        {
            if ( Application.isPlaying )
                TriggerBlock();
        }

        [ContextMenu("Test Dodge Trigger")]
        private void TestDodgeTrigger()
        {
            if ( Application.isPlaying )
                TriggerDodge();
        }

        [ContextMenu("Test Mode Switch Trigger")]
        private void TestModeSwitchTrigger()
        {
            if ( Application.isPlaying )
                TriggerModeSwitch();
        }

        [ContextMenu("Reset Animation State")]
        private void TestResetAnimationState()
        {
            if ( Application.isPlaying )
                ResetAnimationState();
        }

        [ContextMenu("Log Current Animator State")]
        private void TestLogAnimatorState()
        {
            if ( Application.isPlaying )
            {
                var stateInfo = GetCurrentStateInfo();
                Debug.Log($"Current State: {stateInfo.shortNameHash}, Normalized Time: {stateInfo.normalizedTime:F2}");
            }
        }

        /// <summary>
        /// エディタ用：アニメーターパラメータ一覧の出力
        /// </summary>
        [ContextMenu("List All Animator Parameters")]
        private void ListAnimatorParameters()
        {
            if ( animator == null )
            {
                Debug.LogWarning("Animatorが設定されていません");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Animatorパラメータ一覧 ===");

            foreach ( AnimatorControllerParameter parameter in animator.parameters )
            {
                sb.AppendLine($"名前: {parameter.name}, タイプ: {parameter.type}, デフォルト値: {GetParameterDefaultValue(parameter)}");
            }

            Debug.Log(sb.ToString());
        }

        private string GetParameterDefaultValue(AnimatorControllerParameter parameter)
        {
            return parameter.type switch
            {
                AnimatorControllerParameterType.Bool => parameter.defaultBool.ToString(),
                AnimatorControllerParameterType.Float => parameter.defaultFloat.ToString("F2"),
                AnimatorControllerParameterType.Int => parameter.defaultInt.ToString(),
                AnimatorControllerParameterType.Trigger => "Trigger",
                _ => "Unknown"
            };
        }
#endif
    }
}