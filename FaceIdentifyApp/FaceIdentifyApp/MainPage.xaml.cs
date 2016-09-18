using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Media.FaceAnalysis;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media;
using Windows.System.Threading;
using System.Threading;
using Windows.ApplicationModel;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FaceIdentifyApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
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

        private SocketClient socketClient;

        // The media object for controlling and playing audio.
        private MediaElement mediaElement;

        // The object for controlling the speech synthesis engine (voice).
        private SpeechSynthesizer speechSynthesizer;

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

        private AutoCaptureState currentState;
        public event EventHandler<AutoCaptureState> AutoCaptureStateChanged;

        public MainPage()
        {
            this.InitializeComponent();
            mediaCapture = new MediaCapture();
            socketClient = new SocketClient();
            emotionRecognition = new EmotionRecognition();
            faceIdentity = new FaceIdentity();
            speechSynthesizer = new SpeechSynthesizer();
            mediaElement = new MediaElement();
            emotionScoresList = new List<EmotionScore>();
            emotionColor = new EmotionColor();

            InitializeCapture();

           
        }


        //clear textbox text
        private void clearUI()
        {
            imagePreview.Source = null;
            identityTextBox.Text = "";
            emotionTextBox.Text = "";
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

        // start camera capture
        private async void StartCapturePreview_Click(object sender, RoutedEventArgs e)
        {
            clearUI();
            capturePreview.Source = mediaCapture;

            // Cache the media properties as we'll need them later.
            var deviceController = this.mediaCapture.VideoDeviceController;
            this.videoProperties = deviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;

            await mediaCapture.StartPreviewAsync();

            // Use a 66 millisecond interval for our timer, i.e. 15 frames per second
            TimeSpan timerInterval = TimeSpan.FromMilliseconds(66);
            this.frameProcessingTimer = ThreadPoolTimer.CreatePeriodicTimer(new TimerElapsedHandler(ProcessCurrentVideoFrame), timerInterval);

            


            startCaptureButton.IsEnabled = false;
        }

        // capture a photo
        private async void CapturePhoto_Click(object sender, RoutedEventArgs e)
        {
            startCaptureButton.IsEnabled = true;
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
            identityTextBox.Text = identityResult;


            Stream emotionDetectStream = await emotionFile.OpenStreamForReadAsync();
            string emotionResult = await emotionRecognition.GetEmotions(emotionDetectStream);
            emotionTextBox.Text = emotionResult;


            emotionColor.SetColor(identityResult, emotionResult);

        }

        // Show image picker, show jpg, png type files
        private async Task<StorageFile> filePicker()
        {
            clearUI();
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".png");
            StorageFile file = await openPicker.PickSingleFileAsync();
            return file;
        }

        //brouse a image file
        private async void Browse_Click(object sender, RoutedEventArgs e)
        {
            clearUI();
            StorageFile file = await filePicker();
            if (file != null)
            {
                var imageStream = await file.OpenAsync(FileAccessMode.Read);
                Stream facesDetectStream = await file.OpenStreamForReadAsync();
                BitmapImage bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(imageStream);
                imagePreview.Source = bitmapImage;


                Stream identityDetectStream = await file.OpenStreamForReadAsync();
                string identityResult = await faceIdentity.GetIdentity(identityDetectStream);
                identityTextBox.Text = identityResult;

                Stream emotionDetectStream = await file.OpenStreamForReadAsync();
                string emotionResult = await emotionRecognition.GetEmotions(emotionDetectStream);
                emotionTextBox.Text = emotionResult;

                emotionColor.SetColor(identityResult, emotionResult);
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
            Debug.WriteLine("VisualizationCanvas Count:" + VisualizationCanvas.Children.Count);
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
                        Debug.WriteLine("DetectedFace Count:"+faces.Count);
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
                        Debug.WriteLine("SetupVisualization");
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
