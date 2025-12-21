using System;
using RtMidi.Core.Unmanaged.API;
using Serilog;
using System.Runtime.InteropServices;
#if MACCATALYST
using ObjCRuntime;
#endif
namespace RtMidi.Core.Unmanaged.Devices
{
    internal class RtMidiInputDevice : RtMidiDevice, IRtMidiInputDevice
    {
#if MACCATALYST
        /// <summary>
        /// Static callback delegate for AOT compatibility on Mac Catalyst
        /// </summary>
        private static readonly RtMidiCallback _staticCallbackDelegate = HandleRtMidiCallbackStatic;
        private GCHandle _selfHandle;
#else
        /// <summary>
        /// Ensure delegate is not garbage collected (see https://stackoverflow.com/questions/6193711/call-has-been-made-on-garbage-collected-delegate-in-c)
        /// </summary>
        private readonly RtMidiCallback _rtMidiCallbackDelegate;
#endif

        internal RtMidiInputDevice(uint portNumber) : base(portNumber)
        {
#if !MACCATALYST
            _rtMidiCallbackDelegate = HandleRtMidiCallback;
#endif
        }

        public event EventHandler<byte[]> Message;

        protected override IntPtr CreateDevice()
        {
            IntPtr handle = IntPtr.Zero;
            try
            {
                Console.WriteLine("Creating default input device");
                handle = RtMidiC.Input.CreateDefault();
                CheckForError(handle);

                Console.WriteLine("Setting types to ignore");
                RtMidiC.Input.IgnoreTypes(handle, false, true, true);
                CheckForError(handle);

                Console.WriteLine("Setting input callback");
#if MACCATALYST
                // Use GCHandle to pass 'this' through userData for AOT compatibility
                _selfHandle = GCHandle.Alloc(this);
                RtMidiC.Input.SetCallback(handle, _staticCallbackDelegate, GCHandle.ToIntPtr(_selfHandle));
#else
                RtMidiC.Input.SetCallback(handle, _rtMidiCallbackDelegate, IntPtr.Zero);
#endif
                CheckForError(handle);

                return handle;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to create default input device - {e.Message}");

#if MACCATALYST
                if (_selfHandle.IsAllocated)
                    _selfHandle.Free();
#endif

                if (handle != IntPtr.Zero)
                {
                    Console.WriteLine("Freeing input device handle");
                    try
                    {
                        RtMidiC.Input.Free(handle);
                        CheckForError(handle);
                    }
                    catch (Exception e2)
                    {
                        Console.WriteLine("Unable to free input device");
                    }
                }

                return IntPtr.Zero;
            }
        }

#if MACCATALYST
        [MonoPInvokeCallback(typeof(RtMidiCallback))]
        private static void HandleRtMidiCallbackStatic(double timestamp, IntPtr messagePtr, UIntPtr messageSize, IntPtr userData)
        {
            if (userData == IntPtr.Zero) return;

            var handle = GCHandle.FromIntPtr(userData);
            if (!handle.IsAllocated) return;

            var instance = handle.Target as RtMidiInputDevice;
            instance?.HandleRtMidiCallbackInstance(timestamp, messagePtr, messageSize);
        }

        private void HandleRtMidiCallbackInstance(double timestamp, IntPtr messagePtr, UIntPtr messageSize)
        {
            try
            {
                var messageHandlers = Message;
                if (messageHandlers != null)
                {
                    var size = (int)messageSize;
                    var message = new byte[size];
                    Marshal.Copy(messagePtr, message, 0, size);
                    messageHandlers.Invoke(this, message);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception occurred while receiving MIDI message");
            }
        }
#else
        private void HandleRtMidiCallback(double timestamp, IntPtr messagePtr, UIntPtr messageSize, IntPtr userData)
        {
            try
            {
                var messageHandlers = Message;
                if (messageHandlers != null)
                {
                    // Copy message to managed byte array
                    var size = (int)messageSize;
                    var message = new byte[size];
                    Marshal.Copy(messagePtr, message, 0, size);

                    // Invoke message handlers
                    messageHandlers.Invoke(this, message);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception occurred while receiving MIDI message");
                return;
            }
        }
#endif

        protected override void DestroyDevice()
        {
            try
            {
                Log.Debug("Cancelling input callback");
                RtMidiC.Input.CancelCallback(Handle);
                CheckForError();

                Log.Debug("Freeing input device handle");
                RtMidiC.Input.Free(Handle);

#if MACCATALYST
                if (_selfHandle.IsAllocated)
                    _selfHandle.Free();
#endif
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while freeing input device handle");
            }
        }
    }
}
