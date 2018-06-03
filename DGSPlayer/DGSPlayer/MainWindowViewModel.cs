using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls.Dialogs;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace DGSPlayer
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private IDialogCoordinator dialogCoordinator;

        public MainWindowViewModel(IDialogCoordinator instance)
        {
            this.dialogCoordinator = instance;

            this.StartCommand = new MainViewCommand(new EventDelegate(start));
            this.StopCommand = new MainViewCommand(new EventDelegate(stop));
            this.OpenCommand = new MainViewCommand(new EventDelegate(open));
        }
        public MainViewCommand StartCommand { get; set; }
        public MainViewCommand StopCommand { get; set; }
        public MainViewCommand OpenCommand { get; set; }
        private bool isTriggered = false;

        private bool isProcessing = true;
        public bool IsProcessing
        {
            get
            {
                return this.isProcessing;
            }
            set
            {
                this.isProcessing = value;
                this.IsEnableCapture = !value;
                this.RaisePropertyChanged("IsProcessing");
            }
        }

        private bool isEnableCapture = false;
        public bool IsEnableCapture
        {
            get
            {
                return this.isEnableCapture;
            }
            set
            {
                this.isEnableCapture = value;
                this.RaisePropertyChanged("IsEnableCapture");
            }
        }

        private WriteableBitmap imageSource;
        public WriteableBitmap ImageSource
        {
            get
            {
                return this.imageSource;
            }
            set
            {
                this.imageSource = value;
                this.RaisePropertyChanged("ImageSource");
            }
        }

        #region ボタンイベント
        private async void start()
        {
            await this.CaptureAsync();
        }
        private void stop()
        {
            this.isTriggered = true;
        }
        private async void open()
        {
            logger.Info("Show File Dialog");
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var capture = new VideoCapture(openFileDialog.FileName);
                    if (!capture.IsOpened())
                    {
                        // そもそもだめならだめ・・。
                        throw new Exception("Failed IsOpend(): 動画ファイルを正常に開くことができませんでした");
                    }
                    using (var mat = new Mat())
                    {
                        for (var i = 0; i < 3; i++)
                        {
                            // 3フレームくらい読み出せるか試す
                            capture.Read(mat);
                            if (mat.Empty())
                            {
                                throw new Exception($"{i+1}フレーム目で正常に読み出せませんでした（動画ではない可能性があります）");
                            }
                        }
                    }

                    // さあ、再生です
                    await this.PlayAsync(openFileDialog.FileName);
                }
                catch (Exception e)
                {
                    logger.Fatal(e.Message);
                    logger.Fatal(e.StackTrace);
                    String errorMessage = $"Error Code: 2501\n\n" +
                                      $"{openFileDialog.FileName}を開くことができませんでした。\n" +
                                      $"logs/DGSPlayer.logにエラーが記録されました。";
                    await dialogCoordinator.ShowMessageAsync(this, "エラーが発生しています", errorMessage);
                }
            }
            logger.Info("Close File Dialog");
        }
        #endregion

        #region OpenCV
        private async Task PlayAsync(String fileName)
        {
            using (var capture = new VideoCapture(fileName))
            {
                // Task.Delay(TimeSpan delay)
                // + TimeSpan(Int64 ticks)
                //   単一のティックは、100 ナノ秒または 1 つ 1,000万分の 1 秒を表します。 ミリ秒単位で 10,000 タイマー刻みがあります。
                //   (see https://msdn.microsoft.com/ja-jp/library/zz841zbz(v=vs.110).aspx)
                // ---
                // 想定するのは 30FPS ≒ 33.333… ms/frame ≒ 33,333.333… us/frame ≒ 333,333.333… 1/100 ns/frame
                var interval = (Int64)(1000 / capture.Fps) * 1000 * 10;
                while (true)
                {
                    var mat = await this.RetrieveMatAsync(capture);
                    if (mat == null || mat.Empty()) break;
                    this.ImageSource = mat.ToWriteableBitmap();
                    await Task.Delay(new TimeSpan(interval));
                }
            }
        }
        private async Task CaptureAsync()
        {
            this.IsProcessing = false;

            var data1 = new List<Mat>();
            var data2 = new List<Mat>();

            using (var capture = new VideoCapture(0))
            {
                capture.Fps = 30;
                capture.FrameWidth = 640;
                capture.FrameHeight = 480;
                while (true)
                {
                    var mat = await this.RetrieveMatAsync(capture);
                    if (mat == null) break;

                    if (!this.isTriggered)
                    {
                        data1.Add(mat.Clone());
                        if (data1.Count() > 90)
                        {
                            data1.RemoveAt(0);
                        }
                    }
                    else
                    {
                        data2.Add(mat.Clone());
                        if (data2.Count() >= 90)
                        {
                            break;
                        }
                    }
                    this.ImageSource = mat.ToWriteableBitmap();
                    Cv2.WaitKey(1);
                }
            }
            this.isTriggered = false;

            var now = System.DateTime.Now.ToFileTimeUtc();
            using (var writer = new VideoWriter($"test-{now}.wmv", FourCC.MP43, 30, new OpenCvSharp.Size(640, 480)))
            {
                foreach (var mat in data1.Concat(data2))
                {
                    writer.Write(mat);
                }
            }
            Cv2.DestroyAllWindows();

            this.IsProcessing = true;
        }

        private async Task<Mat> RetrieveMatAsync(VideoCapture videoCapture)
        {
            return await Task.Run(() =>
            {
                return videoCapture.RetrieveMat();
            });
        }
        private async Task<Mat> ReadAsync(VideoCapture videoCapture)
        {
            return await Task.Run(() =>
            {
                var mat = new Mat();
                videoCapture.Read(mat);
                return mat.Clone();
            });
        }
        #endregion

        #region INotifyPropertyChanged実装
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler CanExecuteChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}
