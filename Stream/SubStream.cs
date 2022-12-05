using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ModUpdater.Streams;

public class ReadOnlySubStream : Stream
{
    private Stream upstream;
    private long length;
    private long offset;

    public ReadOnlySubStream(Stream upstream, long length)
    {
        this.upstream = upstream;
        try
        {
            offset = upstream.Position;
        }
        catch { }
        this.length = length;
    }

    public event Action<long> OnUpdatePosition;

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => length;

    private long position;
    public override long Position
    {
        get => position;
        set
        {
            position = value;
            OnUpdatePosition?.Invoke(position);
        }
    }

    public override void Flush() => upstream.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => upstream.FlushAsync(cancellationToken);
    public override int Read(byte[] buffer, int offset, int count)
    {
        var len = Math.Min(count, length - Position);
        if (len <= 0) return 0;
        var add = upstream.Read(buffer, offset, (int)len);
        Position += add;
        return add;
    }
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var len = Math.Min(count, length - Position);
        if (len <= 0) return 0;
        var add = await upstream.ReadAsync(buffer, offset, (int)len, cancellationToken);
        Position += add;
        return add;
    }
    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                {
                    if (offset < 0 || offset >= length) throw new ArgumentOutOfRangeException(nameof(offset));
                    Position = offset;
                    upstream.Seek(this.offset + offset, SeekOrigin.Begin);
                }
                break;
            case SeekOrigin.Current:
                {
                    var np = Position + offset;
                    if (np < 0 || np >= length) throw new ArgumentOutOfRangeException(nameof(offset));
                    Position = np;
                    upstream.Seek(offset, SeekOrigin.Current);
                }
                break;
            case SeekOrigin.End:
                {
                    var np = length + offset;
                    if (np < 0 || np >= length) throw new ArgumentOutOfRangeException(nameof(offset));
                    Position = np;
                    upstream.Seek(this.offset + Position, SeekOrigin.Begin);
                }
                break;
            default: throw new ArgumentException(nameof(origin));
        }
        return Position;
    }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
