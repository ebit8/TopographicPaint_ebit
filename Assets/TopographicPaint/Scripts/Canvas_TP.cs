using UnityEngine;

public class Canvas_TP : MonoBehaviour
{
    //テレインオブジェクトのマテリアル
    public Material canvasMat { get; set; }

    private void Start()
    {
        canvasMat = GetComponent<Renderer>().material;
    }
}
