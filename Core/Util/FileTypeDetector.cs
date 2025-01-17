using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Chord.Core.Util
{
    public static class FileTypeDetector
    {
        private static readonly Dictionary<string, byte[]> FileSignatures = new Dictionary<string, byte[]>
        {
            { "ZIP", new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
            { "7Z", new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C } },
            { "RAR", new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07 } },
            { "PDF", new byte[] { 0x25, 0x50, 0x44, 0x46 } },
            { "PNG", new byte[] { 0x89, 0x50, 0x4E, 0x47 } },
            { "JPG", new byte[] { 0xFF, 0xD8, 0xFF } },
            { "GIF", new byte[] { 0x47, 0x49, 0x46, 0x38 } },
            { "SNGPKG", new byte[] { 0x53, 0x4E, 0x47, 0x50, 0x4B, 0x47 } },
        };

        public static string DetectFileType(string filePath)
        {
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    // Read the first 8 bytes (enough for most signatures)
                    byte[] buffer = new byte[8];
                    int bytesRead = fileStream.Read(buffer, 0, buffer.Length);
                    Console.WriteLine("File signature: " + BitConverter.ToString(buffer));

                    foreach (var signature in FileSignatures)
                    {
                        if (bytesRead >= signature.Value.Length &&
                            buffer.Take(signature.Value.Length).SequenceEqual(signature.Value))
                        {
                            return signature.Key;
                        }
                    }

                    return "Unknown";
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error detecting file type: {ex.Message}", ex);
            }
        }
    }
}
