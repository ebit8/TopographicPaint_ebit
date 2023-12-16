using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.ImgprocModule;
public class TopoFromImage : MonoBehaviour
{
    [SerializeField] Texture2D texture;
    [SerializeField, Range(0, 256)] int threshold_binary;


    private Mat mat_src;
    private Mat mat_gray;
    private Mat mat_binary;
    private Mat mat_binary_inv;
    private Mat mat_dst;
    private Mat kernel;
    private Size ksize;
    private Mat mat_opening;
    private Mat mat_closing;

    

    // Start is called before the first frame update
    void Start()
    {
        //Texture2D → Mat
        mat_src = Texture2DToMat(texture);

        //最終的に変換するMat
        mat_dst = new Mat(mat_src.rows(), mat_src.cols(), CvType.CV_8UC3);


        //画像処理ここから////////////////////////////////////////////////////

        //グレースケール処理
        mat_gray = new Mat(mat_src.rows(), mat_src.cols(), CvType.CV_8UC1);
        Imgproc.cvtColor(mat_src, mat_gray, Imgproc.COLOR_RGBA2GRAY);

        //二値化処理
        mat_binary = new Mat(mat_src.rows(), mat_src.cols(), CvType.CV_8UC1);
        Imgproc.threshold(mat_gray, mat_binary, threshold_binary, 255, Imgproc.THRESH_OTSU);

        //画素値反転
        mat_binary_inv = new Mat();
        Core.bitwise_not(mat_binary, mat_binary_inv);

        //モルフォロジー処理
        ksize = new Size(3, 3);
        kernel = Imgproc.getStructuringElement(Imgproc.MORPH_RECT, ksize);

        mat_opening = new Mat(mat_src.rows(), mat_src.cols(), CvType.CV_8UC1);
        Imgproc.morphologyEx(mat_binary_inv, mat_opening, Imgproc.MORPH_OPEN, kernel);

        mat_closing = new Mat(mat_src.rows(), mat_src.cols(), CvType.CV_8UC1);
        Imgproc.morphologyEx(mat_opening, mat_closing, Imgproc.MORPH_CLOSE, kernel);


        //ラベリング ///////////////////////////////////////////////////
        List<MatOfPoint> contours = new List<MatOfPoint>(); //輪郭線のリスト
        Mat hierarchy = new Mat(); //輪郭線の階層情報
        Imgproc.findContours(mat_closing, contours, hierarchy, Imgproc.RETR_TREE, Imgproc.CHAIN_APPROX_SIMPLE);

        //輪郭線の分類
        List<int> contourLevel = new List<int>(); //輪郭線のレベル(高さ)、-1は描画しない

        //hierarchyの0番目: hierarchy.get(0, 0) 最初の輪郭
        //hierarchy(0, i)[next, previous, child, parent]


        //初期化 
        int maxContourLevel = 0;
        int currentLevel = 0;
        for(int i = 0; i < contours.Count; i++)
        {
            contourLevel.Add(-1);
        }
        int idx = 0;
        currentLevel++;
        contourLevel[idx] = currentLevel;

        //ループ
        for(idx = 1; idx < contours.Count; idx++)
        {
            if (idx % 2 == 1) 
            {
                contourLevel[idx] = -1;
            }
            else
            {
                if (hierarchy.get(0, idx)[3] == idx - 1)
                {
                    currentLevel++;
                }
                else
                {
                    currentLevel = contourLevel[(int)hierarchy.get(0, idx)[1]];
                }

                contourLevel[idx] = currentLevel;
            }

            if(maxContourLevel < currentLevel)
            {
                maxContourLevel = currentLevel;
            }
        }

        //log contours
        Debug.Log("hierarchy " + hierarchy);
        for(int i = 0; i < contours.Count; i++)
        {
            Debug.Log("h " + hierarchy.get(0, i)[0] +" "+ hierarchy.get(0, i)[1] +" "+ hierarchy.get(0, i)[2] +" "+ hierarchy.get(0, i)[3] + " contourLevel " + contourLevel[i]);
        }
        Debug.Log("maxContourLevel " + maxContourLevel);

        //determine contours color
        int contourInterval = 128 / maxContourLevel;
        Scalar contourColor = new Scalar(128, 128, 128);
        List<Scalar> contourColors = new List<Scalar>();
        for(int i = 0; i < maxContourLevel + 1; i++)
        {
            contourColors.Add(contourColor);
            contourColor += new Scalar(contourInterval, contourInterval, contourInterval);
        }

        //draw contours color

        

        Imgproc.rectangle(mat_dst, new OpenCVForUnity.CoreModule.Rect(0, 0, mat_dst.width(), mat_dst.height())
            , new Scalar(128, 128, 128), -1);
        for(int i = 0; i < contours.Count; i++)
        {
            if (contourLevel[i] != -1)
            {
                Imgproc.drawContours(mat_dst, contours, i, contourColors[contourLevel[i]], -1);
            }
        }

        //輪郭線デバッグ用
        //determine contours color
        //List<Scalar> colors = new List<Scalar>(contours.Count);
        //for (int i = 0; i < contours.Count; i++)
        //{
        //    colors.Add(new Scalar(Random.Range(0, 255), Random.Range(0, 255), Random.Range(0, 255)));
        //}

        ////draw contours
        //for (int i = 0; i < contours.Count; i++)
        //{
        //    Imgproc.drawContours(mat_dst, contours, i, colors[i], -1);
        //}

        ////bounding box
        //List<OpenCVForUnity.CoreModule.Rect>
        // bounds = new List<OpenCVForUnity.CoreModule.Rect>();
        //for (int i = 0; i < contours.Count; i++)
        //{
        //    bounds.Add(Imgproc.boundingRect(contours[i]));
        //}

        ////draw bounding box of contour
        //for (int i = 0; i < contours.Count; i++)
        //{
        //    int x = bounds[i].x;
        //    int y = bounds[i].y;

        //    Imgproc.putText(mat_dst, "" + i, new Point(x + 5, y + 15), Imgproc.FONT_HERSHEY_PLAIN, 2, colors[i], 2);
        //}


        //ラベリング処理ここまで ///////////////////////////////////////


        //画像処理ここまで////////////////////////////////


        //Mat(8UC1) → Texture2D(TextureFormat.Alpha8)


        //マテリアルのテクスチャに貼る
        GetComponent<Renderer>().material.SetTexture("Texture2D_HeightMap", MatToTexture2D(mat_dst));

    }

    // Update is called once per frame
    void Update()
    {

    }

    public static Mat Texture2DToMat(Texture2D tex)
    {
        var srcTex = tex;
        var srcMat = new Mat(srcTex.height, srcTex.width, CvType.CV_8UC4);
        Utils.texture2DToMat(srcTex, srcMat);
        return srcMat;
    }

    public static Texture2D MatToTexture2D(Mat imgMat)
    {
        var tex = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);
        Utils.matToTexture2D(imgMat, tex);
        return tex;
    }
}
