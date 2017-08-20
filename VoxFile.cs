using System.IO;

namespace Voxels {
    /// <summary>
    /// Support for loading either MagicVoxel or Voxlap .VOX files.
    /// </summary>
    public class VoxFile {
        public static VoxelData Read(Stream stream) {
            var voxelData = MagicaVoxel.Read(stream);
            if (voxelData == null) {
                stream.Seek(0, SeekOrigin.Begin);
                voxelData = Voxlap.Read(stream);
            }
            return voxelData;
        }
    }
}
