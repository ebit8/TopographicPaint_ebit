using UnityEngine;

//カメラ操作：マウス右ドラッグで回転、ホイールドラッグで並行移動、ホイール回転でズームイン・アウト
public class CameraController : MonoBehaviour {
    public GameObject cameraTarget;
    //ズームイン・アウトの速度
    [SerializeField, Range(1f, 500f)] private float wheelSpeed = 100f;
    //並行移動の速度
    [SerializeField, Range(0.1f, 1f)] private float moveSpeed = 0.3f;
    //回転する速度
    [SerializeField, Range(0.1f, 1f)] private float rotateSpeed = 1f;
    private Vector3 preMousePosition;

    private Transform cameraInitialTransform;
    private Transform targetInitialTransform;
    
    private void Start()
    {
        cameraInitialTransform = this.transform;
        targetInitialTransform = cameraTarget.transform;
        
        LookAtTarget();
    }
    
    private void Update()
    {
        float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
        if(scrollWheel != 0.0f)
        {
            //ズーム
            MouseWheel(scrollWheel);
        }
        
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            preMousePosition = Input.mousePosition;
        }
        //回転または平行移動
        MouseDrag(Input.mousePosition);
    }
    
    private void MouseWheel(float _delta)
    {
        transform.position += transform.forward * _delta * wheelSpeed;
    }
    
    private void MouseDrag(Vector3 _mousePosition)
    {
        Vector3 diff = _mousePosition - preMousePosition;
        float distance = GetDistanceFromTarget();
        if (Input.GetMouseButton(1)) //回転
        {
            transform.Translate(-diff * Time.deltaTime * rotateSpeed * distance);
            LookAtTarget();
            transform.position += transform.forward * ((transform.position - cameraTarget.transform.position).magnitude - distance);//直線移動と曲線移動の誤差修正
        }
        else if (Input.GetMouseButton(2)) //平行移動
        {
            transform.Translate(-diff * Time.deltaTime * moveSpeed * distance);
            cameraTarget.transform.Translate(-diff * Time.deltaTime * moveSpeed * distance);
            LookAtTarget();
        }
        preMousePosition = _mousePosition;
    }
    
    //カメラとターゲットの距離を計算
    private float GetDistanceFromTarget()
    {
        return (transform.position - cameraTarget.transform.position).magnitude;
    }
    
    //カメラがターゲットの方を向く
    private void LookAtTarget()
    {
        transform.LookAt(cameraTarget.transform);
        cameraTarget.transform.rotation = transform.rotation;
    }

    //カメラの位置・回転をリセットする
    public void ResetCamera()
    {
        this.transform.position = cameraInitialTransform.position;
        this.transform.rotation = cameraInitialTransform.rotation;
        cameraTarget.transform.position = targetInitialTransform.position;
        cameraTarget.transform.rotation = targetInitialTransform.rotation;
    }
}