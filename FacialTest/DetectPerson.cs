// Project: FacialTest
// Filename; DetectFace.cs
// Created; 10/08/2018
// Edited: 04/09/2018
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;


#if !(__IOS__ || NETFX_CORE)
using Emgu.CV.Cuda;

#endif

namespace FacialTest
{
    public class DetectPerson
    {
        private CudaCascadeClassifier cudaFace = new CudaCascadeClassifier("haarcascade_frontalface_default.xml");

        private CascadeClassifier face = new CascadeClassifier("haarcascade_frontalface_default.xml");

        private CudaCascadeClassifier cudaBody = new CudaCascadeClassifier("cuda_haarcascade_fullbody.xml");

        private CascadeClassifier body = new CascadeClassifier("haarcascade_fullbody.xml");

        public void Detect(
            IInputArray image,
            List<Rectangle> faces)
        {

            using (var iaImage = image.GetInputArray())
            {
                if (Controller.Instance.Cuda)
                {
                    cudaFace.ScaleFactor = 2;
                    cudaFace.MinNeighbors = 3;
                    cudaFace.MinObjectSize = Size.Empty;

                    using (CudaImage<Bgr, byte> gpuImage = new CudaImage<Bgr, byte>(image))
                    using (CudaImage<Gray, byte> gpuGray = gpuImage.Convert<Gray, byte>())
                    using (GpuMat region = new GpuMat())
                    {
                        cudaFace.DetectMultiScale(gpuGray, region);

                        Rectangle[] faceRegion = cudaFace.Convert(region);
                        faces.AddRange(faceRegion);
                    }
                }
                else
                {
                    using (UMat ugray = new UMat())
                    {
                        CvInvoke.CvtColor(image, ugray, ColorConversion.Bgr2Gray);

                        //normalizes brightness and increases contrast of the image
                        CvInvoke.EqualizeHist(ugray, ugray);

                        //Detect the faces  from the gray scale image and store the locations as rectangle
                        //The first dimensional is the channel
                        //The second dimension is the index of the rectangle in the specific channel                     
                        Rectangle[] facesDetected = face.DetectMultiScale(
                        ugray,
                        1.1,
                        10,
                        new Size(20, 20));

                        faces.AddRange(facesDetected);
                    }
                }
            }
        }

        public void FindPerson(IInputArray image, List<Rectangle> People)
        {
            using (var iaImage = image.GetInputArray())
            {
                if (Controller.Instance.Cuda)
                {
                    this.cudaBody.ScaleFactor = 3.5;
                    this.cudaBody.MinNeighbors = 0;
                    this.cudaBody.MinObjectSize = Size.Empty;

                    using (CudaImage<Bgr, byte> gpuImage = new CudaImage<Bgr, byte>(image))
                    using (CudaImage<Gray, byte> gpuGray = gpuImage.Convert<Gray, byte>())
                    using (GpuMat region = new GpuMat())
                    {
                        try
                        {
                            cudaBody.DetectMultiScale(gpuGray, region);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                        

                        Rectangle[] faceRegion = this.cudaBody.Convert(region);
                        People.AddRange(faceRegion);
                    }
                }
                else
                {
                    using (UMat ugray = new UMat())
                    {
                        CvInvoke.CvtColor(image, ugray, ColorConversion.Bgr2Gray);

                        //normalizes brightness and increases contrast of the image
                        CvInvoke.EqualizeHist(ugray, ugray);

                        //Detect the faces  from the gray scale image and store the locations as rectangle
                        //The first dimensional is the channel
                        //The second dimension is the index of the rectangle in the specific channel                     
                        Rectangle[] facesDetected = this.body.DetectMultiScale(
                            ugray,
                            1.1,
                            10,
                            new Size(20, 20));

                        People.AddRange(facesDetected);
                    }
                }
            }
        }
    }
}
