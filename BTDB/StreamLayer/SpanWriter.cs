using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BTDB.Buffer;

namespace BTDB.StreamLayer
{
    public ref struct SpanWriter
    {
        public SpanWriter(Span<byte> initialBuffer)
        {
            Buf = initialBuffer;
            InitialBuffer = initialBuffer;
            HeapBuffer = null;
            Controller = null;
        }

        public SpanWriter(ISpanWriter controller)
        {
            Controller = controller;
            Buf = new Span<byte>();
            InitialBuffer = new Span<byte>();
            HeapBuffer = null;
            Controller.Init(ref this);
        }

        public Span<byte> Buf;
        public readonly Span<byte> InitialBuffer;

        // When this is not null than Buf must be slice of HeapBuffer.AsSpan(), otherwise Buf must be slice of InitialBuffer
        public byte[]? HeapBuffer;
        public ISpanWriter? Controller;

        public ReadOnlySpan<byte> GetSpan()
        {
            if (Controller != null) ThrowCannotBeUsedWithController();
            if (HeapBuffer != null)
            {
                return HeapBuffer.AsSpan(0, HeapBuffer.Length - Buf.Length);
            }

            return InitialBuffer.Slice(0, InitialBuffer.Length - Buf.Length);
        }

        static void ThrowCannotBeUsedWithController()
        {
            throw new InvalidOperationException("Cannot have controller");
        }

        public void Reset()
        {
            if (Controller != null) ThrowCannotBeUsedWithController();
            Buf = HeapBuffer ?? InitialBuffer;
        }

        public ReadOnlySpan<byte> GetSpanAndReset()
        {
            if (Controller != null) ThrowCannotBeUsedWithController();
            if (HeapBuffer != null)
            {
                var buf = HeapBuffer;
                var res = buf.AsSpan(0, buf.Length - Buf.Length);
                HeapBuffer = null;
                Buf = InitialBuffer;
                return res;
            }
            else
            {
                var res = InitialBuffer.Slice(0, InitialBuffer.Length - Buf.Length);
                Buf = InitialBuffer;
                return res;
            }
        }

        public ByteBuffer GetByteBufferAndReset()
        {
            if (Controller != null) ThrowCannotBeUsedWithController();
            if (HeapBuffer != null)
            {
                var buf = HeapBuffer;
                var res = ByteBuffer.NewAsync(buf, 0, buf.Length - Buf.Length);
                HeapBuffer = null;
                Buf = InitialBuffer;
                return res;
            }
            else
            {
                var res = ByteBuffer.NewAsync(InitialBuffer.Slice(0, InitialBuffer.Length - Buf.Length));
                Buf = InitialBuffer;
                return res;
            }
        }

        public long GetCurrentPosition()
        {
            if (Controller != null) return Controller.GetCurrentPosition(this);

            if (HeapBuffer != null)
            {
                return HeapBuffer.Length - Buf.Length;
            }

            return InitialBuffer.Length - Buf.Length;
        }

        public void SetCurrentPosition(long pos)
        {
            if (Controller != null)
            {
                Controller.SetCurrentPosition(ref this, pos);
                return;
            }

            if (HeapBuffer != null)
            {
                Buf = HeapBuffer.AsSpan((int) pos);
                return;
            }

            Buf = InitialBuffer.Slice((int) pos);
        }

        public void Sync()
        {
            if (Controller == null) ThrowCanBeUsedOnlyWithController();
            Controller?.Sync(ref this);
        }

        static void ThrowCanBeUsedOnlyWithController()
        {
            throw new InvalidOperationException("Need controller");
        }

        void Resize(int spaceNeeded)
        {
            if (Controller != null)
            {
                Controller.Flush(ref this);
                return;
            }

            var pos = (uint) GetCurrentPosition();
            if (HeapBuffer == null)
            {
                var newSize = Math.Max((uint) InitialBuffer.Length * 2, 32);
                newSize = Math.Max(newSize, pos + (uint) spaceNeeded);
                newSize = Math.Min(newSize, int.MaxValue);
                HeapBuffer = new byte[newSize];
                InitialBuffer.Slice(0, (int) pos).CopyTo(HeapBuffer);
                Buf = HeapBuffer.AsSpan((int) pos, (int) (newSize - pos));
            }
            else
            {
                var newSize = Math.Max((uint) HeapBuffer.Length * 2, pos + (uint) spaceNeeded);
                newSize = Math.Min(newSize, int.MaxValue);
                Array.Resize(ref HeapBuffer, (int) newSize);
                Buf = HeapBuffer.AsSpan((int) pos, (int) (newSize - pos));
            }
        }

        public void WriteByteZero()
        {
            if (Buf.IsEmpty)
            {
                Resize(1);
            }

            PackUnpack.UnsafeGetAndAdvance(ref Buf, 1) = 0;
        }

        public unsafe void WriteBool(bool value)
        {
            if (Buf.IsEmpty)
            {
                Resize(1);
            }

            PackUnpack.UnsafeGetAndAdvance(ref Buf, 1) = *(byte*) &value;
        }

        public void WriteUInt8(byte value)
        {
            if (Buf.IsEmpty)
            {
                Resize(1);
            }

            PackUnpack.UnsafeGetAndAdvance(ref Buf, 1) = value;
        }

        public void WriteInt8(sbyte value)
        {
            if (Buf.IsEmpty)
            {
                Resize(1);
            }

            PackUnpack.UnsafeGetAndAdvance(ref Buf, 1) = (byte) value;
        }

        public void WriteInt8Ordered(sbyte value)
        {
            if (Buf.IsEmpty)
            {
                Resize(1);
            }

            PackUnpack.UnsafeGetAndAdvance(ref Buf, 1) = (byte) (value + 128);
        }

        public void WriteVInt16(short value)
        {
            WriteVInt64(value);
        }

        public void WriteVUInt16(ushort value)
        {
            WriteVUInt64(value);
        }

        public void WriteVInt32(int value)
        {
            WriteVInt64(value);
        }

        public void WriteVUInt32(uint value)
        {
            WriteVUInt64(value);
        }

        public void WriteVInt64(long value)
        {
            var len = PackUnpack.LengthVInt(value);
            if ((uint) Buf.Length < (uint) len)
            {
                Resize(len);
            }

            PackUnpack.UnsafePackVInt(ref PackUnpack.UnsafeGetAndAdvance(ref Buf, len), value, len);
        }

        public void WriteVUInt64(ulong value)
        {
            var len = PackUnpack.LengthVUInt(value);
            if ((uint) Buf.Length < (uint) len)
            {
                Resize(len);
            }

            PackUnpack.UnsafePackVUInt(ref PackUnpack.UnsafeGetAndAdvance(ref Buf, len), value, len);
        }

        public void WriteInt64(long value)
        {
            if ((uint) Buf.Length < 8u)
            {
                Resize(8);
            }

            Unsafe.As<byte, ulong>(ref PackUnpack.UnsafeGetAndAdvance(ref Buf, 8)) =
                PackUnpack.AsBigEndian((ulong) value);
        }

        public void WriteInt32(int value)
        {
            if ((uint) Buf.Length < 4u)
            {
                Resize(4);
            }

            Unsafe.As<byte, uint>(ref PackUnpack.UnsafeGetAndAdvance(ref Buf, 4)) =
                PackUnpack.AsBigEndian((uint) value);
        }

        public void WriteInt32LE(int value)
        {
            if ((uint) Buf.Length < 4u)
            {
                Resize(4);
            }

            Unsafe.As<byte, uint>(ref PackUnpack.UnsafeGetAndAdvance(ref Buf, 4)) =
                PackUnpack.AsLittleEndian((uint) value);
        }

        public void WriteDateTime(DateTime value)
        {
            WriteInt64(value.ToBinary());
        }

        public void WriteDateTimeForbidUnspecifiedKind(DateTime value)
        {
            if (value.Kind == DateTimeKind.Unspecified)
            {
                if (value == DateTime.MinValue)
                    value = DateTime.MinValue.ToUniversalTime();
                else if (value == DateTime.MaxValue)
                    value = DateTime.MaxValue.ToUniversalTime();
                else
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "DateTime.Kind cannot be stored as Unspecified");
            }

            WriteDateTime(value);
        }

        public void WriteTimeSpan(TimeSpan value)
        {
            WriteVInt64(value.Ticks);
        }

        public unsafe void WriteString(string? value)
        {
            if (value == null)
            {
                WriteByteZero();
                return;
            }

            var l = value.Length;
            if (l == 0)
            {
                WriteUInt8(1);
                return;
            }

            fixed (char* strPtrStart = value)
            {
                var strPtr = strPtrStart;
                var strPtrEnd = strPtrStart + l;
                var toEncode = (uint) (l + 1);
                doEncode:
                WriteVUInt32(toEncode);
                while (strPtr != strPtrEnd)
                {
                    var c = *strPtr++;
                    if (c < 0x80)
                    {
                        if (Buf.IsEmpty)
                        {
                            Resize(1);
                        }

                        PackUnpack.UnsafeGetAndAdvance(ref Buf, 1) = (byte) c;
                    }
                    else
                    {
                        if (char.IsHighSurrogate(c) && strPtr != strPtrEnd)
                        {
                            var c2 = *strPtr;
                            if (char.IsLowSurrogate(c2))
                            {
                                toEncode = (uint) ((c - 0xD800) * 0x400 + (c2 - 0xDC00) + 0x10000);
                                strPtr++;
                                goto doEncode;
                            }
                        }

                        toEncode = c;
                        goto doEncode;
                    }
                }
            }
        }

        public void WriteStringOrdered(string? value)
        {
            if (value == null)
            {
                WriteVUInt32(0x110001);
                return;
            }

            var l = value.Length;
            var i = 0;
            while (i < l)
            {
                var c = value[i];
                if (char.IsHighSurrogate(c) && i + 1 < l)
                {
                    var c2 = value[i + 1];
                    if (char.IsLowSurrogate(c2))
                    {
                        WriteVUInt32((uint) ((c - 0xD800) * 0x400 + (c2 - 0xDC00) + 0x10000) + 1);
                        i += 2;
                        continue;
                    }
                }

                WriteVUInt32((uint) c + 1);
                i++;
            }

            WriteByteZero();
        }

        public void WriteBlock(ReadOnlySpan<byte> data)
        {
            WriteBlock(ref MemoryMarshal.GetReference(data), data.Length);
        }

        public void WriteBlock(ref byte buffer, int length)
        {
            if ((uint) Buf.Length < (uint) length)
            {
                if (Controller != null)
                {
                    if (HeapBuffer != null)
                    {
                        var bufLength = HeapBuffer.Length;
                        if (length < bufLength)
                        {
                            Unsafe.CopyBlockUnaligned(ref MemoryMarshal.GetReference(Buf), ref buffer, (uint) Buf.Length);
                            buffer = Unsafe.AddByteOffset(ref buffer, (IntPtr) Buf.Length);
                            length -= Buf.Length;
                            Buf = new Span<byte>();
                        }
                        Controller.Flush(ref this);
                        if (length < bufLength)
                        {
                            Unsafe.CopyBlockUnaligned(ref PackUnpack.UnsafeGetAndAdvance(ref Buf, length), ref buffer, (uint) length);
                            return;
                        }
                    }
                    Controller.WriteBlock(ref this, ref buffer, length);
                    return;
                }
                Resize(length);
            }

            Unsafe.CopyBlockUnaligned(ref PackUnpack.UnsafeGetAndAdvance(ref Buf, length), ref buffer, (uint) length);
        }

        public void WriteBlock(byte[] buffer, int offset, int length)
        {
            WriteBlock(buffer.AsSpan(offset, length));
        }

        public unsafe void WriteBlock(IntPtr data, int length)
        {
            WriteBlock(ref Unsafe.AsRef<byte>((void*) data), length);
        }

        public void WriteBlock(byte[] data)
        {
            WriteBlock(data.AsSpan());
        }

        public unsafe void WriteGuid(Guid value)
        {
            WriteBlock(new ReadOnlySpan<byte>((byte*) &value, 16));
        }

        public void WriteSingle(float value)
        {
            WriteInt32(BitConverter.SingleToInt32Bits(value));
        }

        public void WriteDouble(double value)
        {
            WriteInt64(BitConverter.DoubleToInt64Bits(value));
        }

        public void WriteDecimal(decimal value)
        {
            var ints = decimal.GetBits(value);
            var header = (byte) ((ints[3] >> 16) & 31);
            if (ints[3] < 0) header |= 128;
            var first = (uint) ints[0] + ((ulong) ints[1] << 32);
            if (ints[2] == 0)
            {
                if (first == 0)
                {
                    WriteUInt8(header);
                }
                else
                {
                    header |= 32;
                    WriteUInt8(header);
                    WriteVUInt64(first);
                }
            }
            else
            {
                if ((uint) ints[2] < 0x10000000)
                {
                    header |= 64;
                    WriteUInt8(header);
                    WriteVUInt32((uint) ints[2]);
                    WriteInt64((long) first);
                }
                else
                {
                    header |= 64 | 32;
                    WriteUInt8(header);
                    WriteInt32(ints[2]);
                    WriteInt64((long) first);
                }
            }
        }

        public void WriteByteArray(byte[]? value)
        {
            if (value == null)
            {
                WriteByteZero();
                return;
            }

            WriteVUInt32((uint) (value.Length + 1));
            WriteBlock(value);
        }

        public void WriteByteArray(ByteBuffer value)
        {
            WriteVUInt32((uint) (value.Length + 1));
            WriteBlock(value);
        }

        public void WriteByteArray(ReadOnlySpan<byte> value)
        {
            WriteVUInt32((uint) (value.Length + 1));
            WriteBlock(value);
        }

        public void WriteByteArrayRaw(byte[]? value)
        {
            if (value == null) return;
            WriteBlock(value);
        }

        public void WriteBlock(ByteBuffer data)
        {
            WriteBlock(data.AsSyncReadOnlySpan());
        }

        public void WriteIPAddress(IPAddress? value)
        {
            if (value == null)
            {
                WriteUInt8(3);
                return;
            }

            if (value.AddressFamily == AddressFamily.InterNetworkV6)
            {
                Span<byte> buf = stackalloc byte[16];
                if (value.ScopeId != 0)
                {
                    value.TryWriteBytes(buf, out _);
                    WriteUInt8(2);
                    if ((uint) Buf.Length < 16)
                    {
                        Resize(16);
                    }

                    Unsafe.CopyBlockUnaligned(ref PackUnpack.UnsafeGetAndAdvance(ref Buf, 16),
                        ref MemoryMarshal.GetReference(buf), 16);
                    WriteVUInt64((ulong) value.ScopeId);
                }
                else
                {
                    value.TryWriteBytes(buf, out _);
                    WriteUInt8(1);
                    if ((uint) Buf.Length < 16)
                    {
                        Resize(16);
                    }

                    Unsafe.CopyBlockUnaligned(ref PackUnpack.UnsafeGetAndAdvance(ref Buf, 16),
                        ref MemoryMarshal.GetReference(buf), 16);
                }
            }
            else
            {
                WriteUInt8(0);
#pragma warning disable 612,618
                WriteInt32LE((int) value.Address);
#pragma warning restore 612,618
            }
        }

        public void WriteVersion(Version? value)
        {
            if (value == null)
            {
                WriteUInt8(0);
                return;
            }

            WriteVUInt32((uint) value.Major + 1);
            WriteVUInt32((uint) value.Minor + 1);
            if (value.Minor == -1) return;
            WriteVUInt32((uint) value.Build + 1);
            if (value.Build == -1) return;
            WriteVUInt32((uint) value.Revision + 1);
        }
    }
}
