//using System.Runtime.CompilerServices;
//using UnityEngine;

//namespace LearningAIGame.CombatSystem
//{
//    public class InputData
//    {
//        public Vector2 movementVector;    // 移動入力ベクトル
//        public Vector2 attackDirection;   // 攻撃方向入力ベクトル
//        public bool jumpPressed;          // ジャンプボタンが押されたか
//        public bool jumpCharged;          // ジャンプボタンが押され続けているか
//        public bool weakAttackPressed;    // 弱攻撃ボタンが押されたか
//        public bool strongAttackPressed;  // 強攻撃ボタンが押されたか
//        public bool skillPressed;         // スキルボタンが押されたか
//        public bool guardHeld;            // ガードボタンが押され続けているか
//        public bool blockPressed;         // ブロックボタンが押されたか
//        public bool dodgePressed;         // 回避ボタンが押されたか
//        public bool boostHeld;            // ブーストボタンが押され続けているか
//        public bool modeSwitchPressed;    // モード切替ボタンが押されたか
//        public bool maneuverPressed;      // マニューバボタンが押されたか
//        public bool quickTurnPressed;     // クイックターンボタンが押されたか
//    }

//    /// <summary>
//    /// プレイヤーコントローラー - 入力に基づいてキャラクターを制御
//    /// </summary>
//    public class PlayerController : BattleCharacterController
//    {
//        [Header("入力設定")]
//        [Tooltip("操作支援を有効にするかどうか")]
//        [SerializeField] private bool _enableAutoAim = true;

//        [Tooltip("入力バッファを有効にするかどうか")]
//        [SerializeField] private bool _enableInputBuffer = true;

//        [Tooltip("入力の感度設定")]
//        [Range(0.1f, 3f)]
//        [SerializeField] private float _inputSensitivity = 1f;

//        [Header("操作状態")]
//        [SerializeField, ReadOnly]
//        [Tooltip("現在の移動入力")]
//        public Vector2 CurrentMoveInput { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }

//        [SerializeField, ReadOnly]
//        [Tooltip("現在の攻撃方向入力")]
//        public Vector2 CurrentAttackInput { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }

//        [SerializeField, ReadOnly]
//        [Tooltip("現在選択中のスキル")]
//        public int SelectedSkillIndex { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = 0;

//        [SerializeField, ReadOnly]
//        [Tooltip("現在選択中のマニューバ")]
//        public int SelectedManeuverIndex { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = 0;

//        // 入力状態
//        private bool _isJumpCharging = false;
//        private bool _wasBoostPressed = false;

//        /// <summary>
//        /// 次の行動を決定（入力ベース）
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        protected override void DecideNextAction()
//        {
//            var input = GetCurrentInput();

//            // 右スティック入力による方向制御
//            directionSystem.UpdateDirectionFromStick(input.attackDirection);

//            ProcessMovementInput(input);
//            ProcessAttackInput(input);
//            ProcessDefenseInput(input);
//            ProcessSpecialInput(input);
//        }

//        /// <summary>
//        /// 現在の入力を取得
//        /// </summary>
//        /// <returns>入力データ</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private InputData GetCurrentInput()
//        {
//            return new InputData
//            {
//                movementVector = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")) * _inputSensitivity,
//                attackDirection = new Vector2(Input.GetAxis("AttackHorizontal"), Input.GetAxis("AttackVertical")),
//                jumpPressed = Input.GetButtonDown("Jump"),
//                jumpCharged = Input.GetButton("Jump") && _isJumpCharging,
//                weakAttackPressed = Input.GetButtonDown("WeakAttack"),
//                strongAttackPressed = Input.GetButtonDown("StrongAttack"),
//                skillPressed = Input.GetButtonDown("Skill"),
//                guardHeld = Input.GetButton("Guard"),
//                blockPressed = Input.GetButtonDown("Block"),
//                dodgePressed = Input.GetButtonDown("Dodge"),
//                boostHeld = Input.GetButton("Boost"),
//                modeSwitchPressed = Input.GetButtonDown("ModeSwitch"),
//                maneuverPressed = Input.GetButtonDown("Maneuver"),
//                quickTurnPressed = Input.GetButtonDown("QuickTurn")
//            };
//        }

//        /// <summary>
//        /// 移動入力の処理
//        /// </summary>
//        /// <param name="input">入力データ</param>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private void ProcessMovementInput(InputData input)
//        {
//            CurrentMoveInput = input.movementVector;

//            // 移動処理
//            if (input.movementVector.magnitude > 0.1f)
//            {
//                Vector3 moveDirection = new Vector3(input.movementVector.x, 0, input.movementVector.y);

//                if (input.boostHeld && !_wasBoostPressed)
//                {
//                    ExecuteBoost(moveDirection);
//                    _wasBoostPressed = true;
//                }
//                else if (!input.boostHeld && _wasBoostPressed)
//                {
//                    //    movementSystem.StopBoost();
//                    _wasBoostPressed = false;
//                }
//                else if (!input.boostHeld)
//                {
//                    ExecuteMovement(moveDirection);
//                }
//            }
//            else if (_wasBoostPressed)
//            {
//                //     movementSystem.StopBoost();
//                _wasBoostPressed = false;
//            }

//        }

//        /// <summary>
//        /// 攻撃入力の処理
//        /// </summary>
//        /// <param name="input">入力データ</param>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private void ProcessAttackInput(InputData input)
//        {
//            CurrentAttackInput = input.attackDirection;

//            // DirectionSystemから現在の方向を取得
//            AttackDirection direction = _enableAutoAim ?
//                GetOptimalAttackDirection() :
//                directionSystem.CurrentDirection;

//            if (stateSystem.CurrentActionMode == ActionMode.Melee)
//            {
//                // 近接攻撃
//                if (input.weakAttackPressed)
//                {
//                    ExecuteWeakAttack(direction);
//                }
//                else if (input.strongAttackPressed)
//                {
//                    ExecuteStrongAttack(direction);
//                }
//                else if (input.skillPressed)
//                {
//                    ExecuteSkill(SelectedSkillIndex);
//                }
//            }
//            else if (stateSystem.CurrentActionMode == ActionMode.Ranged)
//            {
//                // 射撃攻撃
//                if (input.weakAttackPressed)
//                {
//                    attackSystem.ExecuteWeakShoot(direction);
//                }
//                else if (input.strongAttackPressed)
//                {
//                    attackSystem.ExecuteStrongShoot(direction);
//                }
//                else if (input.skillPressed)
//                {
//                    attackSystem.ExecuteShootSkill(SelectedSkillIndex);
//                }

//                // 狙い処理（方向はDirectionSystemが管理）
//                if (input.attackDirection.magnitude > 0.1f)
//                {
//                    Vector3 aimDirection = new Vector3(input.attackDirection.x, 0, input.attackDirection.y);
//                    attackSystem.StartAiming(aimDirection);
//                }
//                else
//                {
//                    attackSystem.StopAiming();
//                }
//            }
//        }

//        /// <summary>
//        /// 防御入力の処理
//        /// </summary>
//        /// <param name="input">入力データ</param>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private void ProcessDefenseInput(InputData input)
//        {
//            if (stateSystem.CurrentActionMode != ActionMode.Melee)
//                return;

//            AttackDirection guardDirection = _enableAutoAim ?
//                OpponentData.CurrentDirection :
//                directionSystem.CurrentDirection;

//            if (input.guardHeld)
//            {
//                ExecuteGuard(guardDirection);
//            }
//            else
//            {
//                defenseSystem.StopGuard();
//            }

//            if (input.blockPressed)
//            {
//                // ブロッキングは自動エイムなし（タイミングが重要）
//                AttackDirection blockDirection = directionSystem.CurrentDirection;
//                ExecuteBlock(blockDirection);
//            }
//        }

//        /// <summary>
//        /// 特殊入力の処理
//        /// </summary>
//        /// <param name="input">入力データ</param>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private void ProcessSpecialInput(InputData input)
//        {
//            if (input.modeSwitchPressed)
//            {
//                SwitchCombatMode();
//            }

//            if (input.maneuverPressed)
//            {
//                ExecuteManeuver(SelectedManeuverIndex);
//            }

//            if (input.quickTurnPressed)
//            {
//                ExecuteQuickTurn();
//            }

//            // スキル・マニューバ選択
//            if (Input.GetKeyDown(KeyCode.Q))
//            {
//                SelectedSkillIndex = (SelectedSkillIndex + 1) % 5;
//            }
//            if (Input.GetKeyDown(KeyCode.E))
//            {
//                SelectedManeuverIndex = (SelectedManeuverIndex + 1) % 3;
//            }
//        }

//        /// <summary>
//        /// 対戦相手の状態変化時の処理
//        /// </summary>
//        /// <param name="newState">新しい状態</param>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        protected override void OnOpponentStateChanged(ActionState newState)
//        {
//            if (_enableAutoAim && newState == ActionState.Attacking)
//            {
//                // 攻撃検知時の自動防御提案（UI表示など）
//                Debug.Log("敵が攻撃中 - 防御推奨");
//            }
//        }

//        /// <summary>
//        /// 体力変化時の処理
//        /// </summary>
//        /// <param name="newHealthPercentage">新しい体力割合</param>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        protected override void OnHealthChanged(float newHealthPercentage)
//        {
//            if (newHealthPercentage < 0.3f)
//            {
//                Debug.Log("体力低下 - 注意が必要");
//            }
//        }

//        /// <summary>
//        /// エネルギー変化時の処理
//        /// </summary>
//        /// <param name="newEnergyPercentage">新しいエネルギー割合</param>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        protected override void OnEnergyChanged(float newEnergyPercentage)
//        {
//            if (newEnergyPercentage < 0.2f)
//            {
//                Debug.Log("エネルギー低下 - エネルギー管理に注意");
//            }
//        }

//    }
//}
