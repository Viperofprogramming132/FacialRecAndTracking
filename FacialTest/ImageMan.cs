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

        public static void FindMatch(Mat modelImage, Mat observedImage, out long matchTime, out VectorOfKeyPoint modelKeyPoints, out VectorOfKeyPoint observedKeyPoints, VectorOfVectorOfDMatch matches, out Mat mask, out Mat homography, out long score)
        {
            int k = 2;
            double uniquenessThreshold = 5;

            Stopwatch watch;
            homography = null;
            mask = null;
            score = 0;
            bool mozan = true;
            modelKeyPoints = new VectorOfKeyPoint();
            observedKeyPoints = new VectorOfKeyPoint();
            if (mozan == true)
            {
                CudaSURF surfGPU = new CudaSURF(500,4,2,false);
                using (CudaImage<Gray, Byte> gpuModelImage = new CudaImage<Gray, byte>(modelImage))
                //extract features from the object image
                using (GpuMat gpuModelKeyPoints = surfGPU.DetectKeyPointsRaw(gpuModelImage, null))
                using (GpuMat gpuModelDescriptors = surfGPU.ComputeDescriptorsRaw(gpuModelImage, null, gpuModelKeyPoints))
                using (CudaBFMatcher matcher = new CudaBFMatcher(DistanceType.L2))
                {
                    
                    surfGPU.DownloadKeypoints(gpuModelKeyPoints, modelKeyPoints);
                    watch = Stopwatch.StartNew();

                    // extract features from the observed image
                    using (CudaImage<Gray, Byte> gpuObservedImage = new CudaImage<Gray, byte>(observedImage))
                    using (GpuMat gpuObservedKeyPoints = surfGPU.DetectKeyPointsRaw(gpuObservedImage, null))
                    using (GpuMat gpuObservedDescriptors = surfGPU.ComputeDescriptorsRaw(gpuObservedImage, null, gpuObservedKeyPoints))
                    using (GpuMat<int> gpuMatchIndices = new GpuMat<int>(gpuObservedDescriptors.Size.Height, k, 1, true))
                    using (GpuMat<float> gpuMatchDist = new GpuMat<float>(gpuObservedDescriptors.Size.Height, k, 1, true))
                    using (GpuMat<Byte> gpuMask = new GpuMat<byte>(gpuMatchIndices.Size.Height, 1, 1))
                    using (Emgu.CV.Cuda.Stream stream = new Emgu.CV.Cuda.Stream())
                    {
                        matcher.KnnMatch(gpuObservedDescriptors, gpuModelDescriptors, matches, k, null);
                        //indices = new Matrix<int>(gpuMatchIndices.Size);
                        //mask = new Matrix<byte>(gpuMask.Size);
                        

                        Features2DToolbox.VoteForUniqueness(matches, uniquenessThreshold,gpuMask.ToMat());
                        /*//gpu implementation of voteForUniquess
                        using (GpuMat col0 = gpuMatchDist.Col(0))
                        using (GpuMat col1 = gpuMatchDist.Col(1))
                        {
                            CudaInvoke.Multiply(col1, new GpuMat(), col1, 1, DepthType.Default, stream);
                            CudaInvoke.Compare(col0, col1, gpuMask, CmpType.LessEqual, stream);
                        }*/

                        
                        surfGPU.DownloadKeypoints(gpuObservedKeyPoints, observedKeyPoints);

                        //wait for the stream to complete its tasks
                        //We can perform some other CPU intesive stuffs here while we are waiting for the stream to complete.
                        stream.WaitForCompletion();

                        gpuMask.Download(mask);
                        //gpuMatchIndices.Download(indices);

                        if (CudaInvoke.CountNonZero(gpuMask) >= 4)
                        {
                            int nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints, matches, mask, 1.5, 20);
                            if (nonZeroCount >= 4)
                                homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeyPoints, observedKeyPoints, matches, mask, 2);
                        }

                        watch.Stop();
                    }

                    for (int i = 0; i < matches.Size; i++)
                    {
                        score++;
                    }
                }
            }
            /*else
            {
                SURF surfCPU = new SURF(500, 4, 2, false);
                //extract features from the object image
                modelKeyPoints = new VectorOfKeyPoint();
                Matrix<float> modelDescriptors = surfCPU.DetectAndCompute(modelImage, null, modelKeyPoints);

                watch = Stopwatch.StartNew();

                // extract features from the observed image
                observedKeyPoints = new VectorOfKeyPoint();
                Matrix<float> observedDescriptors = surfCPU.DetectAndCompute(observedImage, null, observedKeyPoints);
                BruteForceMatcher<float> matcher = new BruteForceMatcher<float>(DistanceType.L2);
                matcher.Add(modelDescriptors);

                indices = new Matrix<int>(observedDescriptors.Rows, k);
                using (Matrix<float> dist = new Matrix<float>(observedDescriptors.Rows, k))
                {
                    matcher.KnnMatch(observedDescriptors, indices, dist, k, null);
                    mask = new Matrix<byte>(dist.Rows, 1);
                    mask.SetValue(255);
                    Features2DToolbox.VoteForUniqueness(dist, uniquenessThreshold, mask);
                }

                int nonZeroCount = CvInvoke.cvCountNonZero(mask);
                if (nonZeroCount >= 4)
                {
                    nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints, indices, mask, 1.5, 20);
                    if (nonZeroCount >= 4)
                        homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeyPoints, observedKeyPoints, indices, mask, 2);
                }

                watch.Stop();
            }*/
            matchTime = 0;
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
            score = 0;
            using (VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch())
            {
                Mat mask;
                FindMatch(modelImage, observedImage, out long matchTime, out modelKeyPoints, out observedKeyPoints, matches,
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

            return null;
        }

        public void NormaliseImage(Image<Gray,byte> image1, Image<Gray, byte> image2, out Image<Gray, byte> image1out, out Image<Gray, byte> image2out)
        {
            image2out = image2.Resize(500, 500, Inter.Cubic);
            image1out = image1.Resize(500, 500, Inter.Cubic);
        }
    }
}
