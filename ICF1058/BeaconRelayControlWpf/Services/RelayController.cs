using System;
using System.Collections.Generic;
using System.Text;

using System;
using System.IO.Ports;


namespace BeaconRelayControlWpf.Services
{
    public sealed class RelayController : IDisposable
    {
        private SerialPort? _sp;
        private RelayProtocolMode _mode = RelayProtocolMode.Hex;

        public bool IsConnected => _sp != null && _sp.IsOpen;

        public void SetMode(RelayProtocolMode mode) => _mode = mode;

        public void Connect(string portName, int baudRate)
        {
            Disconnect();

            var sp = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
            {
                Handshake = Handshake.None,
                Encoding = Encoding.ASCII,
                ReadTimeout = 500,
                WriteTimeout = 500,
                DtrEnable = false,
                RtsEnable = false
            };

            sp.Open();
            _sp = sp;
        }

        public void Disconnect()
        {
            if (_sp == null) return;

            try
            {
                if (_sp.IsOpen) _sp.Close();
            }
            finally
            {
                _sp.Dispose();
                _sp = null;
            }
        }

        public void TurnOn()
        {
            EnsureConnected();
            if (_mode == RelayProtocolMode.Hex)
                WriteBytes(RelayProtocol.HexOn);
            else
                WriteText(RelayProtocol.AsciiOn);
        }

        public void TurnOff()
        {
            EnsureConnected();
            if (_mode == RelayProtocolMode.Hex)
                WriteBytes(RelayProtocol.HexOff);
            else
                WriteText(RelayProtocol.AsciiOff);
        }

        private void WriteBytes(byte[] bytes)
        {
            EnsureConnected();
            _sp!.Write(bytes, 0, bytes.Length);
        }

        private void WriteText(string text)
        {
            EnsureConnected();
            _sp!.Write(text);
        }

        private void EnsureConnected()
        {
            if (!IsConnected)
                throw new InvalidOperationException("시리얼 포트가 연결되어 있지 않습니다. 먼저 연결하세요.");
        }

        public void Dispose() => Disconnect();
    }
}
