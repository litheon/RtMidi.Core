using RtMidi.Core.Unmanaged.Devices;
using System;
using System.Threading;

namespace RtMidi.Core.Devices
{

    internal abstract class MidiDevice : IMidiDevice 
    {
        private readonly IRtMidiDevice _rtMidiDevice;
        private bool _disposed;

        protected MidiDevice(IRtMidiDevice rtMidiDevice, string name)
        {
            Console.WriteLine($"MidiDevice constructor - {name}");
            _rtMidiDevice = rtMidiDevice ?? throw new ArgumentNullException(nameof(rtMidiDevice));
            Name = name;
        }

        public bool IsOpen => _rtMidiDevice.IsOpen;
        public string Name { get; }
        public bool Open() => _rtMidiDevice.Open();
        public void Close() => _rtMidiDevice.Close();
        
        public void Dispose()
        {
            lock(_rtMidiDevice)
            {
                if (_disposed) return;

                try
                {
                    Disposing();
                    _rtMidiDevice.Dispose();
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        protected virtual void Disposing()
        {
        }
    }
}
