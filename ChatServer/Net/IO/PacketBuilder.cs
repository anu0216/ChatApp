using System;
using System.IO;
using System.Text;

namespace ChatServer.Net.IO
{
    public class PacketBuilder
    {
        MemoryStream _ms;

        public PacketBuilder()
        {
            _ms = new MemoryStream();
        }

        public void WriteOpCode(byte opcode)
        {
            _ms.WriteByte(opcode);
        }

        public void WriteString(string msg)
        {
            var msgLength = msg.Length;
            _ms.Write(BitConverter.GetBytes(msgLength), 0, sizeof(int));
            _ms.Write(Encoding.ASCII.GetBytes(msg), 0, msgLength);
        }

        public byte[] GetPacketBytes()
        {
            return _ms.ToArray();
        }
    }
}