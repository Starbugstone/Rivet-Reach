using System.IO;

namespace RivetReach.Editor
{
    // Envelope-only compatibility probes use a single integer body, not a full
    // world. Give them the historical schema whose content fingerprint they test.
    internal static class SaveFixtureEnvelope
    {
        public static byte[] Schema3(byte[] bytes)
        {
            using var stream=new MemoryStream(bytes);using var reader=new BinaryReader(stream);
            reader.ReadString();long offset=stream.Position;using var writer=new BinaryWriter(stream);
            stream.Position=offset;writer.Write(3);return bytes;
        }
    }
}
