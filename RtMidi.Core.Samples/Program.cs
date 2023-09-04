﻿using System;
using System.Collections.Generic;
using System.Linq;
using RtMidi.Core.Devices;
using RtMidi.Core.Messages;
using Serilog;

namespace RtMidi.Core.Samples
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .MinimumLevel.Debug()
                .CreateLogger();

            using (MidiDeviceManager.Default)
            {
                var p = new Program();
            }
        }

        public Program()
        {
            // List all available MIDI API's
            foreach (var api in MidiDeviceManager.Default.GetAvailableMidiApis())
                Console.WriteLine($"Available API: {api}");

            // Listen to all available midi devices
            void ControlChangeHandler(IMidiInputDevice sender, in ControlChangeMessage msg)
            {
                Console.WriteLine($"[{sender.Name}] ControlChange: Channel:{msg.Channel} Control:{msg.Control} Value:{msg.Value}");
            }

            void NoteOnHandler(IMidiInputDevice sender, in NoteOnMessage msg)
            {
                Console.WriteLine($"[{sender.Name}] NoteOn: Channel:{msg.Channel} Key:{msg.Key} Velocity:{msg.Velocity}");
            }

            var devices = new List<IMidiInputDevice>();
            try
            {
                foreach (var inputDeviceInfo in MidiDeviceManager.Default.InputDevices)
                {
                    Console.WriteLine($"Opening {inputDeviceInfo.Name}");

                    var inputDevice = inputDeviceInfo.CreateDevice();
                    devices.Add(inputDevice);

                    inputDevice.ControlChange += ControlChangeHandler;
                    inputDevice.NoteOn += NoteOnHandler;
                    inputDevice.Open();
                }

                Console.WriteLine("Press 'q' key to stop...");
                while (true)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Q)
                    {
                        break;
                    }
                }
            }
            finally
            {
                foreach (var device in devices)
                {
                    device.ControlChange -= ControlChangeHandler;
                    device.Dispose();
                }
            }
        }
    }
}
