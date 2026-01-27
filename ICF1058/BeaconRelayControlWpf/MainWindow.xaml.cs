using BeaconRelayControlWpf.Services;
using System;
using System.IO.Ports;
using System.Linq;
using System.Windows;

namespace BeaconRelayControlWpf
{
    public partial class MainWindow : Window
    {
        private readonly RelayController _relay = new RelayController();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += (_, __) => RefreshPorts();
            Closed += (_, __) => SafeDisconnect();
        }

        private void RefreshPorts()
        {
            try
            {
                var ports = SerialPort.GetPortNames()
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                CmbPorts.ItemsSource = ports;
                if (ports.Length > 0)
                    CmbPorts.SelectedIndex = 0;

                Log($"포트 새로고침: {ports.Length}개 ({string.Join(", ", ports)})");
            }
            catch (Exception ex)
            {
                Log($"[ERROR] 포트 조회 실패: {ex.Message}");
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => RefreshPorts();

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            var port = CmbPorts.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(port))
            {
                MessageBox.Show("COM 포트를 선택하세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(TxtBaud.Text.Trim(), out int baud) || baud <= 0)
            {
                MessageBox.Show("Baud 값을 올바르게 입력하세요. (예: 115200)", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                _relay.Connect(port, baud);
                SetConnectedUi(true);
                Log($"연결 성공: {port}, {baud}bps");
            }
            catch (Exception ex)
            {
                Log($"[ERROR] 연결 실패: {ex.Message}");
                MessageBox.Show($"연결 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDisconnect_Click(object sender, RoutedEventArgs e) => SafeDisconnect();

        private void SafeDisconnect()
        {
            try
            {
                _relay.Disconnect();
            }
            catch { /* ignore */ }

            SetConnectedUi(false);
            Log("연결 해제");
        }

        private void SetConnectedUi(bool connected)
        {
            BtnConnect.IsEnabled = !connected;
            BtnDisconnect.IsEnabled = connected;
            BtnOn.IsEnabled = connected;
            BtnOff.IsEnabled = connected;

            TxtStatus.Text = connected ? "연결됨" : "미연결";
        }

        private RelayProtocolMode GetMode()
            => (RbAscii.IsChecked == true) ? RelayProtocolMode.Ascii : RelayProtocolMode.Hex;

        private void BtnOn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mode = GetMode();
                _relay.SetMode(mode);

                _relay.TurnOn();
                Log($"ON 전송 ({mode})");
            }
            catch (Exception ex)
            {
                Log($"[ERROR] ON 실패: {ex.Message}");
                MessageBox.Show($"ON 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOff_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mode = GetMode();
                _relay.SetMode(mode);

                _relay.TurnOff();
                Log($"OFF 전송 ({mode})");
            }
            catch (Exception ex)
            {
                Log($"[ERROR] OFF 실패: {ex.Message}");
                MessageBox.Show($"OFF 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e) => TxtLog.Clear();

        private void Log(string msg)
        {
            TxtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
            TxtLog.ScrollToEnd();
        }
    }
}
