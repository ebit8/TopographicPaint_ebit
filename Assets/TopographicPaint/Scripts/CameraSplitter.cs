using UnityEngine;
using System.Collections;

public class CameraSplitter : MonoBehaviour {

    //画面の分割モード(横・縦)
    public enum SplitCameraMode {
        horizontal,
        vertical
    };

    public SplitCameraMode splitMode;

    //分割するそれぞれのカメラ
    [SerializeField] private Camera terrainCamera;
    [SerializeField] private Camera canvasCamera;

    // Use this for initialization
    void Start () {
        if (splitMode == SplitCameraMode.horizontal) {
            terrainCamera.rect = new Rect (0f, 0f, 0.5f, 1f);
            canvasCamera.rect = new Rect (0.5f, 0f, 0.5f, 1f);

        } else if (splitMode == SplitCameraMode.vertical) {
            terrainCamera.rect = new Rect (0f, 0.5f, 1f, 0.5f);
            canvasCamera.rect = new Rect (0f, 0f, 1f, 0.5f);
        }
    }
}