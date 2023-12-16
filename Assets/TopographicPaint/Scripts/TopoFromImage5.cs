using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.XimgprocModule;
using UnityEngine.Rendering;
using UnityEngine.XR;
using Random = UnityEngine.Random;

public class TopoFromImage5 : MonoBehaviour
{
    [SerializeField] Texture2D texture;
    [SerializeField, Range(0, 10)] int shrink_level; //2のn乗
    [SerializeField] double gaussianKernel_size = 1;
    [SerializeField, Range(0, 10)] double gaussian_sigma = 1.0;
    
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Renderer>().material.SetTexture("_ParallaxMap", 
            generateTopographicHeightMap(texture, shrink_level, gaussianKernel_size, gaussian_sigma));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnValidate()
    {
        GetComponent<Renderer>().sharedMaterial.SetTexture("_ParallaxMap", 
            generateTopographicHeightMap(texture, shrink_level, gaussianKernel_size, gaussian_sigma));
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
    
    public Texture2D generateTopographicHeightMap(Texture2D texIn, int shrinkLevel,
        double gaussianKernelSize, double gaussianSigma)
    {
        Texture2D texOut;
        Mat mat_src;
        Mat mat_gray;
        Mat mat_binary;
        Mat mat_binary_inv;
        Mat mat_thin;
        Mat mat_contour;
        List<MatOfPoint> contours;
        Mat hierarchy;
        List<int> contourLevel;
        int maxContourLevel;
        int currentLevel;
        int contour_index;
        int contourInterval;
        Scalar contourColor;
        Mat mat_shrink;
        Size size_resize;
        Mat mat_blur;
        Mat mat_dst;
        
        //Texture2D → Mat
        mat_src = Texture2DToMat(texIn);
        
        //画像処理ここから////////////////////////////////////////////////////

        //グレースケール処理
        mat_gray = new Mat(mat_src.rows(), mat_src.cols(), CvType.CV_8UC1);
        Imgproc.cvtColor(mat_src, mat_gray, Imgproc.COLOR_RGBA2GRAY);

        //二値化処理
        mat_binary = new Mat(mat_src.rows(), mat_src.cols(), CvType.CV_8UC1);
        Imgproc.threshold(mat_gray, mat_binary, 128, 255, Imgproc.THRESH_OTSU);

        //画素値反転
        mat_binary_inv = new Mat();
        Core.bitwise_not(mat_binary, mat_binary_inv);
        
        //細線化
        mat_thin = new Mat(mat_src.rows(), mat_src.cols(), CvType.CV_8UC1);
        Ximgproc.thinning(mat_binary_inv, mat_thin);
        
        //輪郭線のラベリング ///////////////////////////////////////////////////
        contours = new List<MatOfPoint>(); //輪郭線のリスト
        hierarchy = new Mat(); //輪郭線の階層情報
        Imgproc.findContours(mat_thin, contours, hierarchy, Imgproc.RETR_TREE, Imgproc.CHAIN_APPROX_SIMPLE);


        if (contours.Count != 0) //輪郭線が1つでも抽出された場合
        {
            //輪郭線の分類
            contourLevel = new List<int>(); //輪郭線のレベル(高さ)、-1は描画しない

            //等高線の高さ情報を決定
            //初期化 
            maxContourLevel = 0;
            currentLevel = 0;
            for (int i = 0; i < contours.Count; i++)
            {
                contourLevel.Add(-1);
            }

            contour_index = 0;
            currentLevel++;
            contourLevel[contour_index] = currentLevel;

            //ループ
            for (contour_index = 1; contour_index < contours.Count; contour_index++)
            {

                if (hierarchy.get(0, contour_index)[3] == contour_index - 1) //1つ前の輪郭線が親なら、高さ +1
                {
                    currentLevel++;
                }
                else //そうでないなら前のやつの高さを参照
                {
                    currentLevel = contourLevel[(int) hierarchy.get(0, contour_index)[1]];
                }

                if (hierarchy.get(0, contour_index)[1] == contour_index - 1 &&
                    hierarchy.get(0, contour_index)[3] != contour_index - 1) //1つ前が前のやつでも親でもないなら、この輪郭線は閉じた円ではないので無効
                {
                    contourLevel[contour_index - 1] = -1;
                }

                contourLevel[contour_index] = currentLevel;


                if (maxContourLevel < currentLevel) //最大高さレベルを求める
                {
                    maxContourLevel = currentLevel;
                }
            }

            //log contours
            // Debug.Log("hierarchy " + hierarchy);
            // for (int i = 0; i < contours.Count; i++)
            // {
            //     Debug.Log("h " + hierarchy.get(0, i)[0] + " " + hierarchy.get(0, i)[1] + " " + hierarchy.get(0, i)[2] + " " + hierarchy.get(0, i)[3] + " contourLevel " + contourLevel[i]);
            // }
            // Debug.Log("maxContourLevel " + maxContourLevel);

            //等高線の色を決定
            Scalar baseContourColor = new Scalar(0, 0, 0);
            contourInterval = 255 / (maxContourLevel + 1); //最高点 ≠ 最も高い等高線ではないため
            contourColor = baseContourColor + new Scalar(contourInterval / 2); //ガウスぼかしを使用している都合で少し足す
            List<Scalar> contourColors = new List<Scalar>();
            for (int i = 0; i < maxContourLevel + 1; i++)
            {
                contourColors.Add(contourColor);
                contourColor += new Scalar(contourInterval, contourInterval, contourInterval);
            }

            //等高線を描画
            mat_contour = new Mat(mat_src.rows(), mat_src.cols(), CvType.CV_8UC3);
            mat_contour.setTo(new Scalar(0, 0, 0));
            for (int i = 0; i < contours.Count; i++)
            {
                if (contourLevel[i] != -1)
                {
                    Imgproc.drawContours(mat_contour, contours, i, contourColors[contourLevel[i]], -1);
                }
            }

            //縮小
            mat_shrink = new Mat();
            double sx = Mathf.Pow(2, shrinkLevel);
            sx = mat_src.cols() / sx;
            double sy = Mathf.Pow(2, shrinkLevel);
            sy = mat_src.rows() / sy;

            size_resize = new Size(sx, sy);
            Imgproc.resize(mat_contour, mat_shrink, size_resize, 1, 1, Imgproc.INTER_NEAREST);

           
            //ここに線形補間アルゴリズム
            
            
            
            //
            
            //拡大
            mat_dst = new Mat();
            // size_resize = new Size(mat_src.cols(), mat_src.rows());
            // Imgproc.resize(mat_blur, mat_dst, size_resize, 1, 1, Imgproc.INTER_LINEAR);


            //輪郭線デバッグ用
            // determine contours color
            // List<Scalar> colors = new List<Scalar>(contours.Count);
            // for (int i = 0; i < contours.Count; i++)
            // {
            //     colors.Add(new Scalar(Random.Range(0f, 255f), Random.Range(0f, 255f), Random.Range(0f, 255f)));
            // }
            //
            // //draw contours
            // for (int i = 0; i < contours.Count; i++)
            // {
            //     Imgproc.drawContours(mat_dst, contours, i, colors[i], -1);
            // }
            //
            // //bounding box
            // List<OpenCVForUnity.CoreModule.Rect>
            //  bounds = new List<OpenCVForUnity.CoreModule.Rect>();
            // for (int i = 0; i < contours.Count; i++)
            // {
            //     bounds.Add(Imgproc.boundingRect(contours[i]));
            // }
            //
            // //draw bounding box of contour
            // for (int i = 0; i < contours.Count; i++)
            // {
            //     int x = bounds[i].x;
            //     int y = bounds[i].y;
            //
            //     Imgproc.putText(mat_dst, "" + i, new Point(x + 5, y + 15), Imgproc.FONT_HERSHEY_PLAIN, 2, colors[i], 2);
            // }

            //ラベリング処理ここまで ///////////////////////////////////////

            //画素値補間 ///////////

            //画素値補間ここまで /////////////


            //画像処理ここまで////////////////////////////////
            //Mat →　Texture2D
            // texOut = MatToTexture2D(mat_dst);
        }

        else
        {
            mat_dst = mat_src.setTo(new Scalar(0, 0, 0));
        }

        texOut = MatToTexture2D(mat_dst);

        return texOut;
    }
}
