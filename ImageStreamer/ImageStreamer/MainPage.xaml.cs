using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App8
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // PUT YOUR IP ADDRESS HERE:
        static string url = "http://10.137.184.197:8000/vroot";
        //static string url = "http://10.137.184.185:8000/vroot";

        DispatcherTimer _dispatcherTimer;
        private MediaCapture _mediaCapture;

        public MainPage()
        {
            this.InitializeComponent();
            var run = Run();
        }

        byte[] capturedBytes = null;

        private async Task<byte[]> GetBytesFromVideo()
        {
            // Can't use media capture to capture too often...
            if (capturedBytes == null)
            {
                _dispatcherTimer.Stop();

                var type = ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8);

                var lowLagCapture = await _mediaCapture.PrepareLowLagPhotoCaptureAsync(type);

                var capturedPhoto = await lowLagCapture.CaptureAsync();
                var softwareBitmap = capturedPhoto.Frame.SoftwareBitmap;
                await lowLagCapture.FinishAsync();

                SoftwareBitmap softwareBitmapBGRA8 = SoftwareBitmap.Convert(softwareBitmap,
                            BitmapPixelFormat.Bgra8,
                            BitmapAlphaMode.Premultiplied);

                byte[] bytes = new byte[softwareBitmapBGRA8.PixelHeight * softwareBitmapBGRA8.PixelWidth * 4];
                softwareBitmapBGRA8.CopyToBuffer(bytes.AsBuffer());

                capturedBytes = bytes;

                _dispatcherTimer.Start();
            }
            return capturedBytes;
        }

        private void _dispatcherTimer_Tick(object sender, object e)
        {
            var up = Upload();
        }

        private async Task Run()
        {
            try
            {
                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync();
                _mediaCapture.Failed += MediaCapture_Failed;

                PreviewControl.Source = _mediaCapture;
                await _mediaCapture.StartPreviewAsync();

                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;

            }
            catch (UnauthorizedAccessException)
            {
                // This will be thrown if the user denied access to the camera in privacy settings
                System.Diagnostics.Debug.WriteLine("The app was denied access to the camera");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("MediaCapture initialization failed. {0}", ex.Message);
            }

            _dispatcherTimer = new DispatcherTimer();
            _dispatcherTimer.Tick += _dispatcherTimer_Tick;
            _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(33);
            //await Task.Delay(1000);
            _dispatcherTimer.Start();
        }

        private void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            throw new NotImplementedException();
        }

        private async Task Upload()
        {
            try
            {
                var client = new HttpClient();

                var requestContent = new MultipartFormDataContent();

                var bytes = await GetBytesFromVideo();
                var imageContent = new ByteArrayContent(bytes);
                imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");

                requestContent.Add(imageContent, "image", "image.jpg");

                await client.PostAsync(url, requestContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Upload failed. {0}", ex.Message);
            }

            // Compare this to basic image transform:
            /*
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var bytes = await GetBytesFromVideo();
            var bytes2 = new byte[bytes.Length];
            for (int i = 0; i < bytes.Length; ++i)
            {
                bytes2[i] = (byte)(bytes[i] + 1);
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            System.Diagnostics.Debug.WriteLine("elapsedMs = {0}", elapsedMs);

            this.text.Text = elapsedMs.ToString();
            */
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //var up = Upload();
        }
    }
}
