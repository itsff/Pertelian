using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PertelianSharp
{
    public abstract class Pertelian
    {
        private readonly Encoding _ascii;
        private readonly byte[] _lineCodes =
        {
            0x80,
            0x80 + 0x40,
            0x80 + 0x14,
            0x80 + 0x40 + 0x14
        };

        public Pertelian()
	{
            _ascii = Encoding.ASCII;
	}

        protected virtual void Initialize()
        {
            WriteCode(0x38); /* initialise display (8 bit interface, shift to right */
            WriteCode(0x06); /* cursor move direction to the right, no automatic shift */
            WriteCode(0x10); /* move cursor on data write */
            WriteCode(0x0c); /* cursor off */
            ClearDisplay();
        }

        protected abstract void SendByteToDevice(byte value);
        protected abstract void Flush();

        /*
         * putcode
         *     This function writes a code byte to the device
         */
        protected void WriteCode(byte value)
        {
            SendByteToDevice(0xFE);
            SendByteToDevice(value);
            Flush();
        }

        protected void Write(byte value)
        {
            SendByteToDevice(value);
            Flush();
        }

        protected void Write(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; ++i)
            {
                Write(bytes[i]);
            }
        }

        protected void Write(string s)
        {
            byte[] bytes = _ascii.GetBytes(s);
            Write(bytes);
        }

        public void Write(int lineIndex, string msg)
        {
            if (lineIndex < 0 || lineIndex >= _lineCodes.Length)
            {
                throw new ArgumentOutOfRangeException(
                    "lineIndex",
                    lineIndex,
                    String.Format("lineIndex must be between 0 and {0}", _lineCodes.Length - 1));
            }
            if (String.IsNullOrEmpty(msg))
            {
                throw new ArgumentException("msg cannot be null or empty", "msg");
            }

            SendByteToDevice(_lineCodes[lineIndex]);
            Write(msg);
        }

        public void EnableBacklight(bool enable)
        {
            WriteCode((byte)(enable ? 0x03 : 0x02));
        }

        public void ClearDisplay()
        {
            WriteCode(0x01);
        }

        public void EnableDisplayAndCursor(bool enableDisplay, bool enableCursor, bool blinkCursor)
        {
            // 8 7 6 5 4 3 2 1
            // 0 0 0 0 1 D C B	
            int code = 0;
            code |= ((1)                   << 4);
            code |= ((enableDisplay ? 1:0) << 3);
            code |= ((enableCursor  ? 1:0) << 2);
            code |= ((blinkCursor   ? 1:0) << 1);

            WriteCode((byte)code);
        }
    }


    public class SerialPertelian : Pertelian, IDisposable
    {
        readonly System.IO.BinaryWriter _writer;
        readonly System.IO.Stream _stream;

        public SerialPertelian(string deviceFilePath)
        {
            _stream = System.IO.File.OpenWrite(deviceFilePath);
            _writer = new System.IO.BinaryWriter(_stream);

            Initialize();
        }

        private bool _isDisposed;
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _writer.Dispose();
                _stream.Dispose();
            }
        }

        protected override void SendByteToDevice(byte value)
        {
            _writer.Write(value);
        }

        protected override void Flush()
        {
            _writer.Flush();
            delay();
        }

        private volatile int _z = 0;
        private void delay()
        {
            _z = 0;
            for (int i = 0; i < 100000; ++i)
            {
                _z += i;
            }
        }
    }
}
