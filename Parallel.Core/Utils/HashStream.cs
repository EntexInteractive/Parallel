// Copyright 2026 Entex Interactive

using System.Security.Cryptography;

namespace Parallel.Core.Utils
{
    public class HashStream : Stream
    {
        private readonly Stream _inner;
        private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        private readonly Action<long>? _reportBytes;
        private long _totalWrite = 0;
        private long _totalRead = 0;

        public HashStream(Stream inner)
        {
            _inner = inner;
        }

        public HashStream(Stream inner, Action<long> reportBytes)
        {
            _inner = inner;
            _reportBytes = reportBytes;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _inner.Dispose();
        }

        public string GetHashHexString()
        {
            return Convert.ToHexStringLower(_hash.GetHashAndReset());
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = _inner.Read(buffer, offset, count);
            if (read <= 0) return read;

            _hash.AppendData(buffer.AsSpan(offset, read));
            _totalRead += read;
            _reportBytes?.Invoke(_totalRead);

            return read;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            int read = await _inner.ReadAsync(buffer, ct);
            if (read <= 0) return read;

            _hash.AppendData(buffer.Span[..read]);
            _totalRead += read;
            _reportBytes?.Invoke(_totalRead);

            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _hash.AppendData(buffer.AsSpan(offset, count));
            _inner.Write(buffer, offset, count);
            _totalWrite += count;

            _reportBytes?.Invoke(_totalWrite);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            _hash.AppendData(buffer.Span);
            await _inner.WriteAsync(buffer, cancellationToken);
            _totalWrite += buffer.Length;

            _reportBytes?.Invoke(_totalWrite);
        }

        #region Stream required overrides

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }

        public override void Flush()
        {
            _inner.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _inner.FlushAsync(cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _inner.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _inner.SetLength(value);
        }

        #endregion
    }
}