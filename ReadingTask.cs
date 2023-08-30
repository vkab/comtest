using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCom
{
    internal class ReadingTask : IDisposable
    {
        private readonly SerialPortReader _portReader;
        private NatsDataSender? _dataSender;

        private ReadingTask(SerialPortReader portReader)
        {
            _portReader = portReader;
            _portReader.ErrorHandler += HandleError;
        }

        public async Task StartWorkAsync()
        {
            var config = Configuration.GetInstance();
            _dataSender = new NatsDataSender(
                config.NatsUrl,
                config.NatsSubject,
                config.NatsSecured);
            _dataSender.ErrorHandler += HandleError;
            await _portReader.BeginListening(data => _dataSender.SendData(data)); 
            Console.WriteLine(Strings.StartWorking);
        }

        public void StopWork()
        {
            _portReader.StopListening();
            if (_dataSender != null)
            {
                _dataSender!.ErrorHandler -= HandleError;
                _dataSender!.Dispose();
                _dataSender = null;
            }
            Console.WriteLine(Strings.StopWorking);
        }

        public void Dispose()
        {
            _portReader.Dispose();
            _portReader.ErrorHandler -= HandleError;
            _dataSender?.Dispose();
        }

        private void HandleError(object? sender, ExceptionEventHandlerArgs e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.Exception.ToString());
            StopWork();
        }

        public static async Task<ReadingTask> CreateInstanceAsync()
        {
            var portReader = await SerialPortReader.CreateAsync(Configuration.GetInstance().PortName);
            return new ReadingTask(portReader);
        }
    }
}
