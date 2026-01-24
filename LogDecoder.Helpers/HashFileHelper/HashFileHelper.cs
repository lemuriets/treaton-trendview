using System.Security.Cryptography;
using System.Text;

namespace LogDecoder.Helpers;
public static class HashFileHelper
{
    // public static async Task<string> GetPartialHashAsync(string filePath, int length = 4 * 1024 * 1024)
    // {
    //     var file = new FileInfo(filePath);
    //     if (!file.Exists)
    //         throw new FileNotFoundException();
    //
    //     var bytesToRead = (int)Math.Min(length, file.Length);
    //     var buffer = new byte[bytesToRead];
    //
    //     using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, length, useAsync: true))
    //     {
    //         var bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length);
    //     }
    //
    //     var lengthBytes = BitConverter.GetBytes(file.Length);
    //     var toHash = new byte[bytesToRead + lengthBytes.Length];
    //
    //     using var md5 = MD5.Create();
    //     var hash = md5.ComputeHash(toHash);
    //
    //     return Encoding.UTF8.GetString(hash);
    // }
    //
    // public static bool Compare(string file, string fileWithHash)
    // {
    //     if (!File.Exists(fileWithHash))
    //         throw new FileNotFoundException();
    //
    //     var newHash = await GetPartialHashAsync(file);
    //     var oldHash = File.ReadAllText(fileWithHash);
    //
    //     return newHash == oldHash;
    // }
}
