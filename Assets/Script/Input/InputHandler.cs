using LearningAIGame.CombatSystem.Core;
using LearningAIGame.CombatSystem.Data;
using LearningAIGame.CombatSystem.Settings;
using LearningAIGame.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using static LearningAIGame.CombatSystem.Core.StateSystem;
using InputSettings = LearningAIGame.CombatSystem.Settings.InputSettings;

//==============================================ファイルヘッダ===========================================================
// InputHandler
// 
// 概要: プレイヤー入力を処理し、BattleCharacterControllerに命令を送るクラス
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// Unity Input Systemから入力を受け取り、適切な形式に変換してBattleCharacterControllerへ送信する。
// InputSettings(ScriptableObject)から設定を読み込み、カスタマイズ可能な入力処理を実現。
// ゲームパッドとキーボード/マウスの両方に対応し、キーコンフィグ可能。
// PSコントローラーのボタン配置に準拠した設計。
// 移動入力と方向入力で個別のデッドゾーン設定が可能。
// 
// 構え管理の設計思想:
// - InputHandlerは入力を「検出」するのみで、構えの「状態」は保持しない
// - Update()で方向入力を取得・変換し、一時保存
// - 攻撃などのアクション発生時は、その時点の方向入力を使用
// - LateUpdate()で方向変更をBattleCharacterControllerに送信
// - 実際の構え状態の管理はStateSystemが担当
// 
// 入力元: Unity Input System (PlayerInputActions)
// 出力先: BattleCharacterController
// 設定元: InputSettings (ScriptableObject)
// 
// ボタン割り当て:
// R1/左クリック: 弱攻撃
// R2/右クリック: 強攻撃  
// L1/Q: 強攻撃キャンセル
// L2/LeftShift: 回避
// □/スペース: ブロッキング
// 左スティック/WASD: 移動
// 右スティック/マウス: 攻撃・ガード方向切り替え
// 
// その他:
// InputSystemの自動生成コード(PlayerInputActions)に依存
// InputSettingsで入力パラメータを一元管理
//=====================================================================================================================

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// プレイヤー入力ハンドラー
    /// 責任範囲: 入力の受付、入力の変換、キャラクターコントローラーへの命令送信
    /// </summary>
    [RequireComponent(typeof(BattleCharacterController))]
    public class InputHandler : MonoBehaviour
    {
        #region フィールド定義

        /// <summary>
        /// 制御対象のバトルキャラクターコントローラー
        /// インスペクターから設定、未設定の場合は自動取得
        /// </summary>
        [Header("制御対象")]
        [Tooltip("制御するキャラクターコントローラー")]
        [SerializeField]
        private BattleCharacterController _characterController;

        /// <summary>
        /// 入力設定データ(ScriptableObject)
        /// すべての入力パラメータをここから取得
        /// </summary>
        [Header("入力設定")]
        [Tooltip("入力パラメータの設定ファイル")]
        [SerializeField]
        private InputSettings _inputSettings;

        /// <summary>
        /// Unity Input Systemの自動生成クラス
        /// 各種入力アクションを定義・管理
        /// </summary>
        private PlayerInputActions _inputActions;

        /// <summary>
        /// 左スティック/WASD からの移動入力
        /// 毎フレーム更新される
        /// </summary>
        private Vector2 _moveInput;

        /// <summary>
        /// 右スティック/マウス からの方向入力
        /// ガード・攻撃方向の指定に使用
        /// </summary>
        private Vector2 _directionInput;

        /// <summary>
        /// 現在フレームで計算された構え方向
        /// Update()で計算され、アクション発生時やLateUpdate()で使用される
        /// 注意: これは「入力から変換された方向」であり、「実際の構え状態」ではない
        /// 実際の構え状態はStateSystemが管理する
        /// </summary>
        private StanceType _currentFrameStance = StanceType.Up;

        /// <summary>
        /// 前フレームで送信した構え方向
        /// 変化検知のために保持
        /// </summary>
        private StanceType _lastFrameStance = StanceType.Up;

        /// <summary>
        /// 構えが変更された時刻
        /// 構え保持時間の計算に使用
        /// </summary>
        private float _stanceChangeTime;

        /// <summary>
        /// このフレームで方向変更入力があったか
        /// LateUpdate()で方向変更を送信するかの判定に使用
        /// </summary>
        private bool _hasDirectionInputThisFrame;

        #endregion

        #region Unityライフサイクル

        /// <summary>
        /// 初期化処理
        /// コンポーネントの参照取得とInput Actionの生成
        /// </summary>
        private void Awake()
        {
            // キャラクターコントローラーの参照取得
            if (_characterController == null)
            {
                _characterController = GetComponent<BattleCharacterController>();

                if (_characterController == null)
                {
                    Debug.LogError($"[InputHandler] BattleCharacterControllerが見つかりません: {gameObject.name}");
                }
            }

            // 入力設定の検証
            if (_inputSettings == null)
            {
                Debug.LogError($"[InputHandler] InputSettingsが設定されていません: {gameObject.name}");
            }

            // Input Actionインスタンスの生成
            _inputActions = new PlayerInputActions();
        }

        /// <summary>
        /// 有効化時の処理
        /// 入力イベントのサブスクライブとアクションマップの有効化
        /// </summary>
        private void OnEnable()
        {
            // ボタンイベントの登録
            // performed: ボタンが押された瞬間に発火
            _inputActions.Player.LightAttack.performed += OnLightAttackPerformed;
            _inputActions.Player.HeavyAttack.performed += OnHeavyAttackPerformed;
            _inputActions.Player.Blocking.performed += OnBlockingPerformed;
            _inputActions.Player.Dodge.performed += OnDodgePerformed;
            _inputActions.Player.HeavyCancel.performed += OnHeavyCancelPerformed;

            // Playerアクションマップを有効化
            // これにより入力の受付が開始される
            _inputActions.Player.Enable();
        }

        /// <summary>
        /// 無効化時の処理
        /// イベントの購読解除とアクションマップの無効化
        /// メモリリーク防止のため必須
        /// </summary>
        private void OnDisable()
        {
            // イベントの購読解除
            _inputActions.Player.LightAttack.performed -= OnLightAttackPerformed;
            _inputActions.Player.HeavyAttack.performed -= OnHeavyAttackPerformed;
            _inputActions.Player.Blocking.performed -= OnBlockingPerformed;
            _inputActions.Player.Dodge.performed -= OnDodgePerformed;
            _inputActions.Player.HeavyCancel.performed -= OnHeavyCancelPerformed;

            // アクションマップの無効化
            _inputActions.Player.Disable();
        }

        /// <summary>
        /// 毎フレーム実行
        /// 継続的な入力(移動、構え変更)の検出と変換
        /// 
        /// 処理の流れ:
        /// 1. 移動入力の処理・送信
        /// 2. 方向入力の検出・変換(構えとして一時保存)
        /// 3. アクションイベント発生時は、ここで計算された構えを使用
        /// </summary>
        private void Update()
        {
            // 設定が無効な場合は処理をスキップ
            if (_inputSettings == null)
                return;

            // フレーム開始時にフラグをリセット
            _hasDirectionInputThisFrame = false;

            // 移動入力の処理
            ProcessMovementInput();

            // 構え・方向変更入力の検出と変換
            ProcessDirectionInput();
        }

        /// <summary>
        /// 全ての更新処理が終わった後に実行
        /// 
        /// 処理の流れ:
        /// 1. このフレームで方向入力があったかチェック
        /// 2. 方向が変化していれば、BattleCharacterControllerに送信
        /// 3. BattleCharacterController側で構え変更可能かを判定
        /// </summary>
        private void LateUpdate()
        {
            // 設定が無効な場合は処理をスキップ
            if (_inputSettings == null)
                return;

            // このフレームで方向入力があり、かつ前フレームと異なる場合のみ送信
            if (_hasDirectionInputThisFrame && _currentFrameStance != _lastFrameStance)
            {
                // 構え保持時間チェック
                // 変更直後は一定時間構えの送信を抑制して、細かい揺れを防ぐ
                float timeSinceChange = Time.time - _stanceChangeTime;
                if (timeSinceChange >= _inputSettings.stanceHoldTime)
                {
                    // BattleCharacterControllerへ構え変更を送信
                    // 注意: ここでは「変更を試みる」だけで、実際に変更されるかは
                    // BattleCharacterController側で判定される
                    _characterController.GuardDirectionChange(_currentFrameStance);

                    _stanceChangeTime = Time.time;

                    // デバッグログ
                    if (_inputSettings.logInputs)
                    {
                        Debug.Log($"[InputHandler] 構え変更送信: {_currentFrameStance}");
                    }
                }
            }

            // 送信した構えを記録
            _lastFrameStance = _currentFrameStance;
        }

        #endregion

        #region 継続的入力処理

        /// <summary>
        /// 移動入力の処理
        /// 左スティック/WASDからの入力を読み取り、キャラクターコントローラーへ送信
        /// 移動専用のデッドゾーン処理を適用
        /// </summary>
        private void ProcessMovementInput()
        {
            // 移動入力値の取得(Vector2)
            Vector2 rawInput = _inputActions.Player.Move.ReadValue<Vector2>();

            // 移動専用のデッドゾーン処理
            // スティックのわずかな傾きを無視して誤入力を防ぐ
            float moveDeadzone = _inputSettings.MoveStickDeadzone;
            if (rawInput.sqrMagnitude < moveDeadzone * moveDeadzone)
            {
                _moveInput = Vector2.zero;
            }
            else
            {
                // デッドゾーン外の入力を正規化し、移動感度を適用
                _moveInput = rawInput.normalized * rawInput.magnitude;
                _moveInput *= _inputSettings.moveStickSensitivity;
            }

            // 2D入力を3D空間の移動ベクトルに変換
            // X: 左右、Z: 前後、Y: 高さ(移動では使用しない)
            Vector3 moveDirection = new Vector3(_moveInput.x, 0, _moveInput.y);

            // キャラクターコントローラーへ移動命令を送信
            _characterController.MoveAct(moveDirection);
        }

        /// <summary>
        /// 方向入力の検出と変換
        /// 右スティック/マウスからの入力を構え(StanceType)に変換
        /// 方向入力専用のデッドゾーンを使用
        /// 
        /// 重要: ここでは入力を「検出・変換」するのみで、「送信」はしない
        /// 送信はLateUpdate()で行われる
        /// ただし、変換結果は_currentFrameStanceに保存され、
        /// アクションイベント発生時に即座に使用可能
        /// </summary>
        private void ProcessDirectionInput()
        {
            // 方向入力値の取得
            _directionInput = _inputActions.Player.DirectionChange.ReadValue<Vector2>();

            // マウス入力の判定と感度調整
            // マウスは相対移動量(delta)を返すため、感度で調整が必要
            bool isMouseInput = Gamepad.current == null &&
                                 Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f;

            if (isMouseInput)
            {
                // マウス入力には感度を適用
                _directionInput *= _inputSettings.mouseSensitivity;

                // Y軸反転設定
                if (_inputSettings.invertMouseY)
                {
                    _directionInput.y = -_directionInput.y;
                }
            }
            else
            {
                // スティック入力には方向入力専用の感度を適用
                _directionInput *= _inputSettings.directionStickSensitivity;
            }

            // 方向入力専用のデッドゾーン処理
            // 構え変更の誤入力を防ぐため、移動とは別のデッドゾーンを使用
            float directionDeadzone = _inputSettings.DirectionStickDeadzone;
            float inputMagnitude = _directionInput.sqrMagnitude;
            float deadzoneThreshold = directionDeadzone * directionDeadzone;

            // デッドゾーン内の入力は無視
            if (inputMagnitude <= deadzoneThreshold)
            {
                return;
            }

            // 構え変更閾値のチェック
            // デッドゾーンを超えていても、閾値未満なら構えを変更しない
            float threshold = _inputSettings.stanceChangeThreshold;
            threshold *= threshold; // 二乗比較で高速化

            if (inputMagnitude > threshold)
            {
                // 入力ベクトルから構えを計算
                _currentFrameStance = ConvertVectorToStance(_directionInput);

                // このフレームで方向入力があったことを記録
                _hasDirectionInputThisFrame = true;
            }
        }

        #endregion

        #region ボタン入力イベントハンドラ

        /// <summary>
        /// 弱攻撃が押された時の処理
        /// R1/右クリック → 弱攻撃
        /// 
        /// 注意: ここで使用する構えは、Update()で計算された_currentFrameStanceであり、
        /// StateSystemが管理する「実際の構え状態」とは異なる可能性がある
        /// これにより、「構えを変えながら攻撃」という操作が可能になる
        /// </summary>
        /// <param name="context">Input Systemのコールバック情報</param>
        private void OnLightAttackPerformed(InputAction.CallbackContext context)
        {
            // 現在フレームで計算された構え方向で弱攻撃を実行
            _characterController.LightAttackAct(_currentFrameStance).Forget();

            // デバッグログ
            if (_inputSettings.logInputs)
            {
                Debug.Log($"[InputHandler] 弱攻撃実行: {_currentFrameStance}");
            }
        }

        /// <summary>
        /// 攻撃ボタンが押された時の処理
        /// R2/左クリック → 強攻撃
        /// 
        /// 注意: ここで使用する構えは、Update()で計算された_currentFrameStanceであり、
        /// StateSystemが管理する「実際の構え状態」とは異なる可能性がある
        /// これにより、「構えを変えながら攻撃」という操作が可能になる
        /// </summary>
        /// <param name="context">Input Systemのコールバック情報</param>
        private void OnHeavyAttackPerformed(InputAction.CallbackContext context)
        {
            // 現在フレームで計算された構え方向で強攻撃を実行
            _characterController.HeavyAttackAct(_currentFrameStance).Forget();

            // デバッグログ
            if (_inputSettings.logInputs)
            {
                Debug.Log($"[InputHandler] 強攻撃実行: {_currentFrameStance}");
            }
        }

        /// <summary>
        /// ブロッキングボタンが押された時の処理
        /// ○/スペース → ブロッキング(タイミング重視の防御)
        /// 
        /// ブロッキング: 相手の攻撃タイミングに合わせて発動する防御技
        /// 成功すると相手の攻撃を弾き、有利な状況を作る
        /// 
        /// 注意: 現在フレームの方向入力を使用するため、
        /// 「構えを変えながらブロッキング」が可能
        /// </summary>
        /// <param name="context">Input Systemのコールバック情報</param>
        private void OnBlockingPerformed(InputAction.CallbackContext context)
        {
            // 現在フレームで計算された構え方向でブロッキングを実行
            _characterController.BlockingAct(_currentFrameStance);

            // デバッグログ
            if (_inputSettings.logInputs)
            {
                Debug.Log($"[InputHandler] ブロッキング実行: {_currentFrameStance}");
            }
        }

        /// <summary>
        /// 回避ボタンが押された時の処理
        /// L2/Shift → 回避(無敵フレーム付き移動)
        /// 
        /// 移動入力の方向に応じて回避方向が決まる:
        /// - 前方入力: 前方回避
        /// - 後方入力: バックステップ
        /// - 左右入力: 横回避
        /// - 入力なし: バックステップ(デフォルト)
        /// </summary>
        /// <param name="context">Input Systemのコールバック情報</param>
        private void OnDodgePerformed(InputAction.CallbackContext context)
        {
            // 現在の移動入力から回避タイプを決定
            MovementReportType dodgeType = ConvertVectorToMovementReport(_moveInput);

            // キャラクターコントローラーへ回避命令を送信
            _characterController.AvoidAct(dodgeType);
        }

        /// <summary>
        /// 強攻撃キャンセルボタンが押された時の処理
        /// L1/Q → 強攻撃キャンセル
        /// 
        /// 強攻撃は出が遅いが、キャンセル可能期間中であれば
        /// このボタンで中断してエネルギーを節約できる
        /// フェイント(見せかけ攻撃)としても使える
        /// </summary>
        /// <param name="context">Input Systemのコールバック情報</param>
        private void OnHeavyCancelPerformed(InputAction.CallbackContext context)
        {
            // キャラクターコントローラーへキャンセル命令を送信
            // 内部で実行可能かどうかの判定が行われる
            _characterController.HeavyAttackCancel();
        }

        #endregion

        #region 入力変換ヘルパーメソッド

        /// <summary>
        /// Vector2の入力を3方向の構え(StanceType)に変換
        /// 
        /// 変換ルール:
        /// - Y軸正方向(上): StanceType.Up
        /// - X軸負方向(左): StanceType.Left  
        /// - X軸正方向(右): StanceType.Right
        /// 
        /// 優先順位: 上 > 左右
        /// 斜め入力の場合は、より強い方向を採用
        /// 
        /// 注意: このメソッドは入力の「変換」のみを行い、
        /// 「現在の構え状態」は参照しない
        /// </summary>
        /// <param name="input">スティック/マウスからの入力ベクトル</param>
        /// <returns>変換された構えタイプ</returns>
        private StanceType ConvertVectorToStance(Vector2 input)
        {
            // 設定から角度閾値を取得(cos値で比較)
            float angleThreshold = _inputSettings.StanceAngleCos;

            // ベクトルを正規化
            Vector2 normalized = input.normalized;

            // Y軸(上方向)が優先
            // 上方向の成分が閾値以上ならUp
            if (normalized.y > angleThreshold)
            {
                return StanceType.Up;
            }
            // X軸(左右)の判定
            // 左右方向の成分が閾値以上なら、その方向
            else if (Mathf.Abs(normalized.x) > angleThreshold)
            {
                return normalized.x < 0 ? StanceType.Left : StanceType.Right;
            }

            // どの方向にも十分に傾いていない場合は、前フレームの値を維持
            // これにより、わずかな入力での構え変更を防ぐ
            return _currentFrameStance;
        }

        /// <summary>
        /// Vector2の移動入力を4方向の移動タイプ(MovementReportType)に変換
        /// 回避方向の決定に使用
        /// 
        /// 変換ルール:
        /// - 前方: FrontStep
        /// - 後方: BackStep
        /// - 左: LeftStep
        /// - 右: RightStep
        /// - 入力なし: BackStep(デフォルト)
        /// 
        /// 優先順位: X軸(左右) > Y軸(前後)
        /// これにより、斜め入力時は横方向が優先される
        /// </summary>
        /// <param name="input">移動スティックからの入力ベクトル</param>
        /// <returns>変換された移動タイプ</returns>
        private MovementReportType ConvertVectorToMovementReport(Vector2 input)
        {
            // 入力がほとんどない場合はバックステップ
            // 緊急回避のデフォルト動作
            if (input.sqrMagnitude < 0.01f)
            {
                return MovementReportType.BackStep;
            }

            // X軸(横)とY軸(縦)の入力の強さを比較
            // 横方向の入力が強い場合
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                return input.x > 0 ? MovementReportType.RightStep : MovementReportType.LeftStep;
            }
            // 縦方向の入力が強い(または同じ)場合
            else
            {
                return input.y > 0 ? MovementReportType.FrontStep : MovementReportType.BackStep;
            }
        }

        #endregion

        #region デバッグ機能

#if UNITY_EDITOR
        /// <summary>
        /// エディタ上でのデバッグ情報表示
        /// Inspector上でリアルタイムの入力状態を確認可能
        /// </summary>
        private void OnGUI()
        {
            // 設定でデバッグ表示が無効なら何もしない
            if (_inputSettings == null || !_inputSettings.showInputDebug)
                return;

            // 表示位置を設定から取得
            Vector2 pos = _inputSettings.debugDisplayPosition;
            Rect area = new Rect(pos.x, pos.y, 400, 320);

            // 背景を描画
            GUI.Box(area, "");

            // 情報表示開始
            GUILayout.BeginArea(area);

            GUILayout.Label("=== 入力デバッグ情報 ===", new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14
            });

            GUILayout.Space(5);

            // 移動入力
            GUILayout.Label($"移動入力: ({_moveInput.x:F2}, {_moveInput.y:F2})");

            // 方向入力
            GUILayout.Label($"方向入力: ({_directionInput.x:F2}, {_directionInput.y:F2})");

            // 現在フレームの構え(入力から計算された値)
            GUILayout.Label($"現在フレーム構え: {_currentFrameStance}");

            // 最後に送信した構え
            GUILayout.Label($"最後送信構え: {_lastFrameStance}");

            // 方向入力フラグ
            if (_hasDirectionInputThisFrame)
            {
                GUILayout.Label("方向入力: あり", new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = Color.green }
                });
            }

            GUILayout.Space(10);

            // 設定情報
            GUILayout.Label("--- 現在の設定 ---", new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold
            });
            GUILayout.Label($"設定名: {_inputSettings.settingName}");
            GUILayout.Label($"マウス感度: {_inputSettings.mouseSensitivity:F3}");
            GUILayout.Label($"移動感度: {_inputSettings.moveStickSensitivity:F2}");
            GUILayout.Label($"方向感度: {_inputSettings.directionStickSensitivity:F2}");
            GUILayout.Label($"移動デッドゾーン: {_inputSettings.MoveStickDeadzone:F3}");
            GUILayout.Label($"方向デッドゾーン: {_inputSettings.DirectionStickDeadzone:F3}");
            GUILayout.Label($"構え閾値: {_inputSettings.stanceChangeThreshold:F2}");

            GUILayout.EndArea();
        }
#endif

        #endregion

        #region ユーティリティメソッド

        /// <summary>
        /// 入力設定を変更
        /// ランタイムで設定を切り替えたい場合に使用
        /// </summary>
        /// <param name="newSettings">新しい設定</param>
        public void ChangeInputSettings(InputSettings newSettings)
        {
            if (newSettings == null)
            {
                Debug.LogWarning("[InputHandler] nullの設定は適用できません");
                return;
            }

            _inputSettings = newSettings;
            Debug.Log($"[InputHandler] 入力設定を変更: {newSettings.settingName}");
        }

        /// <summary>
        /// 現在の入力設定を取得
        /// </summary>
        /// <returns>現在のInputSettings</returns>
        public InputSettings GetCurrentSettings()
        {
            return _inputSettings;
        }

        /// <summary>
        /// 現在フレームで計算された構えを取得(デバッグ用)
        /// 注意: これは入力から変換された値であり、StateSystemが管理する実際の構え状態ではない
        ///       =切り替え不能状態の場合、実際の値には反映されない
        /// </summary>
        /// <returns>現在フレームの構え</returns>
        public StanceType GetCurrentFrameStance()
        {
            return _currentFrameStance;
        }

        #endregion
    }
}