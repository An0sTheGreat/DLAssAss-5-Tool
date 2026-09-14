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
                var buttonMarkup = new XElement(document.Descendants(wpf + "BeginStoryboard")
                    .Single(story => story.Attributes().Any(attribute => attribute.Value == "PlayReadyPulse"))
                    .Ancestors(wpf + "Button").First());
                buttonMarkup.Attribute("Click")?.Remove();
                var grid = (Grid)XamlReader.Parse($"<Grid xmlns='{wpf}' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><Grid.Resources><SolidColorBrush x:Key='AccentBrush' Color='#76B900'/></Grid.Resources>{buttonMarkup}</Grid>");
                var button = (Button)grid.Children[0];
                using var host = new System.Windows.Interop.HwndSource(new System.Windows.Interop.HwndSourceParameters("PLAY pulse test")
                    { Width = 100, Height = 40, WindowStyle = unchecked((int)0x80000000) });
                host.RootVisual = grid; // Hidden native host; no application window or game scan.
                grid.Measure(new Size(100, 40));
                grid.Arrange(new Rect(0, 0, 100, 40));
                grid.UpdateLayout();
                button.ApplyTemplate();
                var readyLayer = (Border?)button.Template.FindName("PlayReadyLayer", button)
                    ?? throw new Exception("PLAY ready layer was not created.");
                foreach (var state in new[] { (false,false), (true,false), (false,true), (true,true), (false,false) })
                {
                    grid.DataContext = new { SelectedGame = new { HasReShade = state.Item1, HasAddon = state.Item2 } };
                    var frame = new DispatcherFrame();
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
                    timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
                    timer.Start(); Dispatcher.PushFrame(frame);
                    var isPulsing = readyLayer.Visibility == Visibility.Visible && readyLayer.Opacity > 0;
                    if (isPulsing != (state.Item1 && state.Item2))
                        throw new Exception($"PLAY pulse installation gate failed: ReShade={state.Item1} addon={state.Item2} opacity={readyLayer.Opacity}");
                }
            }
            catch (Exception error) { failure = error; }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join();
        if (failure is not null) throw failure;
        Console.WriteLine("PASS: PLAY button color animation starts only with both installations, and stops when selection changes.");
    }
}
