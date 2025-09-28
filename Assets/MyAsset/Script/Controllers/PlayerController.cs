using R3;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private float _rotateSpeed = 200f;

    [SerializeField]
    private float _jumpPower = 5f;

    [SerializeField]
    GroundChecker _groundChecker;

    public float speed = 5f;

    private Rigidbody _rb;
    
    private CompositeDisposable _disposables;

    [SerializeField] private InputAction _moveAction;   // Vector2 (WASD/Stick)
    [SerializeField] private InputAction _lookAction;   // Axis (左右回転)
    [SerializeField] private InputAction _jumpAction;   // Button (Space)

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _disposables = new CompositeDisposable();
    }

    private void Start()
    {
        this._groundChecker.IsGround.Subscribe(value =>
        {
            // TODO: ログをserviceに送ってLLMのプロンプトに変更する
            Debug.Log("[PlayerController] IsGround: " + value);
        }).AddTo(_disposables);
    }

    private void OnEnable()
    {
        _moveAction.Enable();
        _lookAction.Enable();
        _jumpAction.Enable();
    }

    private void OnDisable()
    {
        _moveAction.Disable();
        _lookAction.Disable();
        _jumpAction.Disable();
    }

    private void Update()
    {
        // 1) 入力を毎フレーム取得（ポーリング）
        Vector2 move = _moveAction.ReadValue<Vector2>();   // x: 左右, y: 前後
        float look = _lookAction.ReadValue<float>();       // -1..1

        // 前後左右の入力がある時だけ移動
        if (move != Vector2.zero)
        {
            transform.position += transform.TransformDirection(new Vector3(move.x, 0f, move.y)) * speed * Time.deltaTime;
        }

        // 左右回転（lookが0でなければ回す）
        if (Mathf.Abs(look) > 0.0001f)
        {
            transform.Rotate(0, look * _rotateSpeed * Time.deltaTime, 0);
        }

        // ジャンプは「このフレームで押されたか」をifで判定
        if (_jumpAction.WasPressedThisFrame())
        {
            if (_groundChecker.IsGround.Value)
            {
                // 連続ジャンプ対策でY速度をリセット
                Vector3 v = _rb.linearVelocity;
                v.y = 0f;
                _rb.linearVelocity = v;

                _rb.AddForce(Vector3.up * _jumpPower, ForceMode.Impulse);
            }
        }
    }
    
    private void OnDestroy()
    {
        _disposables?.Dispose();
    }
}
