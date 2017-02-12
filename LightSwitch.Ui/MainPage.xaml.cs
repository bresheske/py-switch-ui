using LightSwitch.Ui.Objects;
using Microsoft.Graphics.Canvas.Effects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace LightSwitch.Ui
{
    /// <summary>
    /// Look, this is probably some of the shittiest code i've written.
    /// But hey, it was quick and it works.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Point _manipstarted;
        private SwitchResponse _status;
        private DispatcherTimer _timer;
        private string _currentstate = "Base";
        private double _currentWidth = 0;
        private double _currentHeight = 0;
        private Switch _switch = new Switch()
        {
            Pin = 23,
            Name = "Master Bedroom",
            IP = "192.168.1.152"
        };


        public MainPage()
        {
            this.InitializeComponent();
            UpdateStatus();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(5000);
            _timer.Tick += (sender, e) =>
            {
                UpdateStatus();
            };
            _timer.Start();

        }

        /// <summary>
        /// Function to update the UI when the status changes.
        /// </summary>
        private void UpdateUI()
        {
            var colors = new[]
            {
                new { Status = SwitchResponse.Error, Color = Color.FromArgb(255, 200, 200, 200) },
                new { Status = SwitchResponse.False, Color = Color.FromArgb(255, 255, 50, 50) },
                new { Status = SwitchResponse.True, Color = Color.FromArgb(255, 50, 255, 50) }
            };

            var color = colors.First(x => x.Status == _status).Color;
            outerrect.Stroke = new SolidColorBrush(color);
        }

        private async void UpdateStatus()
        {
            _status = await GetStatus();
            UpdateUI();
        }

        private async void HandleToggleEvent(Point start, Point end)
        {
            var distance = GetDistance(start, end);
            if (distance < 30)
                return;

            // trigger the event.
            _status = await SendRequest();
            UpdateUI();
        }

        private async Task<SwitchResponse> SendRequest()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.TryParseAdd("application/json");

            var json = new JsonObject();
            json["id"] = JsonValue.CreateNumberValue(_switch.Pin);
            var content = new HttpJsonContent(json);

            try
            {
                var result = await client.PostAsync(new Uri("http://" + _switch.IP + "/toggleStatus", UriKind.Absolute), content);
                var rescontent = await result.Content.ReadAsStringAsync();
                var obj = JsonObject.Parse(rescontent);
                return obj["status"].GetBoolean() == true
                    ? SwitchResponse.True
                    : SwitchResponse.False;
            }
            catch
            {
                return SwitchResponse.Error;
            }
        }

        private async Task<SwitchResponse> GetStatus()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.TryParseAdd("application/json");

            var json = new JsonObject();
            json["id"] = JsonValue.CreateNumberValue(_switch.Pin);
            var content = new HttpJsonContent(json);

            try
            {
                var result = await client.PostAsync(new Uri("http://" + _switch.IP + "/getStatus", UriKind.Absolute), content);
                var rescontent = await result.Content.ReadAsStringAsync();
                var obj = JsonObject.Parse(rescontent);
                return obj["status"].GetBoolean() == true
                    ? SwitchResponse.True
                    : SwitchResponse.False;
            }
            catch
            {
                return SwitchResponse.Error;
            }
        }

        private double GetDistance(Point start, Point end)
        {
            return Math.Sqrt(Math.Pow((start.X - end.X), 2) + Math.Pow((start.Y - end.Y), 2));
        }

        private void ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            _manipstarted = e.Position;
        }

        private void ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            HandleToggleEvent(_manipstarted, e.Position);
        }

        private void ToggleScreen(object sender, RoutedEventArgs e)
        {
            var nextstate = _currentstate == "Base" ? "Rotated" : "Base";
            VisualStateManager.GoToState(this, nextstate, true);
            _currentstate = nextstate;
        }
    }
}