using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;

namespace TopographicPaint
{
    public class ImageProcessingUtils
    {
        /// <summary>
        /// Texture2DをMatに変換
        /// </summary>
        /// <param name="tex"></param>
        /// <returns></returns>
        public static Mat Texture2DToMat(Texture2D tex)
        {
            var srcTex = tex;
            var srcMat = new Mat(srcTex.height, srcTex.width, CvType.CV_8UC4);
            Utils.texture2DToMat(srcTex, srcMat);
            return srcMat;
        }

        /// <summary>
        /// MatをTexture2Dに変換
        /// </summary>
        /// <param name="imgMat"></param>
        /// <returns></returns>
        public static Texture2D MatToTexture2D(Mat imgMat)
        {
            var tex = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(imgMat, tex);
            return tex;
        }
    }
}