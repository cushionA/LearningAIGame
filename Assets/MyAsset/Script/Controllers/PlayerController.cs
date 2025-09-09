using System.Runtime.CompilerServices;
using UnityEngine;
using Sirenix.OdinInspector;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// プレイヤーコントローラー - 入力に基づいてキャラクターを制御
    /// </summary>
    public class PlayerController : BattleCharacterController
    {
        [Title("入力設定")]
        [PropertyTooltip("操作支援を有効にするかどうか")]
        [SerializeField] private bool enableAutoAim = true;

        [PropertyTooltip("入力バッファを有効にするかどうか")]
        [SerializeField] private bool enableInputBuffer = true;

        [PropertyTooltip("入力の感度設定")]
        [Range(0.1f, 3f)]
        [SerializeField] private float inputSensitivity = 1f;

        [Title("操作状態")]
        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在の移動入力")]
        public Vector2 CurrentMoveInput { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在の攻撃方向入力")]
        public Vector2 CurrentAttackInput { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在選択中のスキル")]
        public int SelectedSkillIndex { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = 0;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在選択中のマニューバ")]
        public int SelectedManeuverIndex { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = 0;

        // 入力状態
        private bool isJumpCharging = false;
        private bool wasBoostPressed = false;

        /// <summary>
        /// 入力データ構造
        /// </summary>
        public struct InputData
        {
            public Vector2 movementVector;
            public Vector2 attackDirection;
            public bool jumpPressed;
            public bool jumpCharged;
            public bool weakAttackPressed;
            public bool strongAttackPressed;
            public bool skillPressed;
            public bool guardHeld;
            public bool blockPressed;
            public bool dodgePressed;
            public bool boostHeld;
            public bool modeSwitchPressed;
            public bool maneuverPressed;
            public bool quickTurnPressed;
        }

        /// <summary>
        /// 次の行動を決定（入力ベース）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void DecideNextAction()
        {
            var input = GetCurrentInput();
            
            // 右スティック入力による方向制御
            directionSystem.UpdateDirectionFromStick(input.attackDirection);
            
            ProcessMovementInput(input);
            ProcessAttackInput(input);
            ProcessDefenseInput(input);
            ProcessSpecialInput(input);
        }

        /// <summary>
        /// 現在の入力を取得
        /// </summary>
        /// <returns>入力データ</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private InputData GetCurrentInput()
        {
            return new InputData
            {
                movementVector = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")) * inputSensitivity,
                attackDirection = new Vector2(Input.GetAxis("AttackHorizontal"), Input.GetAxis("AttackVertical")),
                jumpPressed = Input.GetButtonDown("Jump"),
                jumpCharged = Input.GetButton("Jump") && isJumpCharging,
                weakAttackPressed = Input.GetButtonDown("WeakAttack"),
                strongAttackPressed = Input.GetButtonDown("StrongAttack"),
                skillPressed = Input.GetButtonDown("Skill"),
                guardHeld = Input.GetButton("Guard"),
                blockPressed = Input.GetButtonDown("Block"),
                dodgePressed = Input.GetButtonDown("Dodge"),
                boostHeld = Input.GetButton("Boost"),
                modeSwitchPressed = Input.GetButtonDown("ModeSwitch"),
                maneuverPressed = Input.GetButtonDown("Maneuver"),
                quickTurnPressed = Input.GetButtonDown("QuickTurn")
            };
        }

        /// <summary>
        /// 移動入力の処理
        /// </summary>
        /// <param name="input">入力データ</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessMovementInput(InputData input)
        {
            CurrentMoveInput = input.movementVector;

            // 移動処理
            if (input.movementVector.magnitude > 0.1f)
            {
                Vector3 moveDirection = new Vector3(input.movementVector.x, 0, input.movementVector.y);
                
                if (input.boostHeld && !wasBoostPressed)
                {
                    ExecuteBoost(moveDirection);
                    wasBoostPressed = true;
                }
                else if (!input.boostHeld && wasBoostPressed)
                {
                    movementSystem.StopBoost();
                    wasBoostPressed = false;
                }
                else if (!input.boostHeld)
                {
                    ExecuteMovement(moveDirection);
                }
            }
            else if (wasBoostPressed)
            {
                movementSystem.StopBoost();
                wasBoostPressed = false;
            }

            // ジャンプ処理
            if (input.jumpPressed && !isJumpCharging)
            {
                movementSystem.StartJumpCharge();
                isJumpCharging = true;
            }
            else if (!Input.GetButton("Jump") && isJumpCharging)
            {
                movementSystem.ReleaseJumpCharge();
                isJumpCharging = false;
            }

            // 回避処理
            if (input.dodgePressed)
            {
                Vector3 dodgeDirection = input.movementVector.magnitude > 0.1f ? 
                    new Vector3(input.movementVector.x, 0, input.movementVector.y) : Vector3.zero;
                ExecuteDodge(dodgeDirection);
            }
        }

        /// <summary>
        /// 攻撃入力の処理
        /// </summary>
        /// <param name="input">入力データ</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessAttackInput(InputData input)
        {
            CurrentAttackInput = input.attackDirection;

            // DirectionSystemから現在の方向を取得
            AttackDirection direction = enableAutoAim ? 
                GetOptimalAttackDirection() : 
                directionSystem.CurrentDirection;

            if (stateSystem.CurrentActionMode == ActionMode.Melee)
            {
                // 近接攻撃
                if (input.weakAttackPressed)
                {
                    ExecuteWeakAttack(direction);
                }
                else if (input.strongAttackPressed)
                {
                    ExecuteStrongAttack(direction);
                }
                else if (input.skillPressed)
                {
                    ExecuteSkill(SelectedSkillIndex);
                }
            }
            else if (stateSystem.CurrentActionMode == ActionMode.Ranged)
            {
                // 射撃攻撃
                if (input.weakAttackPressed)
                {
                    attackSystem.ExecuteWeakShoot(direction);
                }
                else if (input.strongAttackPressed)
                {
                    attackSystem.ExecuteStrongShoot(direction);
                }
                else if (input.skillPressed)
                {
                    attackSystem.ExecuteShootSkill(SelectedSkillIndex);
                }

                // 狙い処理（方向はDirectionSystemが管理）
                if (input.attackDirection.magnitude > 0.1f)
                {
                    Vector3 aimDirection = new Vector3(input.attackDirection.x, 0, input.attackDirection.y);
                    attackSystem.StartAiming(aimDirection);
                }
                else
                {
                    attackSystem.StopAiming();
                }
            }
        }

        /// <summary>
        /// 防御入力の処理
        /// </summary>
        /// <param name="input">入力データ</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessDefenseInput(InputData input)
        {
            if (stateSystem.CurrentActionMode != ActionMode.Melee)
                return;

            AttackDirection guardDirection = enableAutoAim ?
                OpponentData.CurrentDirection :
                directionSystem.CurrentDirection;

            if (input.guardHeld)
            {
                ExecuteGuard(guardDirection);
            }
            else
            {
                defenseSystem.StopGuard();
            }

            if (input.blockPressed)
            {
                // ブロッキングは自動エイムなし（タイミングが重要）
                AttackDirection blockDirection = directionSystem.CurrentDirection;
                ExecuteBlock(blockDirection);
            }
        }

        /// <summary>
        /// 特殊入力の処理
        /// </summary>
        /// <param name="input">入力データ</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessSpecialInput(InputData input)
        {
            if (input.modeSwitchPressed)
            {
                SwitchCombatMode();
            }

            if (input.maneuverPressed)
            {
                ExecuteManeuver(SelectedManeuverIndex);
            }

            if (input.quickTurnPressed)
            {
                ExecuteQuickTurn();
            }

            // スキル・マニューバ選択
            if (Input.GetKeyDown(KeyCode.Q))
            {
                SelectedSkillIndex = (SelectedSkillIndex + 1) % 5;
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                SelectedManeuverIndex = (SelectedManeuverIndex + 1) % 3;
            }
        }

        /// <summary>
        /// 対戦相手の状態変化時の処理
        /// </summary>
        /// <param name="newState">新しい状態</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void OnOpponentStateChanged(ActionState newState)
        {
            if (enableAutoAim && newState == ActionState.Attacking)
            {
                // 攻撃検知時の自動防御提案（UI表示など）
                Debug.Log("敵が攻撃中 - 防御推奨");
            }
        }

        /// <summary>
        /// 体力変化時の処理
        /// </summary>
        /// <param name="newHealthPercentage">新しい体力割合</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void OnHealthChanged(float newHealthPercentage)
        {
            if (newHealthPercentage < 0.3f)
            {
                Debug.Log("体力低下 - 注意が必要");
            }
        }

        /// <summary>
        /// エネルギー変化時の処理
        /// </summary>
        /// <param name="newEnergyPercentage">新しいエネルギー割合</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void OnEnergyChanged(float newEnergyPercentage)
        {
            if (newEnergyPercentage < 0.2f)
            {
                Debug.Log("エネルギー低下 - エネルギー管理に注意");
            }
        }

        #region Debug Methods

        [Title("デバッグ機能")]
        [Button("自動エイム切替", ButtonSizes.Medium)]
        [GUIColor(0.8f, 1f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugToggleAutoAim()
        {
            enableAutoAim = !enableAutoAim;
            Debug.Log($"自動エイム: {(enableAutoAim ? "ON" : "OFF")}");
        }

        [Button("スキル切替", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.8f, 1f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugSwitchSkill()
        {
            SelectedSkillIndex = (SelectedSkillIndex + 1) % 5;
            Debug.Log($"選択スキル: {SelectedSkillIndex}");
        }

        [Button("マニューバ切替", ButtonSizes.Medium)]
        [GUIColor(1f, 0.8f, 1f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugSwitchManeuver()
        {
            SelectedManeuverIndex = (SelectedManeuverIndex + 1) % 3;
            Debug.Log($"選択マニューバ: {SelectedManeuverIndex}");
        }

        #endregion

        #region SRDebugger Integration

        [System.ComponentModel.Category("SRDebugger - プレイヤー")]
        public bool DebugAutoAim
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => enableAutoAim;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => enableAutoAim = value;
        }

        [System.ComponentModel.Category("SRDebugger - プレイヤー")]
        public float DebugInputSensitivity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => inputSensitivity;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => inputSensitivity = Mathf.Clamp(value, 0.1f, 3f);
        }

        [System.ComponentModel.Category("SRDebugger - プレイヤー")]
        public int DebugSelectedSkill
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => SelectedSkillIndex;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SelectedSkillIndex = Mathf.Clamp(value, 0, 4);
        }

        [System.ComponentModel.Category("SRDebugger - プレイヤー")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DebugToggleAutoAimSR() => DebugToggleAutoAim();

        #endregion
    }
}
