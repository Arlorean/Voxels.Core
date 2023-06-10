using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voxels {
    public class VoxelDataBytes : VoxelData<byte> {
        public Color[] Colors { get; set;  }

        public VoxelDataBytes(XYZ size, Color[] colors) : base(size) {
            this.Colors = colors;
        }

        public sealed override Voxel this[XYZ p] {
            get => new Voxel(Get(p));
            set => Set(p, (byte)value.Index);
        }

        protected sealed override Color ColorOf(Voxel voxel) {
            return Colors[voxel.Index];
        }
    }
}
