# バトルシステム用 AnimatorController 設計書

## 1. 全体構造概要

### レイヤー構成
- **Base Layer**: 基本動作（移動・待機・ジャンプ）
- **Combat Layer**: 戦闘動作（攻撃・防御）
- **Special Layer**: 特殊動作（回避・マニューバ・スタン）
- **Additive Layer**: 追加効果（被弾リアクション・エフェクト）

### パラメータ一覧
```
// 基本状態
Bool: isGrounded
Bool: isMoving
Float: moveSpeed (0-1)
Float: verticalVelocity (-1 to 1)

// 戦闘モード
Int: combatMode (0=Melee, 1=Ranged, 2=EnergyBarrier)
Bool: isAttacking
Bool: isGuarding
Bool: isBlocking

// 攻撃システム
Int: attackType (0=Weak, 1=Strong, 2=Skill)
Int: attackDirection (0=Up, 1=Left, 2=Right)
Int: comboCount (0-5)
Bool: airAttack
Trigger: attackTrigger

// 防御システム
Int: guardDirection (0=Up, 1=Left, 2=Right)
Bool: blockSuccess
Bool: guardBroken
Trigger: blockTrigger

// 移動・回避
Bool: isBoosting
Bool: isDodging
Int: dodgeDirection (0=Back, 1=Left, 2=Right, 3=Forward)
Trigger: dodgeTrigger
Bool: isJumping
Bool: inAir

// 特殊状態
Bool: isStunned
Bool: isInvincible
Float: stunGauge (0-1)
Bool: energyDepleted
Trigger: maneuverTrigger
Int: maneuverType (0-9)

// 射撃システム
Bool: isAiming
Bool: isReloading
Float: aimAccuracy (0-1)

// エフェクト・反応
Trigger: hitTrigger
Int: hitDirection (0=Up, 1=Left, 2=Right)
Bool: criticalHit
Float: damageIntensity (0-1)
```

## 2. Base Layer - 基本動作

### State構成
```
Entry → Idle
├── Idle (待機)
├── Walking (歩行)
├── Running (高速移動)
├── Jump_Start (ジャンプ開始)
├── Jump_Loop (空中滞空)
├── Jump_Land (着地)
├── Boost_Start (ブースト開始)
├── Boost_Loop (ブースト継続)
└── Boost_End (ブースト終了)
```

### 主要トランジション
```
// 基本移動
Idle → Walking: isMoving && !isBoosting
Walking → Idle: !isMoving
Walking → Running: isMoving && moveSpeed > 0.8

// ジャンプ系
Any State → Jump_Start: !isGrounded && isJumping
Jump_Start → Jump_Loop: normalizedTime > 0.8
Jump_Loop → Jump_Land: isGrounded

// ブースト系
Any State → Boost_Start: isBoosting
Boost_Start → Boost_Loop: normalizedTime > 0.5
Boost_Loop → Boost_End: !isBoosting
Boost_End → Idle: normalizedTime > 0.8
```

## 3. Combat Layer - 戦闘動作

### Sub-State Machine: Melee Combat
```
Entry → Melee_Idle
├── Melee_Idle (近接待機)
├── Melee_Guard (ガード)
├── Attack_Weak_Up (弱攻撃_上)
├── Attack_Weak_Left (弱攻撃_左)
├── Attack_Weak_Right (弱攻撃_右)
├── Attack_Strong_Up (強攻撃_上)
├── Attack_Strong_Left (強攻撃_左)
├── Attack_Strong_Right (強攻撃_右)
├── Combo_2nd (コンボ2段目)
├── Combo_3rd (コンボ3段目)
├── Combo_Finisher (コンボフィニッシュ)
├── Block_Attempt (ブロッキング)
├── Block_Success (ブロッキング成功)
└── Air_Attack (空中攻撃)
```

### Sub-State Machine: Ranged Combat
```
Entry → Ranged_Idle
├── Ranged_Idle (射撃待機)
├── Aim_Start (エイミング開始)
├── Aim_Loop (エイミング継続)
├── Fire_Weak (弱射撃)
├── Fire_Strong (強射撃)
├── Fire_Skill (スキル射撃)
├── Reload_Start (リロード開始)
├── Reload_Loop (リロード中)
└── Reload_End (リロード終了)
```

### Sub-State Machine: Energy Barrier
```
Entry → Barrier_Idle
├── Barrier_Idle (バリア待機)
├── Barrier_Shield (シールド展開)
├── Barrier_Hit (バリア被弾)
└── Barrier_Recovery (エネルギー回復)
```

### Combat Layer トランジション例
```
// モード切り替え
Any State → Melee Combat: combatMode == 0
Any State → Ranged Combat: combatMode == 1
Any State → Energy Barrier: combatMode == 2

// 攻撃実行
Melee_Idle → Attack_Weak_Up: attackTrigger && attackType == 0 && attackDirection == 0
Attack_Weak_Up → Combo_2nd: attackTrigger && comboCount == 1
Combo_2nd → Combo_3rd: attackTrigger && comboCount == 2

// ガード・ブロッキング
Melee_Idle → Melee_Guard: isGuarding
Melee_Guard → Block_Attempt: blockTrigger
Block_Attempt → Block_Success: blockSuccess
```

## 4. Special Layer - 特殊動作

### State構成
```
Entry → Special_None
├── Special_None (通常状態)
├── Dodge_Back (後方回避)
├── Dodge_Left (左回避)
├── Dodge_Right (右回避)
├── Dodge_Forward (前方回避)
├── Quick_Turn (クイックターン)
├── Maneuver_Execute (マニューバ実行)
├── Stun_Light (軽スタン)
├── Stun_Heavy (重スタン)
├── Invincible (無敵状態)
└── Recovery (回復動作)
```

### 回避システムトランジション
```
// 回避実行
Special_None → Dodge_Back: dodgeTrigger && dodgeDirection == 0
Special_None → Dodge_Left: dodgeTrigger && dodgeDirection == 1
Special_None → Dodge_Right: dodgeTrigger && dodgeDirection == 2
Special_None → Dodge_Forward: dodgeTrigger && dodgeDirection == 3

// 回避完了
Any Dodge → Special_None: normalizedTime > 0.9

// クイックターン
Any State → Quick_Turn: quickTurnTrigger
Quick_Turn → Special_None: normalizedTime > 0.5

// マニューバ
Special_None → Maneuver_Execute: maneuverTrigger
Maneuver_Execute → Special_None: normalizedTime > 0.95
```

## 5. Additive Layer - 追加効果

### ダメージリアクション
```
Entry → No_Reaction
├── No_Reaction (無反応)
├── Hit_Light (軽ダメージ)
├── Hit_Medium (中ダメージ)
├── Hit_Heavy (重ダメージ)
├── Hit_Critical (クリティカル)
├── Block_React (ブロック反応)
└── Guard_React (ガード反応)
```

### エフェクト同期
```
// 被弾エフェクト
No_Reaction → Hit_Light: hitTrigger && damageIntensity < 0.3
No_Reaction → Hit_Medium: hitTrigger && damageIntensity < 0.7
No_Reaction → Hit_Heavy: hitTrigger && damageIntensity >= 0.7
No_Reaction → Hit_Critical: hitTrigger && criticalHit

// 防御エフェクト
No_Reaction → Block_React: blockSuccess
No_Reaction → Guard_React: isGuarding && hitTrigger

// 自動復帰
Any Hit → No_Reaction: normalizedTime > 0.8
```

## 6. ブレンドツリー設計

### 移動ブレンドツリー (Base Layer)
```
Locomotion (2D Freeform Cartesian)
├── Idle (0, 0)
├── Walk_Forward (0, 1)
├── Walk_Back (0, -1)
├── Walk_Left (-1, 0)
├── Walk_Right (1, 0)
├── Walk_Forward_Left (-0.7, 0.7)
├── Walk_Forward_Right (0.7, 0.7)
├── Walk_Back_Left (-0.7, -0.7)
└── Walk_Back_Right (0.7, -0.7)

Parameters: moveX (-1 to 1), moveY (-1 to 1)
```

### 攻撃方向ブレンドツリー
```
Attack_Direction (1D)
├── Attack_Up (0)
├── Attack_Left (1)
└── Attack_Right (2)

Parameter: attackDirection (0-2)
```

### エイミングブレンドツリー
```
Aiming_Precision (1D)
├── Aim_Low (0)
├── Aim_Medium (0.5)
└── Aim_High (1)

Parameter: aimAccuracy (0-1)
```

## 7. 空中戦闘システム

### 空中状態管理
```
Air_Combat (Sub-State Machine)
├── Air_Idle (空中待機)
├── Air_Hover (空中滞空)
├── Air_Attack_Up (空中攻撃_上)
├── Air_Attack_Left (空中攻撃_左)
├── Air_Attack_Right (空中攻撃_右)
├── Air_Combo (空中コンボ)
└── Air_Landing (着地準備)
```

### 空中コンボ専用トランジション
```
// 空中攻撃開始
Air_Idle → Air_Attack_Up: attackTrigger && attackDirection == 0 && inAir
Air_Attack_Up → Air_Combo: attackTrigger && comboCount > 0

// 滞空制御
Air_Attack → Air_Hover: airAttack && comboCount > 0
Air_Hover → Air_Landing: !airAttack || energyDepleted
```

## 8. StateSystem連携

### パラメータ更新スクリプト例
```csharp
public class AnimationStateUpdater : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private StateSystem stateSystem;
    
    private void Update()
    {
        UpdateMovementParameters();
        UpdateCombatParameters();
        UpdateSpecialParameters();
    }
    
    private void UpdateMovementParameters()
    {
        animator.SetBool("isGrounded", stateSystem.IsGrounded);
        animator.SetBool("isMoving", stateSystem.IsMoving);
        animator.SetFloat("moveSpeed", stateSystem.CurrentSpeed);
        animator.SetBool("isBoosting", stateSystem.IsBoosting);
    }
    
    private void UpdateCombatParameters()
    {
        animator.SetInteger("combatMode", (int)stateSystem.CurrentActionMode);
        animator.SetBool("isAttacking", stateSystem.CurrentActionState == ActionState.Attacking);
        animator.SetBool("isGuarding", stateSystem.IsGuarding);
        animator.SetInteger("attackDirection", (int)stateSystem.CurrentDirection);
    }
    
    private void UpdateSpecialParameters()
    {
        animator.SetBool("isStunned", stateSystem.IsStunned);
        animator.SetFloat("stunGauge", stateSystem.StunGaugeNormalized);
        animator.SetBool("energyDepleted", stateSystem.CurrentActionMode == ActionMode.EnergyBarrier);
    }
}
```

## 9. 攻撃アニメーション詳細設計

### コンボアニメーション構造
```
WeaponType_AttackSequence_ComboNumber_Direction

例:
- Sword_Weak_1_Up (刀・弱攻撃・1段目・上方向)
- Sword_Weak_2_Left (刀・弱攻撃・2段目・左方向)
- Axe_Strong_1_Right (斧・強攻撃・1段目・右方向)
- Spear_Combo_3_Up (槍・コンボ・3段目・上方向)
```

### 武器別アニメーション速度
```
// AnimatorController設定
高速型武器: Speed Multiplier = 1.2
バランス型武器: Speed Multiplier = 1.0  
パワー型武器: Speed Multiplier = 0.8
リーチ型武器: Speed Multiplier = 0.9
```

## 10. 射撃アニメーション

### エイミング精度による分岐
```
Aim_Tree (Blend Tree 1D)
├── Aim_Unstable (0.0) // 手ぶれあり
├── Aim_Stable (0.5)   // 安定
└── Aim_Perfect (1.0)  // 完全精密

Parameter: aimAccuracy
```

### 射撃リアクション
```
Fire_Reaction (Blend Tree 1D)
├── Fire_Light (0.0)   // 軽火器反動
├── Fire_Medium (0.5)  // 中火器反動  
└── Fire_Heavy (1.0)   // 重火器反動

Parameter: weaponPower
```

## 11. エネルギー切れ時の特殊アニメーション

### バリアシステム
```
Energy_Barrier_States:
├── Barrier_Activate (バリア展開)
├── Barrier_Idle (バリア維持)
├── Barrier_Hit_Up (上方向被弾)
├── Barrier_Hit_Left (左方向被弾)
├── Barrier_Hit_Right (右方向被弾)
├── Barrier_Overload (過負荷)
└── Barrier_Deactivate (バリア解除)
```

### スタンゲージアニメーション
```
Stun_Buildup (Blend Tree 1D)
├── Normal (0.0)       // 通常状態
├── Warning (0.7)      // 警告状態
└── Critical (1.0)     // 危険状態

Parameter: stunGauge
```

## 12. マニューバアニメーション

### 記録済みマニューバ対応
```
Maneuver_Execution:
├── Maneuver_0 (記録スロット0)
├── Maneuver_1 (記録スロット1)
├── Maneuver_2 (記録スロット2)
├── Maneuver_3 (記録スロット3)
└── Maneuver_Custom (カスタム)

Transition: maneuverType parameter
```

### 複雑機動の表現
```
// 記録された動作の種類に応じて分岐
Complex_Maneuver (Sub-State Machine)
├── Dash_Behind (背後回り込み)  
├── Spiral_Dodge (螺旋回避)
├── Air_Dash (空中ダッシュ)
└── Combo_Maneuver (攻撃込みマニューバ)
```

## 13. 実装時の注意点

### パフォーマンス最適化
- **レイヤー重み**: 不要時は0に設定
- **パラメータ更新**: 変化時のみ更新
- **アニメーション数**: 武器別で分離してメモリ効率化

### デバッグ支援
- **SRDebugger**: 全パラメータをリアルタイム監視可能
- **Odin Inspector**: AnimatorControllerの可視化
- **状態遷移ログ**: 意図しない遷移の追跡

### 拡張性
- **新武器対応**: Sub-State Machineで分離
- **新モード追加**: レイヤー追加で対応
- **AI個性**: パラメータ調整で差別化

## 14. セットアップ手順

1. **AnimatorController作成**: 上記構造でレイヤー・パラメータ設定
2. **アニメーションクリップ**: 各状態に対応するアニメーション割り当て
3. **スクリプト連携**: StateSystemとの同期スクリプト作成
4. **テスト**: SRDebuggerでパラメータ動作確認
5. **調整**: 各武器・AIタイプでの動作最適化

この設計により、バトルシステム仕様書で定義された全ての戦闘要素を適切にアニメーション表現できます。

## 15. 実装用チェックリスト

### Phase 1: 基本構造
- [ ] AnimatorController作成
- [ ] 4レイヤー構造設定（Base/Combat/Special/Additive）
- [ ] 全パラメータ定義
- [ ] 基本状態（Idle, Walking, Jump）設定

### Phase 2: 戦闘システム
- [ ] 近接攻撃状態（弱・強・方向別）
- [ ] 射撃システム状態
- [ ] ガード・ブロッキング状態
- [ ] コンボシステム遷移

### Phase 3: 特殊機能
- [ ] 回避システム
- [ ] マニューバ実行状態
- [ ] エネルギー切れ・バリア状態
- [ ] 空中戦闘システム

### Phase 4: エフェクト・連携
- [ ] 被弾リアクション（Additive Layer）
- [ ] StateSystem連携スクリプト
- [ ] SRDebugger連携
- [ ] 武器別パラメータ調整

### Phase 5: 最適化・テスト
- [ ] パフォーマンス最適化
- [ ] デバッグ機能確認
- [ ] 各AI個性での動作確認
- [ ] バランス調整