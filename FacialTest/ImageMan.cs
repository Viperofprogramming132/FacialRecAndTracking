// Project: FacialTest
// Filename; ImageMan.cs
// Created; 14/08/2018
// Edited: 04/09/2018

namespace FacialTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Emgu.CV;
    using Emgu.CV.Cuda;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Features2D;
    using Emgu.CV.Structure;
    using Emgu.CV.Util;
    using Emgu.CV.XFeatures2D;

    public class ImageMan
    {
        private static ImageMan instance;

        private CudaClahe equalizerCudaClahe = new CudaClahe(40d, new Size(8, 8));
        

        private ImageMan()
        {
        }

        public static ImageMan Instance
        {
            get
            {
                if (instance == null) instance = new ImageMan();

                return instance;
            }
        }

        public async Task<string> CompareImagesAsync(Mat Image)
        {
            List<string> imageDirectory =
                Directory.GetFiles(Directory.GetCurrentDirectory() + "\\photos", "*.png").ToList();
            List<long> scores = new List<long>();
            foreach (string s in imageDirectory)
            {
                Mat i = new Mat(s);
                ////CvInvoke.Imshow("AbsDiff", i);
                this.NormaliseImage(Image, i, out Image, out i);

                await Task.Delay(1);

                await this.Draw(i, Image, out long score, out Mat resultMat).ConfigureAwait(false);

                CvInvoke.Imshow("AbsDiff", resultMat);
                resultMat.Dispose();
                Debug.WriteLine(score);
                scores.Add(score);
            }

            for (int i = 0; i < scores.Count; ++i)
            {
                if (scores[i] > 7) return imageDirectory[i];

                if (scores[i] < 5) return "Unusable";
            }

            return "Failed";
        }

        public List<Mat> CropImage(Mat image, List<Rectangle> ROIs)
        {
            List<Mat> returnImage = new List<Mat>();
            
            foreach (Rectangle roi in ROIs)
            {
                using (Mat i = new Mat(image,roi))
                {
                    CvInvoke.CvtColor(i,i,ColorConversion.Bgr2Gray);
                    returnImage.Add(i);
                }
            }

            return returnImage;
        }

        public void NormaliseImage(
            Mat image1,
            Mat image2,
            out Mat image1out,
            out Mat image2out)
        {
            image1out = new Mat();
            image2out = new Mat();


            CvInvoke.Resize(image1, image1out, new Size(250, 250));
            CvInvoke.Resize(image2, image2out, new Size(250, 250));
        }

        /// <summary>
        /// Save image as png
        /// </summary>
        /// <param name="path">
        /// The path.
        /// </param>
        /// <param name="img">
        /// The img.
        /// </param>
        public async void SavePng(string path, Mat img)
        {
            //Get Current path for location
            path = Directory.GetCurrentDirectory() + "\\photos\\" + path + ".png";

            GpuMat iGpuMat = new GpuMat(img);

            img.Dispose();

            this.equalizerCudaClahe.Apply(iGpuMat, iGpuMat);

            //Saves the image
            iGpuMat.Bitmap.Save(path,ImageFormat.Png);
        }

        /// <summary>
        /// Draw the model image and observed image, the matched features and homography projection.
        /// </summary>
        /// <param name="modelImage">
        /// The model image
        /// </param>
        /// <param name="observedImage">
        /// The observed image
        /// </param>
        /// <param name="score">
        /// The score.
        /// </param>
        /// <returns>
        /// The model image and observed image, the matched features and homography projection.
        /// </returns>
        private Task<Mat> Draw(Mat modelImage, Mat observedImage, out long score, out Mat result)
        {
            score = 0;
            using (VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch())
            {
                this.FindMatch(
                    modelImage,
                    observedImage,
                    out long matchTime,
                    out var modelKeyPoints,
                    out var observedKeyPoints,
                    matches,
                    out var mask,
                    out var homography,
                    out score);

                //Gets the score from the mask
                score = this.CountMatches(mask);


                //Draw the matched keypoints
                result = new Mat();
                Features2DToolbox.DrawMatches(
                    modelImage,
                    modelKeyPoints,
                    observedImage,
                    observedKeyPoints,
                    matches,
                    result,
                    new MCvScalar(255, 255, 255),
                    new MCvScalar(255, 255, 255),
                    mask);

                return Task.FromResult<Mat>(result);
            }

            return null;
        }

        /// <summary>
        /// Finds matching points in the faces using SURF
        /// </summary>
        /// <param name="modelImage">
        /// The model image.
        /// </param>
        /// <param name="observedImage">
        /// The observed image.
        /// </param>
        /// <param name="matchTime">
        /// The match time.
        /// </param>
        /// <param name="modelKeyPoints">
        /// The model key points.
        /// </param>
        /// <param name="observedKeyPoints">
        /// The observed key points.
        /// </param>
        /// <param name="matches">
        /// The matches.
        /// </param>
        /// <param name="mask">
        /// The mask.
        /// </param>
        /// <param name="homography">
        /// The homography.
        /// </param>
        /// <param name="score">
        /// The score.
        /// </param>
        private void FindMatch(
            Mat modelImage,
            Mat observedImage,
            out long matchTime,
            out VectorOfKeyPoint modelKeyPoints,
            out VectorOfKeyPoint observedKeyPoints,
            VectorOfVectorOfDMatch matches,
            out Mat mask,
            out Mat homography,
            out long score)
        {
            int k = 2;
            double uniquenessThreshold = 5;
            Stopwatch watch;
            homography = null;
            mask = null;
            score = 0;

            
            
            modelKeyPoints = new VectorOfKeyPoint();
            observedKeyPoints = new VectorOfKeyPoint();

            

            if (Controller.Instance.Cuda)
            {
                CudaSURF surfGPU = new CudaSURF(700f, 4, 2, false);
                using (CudaImage<Gray, byte> gpuModelImage = new CudaImage<Gray, byte>(modelImage))
                    //extract features from the object image
                using (GpuMat gpuModelKeyPoints = surfGPU.DetectKeyPointsRaw(gpuModelImage, null))
                using (GpuMat gpuModelDescriptors =
                    surfGPU.ComputeDescriptorsRaw(gpuModelImage, null, gpuModelKeyPoints))
                using (CudaBFMatcher matcher = new CudaBFMatcher(DistanceType.L2))
                {
                    surfGPU.DownloadKeypoints(gpuModelKeyPoints, modelKeyPoints);
                    watch = Stopwatch.StartNew();

                    // extract features from the observed image
                    using (CudaImage<Gray, Byte> gpuObservedImage = new CudaImage<Gray, byte>(observedImage))
                    using (GpuMat gpuObservedKeyPoints = surfGPU.DetectKeyPointsRaw(gpuObservedImage, null))
                    using (GpuMat gpuObservedDescriptors = surfGPU.ComputeDescriptorsRaw(
                        gpuObservedImage,
                        null,
                        gpuObservedKeyPoints))
                    using (GpuMat<int> gpuMatchIndices = new GpuMat<int>(
                        gpuObservedDescriptors.Size.Height,
                        k,
                        1,
                        true))
                    using (GpuMat<float> gpuMatchDist = new GpuMat<float>(
                        gpuObservedDescriptors.Size.Height,
                        k,
                        1,
                        true))
                    //using (GpuMat<Byte> gpuMask = new GpuMat<byte>(gpuMatchIndices.Size.Height, 1, 1))
                    using (Emgu.CV.Cuda.Stream stream = new Emgu.CV.Cuda.Stream())
                    {
                        matcher.KnnMatch(gpuObservedDescriptors, gpuModelDescriptors, matches, k, null);
                        //indices = new Matrix<int>(gpuMatchIndices.Size);
                        //mask = new Matrix<byte>(gpuMask.Size);

                        mask = new Mat(matches.Size, 1, DepthType.Cv8U, 1);
                        mask.SetTo(new MCvScalar(255));


                        surfGPU.DownloadKeypoints(gpuObservedKeyPoints, observedKeyPoints);
                        /*//gpu implementation of voteForUniquess
                        using (GpuMat col0 = gpuMatchDist.Col(0))
                        using (GpuMat col1 = gpuMatchDist.Col(1))
                        {
                            CudaInvoke.Multiply(col1, new GpuMat(), col1, 1, DepthType.Default, stream);
                            CudaInvoke.Compare(col0, col1, mask, CmpType.LessEqual, stream);
                        }*/
                        
                        Features2DToolbox.VoteForUniqueness(matches, uniquenessThreshold, mask);

                        //wait for the stream to complete its tasks
                        //We can perform some other CPU intesive stuffs here while we are waiting for the stream to complete.
                        stream.WaitForCompletion();
                        //gpuMatchIndices.Download(indices);
                        if (CudaInvoke.CountNonZero(mask) >= 4)
                        {
                            int nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(
                                modelKeyPoints,
                                observedKeyPoints,
                                matches,
                                mask,
                                1.5,
                                20);
                            if (nonZeroCount >= 4)
                                homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(
                                    modelKeyPoints,
                                    observedKeyPoints,
                                    matches,
                                    mask,
                                    2);
                        }

                        watch.Stop();
                    }

                    for (int i = 0; i < matches.Size; i++) score++;
                }
            }

            //else
            //{
            //    SURF surfCPU = new SURF(500, 4, 2, false);
            //    //extract features from the object image
            //    modelKeyPoints = new VectorOfKeyPoint();
            //    Matrix<float> modelDescriptors = surfCPU.DetectAndCompute(modelImage, null, modelKeyPoints);

            //    watch = Stopwatch.StartNew();

            //    // extract features from the observed image
            //    observedKeyPoints = new VectorOfKeyPoint();
            //    Matrix<float> observedDescriptors = surfCPU.DetectAndCompute(observedImage, null, observedKeyPoints);
            //    BFMatcher matcher = new BFMatcher<float>(DistanceType.L2);
            //    matcher.Add(modelDescriptors);

            //    indices = new Matrix<int>(observedDescriptors.Rows, k);
            //    using (Matrix<float> dist = new Matrix<float>(observedDescriptors.Rows, k))
            //    {
            //        matcher.KnnMatch(observedDescriptors, indices, dist, k, null);
            //        mask = new Matrix<byte>(dist.Rows, 1);
            //        mask.SetValue(255);
            //        Features2DToolbox.VoteForUniqueness(dist, uniquenessThreshold, mask);
            //    }

            //    int nonZeroCount = CvInvoke.cvCountNonZero(mask);
            //    if (nonZeroCount >= 4)
            //    {
            //        nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints, indices, mask, 1.5, 20);
            //        if (nonZeroCount >= 4)
            //            homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeyPoints, observedKeyPoints, indices, mask, 2);
            //    }

            //    watch.Stop();
            //}
            matchTime = 0;
        }

        /// <summary>
        /// The get encoder info.
        /// </summary>
        /// <param name="mimeType">
        /// The mime type.
        /// </param>
        /// <returns>
        /// The <see cref="ImageCodecInfo"/>.
        /// </returns>
        private ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            // Get image codecs for all image formats
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            // Find the correct image codec
            for (int i = 0; i < codecs.Length; i++)
                if (codecs[i].MimeType == mimeType)
                    return codecs[i];
            return null;
        }

        private int CountMatches(Mat inputMat)
        {
            Matrix<byte> xx = new Matrix<byte>(inputMat.Rows, inputMat.Cols);
            inputMat.CopyTo(xx);

            var matched = xx.ManagedArray;
            var list = matched.OfType<byte>().ToList();
            var count = list.Count(a => a.Equals(1));

            Debug.WriteLine(count);

            return count;
        }
    }
}