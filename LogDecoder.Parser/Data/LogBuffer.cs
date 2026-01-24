namespace LogDecoder.Parser.Data;

public readonly struct LogBuffer
{
    private const int PayloadStartPosition = 16;
    public readonly byte[] Header;
    public readonly byte[] Data;
    public readonly int PackagesCount;
    
    public bool IsValid => Header is not null && Data is not null;

    public LogBuffer(byte[] buffer)
    {
        Header = new byte[PayloadStartPosition];
        Buffer.BlockCopy(buffer, 0, Header, 0, PayloadStartPosition);
        
        var dataLength = buffer.Length - PayloadStartPosition;
        Data = new byte[dataLength];
        Buffer.BlockCopy(buffer, PayloadStartPosition, Data, 0, dataLength);
        
        PackagesCount = GetPackagesCount();
    }

    private int GetPackagesCount()
    {
        return BitConverter.ToInt32(Header, 0);
    }
}
