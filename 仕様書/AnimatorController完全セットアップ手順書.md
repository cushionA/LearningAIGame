# AnimatorController セットアップ完全手順書

## 📁 作成されたファイル

1. **AnimatorController設計書.md** - 完全な設計仕様
2. **AnimationStateUpdater.cs** - StateSystem連携スクリプト  
3. **AnimatorController実装ガイド.md** - 実装の詳細手順
4. **BattleCharacterController.controller** - 基本構造（未完成）

## 🚀 Unity内での実装手順

### Step 1: AnimatorController作成
1. **Projectウィンドウで右クリック**
2. **Create → Animator Controller**
3. **名前を「BattleCharacterController」に変更**

### Step 2: パラメータ設定
Animatorウィンドウの「Parameters」タブで以下を追加：

#### 基本状態パラメータ
```
isGrounded (Bool) - Default: true
isMoving (Bool) - Default: false  
moveSpeed (Float) - Default: 0
moveX (Float) - Default: 0
moveY (Float) - Default: 0
verticalVelocity (Float) - Default: 0
```

#### 戦闘モードパラメータ
```
combatMode (Int) - Default: 0
isAttacking (Bool) - Default: false
isGuarding (Bool) - Default: false
isBlocking (Bool) - Default: false
```

#### 攻撃システムパラメータ
```
attackType (Int) - Default: 0
attackDirection (Int) - Default: 0
comboCount (Int) - Default: 0
airAttack (Bool) - Default: false
attackTrigger (Trigger)
```

#### 防御システムパラメータ
```
guardDirection (Int) - Default: 0
blockSuccess (Bool) - Default: false
guardBroken (Bool) - Default: false
blockTrigger (Trigger)
```

#### 移動・回避パラメータ
```
isBoosting (Bool) - Default: false
isDodging (Bool) - Default: false
dodgeDirection (Int) - Default: 0
dodgeTrigger (Trigger)
isJumping (Bool) - Default: false
inAir (Bool) - Default: false
quickTurnTrigger (Trigger)
```

#### 特殊状態パラメータ
```
isStunned (Bool) - Default: false
isInvincible (Bool) - Default: false
stunGauge (Float) - Default: 0
energyDepleted (Bool) - Default: false
maneuverTrigger (Trigger)
maneuverType (Int) - Default: 0
```

#### 射撃システムパラメータ
```
isAiming (Bool) - Default: false
isReloading (Bool) - Default: false
aimAccuracy (Float) - Default: 0
weaponPower (Float) - Default: 0
```

#### エフェクト・反応パラメータ
```
hitTrigger (Trigger)
hitDirection (Int) - Default: 0
criticalHit (Bool) - Default: false
damageIntensity (Float) - Default: 0
```

### Step 3: レイヤー設定
「Layers」タブで以下を設定：

#### 1. Base Layer
- **Weight**: 1.0
- **Blending**: Override

#### 2. Combat Layer
- **Weight**: 1.0  
- **Blending**: Additive
- 右上の⚙️から「Create Layer」→「Override」を選択

#### 3. Special Layer
- **Weight**: 1.0
- **Blending**: Additive

#### 4. Additive Layer
- **Weight**: 1.0
- **Blending**: Additive

### Step 4: Base Layer ステート作成

#### 基本ステート追加
1. **Base Layer選択**
2. **右クリック** → **Create State** → **Empty**
3. **以下のステートを作成**：

```
Idle (デフォルト状態)
Walking
Running
Jump_Start
Jump_Loop
Jump_Land
Boost_Start
Boost_Loop
Boost_End
```

#### トランジション設定例
```
Idle → Walking: isMoving == true && !isBoosting
Walking → Idle: isMoving == false
Walking → Running: isMoving == true && moveSpeed > 0.8
Any State → Jump_Start: !isGrounded && isJumping
Jump_Start → Jump_Loop: Exit Time
Jump_Loop → Jump_Land: isGrounded
Any State → Boost_Start: isBoosting
Boost_Start → Boost_Loop: Exit Time
Boost_Loop → Boost_End: !isBoosting
Boost_End → Idle: Exit Time
```

### Step 5: Combat Layer 設定

#### Sub-State Machine作成
1. **Combat Layer選択**
2. **右クリック** → **Create Sub-State Machine**
3. **3つのSub-State Machine作成**：
   - Melee Combat
   - Ranged Combat  
   - Energy Barrier

#### Melee Combat ステート
```
Melee_Idle (デフォルト)
Melee_Guard
Attack_Weak_Up
Attack_Weak_Left
Attack_Weak_Right
Attack_Strong_Up
Attack_Strong_Left
Attack_Strong_Right
Block_Attempt
Block_Success
```

#### Ranged Combat ステート
```
Ranged_Idle (デフォルト)
Aim_Start
Aim_Loop
Fire_Weak
Fire_Strong
Reload_Start
Reload_Loop
Reload_End
```

#### Energy Barrier ステート
```
Barrier_Idle (デフォルト)
Barrier_Shield
Barrier_Hit
Barrier_Recovery
```

#### Combat Layer トランジション
```
Any State → Melee Combat: combatMode == 0
Any State → Ranged Combat: combatMode == 1
Any State → Energy Barrier: combatMode == 2
```

### Step 6: Special Layer 設定

#### ステート作成
```
Special_None (デフォルト)
Dodge_Back
Dodge_Left
Dodge_Right
Dodge_Forward
Quick_Turn
Maneuver_Execute
Stun_Light
Stun_Heavy
Invincible
Recovery
```

#### トランジション例
```
Special_None → Dodge_Back: dodgeTrigger && dodgeDirection == 0
Special_None → Dodge_Left: dodgeTrigger && dodgeDirection == 1
Special_None → Dodge_Right: dodgeTrigger && dodgeDirection == 2
Special_None → Dodge_Forward: dodgeTrigger && dodgeDirection == 3
Any Dodge → Special_None: Exit Time
Any State → Quick_Turn: quickTurnTrigger
Special_None → Maneuver_Execute: maneuverTrigger
Any State → Stun_Light: isStunned && stunGauge < 0.5
Any State → Stun_Heavy: isStunned && stunGauge >= 0.5
```

### Step 7: Additive Layer 設定

#### ステート作成
```
No_Reaction (デフォルト)
Hit_Light
Hit_Medium
Hit_Heavy
Hit_Critical
Block_React
Guard_React
```

#### トランジション例
```
No_Reaction → Hit_Light: hitTrigger && damageIntensity < 0.3
No_Reaction → Hit_Medium: hitTrigger && damageIntensity < 0.7
No_Reaction → Hit_Heavy: hitTrigger && damageIntensity >= 0.7
No_Reaction → Hit_Critical: hitTrigger && criticalHit
No_Reaction → Block_React: blockSuccess
No_Reaction → Guard_React: isGuarding && hitTrigger
Any Hit → No_Reaction: Exit Time
```

### Step 8: ブレンドツリー設定

#### 移動ブレンドツリー
1. **Walking ステート選択**
2. **Motion欄で** → **Create New Blend Tree**
3. **Blend Type**: 2D Freeform Cartesian
4. **Parameters**: moveX, moveY
5. **Motion追加**：
   - Idle (0, 0)
   - Walk_Forward (0, 1)
   - Walk_Back (0, -1)
   - Walk_Left (-1, 0)
   - Walk_Right (1, 0)
   - Walk_Forward_Left (-0.7, 0.7)
   - Walk_Forward_Right (0.7, 0.7)
   - Walk_Back_Left (-0.7, -0.7)
   - Walk_Back_Right (0.7, -0.7)

### Step 9: スクリプト連携

#### AnimationStateUpdaterの設定
1. **キャラクターオブジェクトに追加**
2. **Animator参照を設定**
3. **StateSystem参照を設定**
4. **デバッグモード有効化（テスト時）**

#### BattleCharacterControllerでの使用
```csharp
public class BattleCharacterController : MonoBehaviour
{
    [SerializeField] private AnimationStateUpdater animationUpdater;
    
    // 攻撃実行時
    public void ExecuteWeakAttack(AttackDirection direction)
    {
        // システム処理
        attackSystem.ExecuteWeakAttack(direction);
        
        // アニメーション発火
        animationUpdater.TriggerAttack();
    }
    
    // 回避実行時
    public void ExecuteDodge(Vector3 direction)
    {
        // システム処理
        movementSystem.ExecuteDodge(direction);
        
        // アニメーション発火
        animationUpdater.TriggerDodge();
    }
}
```

### Step 10: テスト・調整

#### デバッグ機能活用
1. **AnimationStateUpdaterのDebugLog有効化**
2. **SRDebuggerでパラメータ監視**
3. **Inspector上でトリガーテスト**

#### パフォーマンス最適化
- **不要レイヤーのWeight = 0設定**
- **Update頻度の調整**
- **パラメータ更新の最適化**

### Step 11: アニメーションクリップ追加

#### 各ステートにMotion割り当て
1. **ステート選択**
2. **Inspector → Motion欄**
3. **対応するアニメーションクリップを設定**

#### 命名規則
```
武器種_攻撃種類_段数_方向
例: Sword_Weak_1_Up, Axe_Strong_1_Left
```

### Step 12: 最終チェック

#### 動作確認項目
- [ ] 基本移動アニメーション
- [ ] 戦闘モード切り替え
- [ ] 攻撃アニメーション（3方向）
- [ ] 防御アニメーション
- [ ] 回避アニメーション
- [ ] 空中戦闘アニメーション
- [ ] エネルギー切れ時の特殊アニメーション
- [ ] 被弾リアクション
- [ ] マニューバアニメーション

#### バランス調整
- **アニメーション速度調整**
- **トランジション時間調整**
- **ブレンドツリーの動作確認**

## 🔧 トラブルシューティング

### よくある問題と解決法

#### パラメータが更新されない
- AnimationStateUpdaterの参照確認
- StateSystemの動作確認
- Update()メソッドの実行確認

#### トランジションが動作しない
- 条件式の確認
- Has Exit Timeの設定確認
- パラメータ名のスペルチェック

#### アニメーションが滑らかでない
- Transition Durationの調整
- Interpolationの設定確認
- ブレンドツリーの座標確認

## 📚 参考情報

### 設計思想
- **4レイヤー構造**で複雑な戦闘システムを整理
- **StateSystem完全連携**でリアルタイム同期
- **拡張性重視**で新要素追加が容易

### 今後の拡張
- 新武器タイプのアニメーション追加
- AI個性別のアニメーション差別化
- エフェクト連携システムの強化

この手順書に従って実装することで、バトルシステム仕様書に完全対応したAnimatorControllerが完成します！