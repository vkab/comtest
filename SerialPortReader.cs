using System.IO.Ports;

namespace TestCom
{
    internal class SerialPortReader : IDisposable
    {
        private readonly object _syncRoot = new ();
        private SerialPort _port;
        private static readonly CancellationTokenSource _cancellationTokenSource
            = new (Configuration.GetInstance().TimeoutMs);
        private bool _inited = false;
        private SerialDataReceivedEventHandler? _receivedEventHandler;
        private SemaphoreSlim _availableListeners = new (1, 1);
        private Action<byte[]>? _dataReceivedCallback;
        public event EventHandler<ExceptionEventHandlerArgs>? ErrorHandler;

        public bool Inited
        {
            get
            {
                lock (_syncRoot)
                    return _inited;
            }
            private set
            {
                lock (_syncRoot)
                    _inited = value;
            }
        }

        public bool Listening => _availableListeners.CurrentCount == 0;

        private SerialPortReader(SerialPort port)
        {
            _port = port;
        }

        public async Task BeginListening(Action<byte[]> callback)
        {
            await _availableListeners.WaitAsync();
            lock (_syncRoot)
            {
                ClosePortNoLock();

                //So if supported
                _port.DtrEnable = true;
                _port.RtsEnable = true;
                _port.Handshake = Handshake.RequestToSend;

                _dataReceivedCallback = callback;
                _receivedEventHandler = new SerialDataReceivedEventHandler(this.OnDataReceive);
                _port.DataReceived += _receivedEventHandler;

                _port.Open();
            }
        }

        public void StopListening()
        {
            lock (_syncRoot)
            {
                ClosePortNoLock();
            }
            _availableListeners.Release();
        }

        public void ClosePortNoLock()
        {
            if (_port.IsOpen)
                _port.Close();

            if (_receivedEventHandler != null)
            {
                _port.DataReceived -= _receivedEventHandler;
                _receivedEventHandler = null;
            }
        }

        public void Dispose()
        {
            StopListening();
            _port.Dispose();
        }

        private void OnDataReceive(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                var bytes = new byte[_port.BytesToRead];
                _port.Read(bytes, 0, bytes.Length);
                _dataReceivedCallback?.Invoke(bytes);
            }
            catch(Exception ex)
            {
                ErrorHandler?.Invoke(this, new ExceptionEventHandlerArgs(ex, Strings.PortDataReceiveError));
            }
        }

        public static Task<SerialPortReader> CreateAsync(string name)
        {
            return Task.Run(() => CheckAndInit(name), _cancellationTokenSource.Token);
        }

        private static SerialPortReader CheckAndInit(string name)
        {
            var ports = SerialPort.GetPortNames();

            if (!ports.Contains(name))
                ExceptionHelper.ThrowEx(Strings.PortNotFoundError0, name);
            var config = Configuration.GetInstance();
            var port = new SerialPort(name, config.PortBaudRate)
            {
                ReadTimeout = config.TimeoutMs,
                WriteTimeout = config.TimeoutMs,
                ReceivedBytesThreshold = config.PortFrameSize
            };

            try
            {
                port.Open();
                port.Close();
            }
            catch (Exception ex)
            {
                ExceptionHelper.ThrowEx(Strings.PortOpenError01, name, ex.Message);
            }

            return new SerialPortReader(port);
        }
    }
}
