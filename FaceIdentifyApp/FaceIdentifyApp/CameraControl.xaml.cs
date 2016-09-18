using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.FaceAnalysis;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace FaceIdentifyApp.Control
{
    public sealed partial class CameraControl : UserControl
    {
        private MediaCapture mediaCapture;

        /// Cache of properties from the current MediaCapture device which is used for capturing the preview frame.
        private VideoEncodingProperties videoProperties;

        /// References a FaceTracker instance.
        private FaceTracker faceTracker;

        /// A periodic timer to execute FaceTracker on preview frames
        private ThreadPoolTimer frameProcessingTimer;

        /// Semaphore to ensure FaceTracking logic only executes one at a time
        private SemaphoreSlim frameProcessingSemaphore = new SemaphoreSlim(1);

        private EmotionRecognition emotionRecognition;
        private FaceIdentity faceIdentity;

        private List<EmotionScore> emotionScoresList;
        private EmotionColor emotionColor;

        public enum AutoCaptureState
        {
            WaitingForFaces,
            WaitingForStillFaces,
            ShowingCountdownForCapture,
            ShowingCapturedPhoto
        }

        public event EventHandler<AutoCaptureState> AutoCaptureStateChanged;
        private AutoCaptureState autoCaptureState;
        private IEnumerable<DetectedFace> detectedFacesFromPreviousFrame;
        private DateTime timeSinceWaitingForStill;
        private DateTime lastTimeWhenAFaceWasDetected;

        public CameraStreamState CameraStreamState
        {
            get
            {
                return mediaCapture != null ? mediaCapture.CameraStreamState : CameraStreamState.NotStreaming;
            }
        }

        public CameraControl()
        {
            this.InitializeComponent();
            mediaCapture = new MediaCapture();
            emotionRecognition = new EmotionRecognition();
            faceIdentity = new FaceIdentity();
            emotionScoresList = new List<EmotionScore>();
            emotionColor = new EmotionColor();
            InitializeCapture();
        }

        // initial camera capture
        private async void InitializeCapture()
        {
            await mediaCapture.InitializeAsync();
            
            if (this.faceTracker == null)
            {
                this.faceTracker = await FaceTracker.CreateAsync();
            }
        }

        public async void StartPreview()
        {

            captureElement.Source = mediaCapture;
            // Cache the media properties as we'll need them later.
            var deviceController = this.mediaCapture.VideoDeviceController;
            this.videoProperties = deviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;

            await mediaCapture.StartPreviewAsync();

            // Use a 66 millisecond interval for our timer, i.e. 15 frames per second
            TimeSpan timerInterval = TimeSpan.FromMilliseconds(66);
            this.frameProcessingTimer = ThreadPoolTimer.CreatePeriodicTimer(new TimerElapsedHandler(ProcessCurrentVideoFrame), timerInterval);
        }

        public async void CapturePhoto()
        {
            //captureElement.Source = null;
            ImageEncodingProperties imgFormat = ImageEncodingProperties.CreateJpeg();

            // create storage file in local app storage
            StorageFile emotionFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("TestPhoto.jpg", CreationCollisionOption.GenerateUniqueName);

            // take photo
            await mediaCapture.CapturePhotoToStorageFileAsync(imgFormat, emotionFile);

            // Get photo as a BitmapImage
            BitmapImage bmpImage = new BitmapImage(new Uri(emotionFile.Path));

            // imagePreview is a <Image> object defined in XAML
            imagePreview.Source = bmpImage;
            await mediaCapture.StopPreviewAsync();


            //IRandomAccessStream stream = emotionDetectStream.AsRandomAccessStream();


            Stream identityDetectStream = await emotionFile.OpenStreamForReadAsync();
            string identityResult = await faceIdentity.GetIdentity(identityDetectStream);
            //identityTextBox.Text = identityResult;


            Stream emotionDetectStream = await emotionFile.OpenStreamForReadAsync();
            string emotionResult = await emotionRecognition.GetEmotions(emotionDetectStream);
            //emotionTextBox.Text = emotionResult;


            emotionColor.SetColor(identityResult, emotionResult);
        }

        private void OnAutoCaptureStateChanged(AutoCaptureState state)
        {
            if (this.AutoCaptureStateChanged != null)
            {
                this.AutoCaptureStateChanged(this, state);
            }
        }

        private bool AreFacesStill(IEnumerable<DetectedFace> detectedFacesFromPreviousFrame, IEnumerable<DetectedFace> detectedFacesFromCurrentFrame)
        {
            int horizontalMovementThreshold = (int)(videoProperties.Width * 0.02);
            int verticalMovementThreshold = (int)(videoProperties.Height * 0.02);

            int numStillFaces = 0;
            int totalFacesInPreviousFrame = detectedFacesFromPreviousFrame.Count();

            foreach (DetectedFace faceInPreviousFrame in detectedFacesFromPreviousFrame)
            {
                if (numStillFaces > 0 && numStillFaces >= totalFacesInPreviousFrame / 2)
                {
                    // If half or more of the faces in the previous frame are considered still we can stop. It is still enough.
                    break;
                }

                // If there is a face in the current frame that is located close enough to this one in the previous frame, we 
                // assume it is the same face and count it as a still face. 
                if (detectedFacesFromCurrentFrame.Any(f => Math.Abs((int)faceInPreviousFrame.FaceBox.X - (int)f.FaceBox.X) <= horizontalMovementThreshold &&
                                                           Math.Abs((int)faceInPreviousFrame.FaceBox.Y - (int)f.FaceBox.Y) <= verticalMovementThreshold))
                {
                    numStillFaces++;
                }
            }

            if (numStillFaces > 0 && numStillFaces >= totalFacesInPreviousFrame / 2)
            {
                // If half or more of the faces in the previous frame are considered still we consider the group as still
                return true;
            }

            return false;
        }

        private async void UpdateAutoCaptureState(IEnumerable<DetectedFace> detectedFaces)
        {
            const int IntervalBeforeCheckingForStill = 500;
            const int IntervalWithoutFacesBeforeRevertingToWaitingForFaces = 3;

            if (!detectedFaces.Any())
            {
                Debug.WriteLine(this.autoCaptureState);
                if (this.autoCaptureState == AutoCaptureState.WaitingForStillFaces &&
                    (DateTime.Now - this.lastTimeWhenAFaceWasDetected).TotalSeconds > IntervalWithoutFacesBeforeRevertingToWaitingForFaces)
                {
                    this.autoCaptureState = AutoCaptureState.WaitingForFaces;
                    await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        Debug.WriteLine("WaitingForFaces");
                        this.OnAutoCaptureStateChanged(this.autoCaptureState);
                    });
                }

                return;
            }

            this.lastTimeWhenAFaceWasDetected = DateTime.Now;

            switch (this.autoCaptureState)
            {

                case AutoCaptureState.WaitingForFaces:
                    // We were waiting for faces and got some... go to the "waiting for still" state
                    this.detectedFacesFromPreviousFrame = detectedFaces;
                    this.timeSinceWaitingForStill = DateTime.Now;
                    this.autoCaptureState = AutoCaptureState.WaitingForStillFaces;

                    await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        Debug.WriteLine("WaitingForStillFaces");
                        this.OnAutoCaptureStateChanged(this.autoCaptureState);
                    });

                    break;

                case AutoCaptureState.WaitingForStillFaces:
                    // See if we have been waiting for still faces long enough
                    if ((DateTime.Now - this.timeSinceWaitingForStill).TotalMilliseconds >= IntervalBeforeCheckingForStill)
                    {
                        // See if the faces are still enough
                        if (this.AreFacesStill(this.detectedFacesFromPreviousFrame, detectedFaces))
                        {
                            this.autoCaptureState = AutoCaptureState.ShowingCountdownForCapture;
                            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                Debug.WriteLine("ShowingCountdownForCapture");
                                this.OnAutoCaptureStateChanged(this.autoCaptureState);
                            });
                        }
                        else
                        {
                            // Faces moved too much, update the baseline and keep waiting
                            this.timeSinceWaitingForStill = DateTime.Now;
                            this.detectedFacesFromPreviousFrame = detectedFaces;
                        }
                    }
                    break;

                case AutoCaptureState.ShowingCountdownForCapture:
                    break;

                case AutoCaptureState.ShowingCapturedPhoto:
                    break;

                default:
                    break;
            }
        }

        //draw face rectangle
        private void SetupVisualization(Windows.Foundation.Size framePizelSize, IList<DetectedFace> foundFaces)
        {
            this.VisualizationCanvas.Children.Clear();

            double actualWidth = this.VisualizationCanvas.ActualWidth;
            double actualHeight = this.VisualizationCanvas.ActualHeight;

            if (foundFaces != null && actualWidth != 0 && actualHeight != 0)
            {
                double widthScale = framePizelSize.Width / actualWidth;
                double heightScale = framePizelSize.Height / actualHeight;

                foreach (DetectedFace face in foundFaces)
                {
                    // Create a rectangle element for displaying the face box but since we're using a Canvas
                    // we must scale the rectangles according to the frames's actual size.
                    Rectangle box = new Rectangle();
                    box.Width = (uint)(face.FaceBox.Width / widthScale);
                    box.Height = (uint)(face.FaceBox.Height / heightScale);
                    box.Fill = new SolidColorBrush(Windows.UI.Colors.Transparent);
                    box.Stroke = new SolidColorBrush(Windows.UI.Colors.Yellow);
                    box.StrokeThickness = 2.0;
                    box.Margin = new Thickness((uint)(face.FaceBox.X / widthScale), (uint)(face.FaceBox.Y / heightScale), 0, 0);

                    this.VisualizationCanvas.Children.Add(box);
                }
            }
        }


        //face tracking
        private async void ProcessCurrentVideoFrame(ThreadPoolTimer timer)
        {

            /*if (this.currentState != ScenarioState.Streaming)
            {
                return;
            }*/

            // If a lock is being held it means we're still waiting for processing work on the previous frame to complete.
            // In this situation, don't wait on the semaphore but exit immediately.
            if (!frameProcessingSemaphore.Wait(0))
            {
                return;
            }

            try
            {
                IList<DetectedFace> faces = null;

                // Create a VideoFrame object specifying the pixel format we want our capture image to be (NV12 bitmap in this case).
                // GetPreviewFrame will convert the native webcam frame into this format.
                const BitmapPixelFormat InputPixelFormat = BitmapPixelFormat.Nv12;
                using (VideoFrame previewFrame = new VideoFrame(InputPixelFormat, (int)this.videoProperties.Width, (int)this.videoProperties.Height))
                {
                    await this.mediaCapture.GetPreviewFrameAsync(previewFrame);

                    // The returned VideoFrame should be in the supported NV12 format but we need to verify this.
                    if (FaceDetector.IsBitmapPixelFormatSupported(previewFrame.SoftwareBitmap.BitmapPixelFormat))
                    {
                        faces = await this.faceTracker.ProcessNextFrameAsync(previewFrame);
                        
                        this.UpdateAutoCaptureState(faces);
                        
                    }
                    else
                    {
                        throw new NotSupportedException("PixelFormat '" + InputPixelFormat.ToString() + "' is not supported by FaceDetector");
                    }

                    // Create our visualization using the frame dimensions and face results but run it on the UI thread.
                    var previewFrameSize = new Windows.Foundation.Size(previewFrame.SoftwareBitmap.PixelWidth, previewFrame.SoftwareBitmap.PixelHeight);
                    var ignored = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        this.SetupVisualization(previewFrameSize, faces);
                    });
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                frameProcessingSemaphore.Release();
            }

        }
    }
}
