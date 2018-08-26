using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using System.IO;
using System.Drawing.Imaging;
using System.Diagnostics;
using Emgu.CV.Util;
using Emgu.CV.Features2D;
using Emgu.CV.Flann;
using Emgu.CV.CvEnum;
using Emgu.CV.XFeatures2D;
using Emgu.CV.Cuda;

namespace FacialTest
{
    public class ImageMan
    {

        private static ImageMan instance;

        public static ImageMan Instance { get { if (instance == null) { instance = new ImageMan(); } return instance; } }

        private ImageMan()
        {

        }

        public List<Image<Gray, byte>> CropImage(Mat image, List<Rectangle> ROIs)
        {
            List<Image<Gray, byte>> returnImage = new List<Image<Gray, byte>>();

            using (Image<Gray, byte> i = image.ToImage<Gray, byte>())
            {
                foreach (Rectangle roi in ROIs)
                {
                    i.ROI = roi;
                    returnImage.Add(i.Copy());
                }
            }

            return returnImage;
        }

        public async Task<string> CompareImagesAsync(Image<Gray, byte> Image)
        {
            List<string> imageDirectory = Directory.GetFiles(Directory.GetCurrentDirectory() + "\\photos", "*.jpg").ToList();
            List<long> scores = new List<long>();

            foreach (string s in imageDirectory)
            {
                Image<Gray, byte> i = new Image<Gray, byte>(s);
                //CvInvoke.Imshow("AbsDiff", i);


                NormaliseImage(Image, i, out Image, out i);

                Mat im = i.Resize(2, Inter.Cubic).ToUMat().GetMat(AccessType.Fast);
                Mat ima = Image.Resize(2,Inter.Cubic).ToUMat().GetMat(AccessType.Fast);
                i = Draw(im, ima, out long score).ToImage<Gray, byte>();
                im.Dispose();
                ima.Dispose();
                //CvInvoke.Imshow("AbsDiff", i );
                Debug.WriteLine(score);
                scores.Add(score);

              
            }

            for (int i = 0; i < scores.Count; ++i)
            {
                if (scores[i] > 70)
                {
                    return imageDirectory[i];
                }

                if (scores[i] < 10)
                {
                    return "Unuseable";
                }
            }

            return "Failed";
        }

        public void saveJpeg(string path, Bitmap img, long quality)
        {
            // Encoder parameter for image quality

            EncoderParameter qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

            // Jpeg image codec
            ImageCodecInfo jpegCodec = this.getEncoderInfo("image/jpeg");

            if (jpegCodec == null)
                return;

            EncoderParameters encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = qualityParam;

            img.Save(path, jpegCodec, encoderParams);
        }

        private ImageCodecInfo getEncoderInfo(string mimeType)
        {
            // Get image codecs for all image formats
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            // Find the correct image codec
            for (int i = 0; i < codecs.Length; i++)
                if (codecs[i].MimeType == mimeType)
                    return codecs[i];
            return null;
        }

        public static void FindMatch(Mat modelImage, Mat observedImage, out VectorOfKeyPoint modelKeyPoints, out VectorOfKeyPoint observedKeyPoints, VectorOfVectorOfDMatch matches, out Mat mask, out Mat homography, out long score)
        {
            int k = 2;
            double uniquenessThreshold = 1;
            double hessianThresh = 300;

            homography = null;

            modelKeyPoints = new VectorOfKeyPoint();
            observedKeyPoints = new VectorOfKeyPoint();

            CudaSURF surfCuda = new CudaSURF((float)hessianThresh);
            using (GpuMat gpuModelImage = new GpuMat(modelImage))
            //extract features from the object image
            using (GpuMat gpuModelKeyPoints = surfCuda.DetectKeyPointsRaw(gpuModelImage, null))
            using (GpuMat gpuModelDescriptors = surfCuda.ComputeDescriptorsRaw(gpuModelImage, null, gpuModelKeyPoints))
            using (CudaBFMatcher matcher = new CudaBFMatcher(DistanceType.L2))
            {
                surfCuda.DownloadKeypoints(gpuModelKeyPoints, modelKeyPoints);

                // extract features from the observed image
                using (GpuMat gpuObservedImage = new GpuMat(observedImage))
                using (GpuMat gpuObservedKeyPoints = surfCuda.DetectKeyPointsRaw(gpuObservedImage, null))
                using (GpuMat gpuObservedDescriptors = surfCuda.ComputeDescriptorsRaw(gpuObservedImage, null, gpuObservedKeyPoints))
                //using (GpuMat tmp = new GpuMat())
                //using (Stream stream = new Stream())
                {
                    matcher.KnnMatch(gpuObservedDescriptors, gpuModelDescriptors, matches, k);

                    surfCuda.DownloadKeypoints(gpuObservedKeyPoints, observedKeyPoints);

                    mask = new Mat(matches.Size, 1, DepthType.Cv8U, 1);
                    mask.SetTo(new MCvScalar(255));
                    Features2DToolbox.VoteForUniqueness(matches, uniquenessThreshold, mask);

                    score = 0;
                    for (int i = 0; i < matches.Size; i++)
                    {
                        if (mask.GetData(i)[0] == 0) continue;
                        foreach (var e in matches[i].ToArray())
                            ++score;
                    }

                    int nonZeroCount = CvInvoke.CountNonZero(mask);
                    if (nonZeroCount >= 4)
                    {
                        nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints,
                            matches, mask, 1.5, 20);
                        if (nonZeroCount >= 4)
                            homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeyPoints,
                                observedKeyPoints, matches, mask, 2);
                    }
                }
            }
        }

        /// <summary>
        /// Draw the model image and observed image, the matched features and homography projection.
        /// </summary>
        /// <param name="modelImage">The model image</param>
        /// <param name="observedImage">The observed image</param>
        /// <returns>The model image and observed image, the matched features and homography projection.</returns>
        public static Mat Draw(Mat modelImage, Mat observedImage, out long score)
        {
            Mat homography;
            VectorOfKeyPoint modelKeyPoints;
            VectorOfKeyPoint observedKeyPoints;
            using (VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch())
            {
                Mat mask;
                FindMatch(modelImage, observedImage, out modelKeyPoints, out observedKeyPoints, matches,
                   out mask, out homography, out score);

                //Draw the matched keypoints
                Mat result = new Mat();
                
                Features2DToolbox.DrawMatches(modelImage, modelKeyPoints, observedImage, observedKeyPoints,
                   matches, result, new MCvScalar(255, 255, 255), new MCvScalar(255, 255, 255), mask);
                #region draw the projected region on the image

                if (homography != null)
                {
                    //draw a rectangle along the projected model
                    Rectangle rect = new Rectangle(Point.Empty, modelImage.Size);
                    PointF[] pts = new PointF[]
                    {
                  new PointF(rect.Left, rect.Bottom),
                  new PointF(rect.Right, rect.Bottom),
                  new PointF(rect.Right, rect.Top),
                  new PointF(rect.Left, rect.Top)
                    };
                    pts = CvInvoke.PerspectiveTransform(pts, homography);

                    Point[] points = Array.ConvertAll<PointF, Point>(pts, Point.Round);
                    using (VectorOfPoint vp = new VectorOfPoint(points))
                    {
                        CvInvoke.Polylines(result, vp, true, new MCvScalar(255, 0, 0, 255), 5);
                    }
                    //pointsCount = points.Length;
                }

                #endregion

                return result;

            }
        }

        public void NormaliseImage(Image<Gray,byte> image1, Image<Gray, byte> image2, out Image<Gray, byte> image1out, out Image<Gray, byte> image2out)
        {
            image2out = image2.Resize(500, 500, Inter.Cubic);
            image1out = image1.Resize(500, 500, Inter.Cubic);
        }
    }
}
