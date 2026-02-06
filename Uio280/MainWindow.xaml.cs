using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls; 
namespace BlinkTest
{
   
    [StructLayout(LayoutKind.Sequential)] //필드를 코드에 적은 순서대로 메모리에 배치하라는 의미
    public struct USB_INPUT
    {
        public int ProductID;
        public byte Status;
        public byte Button;
        public byte Output;
        public byte Mask;
    }

    public partial class MainWindow : Window
    {
      
        [DllImport("uio.dll", CallingConvention = CallingConvention.StdCall)] private static extern int usb_io_init(int pID);
        [DllImport("uio.dll", CallingConvention = CallingConvention.StdCall)] private static extern void set_usb_events(IntPtr hWnd);
        [DllImport("uio.dll", CallingConvention = CallingConvention.StdCall)] private static extern void get_usb_input(IntPtr lParam, ref USB_INPUT uInput);
        [DllImport("uio.dll", CallingConvention = CallingConvention.StdCall)] private static extern bool usb_io_output(int pID, int cmd, int io1, int io2, int io3, int io4);
        [DllImport("uio.dll", CallingConvention = CallingConvention.StdCall)] private static extern bool usb_io_reset(int pID);
        [DllImport("uio.dll", CallingConvention = CallingConvention.StdCall)] private static extern bool usb_in_request(int pID);

      
        private const int WM_INPUT = 0x00FF;
        private const int WM_DEVICECHANGE = 0x0219;

        private HwndSource? _hwndSource;
        private int _cnt = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

      
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            NativeDllCheck.CheckAndLoadUioDll(); // dll 로드되었는지 확인

            var hwnd = new WindowInteropHelper(this).Handle;

           
            set_usb_events(hwnd);

           
            _hwndSource = HwndSource.FromHwnd(hwnd);
            _hwndSource.AddHook(WndProc);

          
            if (comboBox1.Items.Count > 0 && comboBox1.SelectedIndex < 0)
                comboBox1.SelectedIndex = 0;

            label10.Text = "---";
            label11.Text = "---";
            toolStripStatusLabel2.Text = "";
         

        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(WndProc);
                _hwndSource = null;
            }
        }

      
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_INPUT:
                    {
                        try
                        {
                            USB_INPUT uInput = new USB_INPUT();
                            get_usb_input(lParam, ref uInput);

                            toolStripStatusLabel2.Text =
                                string.Format(" pID:{0:X}, Status:{1:X2}, Button:{2:X2}, Mask:{3:X2}, Count:{4:0} ",
                                    uInput.ProductID, uInput.Status, uInput.Button, uInput.Mask, _cnt++);
                        }
                        catch (Exception ex)
                        {
                            toolStripStatusLabel2.Text = "WM_INPUT error: " + ex.Message;
                        }
                        break;
                    }

                case WM_DEVICECHANGE:
                  
                    break;
            }

            return IntPtr.Zero;
        }


        private int GetSelectedProductIdOrZero()
        {
            if (comboBox1.SelectedIndex < 0)
                return 0;

           
            string text = comboBox1.SelectedItem switch
            {
                ComboBoxItem cbi => (cbi.Content?.ToString() ?? ""),
                _ => comboBox1.Text ?? ""
            };

            text = text.Trim();
            if (text.Length == 0) return 0;

            try
            {
                return Convert.ToInt32(text, 16);
            }
            catch
            {
            
                MessageBox.Show($"Product ID 파싱 실패: '{text}'", "Input Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return 0;
            }
        }

        private void SetResult(string functionCall, string returnValue)
        {
            label10.Text = functionCall;
            label11.Text = returnValue;
        }
       


        private void button13_Click(object sender, RoutedEventArgs e)
        {
            int selectID = GetSelectedProductIdOrZero();
            SetResult($"usb_io_init(0x{selectID:x})", "---");

            int usbID = usb_io_init(selectID);

            string rv = $"0x{usbID:x}";
            if (usbID == 0) rv = "0 = Device not found";
            if (usbID == 0xFFFF) rv = "0xFFFF = Access denide";

            label11.Text = rv;
          
        }

       
        private void button15_Click(object sender, RoutedEventArgs e)
        {
            int selectID = GetSelectedProductIdOrZero();
            SetResult($"usb_io_reset(0x{selectID:x})", "---");

            bool result = usb_io_reset(selectID);
            label11.Text = result ? "True" : "False";
        }

     
        private void button14_Click(object sender, RoutedEventArgs e)
        {
            int selectID = GetSelectedProductIdOrZero();
            SetResult($"usb_in_request(0x{selectID:x})", "---");

            bool result = usb_in_request(selectID);
            label11.Text = result ? "True" : "False";
        }

      
        private void OutputHigh(int outputNo)
        {
            int selectID = GetSelectedProductIdOrZero();
            SetResult($"usb_io_output(0x{selectID:x},0,{outputNo},0,0,0)", "---");

            bool result = usb_io_output(selectID, 0, outputNo, 0, 0, 0);
            label11.Text = result ? "True" : "False";
        }

        private void OutputLow(int outputNo)
        {
            int selectID = GetSelectedProductIdOrZero();
            SetResult($"usb_io_output(0x{selectID:x},0,{-outputNo},0,0,0)", "---");

            bool result = usb_io_output(selectID, 0, -outputNo, 0, 0, 0);
            label11.Text = result ? "True" : "False";
        }

        private void OutputBlink(int outputNo, string highText, string lowText)
        {
            int selectID = GetSelectedProductIdOrZero();

            try
            {
                // WinForms 원본과 동일: 10진수 변환, 실패 시 예외 발생
                int high = Convert.ToInt32((highText ?? "").Trim(), 10);
                int low = Convert.ToInt32((lowText ?? "").Trim(), 10);

                int blink = (high * 16) + low;

                SetResult($"usb_io_output(0x{selectID:x},0x{blink:x},{outputNo},0,0,0)", "---");

                bool result = usb_io_output(selectID, blink, outputNo, 0, 0, 0);
                label11.Text = result ? "True" : "False";
            }
            catch (Exception ex)
            {
                // 최소한 오류를 눈에 보이게
                MessageBox.Show(
                    $"Blink 값이 올바르지 않습니다.\n" +
                    $"High='{highText}', Low='{lowText}'\n\n" +
                    $"원인: {ex.Message}",
                    "Blink Input Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                // 원본에 가까운 느낌으로 return value 영역에도 표시(선택)
                label11.Text = "Blink input error";
            }
        }


       

     
        private void button1_Click(object sender, RoutedEventArgs e) => OutputHigh(1);
        private void button5_Click(object sender, RoutedEventArgs e) => OutputLow(1);
        private void button9_Click(object sender, RoutedEventArgs e)
            => OutputBlink(1, textBox1.Text, textBox5.Text);

      
        private void button2_Click(object sender, RoutedEventArgs e) => OutputHigh(2);
        private void button6_Click(object sender, RoutedEventArgs e) => OutputLow(2);
        private void button10_Click(object sender, RoutedEventArgs e)
            => OutputBlink(2, textBox2.Text, textBox6.Text);

       
        private void button3_Click(object sender, RoutedEventArgs e) => OutputHigh(3);
        private void button7_Click(object sender, RoutedEventArgs e) => OutputLow(3);
        private void button11_Click(object sender, RoutedEventArgs e)
            => OutputBlink(3, textBox3.Text, textBox7.Text);

       
        private void button4_Click(object sender, RoutedEventArgs e) => OutputHigh(4);
        private void button8_Click(object sender, RoutedEventArgs e) => OutputLow(4);
        private void button12_Click(object sender, RoutedEventArgs e)
            => OutputBlink(4, textBox4.Text, textBox8.Text);
    }
}
