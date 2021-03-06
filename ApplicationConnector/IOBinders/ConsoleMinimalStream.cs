﻿using System;
using System.IO;

namespace RabanSoft.ApplicationConnector.IOBinders
{
    class ConsoleMinimalStream : Stream
    {
        public event Action<(byte[] buffer, int offset, int count)> OnData;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => 0;

        public override long Position { get => 0; set { } }

        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => 0;
        public override long Seek(long offset, SeekOrigin origin) => 0;
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) => OnData?.Invoke((buffer, offset, count));
    }
}
