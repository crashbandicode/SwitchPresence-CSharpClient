using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PresenceClient.Avalonia.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 628)]
    public struct RawPacket
    {
        public ulong Magic;
        public ulong PID;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 612)]
        public byte[] NameBytes;
    }

    public class TitlePacket
    {
        public ulong Magic { get; }
        public ulong PID { get; }
        public string Name { get; set; }

        public TitlePacket(byte[] rawData)
        {
            if (rawData.Length < Marshal.SizeOf<RawPacket>())
            {
                throw new ArgumentException("Byte array is too small.");
            }

            var handle = GCHandle.Alloc(rawData, GCHandleType.Pinned);
            try
            {
                var rawPacket = Marshal.PtrToStructure<RawPacket>(handle.AddrOfPinnedObject());
                Magic = rawPacket.Magic;
                PID = rawPacket.PID;
                
                int nullTerminatorIndex = Array.IndexOf(rawPacket.NameBytes, (byte)0);
                int length = nullTerminatorIndex >= 0 ? nullTerminatorIndex : rawPacket.NameBytes.Length;
                
                string decodedName = Encoding.UTF8.GetString(rawPacket.NameBytes, 0, length).Trim();

                Name = PID == 0 ? "Home Menu" : decodedName;
            }
            finally
            {
                handle.Free();
            }
        }
    }
}