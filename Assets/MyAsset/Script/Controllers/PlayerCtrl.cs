using NaughtyAttributes;
using UnityEngine;

public class PlayerCtrl : MonoBehaviour
{
    [SerializeField]
    private float _rotateSpeed = 0.5f;

    [SerializeField]
    private float _jumpPower = 5f;

    public float speed = 5f;

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.S))
        {
            transform.position -= transform.forward * speed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.A))
        {
            transform.position -= transform.right * speed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.D))
        {
            transform.position += transform.right * speed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.LeftArrow) && Input.GetKey(KeyCode.RightArrow))
        {
            //同時押しはなにもしない
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Rotate(0, -_rotateSpeed * Time.deltaTime, 0);
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.Rotate(0, _rotateSpeed * Time.deltaTime, 0);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.AddForce(Vector3.up * _jumpPower, ForceMode.Impulse);
        }
    }
}
