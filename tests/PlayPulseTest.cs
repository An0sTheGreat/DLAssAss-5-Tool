using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Xml.Linq;

static class PlayPulseTest
{
    public static void Run()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                XNamespace wpf = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
                var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "MainWindow.xaml"));
                var pulse = document.Descendants(wpf + "BeginStoryboard")
                    .Single(story => story.Attributes().Any(attribute => attribute.Value == "PlayReadyPulse"))
                    .Ancestors(wpf + "Border").First();
                var grid = (Grid)XamlReader.Parse($"<Grid xmlns='{wpf}'><Grid.Resources><SolidColorBrush xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Key='AccentBrush' Color='#76B900'/></Grid.Resources>{pulse}</Grid>");
                var border = (Border)grid.Children[0];
                using var host = new System.Windows.Interop.HwndSource(new System.Windows.Interop.HwndSourceParameters("PLAY pulse test")
                    { Width = 100, Height = 40, WindowStyle = unchecked((int)0x80000000) });
                host.RootVisual = grid; // Hidden native host; no application window or game scan.
                foreach (var state in new[] { (false,false), (true,false), (false,true), (true,true), (false,false) })
                {
                    grid.DataContext = new { SelectedGame = new { HasReShade = state.Item1, HasAddon = state.Item2 } };
                    var frame = new DispatcherFrame();
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
                    timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
                    timer.Start(); Dispatcher.PushFrame(frame);
                    if ((border.Visibility == Visibility.Visible && border.Opacity > 0) != (state.Item1 && state.Item2))
                        throw new Exception($"PLAY pulse installation gate failed: ReShade={state.Item1} addon={state.Item2} opacity={border.Opacity}");
                }
            }
            catch (Exception error) { failure = error; }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join();
        if (failure is not null) throw failure;
        Console.WriteLine("PASS: actual PLAY border animation starts only with both installations, and stops when selection changes.");
    }
}
