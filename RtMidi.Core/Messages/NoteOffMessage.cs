﻿using RtMidi.Core.Enums;
using Serilog;
using RtMidi.Core.Devices;
namespace RtMidi.Core.Messages
{
    /// <summary>
    /// This message is sent when a note is released (ended). 
    /// </summary>
    public readonly struct NoteOffMessage
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<NoteOffMessage>();

        public NoteOffMessage(Channel channel, Key key, int velocity)
        {
            StructHelper.IsWithin7BitRange(nameof(velocity), velocity);

            Channel = channel;
            Key = key;
            Velocity = velocity;
        }

        /// <summary>
        /// MIDI Channel
        /// </summary>
        public Channel Channel { get; }

        /// <summary>
        /// Key number (0-127)
        /// </summary>
        public Key Key { get; }

        /// <summary>
        /// Velocity value (0-127)
        /// </summary>
        public int Velocity { get; }

        internal byte[] Encode()
        {
            return new[]
            {
                StructHelper.StatusByte(Midi.Status.NoteOffBitmask, Channel),
                StructHelper.DataByte(Key),
                StructHelper.DataByte(Velocity)
            };
        }

        internal static bool TryDecode(byte[] message, out NoteOffMessage msg) 
        {
            if (message.Length != 3)
            {
                Log.Error("Incorrect number of bytes ({Length}) received for Note Off message", message.Length);
                msg = default;
                return false;
            }

            msg = new(
                (Channel) (Midi.ChannelBitmask & message[0]),
                (Key) (Midi.DataBitmask & message[1]),
                Midi.DataBitmask & message[2]
            );
            return true;
        }

        public override string ToString()
        {
            return $"{nameof(Channel)}: {Channel}, {nameof(Key)}: {Key}, {nameof(Velocity)}: {Velocity}";
        }
    }
}