//using UnityEngine;

//namespace BattleSystem.Animation
//{
//    /// <summary>
//    /// StateSystemとAnimatorを同期するクラス
//    /// バトルシステム仕様書のAnimatorController設計に対応
//    /// </summary>
//    public class AnimationStateUpdater : MonoBehaviour
//    {
//        [Header("システム参照")]
//        [SerializeField] private Animator animator;
//        [SerializeField] private StateSystem stateSystem;

//        [Header("デバッグ設定")]
//        [SerializeField] private bool enableDebugLog = false;

//        // パフォーマンス最適化用
//        private int lastFrameUpdate = -1;

//        private void Update()
//        {
//            // 同一フレームでの重複更新を防ぐ
//            if (lastFrameUpdate == Time.frameCount) return;
//            lastFrameUpdate = Time.frameCount;

//            UpdateMovementParameters();
//            UpdateCombatParameters();
//            UpdateSpecialParameters();
//            UpdateEffectParameters();

//            if (enableDebugLog)
//            {
//                LogCurrentState();
//            }
//        }

//        /// <summary>
//        /// 移動関連パラメータの更新
//        /// </summary>
//        private void UpdateMovementParameters()
//        {
//            // 基本移動状態
//            animator.SetBool("isGrounded", stateSystem.IsGrounded);
//            animator.SetBool("isMoving", stateSystem.IsMoving);
//            animator.SetFloat("moveSpeed", stateSystem.CurrentSpeed);
//            animator.SetFloat("verticalVelocity", stateSystem.VerticalVelocity);

//            // 移動方向（ブレンドツリー用）
//            var moveVector = stateSystem.GetMovementVector();
//            animator.SetFloat("moveX", moveVector.x);
//            animator.SetFloat("moveY", moveVector.z); // 3D空間のZ軸をYとして使用

//            // 特殊移動
//            animator.SetBool("isBoosting", stateSystem.IsBoosting);
//            animator.SetBool("isJumping", stateSystem.CurrentActionState == ActionState.Jumping);
//            animator.SetBool("inAir", !stateSystem.IsGrounded);
//        }

//        /// <summary>
//        /// 戦闘関連パラメータの更新
//        /// </summary>
//        private void UpdateCombatParameters()
//        {
//            // 戦闘モード
//            animator.SetInteger("combatMode", (int)stateSystem.CurrentActionMode);

//            // 攻撃状態
//            animator.SetBool("isAttacking", stateSystem.CurrentActionState == ActionState.Attacking);
//            animator.SetInteger("attackDirection", (int)stateSystem.CurrentDirection);
//            animator.SetInteger("comboCount", stateSystem.GetComboCount());
//            animator.SetBool("airAttack", stateSystem.IsAirAttacking);

//            // 防御状態
//            animator.SetBool("isGuarding", stateSystem.IsGuarding);
//            animator.SetBool("isBlocking", stateSystem.IsBlocking);
//            animator.SetInteger("guardDirection", (int)stateSystem.GetGuardDirection());
//            animator.SetBool("blockSuccess", stateSystem.GetLastBlockResult());
//            animator.SetBool("guardBroken", stateSystem.IsGuardBroken);

//            // 射撃システム
//            if (stateSystem.CurrentActionMode == ActionMode.Ranged)
//            {
//                animator.SetBool("isAiming", stateSystem.IsAiming);
//                animator.SetBool("isReloading", stateSystem.IsReloading);
//                animator.SetFloat("aimAccuracy", stateSystem.GetAimAccuracy());
//                animator.SetFloat("weaponPower", stateSystem.GetWeaponPower());
//            }
//        }

//        /// <summary>
//        /// 特殊状態パラメータの更新
//        /// </summary>
//        private void UpdateSpecialParameters()
//        {
//            // 回避システム
//            animator.SetBool("isDodging", stateSystem.CurrentActionState == ActionState.Dodging);
//            animator.SetInteger("dodgeDirection", (int)stateSystem.GetLastDodgeDirection());

//            // スタン・無敵状態
//            animator.SetBool("isStunned", stateSystem.HealthData.isStunned);
//            animator.SetBool("isInvincible", stateSystem.HealthData.isInvincible);
//            animator.SetFloat("stunGauge", stateSystem.HealthData.stunGauge);

//            // エネルギー状態
//            animator.SetBool("energyDepleted", stateSystem.CurrentActionMode == ActionMode.EnergyBarrier);

//            // マニューバ
//            if (stateSystem.CurrentActionState == ActionState.UsingManeuver)
//            {
//                animator.SetInteger("maneuverType", stateSystem.GetCurrentManeuverType());
//            }
//        }

//        /// <summary>
//        /// エフェクト関連パラメータの更新
//        /// </summary>
//        private void UpdateEffectParameters()
//        {
//            // 被弾情報は一時的なのでStateSystemから取得
//            var hitInfo = stateSystem.GetLatestHitInfo();
//            if (hitInfo.HasValue)
//            {
//                animator.SetInteger("hitDirection", (int)hitInfo.Value.direction);
//                animator.SetBool("criticalHit", hitInfo.Value.isCritical);
//                animator.SetFloat("damageIntensity", hitInfo.Value.intensity);

//                // ヒットトリガーを発火
//                animator.SetTrigger("hitTrigger");

//                // 被弾情報をクリア
//                stateSystem.ClearHitInfo();
//            }
//        }

//        /// <summary>
//        /// 外部からトリガーを発火するためのメソッド
//        /// </summary>
//        public void TriggerAttack()
//        {
//            animator.SetTrigger("attackTrigger");
//        }

//        public void TriggerBlock()
//        {
//            animator.SetTrigger("blockTrigger");
//        }

//        public void TriggerDodge()
//        {
//            animator.SetTrigger("dodgeTrigger");
//        }

//        public void TriggerQuickTurn()
//        {
//            animator.SetTrigger("quickTurnTrigger");
//        }

//        public void TriggerManeuver()
//        {
//            animator.SetTrigger("maneuverTrigger");
//        }

//        /// <summary>
//        /// デバッグ用：現在の状態をログ出力
//        /// </summary>
//        private void LogCurrentState()
//        {
//            var sb = new System.Text.StringBuilder();
//            sb.AppendLine($"=== Animation State Debug ===");
//            sb.AppendLine($"Combat Mode: {stateSystem.CurrentActionMode}");
//            sb.AppendLine($"Action State: {stateSystem.CurrentActionState}");
//            sb.AppendLine($"Direction: {stateSystem.CurrentDirection}");
//            sb.AppendLine($"Is Grounded: {stateSystem.IsGrounded}");
//            sb.AppendLine($"Move Speed: {stateSystem.CurrentSpeed:F2}");
//            sb.AppendLine($"Energy Depleted: {stateSystem.CurrentActionMode == ActionMode.EnergyBarrier}");

//            Debug.Log(sb.ToString());
//        }

//        #if UNITY_EDITOR
//        /// <summary>
//        /// エディタ用：Inspector上でのテスト機能
//        /// </summary>
//        [Header("エディタテスト")]
//        [SerializeField] private bool testMode = false;

//        [ContextMenu("Test Attack Trigger")]
//        private void TestAttackTrigger()
//        {
//            if (Application.isPlaying)
//                TriggerAttack();
//        }

//        [ContextMenu("Test Block Trigger")]
//        private void TestBlockTrigger()
//        {
//            if (Application.isPlaying)
//                TriggerBlock();
//        }

//        [ContextMenu("Test Dodge Trigger")]
//        private void TestDodgeTrigger()
//        {
//            if (Application.isPlaying)
//                TriggerDodge();
//        }
//        #endif
//    }

//    /// <summary>
//    /// 被弾情報の構造体
//    /// </summary>
//    public struct HitInfo
//    {
//        public AttackDirection direction;
//        public bool isCritical;
//        public float intensity;

//        public HitInfo(AttackDirection dir, bool critical, float dmgIntensity)
//        {
//            direction = dir;
//            isCritical = critical;
//            intensity = dmgIntensity;
//        }
//    }
//}