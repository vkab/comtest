using NATS.Client;

namespace TestCom
{
    internal class NatsDataSender : IDisposable
    {
        private readonly IConnection _connection;
        private readonly string _subject;
        private int _packetsSent;
        private readonly object _syncRoot = new();
        public event EventHandler<ExceptionEventHandlerArgs>? ErrorHandler;

        public NatsDataSender(string url, string subject, bool secured)
        {
            var connectionFactory = new ConnectionFactory();
            _connection = secured
               ? connectionFactory.CreateSecureConnection(url)
               : connectionFactory.CreateConnection(url);
            _subject = subject;
        }

        public void SendData(byte[] data)
        {
            lock (_syncRoot)
            {
                var packetId = _packetsSent++;
                Task.Run(() => SendDataInternal(data, packetId));
            }
        }

        private void SendDataInternal(byte[] data, int packetId, int retryCount = 3)
        {
            try
            {
                var msgHeader = new MsgHeader();
                msgHeader.Add("PacketId", packetId.ToString());
                var msg = new Msg(_subject, msgHeader, data);
                _connection.Publish(msg);
            }
            catch (NATSReconnectBufferException) when (retryCount > 0)
            {
                Thread.Sleep(1000); //условно
                SendDataInternal(data, packetId, --retryCount);
            }
            catch (Exception ex)
            {
                ErrorHandler?.Invoke(this, new ExceptionEventHandlerArgs(ex, Strings.DataSendingError));
            }
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

    }
}
