// Project: FacialTest
// Filename; DetectFace.cs
// Created; 10/08/2018
// Edited: 04/09/2018

#if !(__IOS__ || NETFX_CORE)
using Emgu.CV.Cuda;

#endif

namespace FacialTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;

    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;
    using Emgu.CV.Util;

    public static class DetectPerson
    {
        public static void Detect(
            IInputArray image,
            string faceFileName,
            string eyeFileName,
            List<Rectangle> faces,
            List<Rectangle> eyes,
            out long detectionTime)
        {
            Stopwatch watch;

            using (var iaImage = image.GetInputArray())
            {
                if (Controller.Instance.Cuda)
                {
                using (CudaCascadeClassifier face = new CudaCascadeClassifier(faceFileName))
                using (CudaCascadeClassifier eye = new CudaCascadeClassifier(eyeFileName))
                {
                    face.ScaleFactor = 1.1;
                    face.MinNeighbors = 10;
                    face.MinObjectSize = Size.Empty;
                    eye.ScaleFactor = 1.1;
                    eye.MinNeighbors = 10;
                    eye.MinObjectSize = Size.Empty;
                    watch = Stopwatch.StartNew();
                    using (CudaImage<Bgr, byte> gpuImage = new CudaImage<Bgr, byte>(image))
                    using (CudaImage<Gray, byte> gpuGray = gpuImage.Convert<Gray, byte>())
                    using (GpuMat region = new GpuMat())
                    {
                        face.DetectMultiScale(gpuGray, region);
                        Rectangle[] faceRegion = face.Convert(region);
                        faces.AddRange(faceRegion);
                        foreach (Rectangle f in faceRegion)
                        {
                            using (CudaImage<Gray, Byte> faceImg = gpuGray.GetSubRect(f))
                            {
                                //For some reason a clone is required.
                                //Might be a bug of CudaCascadeClassifier in opencv
                                using (CudaImage<Gray, byte> clone = faceImg.Clone(null))
                                using (GpuMat eyeRegionMat = new GpuMat())
                                {
                                    eye.DetectMultiScale(clone, eyeRegionMat);
                                    Rectangle[] eyeRegion = eye.Convert(eyeRegionMat);
                                    foreach (Rectangle e in eyeRegion)
                                    {
                                        Rectangle eyeRect = e;
                                        eyeRect.Offset(f.X, f.Y);
                                        eyes.Add(eyeRect);
                                    }
                                }
                            }
                        }
                    }
                    watch.Stop();
                }

                }
                else
                {
                    //Read the HaarCascade objects
                    using (CascadeClassifier face = new CascadeClassifier(faceFileName))
                    using (CascadeClassifier eye = new CascadeClassifier(eyeFileName))
                    {
                        watch = Stopwatch.StartNew();

                        using (UMat ugray = new UMat())
                        {
                            CvInvoke.CvtColor(image, ugray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);

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

                            foreach (Rectangle f in facesDetected)
                            {
                                //Get the region of interest on the faces
                                using (UMat faceRegion = new UMat(ugray, f))
                                {
                                    Rectangle[] eyesDetected = eye.DetectMultiScale(
                                    faceRegion,
                                    1.1,
                                    10,
                                    new Size(20, 20));

                                    foreach (Rectangle e in eyesDetected)
                                    {
                                        Rectangle eyeRect = e;
                                        eyeRect.Offset(f.X, f.Y);
                                        eyes.Add(eyeRect);
                                    }
                                }
                            }
                        }
                        watch.Stop();
                    }
                }
                detectionTime = watch.ElapsedMilliseconds;
            }
        }

        public static Rectangle[] FindPerson(IInputArray image, out long processingTime)
        {
            Stopwatch watch = new Stopwatch();
            Rectangle[] regions = null;


            if (Controller.Instance.Cuda)
            {
                GpuMat GpuImage = new GpuMat(image);

                using (InputArray iaImage = GpuImage.GetInputArray())
                {
                    //if the input array is a GpuMat
                    //check if there is a compatible Cuda device to run pedestrian detection
                    if (iaImage.Kind == InputArray.Type.CudaGpuMat)
                    {
                        //this is the Cuda version
                        using (CudaHOG des = new CudaHOG(
                            new Size(64, 128),
                            new Size(16, 16),
                            new Size(8, 8),
                            new Size(8, 8)))
                        {
                            des.SetSVMDetector(des.GetDefaultPeopleDetector());

                            watch = Stopwatch.StartNew();
                            using (GpuMat cudaBgra = new GpuMat())
                            using (VectorOfRect vr = new VectorOfRect())
                            {
                                CudaInvoke.CvtColor(image, cudaBgra, ColorConversion.Bgr2Bgra);
                                des.DetectMultiScale(cudaBgra, vr);
                                regions = vr.ToArray();
                            }
                        }
                    }
                }
            }
            else
            {
                using (InputArray iaImage = image.GetInputArray())
                {
                    //this is the CPU/OpenCL version
                    using (HOGDescriptor des = new HOGDescriptor())
                    {
                        des.SetSVMDetector(HOGDescriptor.GetDefaultPeopleDetector());
                        watch = Stopwatch.StartNew();

                        MCvObjectDetection[] results = des.DetectMultiScale(image);
                        regions = new Rectangle[results.Length];
                        for (int i = 0; i < results.Length; i++)
                            regions[i] = results[i].Rect;
                        watch.Stop();
                    }
                }

            }
            

                processingTime = watch.ElapsedMilliseconds;

                return regions;
            }
        }
    }
