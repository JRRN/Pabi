using Microsoft.Toolkit.Uwp.UI.Controls;
using Pabi.Helpers;
using Pabi.Models;
using Pabi.Models.TinyYOLO;
using Pabi.OnnxModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.AI.MachineLearning;
using Windows.Media;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Pabi
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private LearningModelYolo _model;
        private IList<YoloBoundingBox> _boxes = new List<YoloBoundingBox>();
        private readonly YoloWinMlParser _parser = new YoloWinMlParser();
        private Stopwatch _stopwatch;

        private readonly SolidColorBrush _lineBrushYellow = new SolidColorBrush(Windows.UI.Colors.Yellow);
        private readonly SolidColorBrush _lineBrushGreen = new SolidColorBrush(Windows.UI.Colors.Green);
        private readonly SolidColorBrush _fillBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);
        private readonly double _lineThickness = 2.0;
        private CameraModel _cameraModel;

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
           _model = await LearningModelYolo.CreateFromStreamAsync();
           _cameraModel = new CameraModel().GetCameraSize(CameraPreview);
            await CameraPreview.StartAsync();
            CameraPreview.CameraHelper.FrameArrived += CameraHelper_FrameArrived;
        }

        private async void CameraHelper_FrameArrived(object sender, Microsoft.Toolkit.Uwp.Helpers.FrameEventArgs e)
        {
            if (e?.VideoFrame?.SoftwareBitmap == null) return;
            _stopwatch = Stopwatch.StartNew();
            ImageFeatureValue _image = ImageFeatureValue.CreateFromVideoFrame(e.VideoFrame);

            var input = new Yolov2Input
            {
                image = _image
            };
            var output = _model.EvaluateAsync(input).GetAwaiter().GetResult();
            _stopwatch.Stop();

            IReadOnlyList<float> vectorImage = output.grid.GetAsVectorView();
            IList<float> imageList = vectorImage.ToList();
            _boxes = _parser.ParseOutputs(vectorImage.ToArray());

            var maxIndex = imageList.IndexOf(imageList.Max());
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                TextBlockInformation.Text = $"{1000f / _stopwatch.ElapsedMilliseconds,4:f1} fps on Width {_cameraModel.Width} x Height {_cameraModel}";
                DrawOverlays(e.VideoFrame);
            });
        }

        private void DrawOverlays(VideoFrame inputImage)
        {
            YoloCanvas.Children.Clear();
            if (_boxes.Count <= 0) return;
            var filteredBoxes = _parser.NonMaxSuppress(_boxes, 5, .5F);

            foreach (var box in filteredBoxes)
                DrawYoloBoundingBox(box, YoloCanvas);
        }
        private void DrawYoloBoundingBox(YoloBoundingBox box, Canvas overlayCanvas)
        {
            // process output boxes
            var x = (uint)Math.Max(box.X, 0);
            var y = (uint)Math.Max(box.Y, 0);
            var w = (uint)Math.Min(overlayCanvas.ActualWidth - x, box.Width);
            var h = (uint)Math.Min(overlayCanvas.ActualHeight - y, box.Height);

            // fit to current canvas and webcam size
            x = _cameraModel.Width * x / 500;
            y = _cameraModel.Height * y / 300;
            w = _cameraModel.Width * w / 500;
            h = _cameraModel.Height * h / 300;

            var rectStroke = box.Label == "person" ? _lineBrushGreen : _lineBrushYellow;

            var r = new Windows.UI.Xaml.Shapes.Rectangle
            {
                Tag = box,
                Width = w,
                Height = h,
                Fill = _fillBrush,
                Stroke = rectStroke,
                StrokeThickness = _lineThickness,
                Margin = new Thickness(x, y, 0, 0)
            };

            var tb = new TextBlock
            {
                Margin = new Thickness(x + 4, y + 4, 0, 0),
                Text = $"{box.Label} ({Math.Round(box.Confidence, 4)})",
                FontWeight = FontWeights.Bold,
                Width = 126,
                Height = 21,
                HorizontalTextAlignment = TextAlignment.Center
            };

            var textBack = new Windows.UI.Xaml.Shapes.Rectangle
            {
                Width = 134,
                Height = 29,
                Fill = rectStroke,
                Margin = new Thickness(x, y, 0, 0)
            };

            overlayCanvas.Children.Add(textBack);
            overlayCanvas.Children.Add(tb);
            overlayCanvas.Children.Add(r);
        }
    }
}

