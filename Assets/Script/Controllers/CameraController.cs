using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
#region インスペクター変数

 #region 入力値
        public Vector2 look;
    
        public bool lockOn = false;
    
    KeyCode strafeInput = KeyCode.Tab;
    PlayerInput _playerInput;

    #endregion


#region  カメラ制御値
  [Header("カメラ追従値")]
        public Transform targetTransform;

        public Transform pivot;

        public float followSpeed = 0.1f, rotateSpeed = 1;
        float lookAngle; 
        float pivotAngle;


  // CameraRange
  [Header("カメラ範囲値")]
        public float TopClamp = 70.0f;
        public float BottomClamp = -10.0f;


    // CameraZoom
        public float zoom, zoomMultiplier = 4, minZoom = 2, maxZoom = 8, velocirty = 0, smoothTime = 0.25f;

    bool  cursorInputForLook = true, isScoll = false;

    #endregion


    #region  ターゲット取得値

 [Header("ターゲット位置取得")]
        public GameObject MonsterTargetLocator;



    #endregion


 [Header("カメラ距離と移動")]
#region カメラ距離と移動
        public GameObject camParent;
        GameObject Cam;
        public Vector3 CamOffset;
        public float SmoothSpeed;

    #endregion


#region カメラコライダー
 [Header("カメラ距離値")]
        public float smooth;
        public float minDistance, maxDistance, distance,  distanceOffset;

    [Header("カメラピボット距離")]
        public float normalMagnitude, movementMagnitude;
        public Vector3 dollyDir;

    Animator anim;
    float saveDistance;

#endregion


#region 角度チェック ～ 未実装
    [Header("角度チェック")]
    [Tooltip("Set Limit of Angle with Camera & Target ")]
    float setAngle;
    float Angle;

    #endregion


#endregion


    private void Awake()
    {
        dollyDir = camParent.transform.localPosition.normalized;
        distance = camParent.transform.localPosition.magnitude;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cam = GameObject.Find("Main Camera");
        _playerInput = GetComponent<PlayerInput>();

        //zoom
        zoom = Cam.GetComponent<Camera>().fieldOfView;

    }

    void Update()
    {
        CameraRotation();
        CameraCollision();
        ZoomInput();
    }
   

    #region カメラ制御

    private void CameraRotation()
    {
        Tick(Time.fixedDeltaTime);
        HandleRotation(Time.fixedDeltaTime, look.x, look.y);
    }
    public void Tick(float delta)
    {
        // set camera follow Lerp value
        Vector3 targetPosition = Vector3.Lerp(transform.position, targetTransform.transform.position, delta / followSpeed);   

        transform.position = targetPosition;
    }

    public void HandleRotation(float delta, float mouseX, float mouseY)
    {

        if (!lockOn || MonsterTargetLocator == null) // if not LockOn mode or Enemy = null, set camera to free mode
        {
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            lookAngle += (mouseX * deltaTimeMultiplier) * (rotateSpeed);   
            pivotAngle +=(mouseY * deltaTimeMultiplier) * (rotateSpeed);  
            pivotAngle = Mathf.Clamp(pivotAngle, BottomClamp, TopClamp);  

            Vector3 euler = Vector3.zero;
            euler.y = lookAngle;
            euler.x = pivotAngle;
            Quaternion targetRotation = Quaternion.Euler(euler);

           
            transform.rotation = targetRotation;
            pivot.rotation = Quaternion.Lerp(pivot.rotation, targetRotation, delta / 0.25f);

        }
        else
        {
            Vector3 lockPos = MonsterTargetLocator.transform.position;
            Vector3 dir = lockPos - transform.position;
            dir.Normalize();
            dir.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(dir);

            Vector3 pivotDir = lockPos - pivot.position;
            pivotDir.Normalize();
            Quaternion pivotTargetRotation = Quaternion.LookRotation(pivotDir);
            Vector3 e = pivotTargetRotation.eulerAngles;
            e.y = 0;    

            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime / 0.25f);
            pivot.localEulerAngles = Vector3.Lerp(pivot.localEulerAngles, e, Time.fixedDeltaTime / 0.25f);

            pivotAngle = 0;    
            lookAngle = transform.eulerAngles.y; 
        }

    }
    // Set CameraDistance
    void setCameraDistance()
    {
        if (lockOn)
        {
            Vector3 newVec3 = camParent.transform.position + CamOffset;
            Cam.transform.position = Vector3.Slerp(Cam.transform.position, newVec3, Time.deltaTime * SmoothSpeed);
        }
        else
            Cam.transform.position = Vector3.Slerp(Cam.transform.position, camParent.transform.position, Time.deltaTime * SmoothSpeed);
    }

    // Zoom Camera
    void ZoomInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            zoom -= scroll * zoomMultiplier;
            zoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            isScoll = true;
            CameraZoom(1, zoom);
        }
    }

    public void CameraZoom(int zoomType, float fieldOfView) 
    {
        int smoothSpeed = 1;

        if (fieldOfView == Cam.GetComponent<Camera>().fieldOfView)
            return;
    

        switch (zoomType) 
        {
            case 0:
                if(isScoll)
                    return;
                break;
            case 1:
                smoothSpeed = 10;
                break;
            case 2:
                isScoll = false;
                break;
        }

        Vector3 SlerpVector = new Vector3(0, 0, Cam.GetComponent<Camera>().fieldOfView);
        SlerpVector = Vector3.Slerp(SlerpVector, new Vector3(0, 0, fieldOfView), smoothTime * smoothSpeed);

        Cam.GetComponent<Camera>().fieldOfView = SlerpVector.z;
    }

    void CameraCollision() 
    {
        Vector3 desiredCameraPos = camParent.transform.parent.TransformPoint(dollyDir * maxDistance);
        RaycastHit hit;
        Debug.DrawRay(camParent.transform.position, desiredCameraPos, Color.red);
       
        if (Physics.Linecast(camParent.transform.parent.position, desiredCameraPos, out hit))
        {
            saveDistance = distance;
            camParent.transform.localPosition = Vector3.Lerp(camParent.transform.localPosition, dollyDir * distance, Time.deltaTime * smooth);
          
            distance = Mathf.Clamp(hit.distance * distanceOffset, minDistance, maxDistance);
        }
        else
        {   
            setCameraDistance();
            distance = saveDistance;
        }

    }

    Vector3 Between(Vector3 v1, Vector3 v2, float percentage)
    {
        return (v2 - v1) * percentage + v1;
    }

    #endregion


    #region ロックオンスターゲット

  

  

    #endregion


    #region 入力取得

    private bool IsCurrentDeviceMouse
    {
        get
        {
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
            return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
        }
    }

    public void OnLook(InputValue value)
    {
        if (cursorInputForLook)
        {
            LookInput(value.Get<Vector2>());
        }
    }
    public void LookInput(Vector2 newLookDirection)
    {
        look = newLookDirection;
    }

    #endregion


}
