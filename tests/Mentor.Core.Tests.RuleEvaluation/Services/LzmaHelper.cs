using System.Text;
using SharpCompress.Compressors.LZMA;

namespace Mentor.Core.Tests.RuleEvaluation.Services
{
    internal static class LzmaHelper
    {
        public static string DecompressToString(byte[] lzmaBytes)
        {
            using var inStream = new MemoryStream(lzmaBytes);
            using var outStream = new MemoryStream();

            // .lzma header: properties (5 bytes) + uncompressed size (8 bytes LE)
            var properties = new byte[5];
            if (inStream.Read(properties, 0, 5) != 5)
            {
                throw new InvalidOperationException("Invalid LZMA stream (properties)");
            }

            var sizeBytes = new byte[8];
            if (inStream.Read(sizeBytes, 0, 8) != 8)
            {
                throw new InvalidOperationException("Invalid LZMA stream (size)");
            }
            long outSize = BitConverter.ToInt64(sizeBytes, 0);
            long compressedSize = inStream.Length - inStream.Position;

            using (var lzma = new LzmaStream(properties, inStream, compressedSize, outSize, null, false))
            {
                lzma.CopyTo(outStream);
            }

            return Encoding.UTF8.GetString(outStream.ToArray());
        }
    }
}

