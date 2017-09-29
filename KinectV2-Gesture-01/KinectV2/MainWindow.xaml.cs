using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Microsoft.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;
using System.Drawing;

namespace KinectV2
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor kinect;
        // Color
        ColorFrameReader colorFrameReader;
        FrameDescription colorFrameDesc;
        ColorImageFormat colorFormat = ColorImageFormat.Bgra;
        // Body
        int BODY_COUNT;
        BodyFrameReader bodyFrameReader;
        Body[] bodies;
        // Gesture
        VisualGestureBuilderFrameReader[] gestureFrameReaders;
        IReadOnlyList<Gesture> gestures;

        // WPF
        WriteableBitmap colorBitmap;
        byte[] colorBuffer;
        int colorStride;
        Int32Rect colorRect;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded( object sender, RoutedEventArgs e )
        {
            try {
                kinect = KinectSensor.GetDefault();
                if ( kinect == null ) {
                    throw new Exception( "Kinectを開けません" );
                }

                kinect.Open();
                // カラー画像の情報を作成する(BGRAフォーマット)
                colorFrameDesc = kinect.ColorFrameSource.CreateFrameDescription(
                                                        colorFormat );

                // カラーリーダーを開く
                colorFrameReader = kinect.ColorFrameSource.OpenReader();
                colorFrameReader.FrameArrived += colorFrameReader_FrameArrived;

                // カラー用のビットマップを作成する
                colorBitmap = new WriteableBitmap(
                                    colorFrameDesc.Width, colorFrameDesc.Height,
                                    96, 96, PixelFormats.Bgra32, null );
                colorStride = colorFrameDesc.Width * (int)colorFrameDesc.BytesPerPixel;
                colorRect = new Int32Rect( 0, 0,
                                    colorFrameDesc.Width, colorFrameDesc.Height );
                colorBuffer = new byte[colorStride * colorFrameDesc.Height];
                ImageColor.Source = colorBitmap;

                // Bodyの最大数を取得する
                BODY_COUNT = kinect.BodyFrameSource.BodyCount;

                // Bodyを入れる配列を作る
                bodies = new Body[BODY_COUNT];

                // ボディーリーダーを開く
                bodyFrameReader = kinect.BodyFrameSource.OpenReader();
                bodyFrameReader.FrameArrived += bodyFrameReader_FrameArrived;

                InitializeGesture();
            }
            catch ( Exception ex ) {
                MessageBox.Show( ex.Message );
                Close();
            }
        }

        void colorFrameReader_FrameArrived( object sender, ColorFrameArrivedEventArgs e )
        {
            UpdateColorFrame( e );
            DrawColorFrame();
        }
        void UpdateColorFrame( ColorFrameArrivedEventArgs e )
        {
            // カラーフレームを取得する
            using ( var colorFrame = e.FrameReference.AcquireFrame() ) {
                if ( colorFrame == null ) {
                    return;
                }

                // BGRAデータを取得する
                colorFrame.CopyConvertedFrameDataToArray(
                                            colorBuffer, colorFormat );
            }
        }
        private void DrawColorFrame()
        {
            // ビットマップにする
            colorBitmap.WritePixels( colorRect, colorBuffer,
                                            colorStride, 0 );
        }
        void bodyFrameReader_FrameArrived( object sender, BodyFrameArrivedEventArgs e )
        {
            UpdateBodyFrame();
        }
        void InitializeGesture()
        {
            gestureFrameReaders = new VisualGestureBuilderFrameReader[BODY_COUNT];
            for ( int count =0; count < BODY_COUNT; count++ ) {
                VisualGestureBuilderFrameSource gestureFrameSource;
                gestureFrameSource = new VisualGestureBuilderFrameSource( kinect, 0 );
                gestureFrameReaders[count] = gestureFrameSource.OpenReader();
                gestureFrameReaders[count].FrameArrived += gestureFrameReaders_FrameArrived;
            }

            VisualGestureBuilderDatabase gestureDatabase;
            // gbdデータベースファイルの読み込み (ここを書き換えるだけで全部動く)
            gestureDatabase = new VisualGestureBuilderDatabase("gesture_test_1.gbd");

            uint gestureCount;
            gestureCount = gestureDatabase.AvailableGesturesCount;
            gestures = gestureDatabase.AvailableGestures;
            for ( int count = 0; count<BODY_COUNT; count++ ) {
                VisualGestureBuilderFrameSource gestureFrameSource;
                gestureFrameSource = gestureFrameReaders[count].VisualGestureBuilderFrameSource;
                gestureFrameSource.AddGestures( gestures );
                foreach ( var g in gestures ) {
                    gestureFrameSource.SetIsEnabled( g, true );
                }
            }
        }

        void gestureFrameReaders_FrameArrived( object sender, VisualGestureBuilderFrameArrivedEventArgs e )
        {
            VisualGestureBuilderFrame gestureFrame = e.FrameReference.AcquireFrame();
            if ( gestureFrame==null ) {
                return;
            }
            UpdateGestureFrame( gestureFrame );
            gestureFrame.Dispose();
        }
        void UpdateGestureFrame( VisualGestureBuilderFrame gestureFrame )
        {
            bool tracked;
            tracked = gestureFrame.IsTrackingIdValid;
            if ( !tracked ) {
                return;
            }
            foreach ( var g in gestures ) {
                result( gestureFrame, g );
            }
        }
        void result( VisualGestureBuilderFrame gestureFrame, Gesture gesture )
        {
            int count = GetIndexofGestureReader( gestureFrame );
            GestureType gestureType;
            gestureType = gesture.GestureType;
            switch ( gestureType ) {
            case GestureType.Discrete:
                DiscreteGestureResult dGestureResult;
                dGestureResult = gestureFrame.DiscreteGestureResults[gesture];

                bool detected;
                detected = dGestureResult.Detected;
                if ( !detected ) {
                        image.Source = null;
                        break;
                }

                    float confidence = dGestureResult.Confidence;

                    // ポーズ認識したら最前面にPNG画像を表示 (各自記述)
                    // ポーズ認識の具合(0.0-1.0)を条件文に入れたいがどれがその値か現時点で理解できず。2017-06-21 Ohyama
                    // RaiseYourHands -> banzai.png
                    if (gesturetostring(gesture)=="RaiseYourHands")
                    {
                        BitmapImage imageSource = new BitmapImage(new Uri("D:/Desktop/プロジェクト学習/KinectV2-Gesture-01/KinectV2/banzai54.png", UriKind.Absolute));
                        image.Source = imageSource;
                    }
                    // gattu_Left -> chikyuu.png
                    if (gesturetostring(gesture) == "gattu_Left")
                    {
                        BitmapImage imageSource = new BitmapImage(new Uri("D:/Desktop/KinectV2-Gesture-01/KinectV2/chikyuu.png", UriKind.Absolute));
                        image.Source = imageSource;
                    }

                    string discrete = gesturetostring( gesture )
                            + " : Detected (" + confidence.ToString() + ")";
                    //GetTextBlock( count ).Text = discrete;//WPFのTextBlockに表示

                    break;

            case GestureType.Continuous:
                ContinuousGestureResult cGestureResult;
                cGestureResult = gestureFrame.ContinuousGestureResults[gesture];

                    float progress;
                progress = cGestureResult.Progress;
                    string continuous = gesturetostring( gesture )
                        + " : Progress " + progress.ToString();
                GetTextBlock( count ).Text = continuous;//WPFのTextBlockに表示
                break;
            default:
                break;
            }
        }
        TextBlock GetTextBlock( int index )
        {
            return null;
            /*switch ( index ) {
            case 1:
                return TextBlock1;
            case 2:
                return TextBlock2;
            case 3:
                return TextBlock3;
            case 4:
                return TextBlock4;
            case 5:
                return TextBlock5;
            default:
                return TextBlock6;
            }*/
        }
        string gesturetostring( Gesture gesture )
        {
            return gesture.Name.Trim();
        }
        int GetIndexofGestureReader( VisualGestureBuilderFrame gestureFrame )
        {
            for ( int index =0; index<BODY_COUNT; index++ ) {
                if ( gestureFrame.TrackingId
                    == gestureFrameReaders[index].VisualGestureBuilderFrameSource.TrackingId ) {
                    return index;
                }
            }
            return -1;
        }
        void UpdateBodyFrame()
        {
            if ( bodyFrameReader==null ) {
                return;
            }
            BodyFrame bodyFrame;
            bodyFrame = bodyFrameReader.AcquireLatestFrame();
            if ( bodyFrame==null ) {
                return;
            }
            bodyFrame.GetAndRefreshBodyData( bodies );
            for ( int count = 0; count < BODY_COUNT; count++ ) {
                Body body = bodies[count];
                bool tracked = body.IsTracked;
                if ( !tracked ) {
                    continue;
                }
                ulong trackingId = body.TrackingId;
                VisualGestureBuilderFrameSource gestureFrameSource;
                gestureFrameSource = gestureFrameReaders[count].VisualGestureBuilderFrameSource;
                gestureFrameSource.TrackingId = trackingId;
            }
            bodyFrame.Dispose();
        }
        private void Window_Closing( object sender, System.ComponentModel.CancelEventArgs e )
        {
            if ( kinect != null ) {
                kinect.Close();
                kinect = null;
            }
        }
    }
}
