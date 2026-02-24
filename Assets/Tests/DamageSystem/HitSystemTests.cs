using Cysharp.Threading.Tasks;
using LearningAIGame.CombatSystem.Core;
using LearningAIGame.CombatSystem.Data;
using LearningAIGame.CombatSystem.Systems;
using NUnit.Framework;
using R3;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using static LearningAIGame.CombatSystem.Core.StateSystem;

//==============================================ファイルヘッダ===========================================================
// HitSystemTests
// 
// 概要: HitSystemの攻撃判定と攻撃結果処理を検証するテストコード
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// HitSystemとDamageSystemMockを使用した攻撃判定の統合テスト。
// 攻撃側・防御側双方のStateSystem状態遷移とObservable通知を検証する。
// 実際のCollider接触による判定検出をテストする。
// 
// テスト項目:
// 1. Hit(弱攻撃) - 被弾時の怯み状態遷移
// 2. Hit(強攻撃) - 被弾時の怯み状態遷移
// 3. Guard成功 - 攻撃側・防御側の状態遷移
// 4. Block成功 - 攻撃側・防御側の状態遷移 + エネルギー回復
// 5. Avoid成功 - 攻撃側の状態のみ
// 6. Miss(空振り) - 通知なし
// 7. Cancel(キャンセル) - キャンセル処理
// 8. 死亡テスト - HP=0での死亡状態遷移
// 9. 攻撃判定制御 - 開始/終了/持続時間
// 10. 攻撃ID管理 - 複数攻撃のキャンセル
// 
// 必要なプレハブ:
// - Resources/TestPrefabs/HitSystemTestAttacker.prefab
//   - HitSystem, StateSystem, 攻撃判定Collider(Trigger)
//   - 子オブジェクトとして防御側を配置
// - Resources/TestPrefabs/HitSystemTestDefender.prefab
//   - DamageSystemMock, StateSystem, 被弾判定Collider
// 
// その他:
// Unity Test Runner PlayMode専用
//=====================================================================================================================

namespace LearningAIGame.CombatSystem.Tests
{
    /// <summary>
    /// HitSystemの攻撃判定テストクラス
    /// </summary>
    [TestFixture]
    public class HitSystemTests
    {
        #region フィールド

        // 攻撃側
        private GameObject _attackerObject;
        private HitSystem _attackerHitSystem;
        private StateSystem _attackerStateSystem;
        private Collider _attackerCollider;

        // 防御側
        private GameObject _defenderObject;
        private DamageSystemMock _defenderDamageSystem;
        private StateSystem _defenderStateSystem;

        // テスト設定
        private const float k_DEFAULT_TIMEOUT = 3f;
        private const int k_DEFAULT_ATTACK_DURATION_FRAMES = 10;
        private const int k_DAMAGE_WEAK = 10;
        private const int k_DAMAGE_HEAVY = 30;

        // プレハブパス
        private const string k_ATTACKER_PREFAB_PATH = "DamageSystemTestPlayer";

        #endregion

        #region セットアップ・ティアダウン

        [SetUp]
        public void SetUp()
        {
            // プレハブの読み込みとインスタンス化
            LoadAndInstantiatePrefabs();

            // コンポーネントの取得
            GetComponents();

            // 初期状態の設定
            SetupInitialState();

            Debug.Log("[HitSystemTest SetUp] テスト環境のセットアップが完了しました");
        }

        [TearDown]
        public void TearDown()
        {
            if (_attackerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_attackerObject);
            }

            Debug.Log("[HitSystemTest TearDown] テスト環境のクリーンアップが完了しました");
        }

        #endregion

        #region セットアップヘルパー

        /// <summary>
        /// プレハブの読み込みとインスタンス化
        /// </summary>
        private void LoadAndInstantiatePrefabs()
        {
            // 攻撃側プレハブの読み込み
            var attackerPrefab = Resources.Load<GameObject>(k_ATTACKER_PREFAB_PATH);
            Assert.IsNotNull(attackerPrefab,
                $"攻撃側プレハブが見つかりません: {k_ATTACKER_PREFAB_PATH}");

            // 攻撃側のインスタンス化
            _attackerObject = UnityEngine.Object.Instantiate(attackerPrefab);
            _attackerObject.name = "TestAttacker";

            Debug.Log("[Setup] 攻撃側プレハブをインスタンス化しました");

            // 防御側は攻撃側の子として既に配置されている前提
            _defenderObject = _attackerObject.transform.GetChild(1).gameObject;
            Assert.IsNotNull(_defenderObject, "防御側オブジェクトが子オブジェクトとして見つかりません");

            Debug.Log("[Setup] 防御側オブジェクトを取得しました");
        }

        /// <summary>
        /// コンポーネントの取得
        /// </summary>
        private void GetComponents()
        {
            // 攻撃側コンポーネント
            _attackerHitSystem = _attackerObject.GetComponent<HitSystem>();
            Assert.IsNotNull(_attackerHitSystem, "HitSystemが設定されていません");

            _attackerStateSystem = _attackerObject.GetComponent<StateSystem>();
            Assert.IsNotNull(_attackerStateSystem, "攻撃側StateSystemが設定されていません");

            _attackerCollider = _attackerObject.GetComponentInChildren<Collider>(true);
            Assert.IsNotNull(_attackerCollider, "攻撃判定Colliderが設定されていません");
            Assert.IsTrue(_attackerCollider.isTrigger, "Colliderがトリガーに設定されていません");

            // 防御側コンポーネント
            _defenderDamageSystem = _defenderObject.GetComponent<DamageSystemMock>();
            Assert.IsNotNull(_defenderDamageSystem, $"DamageSystemMockが設定されていません{_defenderObject.name}");

            _defenderStateSystem = _defenderObject.GetComponent<StateSystem>();
            Assert.IsNotNull(_defenderStateSystem, "防御側StateSystemが設定されていません");

            Debug.Log("[Setup] すべてのコンポーネントを取得しました");
        }

        /// <summary>
        /// 初期状態の設定
        /// </summary>
        private void SetupInitialState()
        {
            // 攻撃判定を無効化（テストで明示的に有効化する）
            _attackerCollider.enabled = false;

            // 防御側のHP/Energyをリセット
            // ※CharacterDataの実装に応じて調整が必要
            // _defenderCharacterData.ResetToFull(); // 例

            Debug.Log("[Setup] 初期状態を設定しました");
        }

        #endregion

        #region ヘルパーメソッド

        /// <summary>
        /// 攻撃情報を作成
        /// </summary>
        private AttackInfo CreateAttackInfo(AttackType attackType, StanceType stance, int damage)
        {
            var info = new AttackInfo();
            info.SetInfo(damage, stance, attackType);
            return info;
        }

        /// <summary>
        /// 攻撃側StateSystem状態変化を監視
        /// </summary>
        private IDisposable ObserveAttackerState(ActionState expectedState, Action callback)
        {
            return _attackerStateSystem.CurrentState
                .Where(state => state == expectedState)
                .Subscribe(_ =>
                {
                    callback?.Invoke();
                    Debug.Log($"[Observable] 攻撃側状態変化: {expectedState}");
                });
        }

        /// <summary>
        /// 防御側StateSystem状態変化を監視
        /// </summary>
        private IDisposable ObserveDefenderState(ActionState expectedState, Action callback)
        {
            return _defenderStateSystem.CurrentState
                .Where(state => state == expectedState)
                .Subscribe(_ =>
                {
                    callback?.Invoke();
                    Debug.Log($"[Observable] 防御側状態変化: {expectedState}");
                });
        }

        /// <summary>
        /// HitSystem通知を監視
        /// </summary>
        private IDisposable ObserveHitReport(Action<HitReportInfo> callback)
        {
            return _attackerHitSystem.Observable
                .Subscribe(data =>
                {
                    callback?.Invoke(data);
                    Debug.Log($"[Observable] HitReport受信: {data.hitResultType}");
                });
        }

        /// <summary>
        /// DamageSystem通知を監視
        /// </summary>
        private IDisposable ObserveDamageReport(Action<DamageReportInfo> callback)
        {
            return _defenderDamageSystem.Observable
                .Subscribe(data =>
                {
                    callback?.Invoke(data);
                    Debug.Log($"[Observable] DamageReport受信: Damage={data.Damage}");
                });
        }

        /// <summary>
        /// 攻撃判定の持続フレームを待機
        /// </summary>
        private IEnumerator WaitForAttackDuration(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                yield return null;
            }
        }

        #endregion

        #region Hit(弱攻撃)テスト

        [UnityTest]
        public IEnumerator HitResult_WeakAttack_Hit_ShouldCauseMinorStun()
        {
            // Arrange
            var attackInfo = CreateAttackInfo(AttackType.WeakAttack, StanceType.Up, k_DAMAGE_WEAK);

            // 防御側: 防御なし(常に被弾)
            _defenderDamageSystem.MockSetting(DefenseType.None, StanceType.Up);

            int initialHP = _defenderStateSystem.Hp;
            bool hitReportReceived = false;
            bool damageReportReceived = false;
            bool defenderStunned = false;

            HitResultType receivedHitResult = HitResultType.Miss;
            int receivedDamage = 0;

            // Observable購読
            var hitSub = ObserveHitReport(report =>
            {
                hitReportReceived = true;
                receivedHitResult = report.hitResultType;
            });

            var damageSub = ObserveDamageReport(report =>
            {
                damageReportReceived = true;
                receivedDamage = report.Damage;
            });

            var defenderStateSub = ObserveDefenderState(ActionState.小怯み, () =>
            {
                defenderStunned = true;
            });

            // Act
            _attackerHitSystem.DamageStart(attackInfo, k_DEFAULT_ATTACK_DURATION_FRAMES);

            // Assert - 攻撃判定有効化
            Assert.IsTrue(_attackerHitSystem.IsAttacking, "攻撃判定が有効化されているべき");

            // 判定持続フレーム待機
            yield return WaitForAttackDuration(k_DEFAULT_ATTACK_DURATION_FRAMES + 5);

            // HitReport検証
            Assert.IsTrue(hitReportReceived, "HitReportを受信すべき");
            Assert.AreEqual(HitResultType.Hit, receivedHitResult, "ヒット結果はHitであるべき");

            // DamageReport検証
            Assert.IsTrue(damageReportReceived, "DamageReportを受信すべき");
            Assert.AreEqual(k_DAMAGE_WEAK, receivedDamage, "ダメージ値が一致すべき");

            // 防御側状態検証
            Assert.IsTrue(defenderStunned, "防御側は小怯み状態になるべき");
            Assert.AreEqual(ActionState.小怯み, _defenderStateSystem.CurrentState.CurrentValue,
                "防御側の現在状態は小怯みであるべき");

            // HP減少検証
            Assert.AreEqual(initialHP - k_DAMAGE_WEAK, _defenderStateSystem.Hp,
                "HPが正しく減少すべき");

            // 攻撃側状態検証(OnHitではリアクションなし)
            Assert.AreNotEqual(ActionState.弱攻撃ガード, _attackerStateSystem.CurrentState.CurrentValue,
                "攻撃側はリアクション状態にならないべき");

            // クリーンアップ
            hitSub.Dispose();
            damageSub.Dispose();
            defenderStateSub.Dispose();

            Debug.Log("[Test] Hit(弱攻撃)テスト完了");
        }

        #endregion

        #region Hit(強攻撃)テスト

        [UnityTest]
        public IEnumerator HitResult_HeavyAttack_Hit_ShouldCauseMajorStun()
        {
            // Arrange
            var attackInfo = CreateAttackInfo(AttackType.HeavyAttack, StanceType.Right, k_DAMAGE_HEAVY);

            // 防御側: 防御なし
            _defenderDamageSystem.MockSetting(DefenseType.None, StanceType.Right);

            int initialHP = _defenderStateSystem.Hp;
            bool hitReportReceived = false;
            bool damageReportReceived = false;
            bool defenderStunned = false;

            HitResultType receivedHitResult = HitResultType.Miss;
            int receivedDamage = 0;

            // Observable購読
            var hitSub = ObserveHitReport(report =>
            {
                hitReportReceived = true;
                receivedHitResult = report.hitResultType;
            });

            var damageSub = ObserveDamageReport(report =>
            {
                damageReportReceived = true;
                receivedDamage = report.Damage;
            });

            var defenderStateSub = ObserveDefenderState(ActionState.大怯み, () =>
            {
                defenderStunned = true;
            });

            // Act
            _attackerHitSystem.DamageStart(attackInfo, k_DEFAULT_ATTACK_DURATION_FRAMES);

            // Assert
            yield return WaitForAttackDuration(k_DEFAULT_ATTACK_DURATION_FRAMES + 5);

            // HitReport検証
            Assert.IsTrue(hitReportReceived, "HitReportを受信すべき");
            Assert.AreEqual(HitResultType.Hit, receivedHitResult, "ヒット結果はHitであるべき");

            // DamageReport検証
            Assert.IsTrue(damageReportReceived, "DamageReportを受信すべき");
            Assert.AreEqual(k_DAMAGE_HEAVY, receivedDamage, "ダメージ値が一致すべき");

            // 防御側状態検証
            Assert.IsTrue(defenderStunned, "防御側は大怯み状態になるべき");
            Assert.AreEqual(ActionState.大怯み, _defenderStateSystem.CurrentState.CurrentValue,
                "防御側の現在状態は大怯みであるべき");

            // HP減少検証
            Assert.AreEqual(initialHP - k_DAMAGE_HEAVY, _defenderStateSystem.Hp,
                "HPが正しく減少すべき");

            // クリーンアップ
            hitSub.Dispose();
            damageSub.Dispose();
            defenderStateSub.Dispose();

            Debug.Log("[Test] Hit(強攻撃)テスト完了");
        }

        #endregion

        #region Guard成功テスト

        [UnityTest]
        public IEnumerator HitResult_Guard_ShouldTriggerBothStates()
        {
            // Arrange
            var attackInfo = CreateAttackInfo(AttackType.WeakAttack, StanceType.Right, k_DAMAGE_WEAK);

            // 防御側: ガード中、同じ方向
            _defenderDamageSystem.MockSetting(DefenseType.Guard, StanceType.Left);

            int initialHP = _defenderStateSystem.Hp;
            bool hitReportReceived = false;
            bool damageReportReceived = false;
            bool attackerGuardReaction = false;
            bool defenderGuardSuccess = false;

            HitResultType receivedHitResult = HitResultType.Miss;
            int receivedDamage = -1;

            // Observable購読
            var hitSub = ObserveHitReport(report =>
            {
                hitReportReceived = true;
                receivedHitResult = report.hitResultType;
            });

            var damageSub = ObserveDamageReport(report =>
            {
                damageReportReceived = true;
                receivedDamage = report.Damage;
            });

            var attackerStateSub = ObserveAttackerState(ActionState.弱攻撃ガード, () =>
            {
                attackerGuardReaction = true;
            });

            var defenderStateSub = ObserveDefenderState(ActionState.ガード成功, () =>
            {
                defenderGuardSuccess = true;
            });

            // Act
            _attackerHitSystem.DamageStart(attackInfo, k_DEFAULT_ATTACK_DURATION_FRAMES);

            // Assert
            yield return WaitForAttackDuration(k_DEFAULT_ATTACK_DURATION_FRAMES + 5);

            // HitReport検証
            Assert.IsTrue(hitReportReceived, "HitReportを受信すべき");
            Assert.AreEqual(HitResultType.Guard, receivedHitResult, $"ヒット結果はGuardであるべき{receivedHitResult}");

            // DamageReport検証(防御成功を示すために通知される)
            Assert.IsTrue(damageReportReceived, "防御成功時もDamageReportを受信すべき");
            Assert.AreEqual(0, receivedDamage, "防御成功時のダメージは0であるべき");

            // 攻撃側状態検証
            Assert.IsTrue(attackerGuardReaction, "攻撃側は弱攻撃ガード状態になるべき");
            Assert.AreEqual(ActionState.弱攻撃ガード, _attackerStateSystem.CurrentState.CurrentValue,
                "攻撃側の現在状態は弱攻撃ガードであるべき");

            // 防御側状態検証
            Assert.IsTrue(defenderGuardSuccess, "防御側はガード成功状態になるべき");
            Assert.AreEqual(ActionState.ガード成功, _defenderStateSystem.CurrentState.CurrentValue,
                "防御側の現在状態はガード成功であるべき");

            // HP不変検証
            Assert.AreEqual(initialHP, _defenderStateSystem.Hp,
                "HPは減少しないべき");

            // 攻撃判定即座終了検証
            Assert.IsFalse(_attackerHitSystem.IsAttacking, "ガード成功時は攻撃判定が即座に終了すべき");

            // クリーンアップ
            hitSub.Dispose();
            damageSub.Dispose();
            attackerStateSub.Dispose();
            defenderStateSub.Dispose();

            Debug.Log("[Test] Guard成功テスト完了");
        }

        #endregion

        #region Block成功 + エネルギー回復テスト

        [UnityTest]
        public IEnumerator HitResult_Block_ShouldTriggerBothStatesAndRecoverEnergy()
        {
            // Arrange - 弱攻撃のブロッキング
            var attackInfo = CreateAttackInfo(AttackType.WeakAttack, StanceType.Up, k_DAMAGE_WEAK);

            // 防御側: ブロッキング中、同じ方向
            _defenderDamageSystem.MockSetting(DefenseType.Blocking, StanceType.Up);

            int initialHP = _defenderStateSystem.Hp;
            _defenderStateSystem.Energy = 50; // 初期エネルギー設定
            int initialEnergy = _defenderStateSystem.Energy;
            Debug.Log($"[Test Setup] 防御側初期エネルギー: {initialEnergy}");

            bool hitReportReceived = false;
            bool damageReportReceived = false;
            bool attackerBlockReaction = false;
            bool defenderBlockSuccess = false;

            HitResultType receivedHitResult = HitResultType.Miss;
            int receivedDamage = -1;

            // Observable購読
            var hitSub = ObserveHitReport(report =>
            {
                hitReportReceived = true;
                receivedHitResult = report.hitResultType;
            });

            var damageSub = ObserveDamageReport(report =>
            {
                damageReportReceived = true;
                receivedDamage = report.Damage;
            });

            var attackerStateSub = ObserveAttackerState(ActionState.弱攻撃ブロッキング, () =>
            {
                attackerBlockReaction = true;
            });

            // Act
            _attackerHitSystem.DamageStart(attackInfo, k_DEFAULT_ATTACK_DURATION_FRAMES);

            // Assert
            yield return WaitForAttackDuration(k_DEFAULT_ATTACK_DURATION_FRAMES + 5);

            // HitReport検証
            Assert.IsTrue(hitReportReceived, "HitReportを受信すべき");
            Assert.AreEqual(HitResultType.Block, receivedHitResult, "ヒット結果はBlockであるべき");

            // DamageReport検証(防御成功を示すために通知される)
            Assert.IsTrue(damageReportReceived, "防御成功時もDamageReportを受信すべき");
            Assert.AreEqual(0, receivedDamage, "防御成功時のダメージは0であるべき");

            // 攻撃側状態検証
            Assert.IsTrue(attackerBlockReaction, "攻撃側は弱攻撃ブロッキング状態になるべき");


            var defenderStateSub = ObserveDefenderState(ActionState.ブロッキング成功, () =>
            {
                defenderBlockSuccess = true;
            });

            Assert.AreEqual(ActionState.弱攻撃ブロッキング, _attackerStateSystem.CurrentState.CurrentValue,
                "攻撃側の現在状態は弱攻撃ブロッキングであるべき");

            // 防御側状態検証
            Assert.IsTrue(defenderBlockSuccess, "防御側はブロッキング成功状態になるべき");
            Assert.AreEqual(ActionState.ブロッキング成功, _defenderStateSystem.CurrentState.CurrentValue,
                "防御側の現在状態はブロッキング成功であるべき");

            // HP不変検証
            Assert.AreEqual(initialHP, _defenderStateSystem.Hp,
                "HPは減少しないべき");

            // エネルギー回復検証
            // ※ActionSettingへのアクセス方法に応じて調整が必要
            Assert.Greater(_defenderStateSystem.Energy, initialEnergy,
                "エネルギーが回復すべき");

            // 攻撃判定即座終了検証
            Assert.IsFalse(_attackerHitSystem.IsAttacking, "ブロッキング成功時は攻撃判定が即座に終了すべき");

            // クリーンアップ
            hitSub.Dispose();
            damageSub.Dispose();
            attackerStateSub.Dispose();
            defenderStateSub.Dispose();

            Debug.Log("[Test] Block成功 + エネルギー回復テスト完了");
        }

        [UnityTest]
        public IEnumerator HitResult_HeavyAttackBlock_ShouldTriggerHeavyBlockReaction()
        {
            // Arrange - 強攻撃のブロッキング
            var attackInfo = CreateAttackInfo(AttackType.HeavyAttack, StanceType.Right, k_DAMAGE_HEAVY);

            // 防御側: ブロッキング中、同じ方向
            _defenderDamageSystem.MockSetting(DefenseType.Blocking, StanceType.Left);

            bool attackerBlockReaction = false;

            var attackerStateSub = ObserveAttackerState(ActionState.強攻撃ブロッキング, () =>
            {
                attackerBlockReaction = true;
            });

            // Act
            _attackerHitSystem.DamageStart(attackInfo, k_DEFAULT_ATTACK_DURATION_FRAMES);

            // Assert
            yield return WaitForAttackDuration(k_DEFAULT_ATTACK_DURATION_FRAMES + 5);

            // 攻撃側状態検証(強攻撃の場合)
            Assert.IsTrue(attackerBlockReaction, "攻撃側は強攻撃ブロッキング状態になるべき");
            Assert.AreEqual(ActionState.強攻撃ブロッキング, _attackerStateSystem.CurrentState.CurrentValue,
                "攻撃側の現在状態は強攻撃ブロッキングであるべき");

            // クリーンアップ
            attackerStateSub.Dispose();

            Debug.Log("[Test] 強攻撃Block成功テスト完了");
        }

        #endregion

        #region Avoid成功テスト

        [UnityTest]
        public IEnumerator HitResult_Avoid_ShouldNotTriggerDamageReport()
        {
            // Arrange
            var attackInfo = CreateAttackInfo(AttackType.WeakAttack, StanceType.Right, k_DAMAGE_WEAK);

            // 防御側: 回避中
            _defenderDamageSystem.MockSetting(DefenseType.Avoid, StanceType.Right);

            int initialHP = _defenderStateSystem.Hp;
            bool hitReportReceived = false;
            bool damageReportReceived = false;

            HitResultType receivedHitResult = HitResultType.Miss;

            // Observable購読
            var hitSub = ObserveHitReport(report =>
            {
                hitReportReceived = true;
                receivedHitResult = report.hitResultType;
            });

            var damageSub = ObserveDamageReport(report =>
            {
                damageReportReceived = true;
            });

            // Act
            _attackerHitSystem.DamageStart(attackInfo, k_DEFAULT_ATTACK_DURATION_FRAMES);

            // Assert
            yield return WaitForAttackDuration(k_DEFAULT_ATTACK_DURATION_FRAMES + 5);

            // HitReport検証
            Assert.IsTrue(hitReportReceived, "HitReportを受信すべき");
            Assert.AreEqual(HitResultType.Avoid, receivedHitResult, "ヒット結果はAvoidであるべき");

            // DamageReport検証(回避時は通知なし)
            Assert.IsTrue(damageReportReceived, "回避時はDamageReportを受信すべき");

            // HP不変検証
            Assert.AreEqual(initialHP, _defenderStateSystem.Hp,
                "HPは減少しないべき");

            // 攻撃側状態検証(リアクションなし)
            Assert.AreNotEqual(ActionState.弱攻撃ガード, _attackerStateSystem.CurrentState.CurrentValue,
                "攻撃側はリアクション状態にならないべき");
            Assert.AreNotEqual(ActionState.弱攻撃ブロッキング, _attackerStateSystem.CurrentState.CurrentValue,
                "攻撃側はリアクション状態にならないべき");

            // クリーンアップ
            hitSub.Dispose();
            damageSub.Dispose();

            Debug.Log("[Test] Avoid成功テスト完了");
        }

        #endregion

        #region Miss(空振り)テスト

        [UnityTest]
        public IEnumerator HitResult_Miss_ShouldNotTriggerAnyReport()
        {
            // Arrange
            var attackInfo = CreateAttackInfo(AttackType.WeakAttack, StanceType.Up, k_DAMAGE_WEAK);

            // 防御側を攻撃範囲外に移動(実際は子オブジェクトを無効化)
            _defenderObject.SetActive(false);

            bool hitReportReceived = false;
            bool damageReportReceived = false;

            HitResultType receivedHitResult = HitResultType.Hit; // 初期値を意図的に変更

            // Observable購読
            var hitSub = ObserveHitReport(report =>
            {
                hitReportReceived = true;
                receivedHitResult = report.hitResultType;
            });

            var damageSub = ObserveDamageReport(report =>
            {
                damageReportReceived = true;
            });

            // Act
            _attackerHitSystem.DamageStart(attackInfo, k_DEFAULT_ATTACK_DURATION_FRAMES);

            // Assert
            yield return WaitForAttackDuration(k_DEFAULT_ATTACK_DURATION_FRAMES + 5);

            // HitReport検証(Miss結果を受信)
            Assert.IsTrue(hitReportReceived, "HitReportを受信すべき");
            Assert.AreEqual(HitResultType.Miss, receivedHitResult, "ヒット結果はMissであるべき");

            // DamageReport検証(空振り時は通知なし)
            Assert.IsFalse(damageReportReceived, "空振り時はDamageReportを受信しないべき");

            // 防御側を再度有効化(後続テストのため)
            _defenderObject.SetActive(true);

            // クリーンアップ
            hitSub.Dispose();
            damageSub.Dispose();

            Debug.Log("[Test] Miss(空振り)テスト完了");
        }

        #endregion

        #region Cancel(キャンセル)テスト

        [UnityTest]
        public IEnumerator HitResult_Cancel_ShouldStopAttackImmediately()
        {
            // Arrange
            var attackInfo = CreateAttackInfo(AttackType.HeavyAttack, StanceType.Left, k_DAMAGE_HEAVY);

            // 防御側: 防御なし(キャンセルするので実際には被弾しない)
            _defenderDamageSystem.MockSetting(DefenseType.None, StanceType.Left);

            bool hitReportReceived = false;
            bool damageReportReceived = false;

            HitResultType receivedHitResult = HitResultType.Miss;

            // Observable購読
            var hitSub = ObserveHitReport(report =>
            {
                hitReportReceived = true;
                receivedHitResult = report.hitResultType;
            });

            var damageSub = ObserveDamageReport(report =>
            {
                damageReportReceived = true;
            });

            // Act
            _attackerHitSystem.DamageStart(attackInfo, k_DEFAULT_ATTACK_DURATION_FRAMES);

            // 数フレーム待機後にキャンセル
            yield return WaitForAttackDuration(3);

            _attackerHitSystem.DamageStop(isSelfStop: true);

            // Assert - 即座に判定終了
            Assert.IsFalse(_attackerHitSystem.IsAttacking, "攻撃判定が即座に終了すべき");

            // さらに待機して通知を確認
            yield return WaitForAttackDuration(5);

            // HitReport検証
            Assert.IsTrue(hitReportReceived, "HitReportを受信すべき");
            Assert.AreEqual(HitResultType.Cancel, receivedHitResult, "ヒット結果はCancelであるべき");

            // DamageReport検証(キャンセル時は通知なし)
            Assert.IsTrue(damageReportReceived, "キャンセル時はDamageReportを受信する");

            // クリーンアップ
            hitSub.Dispose();
            damageSub.Dispose();

            Debug.Log("[Test] Cancel(キャンセル)テスト完了");
        }

        #endregion

        #region 死亡テスト

        [UnityTest]
        public IEnumerator HitResult_Death_ShouldTransitionToDeathState()
        {
            // Arrange
            // 防御側のHPを致死ダメージ以下に設定
            _defenderStateSystem.Hp = k_DAMAGE_WEAK - 1;
            int initialHP = _defenderStateSystem.Hp;

            var attackInfo = CreateAttackInfo(AttackType.WeakAttack, StanceType.Up, k_DAMAGE_WEAK);

            // 防御側: 防御なし
            _defenderDamageSystem.MockSetting(DefenseType.None, StanceType.Up);

            bool defenderDied = false;

            var defenderStateSub = ObserveDefenderState(ActionState.死亡, () =>
            {
                defenderDied = true;
            });

            int damage = 0;
            var damageSub = ObserveDamageReport(report =>
            {
                damage = report.Damage;
            });

            // Act
            _attackerHitSystem.DamageStart(attackInfo, k_DEFAULT_ATTACK_DURATION_FRAMES);

            // Assert
            yield return WaitForAttackDuration(k_DEFAULT_ATTACK_DURATION_FRAMES + 5);

            Debug.Log($"[Test] 防御側初期HP: {initialHP}, 攻撃ダメージ: {k_DAMAGE_WEAK}, 攻撃後HP: {_defenderStateSystem.Hp},防御{damage}");

            // 死亡状態検証
            Assert.IsTrue(defenderDied, "防御側は死亡状態になるべき");
            Assert.AreEqual(ActionState.死亡, _defenderStateSystem.CurrentState.CurrentValue,
                "防御側の現在状態は死亡であるべき");

            // HP検証
            Assert.IsTrue(_defenderStateSystem.CurrentState.CurrentValue == ActionState.死亡, "CharacterDataのIsDeadフラグが立つべき");

            // クリーンアップ
            defenderStateSub.Dispose();

            Debug.Log("[Test] 死亡テスト完了");
        }

        #endregion

        #region 攻撃判定制御テスト

        [UnityTest]
        public IEnumerator AttackControl_DamageStartAndStop_ShouldWorkCorrectly()
        {
            // Arrange
            var attackInfo = CreateAttackInfo(AttackType.WeakAttack, StanceType.Right, k_DAMAGE_WEAK);

            // Act - 攻撃開始
            _attackerHitSystem.DamageStart(attackInfo, k_DEFAULT_ATTACK_DURATION_FRAMES);

            // Assert - 判定有効化
            Assert.IsTrue(_attackerHitSystem.IsAttacking, "攻撃判定が有効化されるべき");

            yield return null;

            // Act - 攻撃停止
            _attackerHitSystem.DamageStop(isSelfStop: true);

            // Assert - 判定無効化
            Assert.IsFalse(_attackerHitSystem.IsAttacking, "攻撃判定が無効化されるべき");

            Debug.Log("[Test] 攻撃判定制御テスト完了");
        }

        [UnityTest]
        public IEnumerator AttackControl_AttackDuration_ShouldEndAfterFrames()
        {
            // Arrange
            var attackInfo = CreateAttackInfo(AttackType.WeakAttack, StanceType.Up, k_DAMAGE_WEAK);
            int testDuration = 5;

            _defenderObject.SetActive(false); // 被弾影響を排除

            // Act
            _attackerHitSystem.DamageStart(attackInfo, testDuration);

            // Assert - 初期状態
            Assert.IsTrue(_attackerHitSystem.IsAttacking, "攻撃判定が有効化されるべき");

            // 持続フレーム未満で待機
            yield return WaitForAttackDuration(testDuration - 1);
            Assert.IsTrue(_attackerHitSystem.IsAttacking, "まだ攻撃判定が有効であるべき");

            // 持続フレーム経過後
            yield return WaitForAttackDuration(3);
            Assert.IsFalse(_attackerHitSystem.IsAttacking, "攻撃判定が自動終了すべき");

            _defenderObject.SetActive(true); // 後続テストのため再有効化

            Debug.Log("[Test] 攻撃持続時間テスト完了");
        }

        #endregion

        #region 攻撃ID管理テスト

        [UnityTest]
        public IEnumerator AttackID_MultipleAttacks_ShouldCancelPrevious()
        {
            // Arrange
            var attackInfo1 = CreateAttackInfo(AttackType.WeakAttack, StanceType.Up, k_DAMAGE_WEAK);
            var attackInfo2 = CreateAttackInfo(AttackType.HeavyAttack, StanceType.Left, k_DAMAGE_HEAVY);

            // Act - 1回目の攻撃
            _attackerHitSystem.DamageStart(attackInfo1, k_DEFAULT_ATTACK_DURATION_FRAMES);
            Assert.IsTrue(_attackerHitSystem.IsAttacking, "1回目の攻撃判定が有効化されるべき");

            yield return WaitForAttackDuration(3);

            // Act - 2回目の攻撃(1回目をキャンセル)
            _attackerHitSystem.DamageStart(attackInfo2, k_DEFAULT_ATTACK_DURATION_FRAMES);

            // Assert - 1回目はキャンセルされ、2回目が有効
            Assert.IsTrue(_attackerHitSystem.IsAttacking, "2回目の攻撃判定が有効化されるべき");

            // 2回目の攻撃が正常に終了するまで待機
            yield return WaitForAttackDuration(k_DEFAULT_ATTACK_DURATION_FRAMES + 5);

            Assert.IsFalse(_attackerHitSystem.IsAttacking, "2回目の攻撃判定が終了すべき");

            Debug.Log("[Test] 攻撃ID管理テスト完了");
        }

        #endregion
    }
}