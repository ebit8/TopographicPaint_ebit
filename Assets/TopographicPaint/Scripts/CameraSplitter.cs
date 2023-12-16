using UnityEngine;
using System.Collections;

public class CameraSplitter : MonoBehaviour {

    //　カメラの分割方法
    public enum SplitCameraMode {
        horizontal,
        vertical
    };

    public SplitCameraMode mode;	//　カメラの分割方法

    //　分割するそれぞれのカメラ
    public Camera MainCamera;
    public Camera SubCamera;

    // Use this for initialization
    void Start () {
        //　横分割
        if (mode == SplitCameraMode.horizontal) {
            //　カメラのViewPortRectの変更
            MainCamera.rect = new Rect (0f, 0f, 0.5f, 1f);
            SubCamera.rect = new Rect (0.5f, 0f, 0.5f, 1f);

            //　縦分割
        } else if (mode == SplitCameraMode.vertical) {
            //　カメラのViewPortRectの変更
            MainCamera.rect = new Rect (0f, 0.5f, 1f, 0.5f);
            SubCamera.rect = new Rect (0f, 0f, 1f, 0.5f);
        }
    }
}