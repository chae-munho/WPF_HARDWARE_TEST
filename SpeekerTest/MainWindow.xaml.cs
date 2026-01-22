using NAudio.Wave;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Speech.Synthesis;
namespace SpeekerTest
{
    
    public partial class MainWindow : Window
    {
        private sealed class OutputDeviceItem
        {
            public int DeviceNumber { get; init; }
            public string Name { get; init; } = "";
            public override string ToString() => $"[{DeviceNumber}] {Name}";
        }
        private readonly List<OutputDeviceItem> _devices = new();

        // 재생 중 GC 방지용
        private IWavePlayer? _player;
        private WaveStream? _waveStream;

        public MainWindow()
        {
            InitializeComponent();
            LoadOutputDevices();

        }
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadOutputDevices();
        }

        private void LoadOutputDevices()
        {
            _devices.Clear();
            DeviceCombo.ItemsSource = null;

            Log("=== 오디오 출력 장치 목록 ===");
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var caps = WaveOut.GetCapabilities(i);
                _devices.Add(new OutputDeviceItem { DeviceNumber = i, Name = caps.ProductName });
                Log($"Device {i}: {caps.ProductName}");
            }

            DeviceCombo.ItemsSource = _devices;
            if (_devices.Count > 0) DeviceCombo.SelectedIndex = 0;

            Log("============================");
        }

        private void BtnSpeak_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceCombo.SelectedItem is not OutputDeviceItem dev)
            {
                Log("출력 장치를 선택하세요.");
                return;
            }

            string text = (MessageBox.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                Log("방송 문구가 비어있습니다.");
                return;
            }

            try
            {
                // 기존 재생 정리
                StopPlayback();

                // 1) TTS를 WAV 메모리 스트림으로 생성
                var ms = new MemoryStream();
                using (var synth = new SpeechSynthesizer())
                {
                    synth.SetOutputToWaveStream(ms); // 기본 장치가 아니라 '파일(스트림)'로 뽑는다
                    synth.Speak(text);               // 동기 생성 (짧은 문구면 충분히 빠름)
                }
                ms.Position = 0;

                // 2) 선택된 출력 장치로 재생
                _waveStream = new WaveFileReader(ms); // ms는 _waveStream이 Dispose될 때 같이 해제됨
                _player = new WaveOutEvent { DeviceNumber = dev.DeviceNumber };
                _player.Init(_waveStream);
                _player.PlaybackStopped += (_, __) =>
                {
                    Dispatcher.Invoke(StopPlayback);
                    Dispatcher.Invoke(() => Log("재생 종료"));
                };

                Log($"송출 시작 → 장치: {dev} / 문구: \"{text}\"");
                _player.Play();
            }
            catch (Exception ex)
            {
                Log($"오류: {ex.Message}");
                StopPlayback();
            }
        }

        private void StopPlayback()
        {
            try { _player?.Stop(); } catch {  }

            _player?.Dispose();
            _player = null;

            _waveStream?.Dispose();
            _waveStream = null;
        }

        private void Log(string msg)
        {
            LogBox.AppendText($"{DateTime.Now:HH:mm:ss}  {msg}{Environment.NewLine}");
            LogBox.ScrollToEnd();
        }

    }
}