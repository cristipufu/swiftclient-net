using System;
using System.IO;

namespace SwiftClient.Utils
{
    public class BufferedHTTPStream : Stream
    {
        private long _cacheLength;
        private const int _noDataAvaiable = 0;
        private MemoryStream _stream = null;
        private long _currentChunkNumber = -1;
        private long? _length;

        private Func<long, long, Stream> _getStream;
        private Func<long> _getContentLength;

        private bool isDisposed = false;


        public BufferedHTTPStream(Func<long, long, Stream> streamFunc, Func<long> lengthFunc, long bufferLength = 4096)
        {
            _getStream = streamFunc;
            _getContentLength = lengthFunc;
            _cacheLength = bufferLength;
        }

        public override bool CanRead
        {
            get
            {
                EnsureNotDisposed();
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                EnsureNotDisposed();
                return false;
            }
        }

        public override bool CanSeek
        {
            get
            {
                EnsureNotDisposed();
                return true;
            }
        }

        public override long Length
        {
            get
            {
                EnsureNotDisposed();
                if (_length == null)
                {
                    _length = _getContentLength();
                }
                return _length.Value;
            }
        }

        public override long Position
        {
            get
            {
                EnsureNotDisposed();
                long streamPosition = (_stream != null) ? _stream.Position : 0;
                long position = (_currentChunkNumber != -1) ? _currentChunkNumber * _cacheLength : 0;

                return position + streamPosition;
            }
            set
            {
                EnsureNotDisposed();
                EnsurePositiv(value, "Position");
                Seek(value);
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            EnsureNotDisposed();
            switch (origin)
            {
                case SeekOrigin.Begin:
                    break;
                case SeekOrigin.Current:
                    offset = Position + offset;
                    break;
                default:
                    offset = Length + offset;
                    break;
            }

            return Seek(offset);
        }

        private long Seek(long offset)
        {
            long chunkNumber = offset / _cacheLength;

            if (_currentChunkNumber != chunkNumber)
            {
                ReadChunk(chunkNumber);
                _currentChunkNumber = chunkNumber;
            }

            offset = offset - _currentChunkNumber * _cacheLength;

            _stream.Seek(offset, SeekOrigin.Begin);

            return Position;
        }

        private void ReadNextChunk()
        {
            _currentChunkNumber += 1;
            ReadChunk(_currentChunkNumber);
        }

        private void ReadChunk(long chunkNumberToRead)
        {
            long rangeStart = chunkNumberToRead * _cacheLength;

            if (rangeStart >= Length) { return; }

            long rangeEnd = rangeStart + _cacheLength - 1;
            if (rangeStart + _cacheLength > Length)
            {
                rangeEnd = Length - 1;
            }

            if (_stream != null) { _stream.Dispose(); }
            _stream = new MemoryStream((int)_cacheLength);

            var responseStream = _getStream(rangeStart, rangeEnd);

            responseStream.Position = 0;
            responseStream.CopyTo(_stream);
            responseStream.Dispose();

            _stream.Position = 0;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            EnsureNotDisposed();

            EnsureNotNull(buffer, "buffer");
            EnsurePositiv(offset, "offset");
            EnsurePositiv(count, "count");

            if (buffer.Length - offset < count) { throw new ArgumentException("count"); }

            if (_stream == null) { ReadNextChunk(); }

            if (Position >= Length) { return _noDataAvaiable; }

            if (Position + count > Length)
            {
                count = (int)(Length - Position);
            }

            int bytesRead = _stream.Read(buffer, offset, count);
            int totalBytesRead = bytesRead;
            count -= bytesRead;

            while (count > _noDataAvaiable)
            {
                ReadNextChunk();
                offset = offset + bytesRead;
                bytesRead = _stream.Read(buffer, offset, count);
                count -= bytesRead;
                totalBytesRead = totalBytesRead + bytesRead;
            }

            return totalBytesRead;
        }

        public override void SetLength(long value)
        {
            EnsureNotDisposed();
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            EnsureNotDisposed();
            throw new NotImplementedException();
        }

        public override void Flush()
        {
            EnsureNotDisposed();
        }

        private void EnsureNotNull(object obj, string name)
        {
            if (obj != null) { return; }
            throw new ArgumentNullException(name);
        }

        private void EnsureNotDisposed()
        {
            if (!isDisposed) { return; }
            throw new ObjectDisposedException("BufferedHTTPStream");
        }

        private void EnsurePositiv(int value, string name)
        {
            if (value > -1) { return; }
            throw new ArgumentOutOfRangeException(name);
        }

        private void EnsurePositiv(long value, string name)
        {
            if (value > -1) { return; }
            throw new ArgumentOutOfRangeException(name);
        }

        private void EnsureNegativ(long value, string name)
        {
            if (value < 0) { return; }
            throw new ArgumentOutOfRangeException(name);
        }
    }
}
