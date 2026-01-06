using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ColourSequenceGuessingGame
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Point _dragStart;
        public ObservableCollection<Brush> Colours { get; } =
            new ObservableCollection<Brush>
            {
            Brushes.Red,
            Brushes.Green,
            Brushes.Blue,
            Brushes.Yellow,
            Brushes.DarkViolet
            };

        private readonly List<Brush> _targetPattern = new();

        private static readonly Brush[] AvailableColours =
        {
            Brushes.Red,
            Brushes.Green,
            Brushes.Blue,
            Brushes.Yellow,
            Brushes.DarkViolet
        };

        private int _matchCount;

        public int MatchCount
        {
            get => _matchCount;
            set
            {
                if (_matchCount == value)
                    return;

                _matchCount = value;
                OnPropertyChanged(nameof(MatchCount));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            GeneratePattern();
            MatchCount = CalculateMatches();
            DataContext = this;
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(null);
        }

        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            Point pos = e.GetPosition(null);
            if (Math.Abs(pos.X - _dragStart.X) < 5 &&
                Math.Abs(pos.Y - _dragStart.Y) < 5)
                return;

            Border border = (Border)sender;
            Brush brush = (Brush)border.DataContext;

            DataObject data = new DataObject("ColourBrush", brush);
            DragDrop.DoDragDrop(border, data, DragDropEffects.Move);
        }

        private void Border_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("ColourBrush"))
                return;

            Brush source = (Brush)e.Data.GetData("ColourBrush");
            Border targetBorder = (Border)sender;
            Brush target = (Brush)targetBorder.DataContext;

            int sourceIndex = Colours.IndexOf(source);
            int targetIndex = Colours.IndexOf(target);

            if (sourceIndex < 0 | targetIndex < 0 || sourceIndex == targetIndex)
                return;

            Border border = (Border)sender;
            border.BorderBrush = Brushes.White;
            border.BorderThickness = new Thickness(3);
        }

        private void Border_DragLeave(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("ColourBrush"))
                return;

            Border border = (Border)sender;
            border.BorderBrush = null;
            border.BorderThickness = new Thickness(0);
        }

        private void Border_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("ColourBrush"))
                return;

            Brush source = (Brush)e.Data.GetData("ColourBrush");
            Border targetBorder = (Border)sender;
            Brush target = (Brush)targetBorder.DataContext;

            int sourceIndex = Colours.IndexOf(source);
            int targetIndex = Colours.IndexOf(target);

            targetBorder.BorderThickness = new Thickness(0);

            if (sourceIndex < 0 | targetIndex < 0 || sourceIndex == targetIndex)
                return;

            SwapWithAnimation(sourceIndex, targetIndex);
        }

        private async void SwapWithAnimation(int indexA, int indexB)
        {
            Border borderA = GetBorderForIndex(indexA);
            Border borderB = GetBorderForIndex(indexB);

            if (borderA == null || borderB == null)
                return;

            await Fade(borderA, 1, 0);
            await Fade(borderB, 1, 0);

            Brush temp = Colours[indexA];
            Colours[indexA] = Colours[indexB];
            Colours[indexB] = temp;

            await Fade(borderA, 0, 1);
            await Fade(borderB, 0, 1);

            MatchCount = CalculateMatches();
        }

        private Task Fade(UIElement element, double from, double to)
        {
            TaskCompletionSource<bool> tcs = new();

            DoubleAnimation animation = new()
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(150)
            };

            animation.Completed += (_, _) => tcs.SetResult(true);

            element.BeginAnimation(UIElement.OpacityProperty, animation);

            return tcs.Task;
        }

        private Border? GetBorderForIndex(int index)
        {
            return ItemsControlColours
                .ItemContainerGenerator
                .ContainerFromIndex(index) is DependencyObject container
                ? FindVisualChild<Border>(container)
                : null;
        }

        public static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                T? result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void GeneratePattern()
        {
            var rnd = new Random();

            _targetPattern.Clear();

            var shuffled = AvailableColours
                .OrderBy(_ => rnd.Next())
                .Take(Colours.Count);

            _targetPattern.AddRange(shuffled);
        }

        private int CalculateMatches()
        {
            int matches = 0;

            for (int i = 0; i < Colours.Count; i++)
            {
                if (Colours[i] == _targetPattern[i])
                    matches++;
            }

            return matches;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }    
}