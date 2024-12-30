using System.Diagnostics;
using System.IO;
using System.Windows;
using CefSharp.Wpf;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.Linq;

namespace application
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*"
            };

            var messages = new List<parser.Message>();
            if (dialog.ShowDialog() == true)
            {
                var results = parser.Parser.iterate(File.ReadAllLines(dialog.FileName));

                var users = new HashSet<string>();
                foreach (var result in results)
                {
                    if (result is parser.Message message)
                    {
                        messages.Add(message);
                        users.Add(message.Nickname);
                    }
                }
                UsersView.ItemsSource = users;
            }
            ConversationsView.ItemsSource = messages;
            var plotModel = new PlotModel();
            var groups = messages.GroupBy(x => x.Nickname)
                .Select(x => new { Nickname = x.Key, Count = x.Count() })
                .OrderBy(x => x.Count)
                .Reverse();

            var model = new PlotModel();
            var barSeries = new BarSeries
            {
                XAxisKey = "Count",
                YAxisKey = "Nickname",
                ItemsSource = groups.Select(group => new BarItem { Value = group.Count })
            };
            model.Series.Add(barSeries);

            var categoryAxis = new CategoryAxis { Position = AxisPosition.Bottom, Key = "Nickname", Angle = 90 };
            categoryAxis.Labels.AddRange(groups.Select(group => group.Nickname));
            model.Axes.Add(categoryAxis);

            var valueAxis = new LinearAxis { Position = AxisPosition.Left, Key = "Count" };
            model.Axes.Add(valueAxis);

            plotView.Model = model;
        }
    }
}