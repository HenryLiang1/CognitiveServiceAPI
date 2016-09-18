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
using Windows.UI.Core;
using static FaceIdentifyApp.Control.CameraControl;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FaceIdentifyApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private SocketClient socketClient;

        


        public MainPage()
        {
            this.InitializeComponent();
            socketClient = new SocketClient();
            

            Window.Current.Activated += CurrentWindowActivationStateChanged;
            this.cameraControl.AutoCaptureStateChanged += CameraControl_AutoCaptureStateChanged;
        }


        private void CurrentWindowActivationStateChanged(object sender, WindowActivatedEventArgs e)
        {
            Debug.WriteLine(this.cameraControl.CameraStreamState);
            if ((e.WindowActivationState == CoreWindowActivationState.CodeActivated ||
                e.WindowActivationState == CoreWindowActivationState.PointerActivated) &&
                this.cameraControl.CameraStreamState == Windows.Media.Devices.CameraStreamState.NotStreaming)
            {
                // When our Window loses focus due to user interaction Windows shuts it down, so we 
                // detect here when the window regains focus and trigger a restart of the camera.
               
                this.cameraControl.StartPreview();
            }
        }

        private async void CameraControl_AutoCaptureStateChanged(object sender, AutoCaptureState e)
        {
            switch (e)
            {
                case AutoCaptureState.WaitingForFaces:
                    //this.cameraGuideBallon.Opacity = 1;
                    //this.cameraGuideText.Text = "Step in front of the camera to start!";
                    //this.cameraGuideHost.Opacity = 1;
                    break;
                case AutoCaptureState.WaitingForStillFaces:
                    //this.cameraGuideText.Text = "Hold still...";
                    break;
                case AutoCaptureState.ShowingCountdownForCapture:
                    //this.cameraGuideText.Text = "";
                    //this.cameraGuideBallon.Opacity = 0;

                    this.cameraGuideCountdownHost.Opacity = 1;
                    this.countDownTextBlock.Text = "3";
                    await Task.Delay(750);
                    this.countDownTextBlock.Text = "2";
                    await Task.Delay(750);
                    this.countDownTextBlock.Text = "1";
                    await Task.Delay(750);
                    this.cameraGuideCountdownHost.Opacity = 0;

                    //this.cameraControl.CapturePhoto();
                    break;
                case AutoCaptureState.ShowingCapturedPhoto:
                    //this.cameraGuideHost.Opacity = 0;
                    break;
                default:
                    break;
            }
        }


        //clear textbox text
        private void clearUI()
        {
            //imagePreview.Source = null;
            identityTextBox.Text = "";
            emotionTextBox.Text = "";
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
            /*clearUI();
            StorageFile file = await filePicker();
            if (file != null)
            {
                var imageStream = await file.OpenAsync(FileAccessMode.Read);
                Stream facesDetectStream = await file.OpenStreamForReadAsync();
                BitmapImage bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(imageStream);
                //imagePreview.Source = bitmapImage;


                Stream identityDetectStream = await file.OpenStreamForReadAsync();
                string identityResult = await faceIdentity.GetIdentity(identityDetectStream);
                identityTextBox.Text = identityResult;

                Stream emotionDetectStream = await file.OpenStreamForReadAsync();
                string emotionResult = await emotionRecognition.GetEmotions(emotionDetectStream);
                emotionTextBox.Text = emotionResult;

                emotionColor.SetColor(identityResult, emotionResult);
            }*/
        }


      

        }

    }




