using System;
using Google.Protobuf;

namespace eyetuitive.NET.classes
{
    internal static class Helper
    {
        internal static Guid FromByteString(ByteString byteString)
        {
            if (byteString == null || byteString.IsEmpty) return Guid.Empty;
            try
            {
                return new Guid(byteString.ToByteArray());
            }
            catch (Exception) { }
            return Guid.Empty;
        }
    }
}
