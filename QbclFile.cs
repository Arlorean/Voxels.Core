using System;
using System.IO;
using System.Text;

namespace Voxels {
    /// <summary>
    /// Support for reading Qubicle Project File (thumbnail only for now).
    /// </summary>
    public class QbclFile {
        const int QBCL = ('Q') + ('B' << 8) + ('C' << 16) + ('L' << 24);

        public class Thumbnail {
            public int Width;
            public int Height;
            public byte[] Bytes;
        }

        static Thumbnail Read(BinaryReader reader) {
            var magic = reader.ReadUInt32();

            if (magic != QBCL) {
                return null;
            }

            var version = reader.ReadInt32();
            var flags = reader.ReadInt32();
            var thumbWidth = reader.ReadInt32();
            var thumbHeight = reader.ReadInt32();
            var thumbBytes = reader.ReadBytes(thumbWidth * thumbHeight * 4);

            return new Thumbnail() { Width = thumbWidth, Height = thumbHeight, Bytes = thumbBytes, };
#if false
            var title = ReadString(reader);
            var description = ReadString(reader);
            var keywords = ReadString(reader);
            var creator = ReadString(reader);
            var company = ReadString(reader);
            var website = ReadString(reader);
            var copyright = ReadString(reader);
            var guid = new Guid(reader.ReadBytes(16));

            // No idea what these are
            var a = reader.ReadInt32(); // 01
            var b = reader.ReadInt32(); // 01

            var modelType = ReadString(reader); // "Model"
            var f0 = reader.ReadBytes(3); // 01 01 00

            var x = reader.ReadInt32();
            var y = reader.ReadInt32();
            var z = reader.ReadInt32();

            var r = reader.ReadBytes(6*4); // ?
            var n = reader.ReadInt32(); // Number of nodes
            var u = reader.ReadInt32();
            var v = reader.ReadInt32();

            // Type
            var node1 = ReadString(reader); // "Matrix"
            var f2 = reader.ReadBytes(3); // 01 01 00

            // ?
            var mx = reader.ReadInt32(); // 32
            var my = reader.ReadInt32(); // 32
            var mz = reader.ReadInt32(); // 32

            var matrix = reader.ReadBytes(115); // ?

            // Name
            var boxRed = ReadString(reader); // "BoxRed"
            var f3 = reader.ReadBytes(3); // 01 01 00

            // Size
            var sx = reader.ReadInt32();
            var sy = reader.ReadInt32();
            var sz = reader.ReadInt32();
            // Position?
            var ox = reader.ReadInt32();
            var oy = reader.ReadInt32();
            var oz = reader.ReadInt32();
            // Pivot?
            var px = reader.ReadSingle();
            var py = reader.ReadSingle();
            var pz = reader.ReadSingle();
            // Flags?
            var w0 = reader.ReadInt32();
            var w1 = reader.ReadInt32();
            // Colors?
            var redFaces = reader.ReadBytes(26); // ""

            var boxGreen = ReadString(reader); // "BoxGreen"

            var voxelData = new VoxelData(XYZ.One, null);
            return voxelData;
#endif
        }

        static string ReadString(BinaryReader reader) {
            var length = reader.ReadInt32();
            var bytes = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }

        public static Thumbnail Read(Stream stream) {
            return Read(new BinaryReader(stream));
        }
    }
}
