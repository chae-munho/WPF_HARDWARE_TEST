using NAudio.Wave;   // Wave(오디오 재싱/녹음/처리) 관련 클래스들을 쓰기 위해 네임스페이스를 가져옴
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
        //  (장치명에 포함되는 문자열)
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
            SpeakButton.IsEnabled = connected; // 연결 실패면 송출 버튼 비활성화
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
                //전에 재생하던 소리가 있으면 멈추고 _player, _waveStream 등을 Dispose 해버림
                StopPlayback();

                // TTS 결과를 WAV 파일 형태로 ms 변수에 저장. 디스크 파일로 저장하는게 아니라 메모리에 임시로 wav 생성
                var ms = new MemoryStream();
                //SpeechSysthesizer는 윈도우 내장 TTS 엔진
                using (var synth = new SpeechSynthesizer())
                {
                    synth.Volume = 100; //TTS 볼륨 최대 (0~100)
                    synth.Rate = 0;     // 필요하면 속도 조절 -10~10  -10이면 매우 느림

                    //원래 TTS는 PC 기본 출력장치로 바로 송출 가능하지만 이 코드는 출력을 스피커가 아니라 ms메모리 스트림으로 보내겠다는 뜻임 즉 말하는 소리를 wav 데이터로 뽑아내겠다는 의미
                    synth.SetOutputToWaveStream(ms);
                    //실제로 text 문장을 음성으로 변환
                    synth.Speak(text);
                }
                //방금 ms에 wav 데이터를 쭉 썼으니까 스트림 커서가 맨 끝에 있음 다시 읽어서 재생하려면 커서를 맨 앞으로 돌려야 함
                //그래서 position을 0으로 설정 즉 처음부터 다시 읽겠다.
                ms.Position = 0;

                // 목표 장치로 재생
                _waveStream = new WaveFileReader(ms);

                var wo = new WaveOutEvent { DeviceNumber = _targetDeviceNumber.Value };
                wo.Volume = 1.0f;      //재생 볼륨 최대 (0.0~1.0)
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
            try { _player?.Stop(); } catch { /* ignore */ }

            _player?.Dispose();
            _player = null;

            _waveStream?.Dispose();
            _waveStream = null;
        }

        private void Log(string msg)
        {
            
            LogBox.AppendText($"{DateTime.Now:HH:mm:ss}  {msg}{Environment.NewLine}"); //Environment.NewLine 의 의미는 /r/n
            LogBox.ScrollToEnd();  //텍스트 박스의 스크롤바를 가장 아래로 강제로 이동시킴
        }

        protected override void OnClosed(EventArgs e)
        {
            StopPlayback();
            base.OnClosed(e);
        }
    }
}
