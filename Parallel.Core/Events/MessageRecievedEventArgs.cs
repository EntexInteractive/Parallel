// Copyright 2026 Entex Interactive

using System.Net.Sockets;
using System.Text;
using Parallel.Core.Security;
using Parallel.Core.Utils;

namespace Parallel.Core.Events
{
    public class MessageRecievedEventArgs(UdpReceiveResult result)
    {
        public DateTime TimeStamp { get; } = DateTime.Now;
        public string Message { get; } = Encryption.Decode(Encoding.UTF8.GetString(result.Buffer));
    }
}