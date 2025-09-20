# バトルシステム用 AnimatorController 完全版

## 使用方法
このファイルをUnityのAssetsフォルダ内にコピーし、.controllerファイルとして保存してください。

## パラメータ設定
以下のパラメータが設定されています：

### 基本状態
- isGrounded (Bool): 地面にいるかどうか
- isMoving (Bool): 移動中かどうか
- moveSpeed (Float): 移動速度 (0-1)
- moveX, moveY (Float): 移動方向
- verticalVelocity (Float): 垂直速度

### 戦闘モード
- combatMode (Int): 0=Melee, 1=Ranged, 2=EnergyBarrier
- isAttacking, isGuarding, isBlocking (Bool)

### 攻撃システム
- attackType (Int): 0=Weak, 1=Strong, 2=Skill
- attackDirection (Int): 0=Up, 1=Left, 2=Right
- comboCount (Int): コンボ数
- airAttack (Bool): 空中攻撃
- attackTrigger (Trigger)

### 防御システム
- guardDirection (Int): ガード方向
- blockSuccess, guardBroken (Bool)
- blockTrigger (Trigger)

### 移動・回避
- isBoosting, isDodging (Bool)
- dodgeDirection (Int): 回避方向
- dodgeTrigger, quickTurnTrigger (Trigger)
- isJumping, inAir (Bool)

### 特殊状態
- isStunned, isInvincible (Bool)
- stunGauge (Float): スタンゲージ値
- energyDepleted (Bool)
- maneuverTrigger (Trigger)
- maneuverType (Int): マニューバ種類

### 射撃システム
- isAiming, isReloading (Bool)
- aimAccuracy, weaponPower (Float)

### エフェクト・反応
- hitTrigger (Trigger)
- hitDirection (Int): 被弾方向
- criticalHit (Bool)
- damageIntensity (Float): ダメージ強度

## レイヤー構成
1. **Base Layer**: 基本動作 (Idle, Walking, Running, Jump, Boost)
2. **Combat Layer**: 戦闘動作 (Melee/Ranged/EnergyBarrier)
3. **Special Layer**: 特殊動作 (Dodge, QuickTurn, Maneuver)
4. **Additive Layer**: 追加効果 (Hit反応, エフェクト)

## ステート構成

### Base Layer States
- Idle: 待機状態
- Walking: 歩行
- Running: 高速移動  
- Jump_Start/Loop/Land: ジャンプシーケンス
- Boost_Start/Loop/End: ブーストシーケンス

### Combat Layer Sub-States

#### Melee Combat
- Melee_Idle: 近接待機
- Melee_Guard: ガード
- Attack_Weak_Up/Left/Right: 弱攻撃（3方向）
- Attack_Strong_Up/Left/Right: 強攻撃（3方向）
- Block_Attempt/Success: ブロッキング

#### Ranged Combat  
- Ranged_Idle: 射撃待機
- Aim_Start/Loop: エイミング
- Fire_Weak/Strong: 射撃
- Reload_Start/Loop/End: リロード

#### Energy Barrier
- Barrier_Idle: バリア待機
- Barrier_Shield: シールド展開
- Barrier_Hit: バリア被弾
- Barrier_Recovery: エネルギー回復

### Special Layer States
- Special_None: 通常状態
- Dodge_Back/Left/Right/Forward: 4方向回避
- Quick_Turn: クイックターン
- Maneuver_Execute: マニューバ実行

### Additive Layer States
- No_Reaction: 無反応
- Hit_Light/Medium/Heavy/Critical: 被弾反応
- Block_React/Guard_React: 防御反応

## 実装手順
1. UnityでAnimatorControllerを新規作成
2. 上記パラメータを全て追加
3. 4つのレイヤーを作成（BlendingMode設定）
4. 各レイヤーにステートを追加
5. アニメーションクリップを割り当て
6. トランジション条件を設定

## StateSystem連携
AnimationStateUpdaterスクリプトを使用してStateSystemと同期：

```csharp
public class AnimationStateUpdater : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private StateSystem stateSystem;
    
    private void Update()
    {
        // StateSystemの値をAnimatorに反映
        animator.SetBool("isGrounded", stateSystem.IsGrounded);
        animator.SetBool("isMoving", stateSystem.IsMoving);
        animator.SetFloat("moveSpeed", stateSystem.CurrentSpeed);
        animator.SetInteger("combatMode", (int)stateSystem.CurrentActionMode);
        // ... その他のパラメータ
    }
}
```

この設計により、バトルシステム仕様書で定義された全ての戦闘要素が適切にアニメーション表現できます。
