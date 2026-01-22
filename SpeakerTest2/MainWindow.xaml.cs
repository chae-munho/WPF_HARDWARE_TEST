using NAudio.Wave;   
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;   //tts 기능 사용
using System.Windows;
using System.Windows.Media;

namespace SpeakerTest2
{
    public partial class MainWindow : Window
    {
        //  장치명에 포함되는 문자열
        private const string TargetDeviceKeyword = "Speakers";

        private int? _targetDeviceNumber = null;

       //  _player는 오디오를 실제로 재생하는 플레이어 객체를 담아두는 변수. NAudio에서 제공하는 재생장치
        private IWavePlayer? _player;
        //재생할 오디오 데이터를 담는 데이터 변수
        private WaveStream? _waveStream;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DetectDeviceAndUpdateUi();
        }

        private void DetectDeviceAndUpdateUi()
        {
            _targetDeviceNumber = FindDeviceNumberByKeyword(TargetDeviceKeyword);

            if (_targetDeviceNumber.HasValue)
            {
                SetStatus(connected: true, msg: "장치와 연결됨");
                Log($"연결 대상 장치 발견: DeviceNumber={_targetDeviceNumber.Value} (키워드: {TargetDeviceKeyword})");
            }
            else
            {
                SetStatus(connected: false, msg: "장치와 연결 실패함");
                Log($"장치를 찾지 못했습니다 (키워드: {TargetDeviceKeyword}).");
                Log("현재 PC에 잡힌 출력 장치 목록:");
                DumpDeviceListToLog();
            }
        }

        private void SetStatus(bool connected, string msg)
        {
            StatusText.Text = msg;
            StatusText.Foreground = connected ? Brushes.DodgerBlue : Brushes.IndianRed;
            SpeakButton.IsEnabled = connected; 
        }

        private int? FindDeviceNumberByKeyword(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return null;

            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var caps = WaveOut.GetCapabilities(i);
                var name = caps.ProductName ?? "";

                if (name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    return i;
            }
            return null;
        }

        private void DumpDeviceListToLog()
        {
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var caps = WaveOut.GetCapabilities(i);
                Log($"Device {i}: {caps.ProductName}");
            }
        }
        //버튼을 누르면 텍스트를 TTS로 WAV로 만든 다음 지정된 출력장치로 재생하고 로그 남김
        private void BtnSpeak_Click(object sender, RoutedEventArgs e)
        {
            if (!_targetDeviceNumber.HasValue)
            {
                Log("현재 장치가 연결되지 않아 송출할 수 없습니다.");
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
               
                StopPlayback();

               
                var ms = new MemoryStream();
              
                using (var synth = new SpeechSynthesizer())
                {
                    synth.Volume = 100; //TTS 볼륨 최대 (0~100)
                    synth.Rate = 0;     // 필요하면 속도 조절 -10~10  -10이면 매우 느림

                 
                    synth.SetOutputToWaveStream(ms);
                  
                    synth.Speak(text);
                }
               
                ms.Position = 0;

             
                _waveStream = new WaveFileReader(ms);

                var wo = new WaveOutEvent { DeviceNumber = _targetDeviceNumber.Value };
                wo.Volume = 1.0f;     
                _player = wo;

                _player.Init(_waveStream);
                _player.PlaybackStopped += (_, __) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        StopPlayback();
                        Log("재생 종료");
                    });
                };

                Log($"송출 시작 → DeviceNumber={_targetDeviceNumber.Value} / 문구: \"{text}\"");
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
            try { _player?.Stop(); } catch { }

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

        protected override void OnClosed(EventArgs e)
        {
            StopPlayback();
            base.OnClosed(e);
        }
    }
}
