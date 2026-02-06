using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JkUsbApp.Native;

namespace JkUsbApp
{
    public partial class MainWindow : Window
    {
        // C++: m_pDeviceList
        private IntPtr _enumListHead = IntPtr.Zero;

        // C++: m_mapIndexToDevice (serial -> node ptr)
        private readonly Dictionary<string, IntPtr> _serialToNode = new();

        // C++: m_hCurDevice, m_bOpened, m_hCureDeviceIndex
        private int _deviceHandle = 0;
        private bool _opened = false;
        private int _channelCount = 0;

        private Border[] _relayStatus;

        public MainWindow()
        {
            InitializeComponent();

            _relayStatus = new[]
            {
                RelayStatus1, RelayStatus2, RelayStatus3, RelayStatus4,
                RelayStatus5, RelayStatus6, RelayStatus7, RelayStatus8
            };
        }

       
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            int init = UsbRelayNative.usb_relay_init();
            Log($"usb_relay_init() = {init}");

            SetDeviceOpened(false);
            SetRelayUiState(0, 0);
        }

      
        private void Window_Closed(object sender, EventArgs e)
        {
            SafeCloseDevice();
            SafeFreeEnumerate();

            int ex = UsbRelayNative.usb_relay_exit();
            Log($"usb_relay_exit() = {ex}");
        }

       
        private void BtnFind_Click(object sender, RoutedEventArgs e)
        {
            if (_opened)
            {
                MessageBox.Show("Close Current Device First", "Attention");
                return;
            }

            SafeFreeEnumerate();
            _serialToNode.Clear();
            ComboDevices.Items.Clear();

            var list = UsbRelayHelpers.Enumerate(out _enumListHead);
            foreach (var (serial, nodePtr, type) in list)
            {
                ComboDevices.Items.Add(serial);
                _serialToNode[serial] = nodePtr;

                Log($"Found: serial={serial}, type={type}");
            }

            if (ComboDevices.Items.Count > 0)
                ComboDevices.SelectedIndex = 0;
            else
                Log("No devices found.");
        }

       
        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            if (_opened)
            {
                MessageBox.Show("Close Current Device First", "Attention");
                return;
            }

            if (ComboDevices.SelectedItem is not string serial ||
                !_serialToNode.TryGetValue(serial, out IntPtr nodePtr) ||
                nodePtr == IntPtr.Zero)
            {
                MessageBox.Show("Select the device first", "Error");
                return;
            }

           
            _deviceHandle = UsbRelayNative.usb_relay_device_open(nodePtr);
            if (_deviceHandle == 0)
            {
                MessageBox.Show("Open Device Error!!", "Error");
                return;
            }

          
            var info = Marshal.PtrToStructure<UsbRelayDeviceInfo>(nodePtr);
            _channelCount = UsbRelayHelpers.ChannelCount(info.type);
            _opened = true;

           
            if (UsbRelayNative.usb_relay_device_get_status(_deviceHandle, out uint status) != 0)
            {
                UsbRelayNative.usb_relay_device_close(_deviceHandle);
                _deviceHandle = 0;
                _opened = false;
                _channelCount = 0;

                MessageBox.Show("Get Status Error!!", "Error");
                return;
            }

            Log($"Opened: serial={serial}, channels={_channelCount}, status=0x{status:X8}");

          
            SetDeviceOpened(true);
            SetRelayUiState(_channelCount, status);
        }

        
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            if (!_opened)
            {
                MessageBox.Show("Open Device First", "Error");
                return;
            }

            SafeCloseDevice();
            Log("Device closed.");
        }

       
        private void RelayOpen_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureOpened()) return;

            if (sender is not Button btn || !int.TryParse(btn.Tag?.ToString(), out int channel))
                return;

            if (channel < 1 || channel > _channelCount)
            {
                MessageBox.Show("Channel out of range for this device.", "Error");
                return;
            }

            //channel에는 첫번째 버튼인지 두번째 버튼인지 저장함
            int ret = UsbRelayNative.usb_relay_device_open_one_relay_channel(_deviceHandle, channel);
            Log($"Relay {channel} OPEN: ret={ret}");

            if (ret == 0) SetRelayStatus(channel, true);
        }

       
        private void RelayClose_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureOpened()) return;

            if (sender is not Button btn || !int.TryParse(btn.Tag?.ToString(), out int channel))
                return;

            if (channel < 1 || channel > _channelCount)
            {
                MessageBox.Show("Channel out of range for this device.", "Error");
                return;
            }

            int ret = UsbRelayNative.usb_relay_device_close_one_relay_channel(_deviceHandle, channel);
            Log($"Relay {channel} CLOSE: ret={ret}");

            if (ret == 0) SetRelayStatus(channel, false);
        }

      
        private void BtnOpenAll_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureOpened()) return;

            int ret = UsbRelayNative.usb_relay_device_open_all_relay_channel(_deviceHandle);
            Log($"OPEN ALL: ret={ret}");

            if (ret == 0)
            {
                for (int ch = 1; ch <= _channelCount; ch++)
                    SetRelayStatus(ch, true);
            }
        }

      
        private void BtnCloseAll_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureOpened()) return;

            int ret = UsbRelayNative.usb_relay_device_close_all_relay_channel(_deviceHandle);
            Log($"CLOSE ALL: ret={ret}");

            if (ret == 0)
            {
                for (int ch = 1; ch <= _channelCount; ch++)
                    SetRelayStatus(ch, false);
            }
        }

       
        private bool EnsureOpened()
        {
            if (!_opened || _deviceHandle == 0)
            {
                MessageBox.Show("Open Device First", "Error");
                return false;
            }
            return true;
        }

        private void SafeCloseDevice()
        {
            if (_deviceHandle != 0)
            {
                UsbRelayNative.usb_relay_device_close(_deviceHandle);
                _deviceHandle = 0;
            }

            _opened = false;
            _channelCount = 0;

            SetDeviceOpened(false);
            SetRelayUiState(0, 0);
        }

        private void SafeFreeEnumerate()
        {
            if (_enumListHead != IntPtr.Zero)
            {
                UsbRelayNative.usb_relay_device_free_enumerate(_enumListHead);
                _enumListHead = IntPtr.Zero;
            }
        }

        private void SetDeviceOpened(bool opened)
        {
            OpenStatus.Background = opened ? Brushes.LimeGreen : Brushes.Red;
            BtnOpenAll.IsEnabled = opened;
            BtnCloseAll.IsEnabled = opened;
        }

      
        private void SetRelayUiState(int channels, uint status)
        {
          
            for (int i = 0; i < 8; i++)
            {
                _relayStatus[i].Background = Brushes.Red;

                bool enabled = (channels > 0 && (i + 1) <= channels);
                _relayStatus[i].Opacity = enabled ? 1.0 : 0.25;
            }

            if (channels <= 0) return;

          
            for (int i = 0; i < channels; i++)
            {
                bool isOpen = ((status >> i) & 0x1) == 1;
                _relayStatus[i].Background = isOpen ? Brushes.LimeGreen : Brushes.Red;
            }
        }

        private void SetRelayStatus(int channel, bool isOpen)
        {
            if (channel < 1 || channel > 8) return;
            _relayStatus[channel - 1].Background = isOpen ? Brushes.LimeGreen : Brushes.Red;
        }

        private void Log(string msg)
        {
            TxtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\r\n");
            TxtLog.ScrollToEnd();
        }
    }
}
