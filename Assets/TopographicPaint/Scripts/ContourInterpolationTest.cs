using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UtilsModule;
using OpenCVForUnity.XimgprocModule;
using UnityEngine.Rendering;
using UnityEngine.XR;
using Random = UnityEngine.Random;

public class ContourInterpolationTest : MonoBehaviour
{
    [SerializeField] private Texture2D inputTexture;
    [SerializeField, Range(1, 1024)] private int shrinkSize;

    private Mat mat_src;
    private Mat mat_gray;
    private Mat mat_binary;
    private Mat mat_shrink;
    private Mat mat_contour;
    private Mat mat_thin;

    private Mat mat_dst;
    private Mat mat_interpolated;
    private Mat mat_relaxed;

    // Start is called before the first frame update
    void Start()
    {
        //入力画像をMatに変換
        mat_src = new Mat(inputTexture.height,
            inputTexture.width, CvType.CV_8UC4);
        Utils.texture2DToMat(inputTexture, mat_src);

        //グレイスケール化
        mat_gray = new Mat(mat_src.width(), mat_src.height(), CvType.CV_8UC1);
        Imgproc.cvtColor(mat_src, mat_gray, Imgproc.COLOR_RGBA2GRAY);

        //画像を縮小
        mat_shrink = new Mat();
        Imgproc.resize(mat_gray, mat_shrink, new Size(shrinkSize, shrinkSize),
            Imgproc.INTER_AREA);

        //2値化
        mat_binary = new Mat(mat_shrink.width(),
            mat_shrink.height(), CvType.CV_8UC1);
        Imgproc.threshold(mat_shrink, mat_binary, 128,
            255, Imgproc.THRESH_OTSU);
        Core.bitwise_not(mat_binary, mat_binary);

        //細線化
        // mat_thin = new Mat(mat_shrink.height(),
        //     mat_shrink.width(), CvType.CV_8UC1);
        // Ximgproc.thinning(mat_binary, mat_thin);
        // Mat mat_thin_preview = mat_thin.clone();
        
        

        //輪郭線のラベリング ///////////////////////////////////////////////////


        List<MatOfPoint> contours = new List<MatOfPoint>(); //輪郭線のリスト
        Mat hierarchy = new Mat(); //輪郭線の階層情報
        Imgproc.findContours(mat_binary, contours, hierarchy, Imgproc.RETR_TREE, Imgproc.CHAIN_APPROX_NONE);

        List<int> contourLevel;
        List<int> contourDrawFlag;
        int maxContourLevel;
        int currentLevel;
        int contour_index;
        int contour_Maxlevel;
        int contourInterval;
        Scalar contourColor;
        if (contours.Count > 1) //輪郭線が1つでも抽出された場合
        {
            //輪郭線の分類
            contourLevel = new List<int>(); //輪郭線のレベル(高さ)、-10は描画しない
            contourDrawFlag = new List<int>(); //輪郭線を描画するかどうかのフラグ、負なら描画しない

            //等高線の高さ情報を決定
            //初期化 
            for (int i = 0; i < contours.Count; i++)
            {
                contourLevel.Add(0);
                contourDrawFlag.Add(-10);
            }

            maxContourLevel = 0;
            currentLevel = 0;
            contour_index = 0;
            contourLevel[0] = 0;


            //hierarchy [次、前、子、親]
            //ループ
            for (contour_index = 1; contour_index < contours.Count; contour_index++)
            {
                if ((int) hierarchy.get(0, contour_index)[3] != contour_index - 1) //1つ前の輪郭線が親でないなら
                {
                    currentLevel = contourLevel[(int) hierarchy.get(0, contour_index)[1]];
                    contourLevel[contour_index] = currentLevel;
                    contourDrawFlag[contour_index] = -10;
                }
                else //1つ前の輪郭が親なら
                {
                    if (contourDrawFlag[contour_index - 1] != -10) //前の等高線レベルが-10でないなら
                    {
                        contourDrawFlag[contour_index] = -10;
                        contourLevel[contour_index] = currentLevel;
                    }
                    else
                    {
                        currentLevel++;
                        contourLevel[contour_index] = currentLevel;
                        contourDrawFlag[contour_index] = 10;
                    }
                }

                if (maxContourLevel < currentLevel) //最大高さレベルを求める
                {
                    maxContourLevel = currentLevel;
                }
            }


            contour_Maxlevel = maxContourLevel;

            //log contours
            Debug.Log("hierarchy " + hierarchy);
            for (int i = 0; i < contours.Count; i++)
            {
                Debug.Log("Hierarchy Information "+"index "+ i +"next "+ hierarchy.get(0, i)[0] 
                          + "previous " + hierarchy.get(0, i)[1] + "child " + hierarchy.get(0, i)[2] +
                          "parent " + hierarchy.get(0, i)[3] + " contourLevel " + contourLevel[i]);
            }

            Debug.Log("maxContourLevel " + maxContourLevel);

            //等高線の色を決定
            Scalar baseContourColor = new Scalar(0, 0, 0);
            contourInterval = 255 / (maxContourLevel + 1); //最高点 ≠ 最も高い等高線ではないため
            contourColor = baseContourColor +
                           new Scalar(contourInterval / 2, contourInterval / 2,
                               contourInterval / 2); //ガウスぼかしを使用している都合で少し足す
            List<Scalar> contourColors = new List<Scalar>();
            for (int i = 0; i < maxContourLevel + 1; i++)
            {
                contourColors.Add(contourColor);
                contourColor += new Scalar(contourInterval, contourInterval, contourInterval);
            }

            //等高線を描画
            mat_contour = new Mat(mat_shrink.rows(), mat_shrink.cols(), CvType.CV_8UC1);
            mat_contour.setTo(new Scalar(0, 0, 0));
            for (int i = 0; i < contours.Count; i++)
            {
                if (contourDrawFlag[i] > 0)
                {
                    Imgproc.drawContours(mat_contour, contours, i, contourColors[contourLevel[i]], 1);
                }
            }
            Debug.Log("mat_contour" + mat_contour);
            //dilate処理
            Mat kernel_dilate = Imgproc.getStructuringElement(Imgproc.MORPH_RECT, new Size(3, 3));
            Imgproc.dilate(mat_contour, mat_contour, kernel_dilate);
            
            mat_interpolated = new Mat(mat_contour.width(), mat_contour.height(), CvType.CV_8UC1);
            InterpoplatedHeight(mat_contour, mat_interpolated);

            mat_relaxed = new Mat();
            Imgproc.GaussianBlur(mat_interpolated, mat_relaxed,
                new Size(5, 5), 1);
            
            mat_dst = mat_relaxed;

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
            //     if (contourLevel[i] != -1)
            //     {
            //         Imgproc.drawContours(mat_dst, contours, i, colors[i], 1, Imgproc.LINE_8);
            //     }
            // }
            //
            //bounding box
            // List<OpenCVForUnity.CoreModule.Rect>
            //     bounds = new List<OpenCVForUnity.CoreModule.Rect>();
            // for (int i = 0; i < contours.Count; i++)
            // {
            //     bounds.Add(Imgproc.boundingRect(contours[i]));
            // }
            //
            // draw bounding box of contour
            //  for (int i = 0; i < contours.Count; i++)
            //  {
            //      int x = bounds[i].x;
            //      int y = bounds[i].y;
            //
            //      Imgproc.putText(mat_dst, "" + i, new Point(x + 5, y + 15), Imgproc.FONT_HERSHEY_PLAIN, 2, colors[i], 2);
            //  }

            
            //MatをTexture2Dに変換してマテリアルにセット
            Texture2D outTexture = new Texture2D(mat_dst.cols(),
                mat_dst.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(mat_interpolated, outTexture);
            outTexture.filterMode = FilterMode.Point;
            GetComponent<Renderer>().material.mainTexture = outTexture;
        }

        
    }
    // Update is called once per frame
    void Update()
    {

    }
    
    
    void InterpoplatedHeight(Mat src, Mat dst) //src, dstともに1チャネル画像(ビット数はまだ決めてない)
    {
        byte[] src_array = new byte[src.total() * src.channels()];
        byte[] dst_array = new byte[src.total() * src.channels()];
        MatUtils.copyFromMat(src, src_array);
        MatUtils.copyFromMat(src, dst_array);

        long step0 = src.step1(0); //row（行）の要素間隔
        long step1 = src.step1(1); //col（列）の要素間隔

        int rows = src.rows();
        int cols = src.cols();

        //配列を使ってピクセル操作するやつ https://enoxsoftware.com/opencvforunity/mat-basic-processing2/
        // byte[] src_array = new byte[src.total() * src.channels()];
        // MatUtils.copyFromMat(src, src_array);
        //
        // long step0 = src.step1(0); //row（行）の要素間隔
        // long step1 = src.step1(1); //col（列）の要素間隔
        //
        // int rows = src.rows();
        // int cols = src.cols();
        // for (int i0 = 0; i0 < rows; i0++)
        // {
        //     for (int i1 = 0; i1 < cols; i1++)
        //     {
        //         long p1 = step0 * i0 + step1 * i1;
        //         src_array[p1] = (byte)(src_array[p1] + 127);
        //     }
        // }
        // // Copies a pixel data Array to an OpenCV Mat data.
        // MatUtils.copyToMat(src_array, dst);
        
        
            
        //kPjの座標
        int[][][] kPj = new int[16][][]; //等高線との交点の座標(x,y,標高,距離)　[j][k][x,y,h,d], jは方位(0が北,1が北東,2が東,,,の時計回り), kは補間画素から近い順の番号, hは標高, dは距離
        //初期化
        for (int d1=0; d1 < 16; d1++)
        {
            kPj[d1] = new int[1][];
            
            for (int d2 = 0; d2 < 1; d2++)
            {
                kPj[d1][d2] = new int[4];
                for (int d3 = 0; d3 < 4; d3++)
                {
                    kPj[d1][d2][d3] = 0;
                }
            }
        }
        
        int px, py; //補間画素の位置
        float x; //注目画素のx座標
        float y; //注目画素のy座標
        float inc_x; //xに足す数
        float inc_y; //xに足す数
        int h; //注目画素の画素値(標高)
        float d; //距離
        float inc_d; //dに足す数
        float root2 = 1.414f;
        float root5by2 = 1.118f;
        int scale_d = 1000; //floatであるdを大きくしてからintにキャストする 計算誤差と小数桁の兼ね合い
        int interValue;

        for (py = 0; py < rows; py++)
        {
            for (px = 0; px < cols; px++)
            {
                if (src_array[step0 * py + step1 * px] > 0) //すでに高さを持つ画素は補間する必要がない
                {
                    interValue = src_array[step0 * py + step1 * px];
                }
                
                else
                {
                    for (int direction = 0; direction < 16; direction++)
                    {
                        x = px;
                        y = py;
                        d = 0;
                        int limit; //（補間画素の位置 - 画像の端）をループの最大回数
                        if (direction == 0)
                        {
                            limit = py;
                            inc_x = 0f;
                            inc_y = -1f;
                            inc_d = 1f;
                        }
                        else if (direction == 1)
                        {
                            limit = Math.Min(cols-1 - px, py);
                            inc_x = 0.5f;
                            inc_y = -1f;
                            inc_d = root5by2;
                        }
                        else if (direction == 2)
                        {
                            limit = Math.Min(cols-1 - px, py);
                            inc_x = 1f;
                            inc_y = -1f;
                            inc_d = root2;
                        }
                        else if (direction == 3)
                        {
                            limit = Math.Min(cols-1 - px, py);
                            inc_x = 1f;
                            inc_y = -0.5f;
                            inc_d = root5by2;
                        }
                        else if (direction == 4)
                        {
                            limit = cols-1 - px;
                            inc_x = 1f;
                            inc_y = 0f;
                            inc_d = 1f;
                        }
                        else if (direction == 5)
                        {
                            limit = Math.Min(cols-1 - px, rows-1 - py);
                            inc_x = 1f;
                            inc_y = 0.5f;
                            inc_d = root5by2;
                        }
                        else if (direction == 6)
                        {
                            limit = Math.Min(cols-1 - px, rows-1 - py);
                            inc_x = 1f;
                            inc_y = 1f;
                            inc_d = root2;
                        }
                        else if (direction == 7)
                        {
                            limit = Math.Min(cols-1 - px, rows-1 - py);
                            inc_x = 0.5f;
                            inc_y = 1f;
                            inc_d = root5by2;
                        }
                        else if (direction == 8)
                        {
                            limit = rows-1 - py;
                            inc_x = 0f;
                            inc_y = 1f;
                            inc_d = 1f;
                        }
                        else if (direction == 9)
                        {
                            limit = Math.Min(px, rows-1 - py);
                            inc_x = -0.5f;
                            inc_y = 1f;
                            inc_d = root5by2;
                        }
                        else if (direction == 10)
                        {
                            limit = Math.Min(px, rows-1 - py);
                            inc_x = -1f;
                            inc_y = 1f;
                            inc_d = root2;
                        }
                        else if (direction == 11)
                        {
                            limit = Math.Min(px, rows-1 - py);
                            inc_x = -1f;
                            inc_y = 0.5f;
                            inc_d = root5by2;
                        }
                        else if (direction == 12)
                        {
                            limit = px;
                            inc_x = -1f;
                            inc_y = 0f;
                            inc_d = 1f;
                        }
                        else if (direction == 13)
                        {
                            limit = Math.Min(px, py);
                            inc_x = -1f;
                            inc_y = -0.5f;
                            inc_d = root5by2;
                        }
                        else if(direction == 14)
                        {
                            limit = Math.Min(px, py);
                            inc_x = -1f;
                            inc_y = -1f;
                            inc_d = root2;
                        }
                        else
                        {
                            limit = Math.Min(px, py);
                            inc_x = -0.5f;
                            inc_y = -1f;
                            inc_d = root5by2;
                        }

                        int k = 0;
                        for (k=0; k<limit; k++) //kPjを求める
                        {
                            x += inc_x;
                            y += inc_y;
                            d += inc_d;
                            h = (int)src_array[step0 * (int)y + step1 * (int)x];

                            if (h > 0) //値を持つ画素=等高線なので、ここが交点
                            { 
                                //交点の座標jP0の決定
                                kPj[direction][0][0] = (int)x;
                                kPj[direction][0][1] = (int)y;
                                kPj[direction][0][2] = h;
                                kPj[direction][0][3] = (int)(scale_d * d);
                                break;
                            }
                        }
                        if (k == limit)
                        {
                            kPj[direction][0][0] = (int)x;
                            kPj[direction][0][1] = (int)y;
                            kPj[direction][0][2] = (int)src_array[step0 * (int)y + step1 * (int)x]; //基準標高？
                            kPj[direction][0][3] = (int)(scale_d * d);
                        }
                    }
                    
                    //0Pjと0Pj+8の勾配を求め、傾斜地にあるかどうか判断する
                    float maxGradient = 0; //最大勾配(絶対値)
                    int mostSteepDirection = 0; //最急方位
                    float gradient = 0;
                    int onSlope = 0;
                    
                    for (int l = 0; l < 8; l++)
                    {
                        if (Math.Abs(kPj[l][0][2] - kPj[l+8][0][2]) > 0.01)
                        {
                            onSlope = 1;
                            gradient = Math.Abs(kPj[l][0][2] - kPj[l+8][0][2]) / (float)(kPj[l][0][3]+kPj[l+8][0][3]+1);
                            if (gradient > maxGradient)
                            {
                                mostSteepDirection = l;
                                maxGradient = gradient;
                            }
                        }
                    }
    
                    if (onSlope == 1) //傾斜地にある場合
                    {
                        //最急方位に沿って補間する
                        float interPoint = kPj[mostSteepDirection][0][3] / (float)(kPj[mostSteepDirection][0][3] + kPj[mostSteepDirection+8][0][3]);
                        interValue = (int)(kPj[mostSteepDirection][0][2] * (1-interPoint) +
                                           kPj[mostSteepDirection+8][0][2] * interPoint);
                    }
                    else //傾斜地にない場合
                    {
                        interValue = kPj[0][0][2];
                    }
                    
                }
                
                //補間した値を描き込む
                dst_array[step0 * py + step1 * px] = (byte)interValue;
            }
        }

        //配列をdstに書き込む
        MatUtils.copyToMat(dst_array, dst);
    }
}
