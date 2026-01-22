using System;
using System.Collections.Generic;
using System.Text;

namespace BeaconRelayControlWpf.Services
{
    public enum RelayProtocolMode
    {
        Hex,
        Ascii
    }

    public static class RelayProtocol
    {
      
     
        public static readonly byte[] HexOn = new byte[] { 0xA0, 0x01, 0x01, 0xA2 };
        public static readonly byte[] HexOff = new byte[] { 0xA0, 0x01, 0x00, 0xA1 };

      
        public const string AsciiOn = "ON\r\n";
        public const string AsciiOff = "OFF\r\n";
    }
}
