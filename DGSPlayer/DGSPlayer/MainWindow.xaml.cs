using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace DGSPlayer
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        MainWindowViewModel vm = new MainWindowViewModel(DialogCoordinator.Instance);
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this.vm;
            this.setChartControl();

        }
        private void setChartControl()
        {
            var windowsFormsHost = grid.FindName("WindowsFormsHost1") as WindowsFormsHost;
            //windowsFormsHost.Background = Brushes.Red;
            //windowsFormsHost.Opacity = 0;

            var chart = windowsFormsHost.Child as Chart;
            chart.BackColor = System.Drawing.Color.Transparent;

            // ChartArea追加
            chart.ChartAreas.Add("ChartArea1");
            chart.ChartAreas[0].BackColor = System.Drawing.Color.Transparent;

            // Seriesの作成と値の追加
            Series seriesSin = new Series();
            seriesSin.ChartType = SeriesChartType.Line;
            seriesSin.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Circle;

            Series seriesCos = new Series();
            seriesCos.ChartType = SeriesChartType.Line;
            seriesCos.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Circle;

            for (double x = 0.0; x <= 2 * Math.PI; x = x + 0.1)
            {
                seriesSin.Points.AddXY(x, Math.Sin(x));
                seriesCos.Points.AddXY(x, Math.Cos(x));
            }

            chart.Series.Add(seriesSin);
            chart.Series.Add(seriesCos);

        }
    }
}
