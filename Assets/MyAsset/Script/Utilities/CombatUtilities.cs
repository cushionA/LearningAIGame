//using System.Runtime.CompilerServices;
//using UnityEngine;

//namespace LearningAIGame.CombatSystem
//{
//    /// <summary>
//    /// ダメージ計算と適用を行う静的ユーティリティクラス
//    /// </summary>
//    public static class CombatUtilities
//    {
//        /// <summary>
//        /// 攻撃のヒット判定とダメージ計算を行う
//        /// </summary>
//        /// <param name="attackInfo">攻撃情報</param>
//        /// <param name="defender">防御側キャラクター</param>
//        /// <returns>ダメージ結果</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static DamageResult CalculateHit(AttackInfo attackInfo, BattleCharacterController defender)
//        {
//            if ( defender == null )
//                return DamageResult.CreateMiss();

//            var stateSystem = defender.GetComponent<StateSystem>();
//            if ( stateSystem == null )
//                return DamageResult.CreateMiss();

//            // 無敵状態チェック
//            if ( stateSystem.HealthData.isInvincible )
//                return DamageResult.CreateMiss();

//            // 防御システムがある場合は防御判定を委譲
//            var defenseSystem = defender.GetComponent<DefenseSystem>();
//            if ( defenseSystem != null )
//            {
//                return defenseSystem.ProcessDefense(attackInfo);
//            }

//            // 防御システムがない場合は直接ダメージ計算
//            var result = CalculateDirectDamage(attackInfo, defender);

//            // 空中攻撃の威力倍率を適用
//            if ( attackInfo.isAerialAttack )
//            {
//                result.actualDamage *= attackInfo.isAerialAttack ? 2f : 1f;
//                result.wasAerialAttack = true;
//            }

//            // カウンター攻撃の判定
//            if ( IsCounterAttack(attackInfo, defender) )
//            {
//                result.actualDamage *= 1.5f;
//                result.wasCounterAttack = true;
//            }

//            // クリティカルヒットの判定
//            if ( IsCriticalHit(attackInfo, defender) )
//            {
//                result.actualDamage *= 1.3f;
//                result.wasCriticalHit = true;
//            }

//            return result;
//        }

//        /// <summary>
//        /// 直接ダメージを計算
//        /// </summary>
//        /// <param name="attackInfo">攻撃情報</param>
//        /// <param name="defender">防御側</param>
//        /// <returns>ダメージ結果</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static DamageResult CalculateDirectDamage(AttackInfo attackInfo, BattleCharacterController defender)
//        {
//            return new DamageResult
//            {
//                actualDamage = attackInfo.baseDamage,
//                stunAccumulation = attackInfo.stunAccumulation,
//                energyDamage = attackInfo.energyDamage,
//                wasHit = true,
//                wasGuarded = false,
//                wasBlocked = false,
//                wasJustDodged = false,
//                causedStun = false,
//                brokeCombo = false,
//                hitPosition = defender.Position, // ← キャッシュ使用
//                hitDirection = -(defender.Position - attackInfo.attackerPosition).normalized, // ← ノックバック用に攻撃方向を逆転
//                wasAerialAttack = attackInfo.isAerialAttack,
//                wasCounterAttack = false,
//                wasCriticalHit = false
//            };
//        }

//        /// <summary>
//        /// カウンター攻撃かどうかを判定
//        /// </summary>
//        /// <param name="attackInfo">攻撃情報</param>
//        /// <param name="defender">防御側</param>
//        /// <returns>カウンター攻撃かどうか</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static bool IsCounterAttack(AttackInfo attackInfo, BattleCharacterController defender)
//        {
//            var defenderStateSystem = defender.GetComponent<StateSystem>();
//            if ( defenderStateSystem == null )
//                return false;

//            // 相手が攻撃中、またはコンボ中の場合はカウンター判定
//            return defenderStateSystem.CurrentActionState == ActionState.Attacking ||
//                   defenderStateSystem.AnalysisData.isInCombo;
//        }

//        /// <summary>
//        /// クリティカルヒットかどうかを判定
//        /// </summary>
//        /// <param name="attackInfo">攻撃情報</param>
//        /// <param name="defender">防御側</param>
//        /// <returns>クリティカルヒットかどうか</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static bool IsCriticalHit(AttackInfo attackInfo, BattleCharacterController defender)
//        {
//            // 回避攻撃や空中攻撃はクリティカル率が高い
//            float criticalRate = 0.05f; // 基本5%

//            if ( attackInfo.isDodgeAttack )
//                criticalRate += 0.1f; // 回避攻撃は+10%

//            if ( attackInfo.isAerialAttack )
//                criticalRate += 0.05f; // 空中攻撃は+5%

//            if ( attackInfo.isCounterAttack )
//                criticalRate += 0.15f; // カウンター攻撃は+15%

//            return Random.value < criticalRate;
//        }

//        /// <summary>
//        /// ダメージ結果を適用する
//        /// </summary>
//        /// <param name="target">対象キャラクター</param>
//        /// <param name="result">ダメージ結果</param>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static void ApplyDamage(BattleCharacterController target, DamageResult result)
//        {
//            if ( target == null || result == null )
//                return;

//            target.ReceiveAttack(result);
//        }

//        /// <summary>
//        /// ジャスト回避の判定
//        /// </summary>
//        /// <param name="dodgeStartTime">回避開始時間</param>
//        /// <param name="attackHitTime">攻撃ヒット時間</param>
//        /// <param name="justWindow">ジャスト判定ウィンドウ</param>
//        /// <returns>ジャスト回避かどうか</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static bool IsJustDodge(float dodgeStartTime, float attackHitTime, float justWindow = 0.15f)
//        {
//            float timeDifference = Mathf.Abs(attackHitTime - dodgeStartTime);
//            return timeDifference <= justWindow;
//        }

//        /// <summary>
//        /// カウンター攻撃のダメージ倍率を計算
//        /// </summary>
//        /// <param name="attackInfo">攻撃情報</param>
//        /// <param name="isCounter">カウンター攻撃かどうか</param>
//        /// <returns>倍率適用後のダメージ</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static float CalculateCounterDamage(AttackInfo attackInfo, bool isCounter)
//        {
//            return isCounter ? attackInfo.baseDamage * 1.5f : attackInfo.baseDamage;
//        }

//        /// <summary>
//        /// 距離によるダメージ減衰を計算
//        /// </summary>
//        /// <param name="baseDamage">基本ダメージ</param>
//        /// <param name="distance">距離</param>
//        /// <param name="maxRange">最大射程</param>
//        /// <returns>減衰後のダメージ</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static float CalculateDistanceDamage(float baseDamage, float distance, float maxRange)
//        {
//            if ( distance >= maxRange )
//                return 0f;

//            float falloffRatio = 1f - (distance / maxRange);
//            return baseDamage * Mathf.Max(0.3f, falloffRatio); // 最低30%のダメージは保証
//        }

//        /// <summary>
//        /// 踏み込み距離を計算
//        /// </summary>
//        /// <param name="attackInfo">攻撃情報</param>
//        /// <param name="characterSettings">キャラクター設定</param>
//        /// <param name="dodgeDirection">回避方向（回避攻撃時）</param>
//        /// <param name="toEnemyDirection">敵への方向（回避攻撃時）</param>
//        /// <returns>最終踏み込み距離</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static float CalculateLungeDistance(AttackInfo attackInfo, CharacterSettings characterSettings,
//            Vector3 dodgeDirection = default, Vector3 toEnemyDirection = default)
//        {
//            if ( characterSettings == null )
//                return attackInfo.lungeDistance;

//            return characterSettings.CalculateLungeDistance(
//                attackInfo.attackType,
//                attackInfo.comboIndex,
//                attackInfo.isAerialAttack,
//                attackInfo.isDodgeAttack,
//                dodgeDirection,
//                toEnemyDirection
//            );
//        }

//        /// <summary>
//        /// 最終ダメージを計算
//        /// </summary>
//        /// <param name="attackInfo">攻撃情報</param>
//        /// <param name="characterSettings">キャラクター設定</param>
//        /// <returns>最終ダメージ</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static float CalculateFinalDamage(AttackInfo attackInfo, CharacterSettings characterSettings)
//        {
//            if ( characterSettings == null )
//                return attackInfo.baseDamage;

//            return characterSettings.CalculateFinalDamage(
//                attackInfo.attackType,
//                attackInfo.comboIndex,
//                attackInfo.isAerialAttack
//            );
//        }
//    }

//    /// <summary>
//    /// 方向システム - 攻撃方向の管理と変換を行う
//    /// </summary>
//    public static class DirectionUtilities
//    {
//        /// <summary>
//        /// 入力ベクトルから攻撃方向を取得
//        /// </summary>
//        /// <param name="inputVector">入力ベクトル</param>
//        /// <returns>攻撃方向</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static AttackDirection GetDirectionFromInput(Vector2 inputVector)
//        {
//            if ( inputVector.magnitude < 0.1f )
//                return AttackDirection.Up;

//            float angle = Mathf.Atan2(inputVector.y, inputVector.x) * Mathf.Rad2Deg;

//            // 角度を正規化 (0-360)
//            if ( angle < 0 )
//                angle += 360f;

//            // 3方向に分割
//            if ( angle >= 315f || angle < 45f )
//                return AttackDirection.Right;
//            else if ( angle >= 45f && angle < 135f )
//                return AttackDirection.Up;
//            else if ( angle >= 135f && angle < 225f )
//                return AttackDirection.Left;
//            else
//                return AttackDirection.Up; // 下方向は上として扱う
//        }

//        /// <summary>
//        /// 攻撃方向をワールド方向ベクトルに変換
//        /// </summary>
//        /// <param name="direction">攻撃方向</param>
//        /// <param name="characterTransform">キャラクターのTransform</param>
//        /// <returns>ワールド方向ベクトル</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static Vector3 DirectionToWorldVector(AttackDirection direction, Transform characterTransform)
//        {
//            switch ( direction )
//            {
//                case AttackDirection.Up:
//                    return characterTransform.forward;
//                case AttackDirection.Left:
//                    return -characterTransform.right;
//                case AttackDirection.Right:
//                    return characterTransform.right;
//                default:
//                    return characterTransform.forward;
//            }
//        }

//        /// <summary>
//        /// 相手の位置から最適な攻撃方向を計算
//        /// </summary>
//        /// <param name="attackerTransform">攻撃者のTransform</param>
//        /// <param name="targetPosition">目標位置</param>
//        /// <returns>最適な攻撃方向</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static AttackDirection GetOptimalAttackDirection(Transform attackerTransform, Vector3 targetPosition)
//        {
//            Vector3 directionToTarget = (targetPosition - attackerTransform.position).normalized;
//            Vector3 localDirection = attackerTransform.InverseTransformDirection(directionToTarget);

//            float angle = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;

//            if ( angle >= -60f && angle <= 60f )
//                return AttackDirection.Up;
//            else if ( angle > 60f && angle <= 180f )
//                return AttackDirection.Right;
//            else
//                return AttackDirection.Left;
//        }

//        /// <summary>
//        /// 対戦相手の防御方向を回避する攻撃方向を取得
//        /// </summary>
//        /// <param name="opponentGuardDirection">相手のガード方向</param>
//        /// <returns>回避攻撃方向</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static AttackDirection GetCounterDirection(AttackDirection opponentGuardDirection)
//        {
//            switch ( opponentGuardDirection )
//            {
//                case AttackDirection.Up:
//                    return Random.value > 0.5f ? AttackDirection.Left : AttackDirection.Right;
//                case AttackDirection.Left:
//                    return Random.value > 0.5f ? AttackDirection.Up : AttackDirection.Right;
//                case AttackDirection.Right:
//                    return Random.value > 0.5f ? AttackDirection.Up : AttackDirection.Left;
//                default:
//                    return AttackDirection.Up;
//            }
//        }

//        /// <summary>
//        /// 回避攻撃の踏み込み強化度を計算
//        /// </summary>
//        /// <param name="dodgeDirection">回避方向</param>
//        /// <param name="toEnemyDirection">敵への方向</param>
//        /// <returns>踏み込み強化倍率</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static float CalculateDodgeAttackMultiplier(Vector3 dodgeDirection, Vector3 toEnemyDirection)
//        {
//            float dot = Vector3.Dot(dodgeDirection.normalized, toEnemyDirection.normalized);
//            // cosの値（-1 to 1）を（1.1 to 3.0）にマッピング
//            float t = (dot + 1f) * 0.5f; // 0 to 1 の範囲に変換
//            return Mathf.Lerp(1.1f, 3f, t);
//        }
//    }

//    /// <summary>
//    /// タイミングシステム - アクションタイミングの管理と判定
//    /// </summary>
//    public static class TimingSystem
//    {
//        /// <summary>
//        /// ジャストタイミングの判定
//        /// </summary>
//        /// <param name="actionStartTime">アクション開始時間</param>
//        /// <param name="checkTime">チェック時間</param>
//        /// <param name="perfectWindow">完璧な判定ウィンドウ</param>
//        /// <param name="goodWindow">良い判定ウィンドウ</param>
//        /// <returns>タイミング判定結果</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static TimingResult CheckTiming(float actionStartTime, float checkTime, float perfectWindow = 0.1f, float goodWindow = 0.2f)
//        {
//            float timeDifference = Mathf.Abs(checkTime - actionStartTime);

//            if ( timeDifference <= perfectWindow )
//                return TimingResult.Perfect;
//            else if ( timeDifference <= goodWindow )
//                return TimingResult.Good;
//            else
//                return TimingResult.Miss;
//        }

//        /// <summary>
//        /// コンボタイミングの判定
//        /// </summary>
//        /// <param name="lastActionTime">最後のアクション時間</param>
//        /// <param name="currentTime">現在時間</param>
//        /// <param name="comboWindow">コンボウィンドウ</param>
//        /// <returns>コンボ継続可能かどうか</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static bool IsWithinComboWindow(float lastActionTime, float currentTime, float comboWindow = 0.5f)
//        {
//            return (currentTime - lastActionTime) <= comboWindow;
//        }

//        /// <summary>
//        /// 攻撃キャンセルタイミングの判定
//        /// </summary>
//        /// <param name="attackStartTime">攻撃開始時間</param>
//        /// <param name="currentTime">現在時間</param>
//        /// <param name="cancelWindow">キャンセルウィンドウ</param>
//        /// <returns>キャンセル可能かどうか</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static bool CanCancelAttack(float attackStartTime, float currentTime, float cancelWindow = 0.3f)
//        {
//            return (currentTime - attackStartTime) <= cancelWindow;
//        }

//        /// <summary>
//        /// 空中コンボタイミングの判定
//        /// </summary>
//        /// <param name="airTime">空中時間</param>
//        /// <param name="maxAirTime">最大空中時間</param>
//        /// <returns>空中コンボ継続可能かどうか</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static bool CanContinueAerialCombo(float airTime, float maxAirTime = 5f)
//        {
//            return airTime < maxAirTime;
//        }

//        /// <summary>
//        /// 回避攻撃タイミングの判定
//        /// </summary>
//        /// <param name="dodgeTime">回避時間</param>
//        /// <param name="currentTime">現在時間</param>
//        /// <param name="dodgeAttackWindow">回避攻撃ウィンドウ</param>
//        /// <returns>回避攻撃可能かどうか</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static bool CanDodgeAttack(float dodgeTime, float currentTime, float dodgeAttackWindow = 0.5f)
//        {
//            return (currentTime - dodgeTime) <= dodgeAttackWindow;
//        }
//    }

//    /// <summary>
//    /// コンボシステム - コンボ管理のユーティリティ
//    /// </summary>
//    public static class ComboSystem
//    {
//        /// <summary>
//        /// コンボ継続可能かどうかを判定
//        /// </summary>
//        /// <param name="currentCount">現在のコンボ数</param>
//        /// <param name="maxCount">最大コンボ数</param>
//        /// <param name="lastAttackTime">最後の攻撃時間</param>
//        /// <param name="currentTime">現在時間</param>
//        /// <param name="comboWindow">コンボウィンドウ</param>
//        /// <returns>コンボ継続可能かどうか</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static bool CanContinueCombo(int currentCount, int maxCount, float lastAttackTime, float currentTime, float comboWindow)
//        {
//            return currentCount < maxCount &&
//                   TimingSystem.IsWithinComboWindow(lastAttackTime, currentTime, comboWindow);
//        }

//        /// <summary>
//        /// 強攻撃フィニッシュが可能かどうかを判定
//        /// </summary>
//        /// <param name="currentCount">現在のコンボ数</param>
//        /// <param name="isInCombo">コンボ中かどうか</param>
//        /// <returns>強攻撃フィニッシュ可能かどうか</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static bool CanFinishWithStrong(int currentCount, bool isInCombo)
//        {
//            return isInCombo && currentCount > 0;
//        }

//        /// <summary>
//        /// コンボボーナスダメージを計算
//        /// </summary>
//        /// <param name="baseDamage">基本ダメージ</param>
//        /// <param name="comboCount">コンボ数</param>
//        /// <param name="bonusPerHit">ヒットごとのボーナス</param>
//        /// <returns>ボーナス適用後のダメージ</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static float CalculateComboDamage(float baseDamage, int comboCount, float bonusPerHit = 0.1f)
//        {
//            float bonus = 1f + (comboCount * bonusPerHit);
//            return baseDamage * bonus;
//        }
//    }

//    /// <summary>
//    /// タイミング判定結果
//    /// </summary>
//    public enum TimingResult
//    {
//        Miss,
//        Good,
//        Perfect
//    }
//}
