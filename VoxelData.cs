using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Voxels {
    /// <summary>
    /// Represents a set of voxels in a fixed size grid.
    /// NOTE: XY is the horizontal plane and Z is the vertical axis.
    /// </summary>
    public class VoxelData : IEnumerable<XYZ> {
        Voxel[] voxels;

        public XYZ Size { get; }
        public Color[] Colors { get; set; }

        public VoxelData(XYZ size, Color[] colors=null) {
            this.voxels = new Voxel[size.Volume];
            this.Size = size;
            this.Colors = colors;
        }

        public int Count {
            get { return voxels.Count(v => v.IsVisible); }
        }

        public bool IsValid(XYZ p) {
            return (p.X >= 0 && p.X < Size.X)
                && (p.Y >= 0 && p.Y < Size.Y)
                && (p.Z >= 0 && p.Z < Size.Z);
        }

        public Voxel this[XYZ p] {
            get {
                if (IsValid(p)) {
                    return voxels[p.X * (Size.Y * Size.Z) + p.Y * Size.Z + p.Z];
                }
                return Voxel.Empty;
            }
            set {
                if (IsValid(p)) {
                    voxels[p.X * (Size.Y * Size.Z) + p.Y * Size.Z + p.Z] = value;
                }
                else {
                    throw new ArgumentOutOfRangeException("p", p, "point not in voxel data set.");
                }
            }
        }

        public void Add(VoxelData voxelData) {
            foreach (var p in voxelData) {
                this[p] = voxelData[p];
            }
        }

        public Color ColorOf(Voxel voxel) {
            return Colors != null ? Colors[voxel.Index] : voxel.Color;
        }
        public Color ColorOf(XYZ p) {
            return ColorOf(this[p]);
        }

        public VoxelData CreatePalette() {
            var colors = new List<Color>() { Color.Transparent };
            var voxelData = new VoxelData(this.Size, new Color[256]);
            foreach (var v in this) {
                var c = ColorOf(v);
                var i = colors.IndexOf(c);
                if (i == -1) {
                    i = colors.Count;
                    colors.Add(c);
                }
                voxelData[v] = new Voxel((uint)i);
            }
            for (var i = 0; i < 256; i++) {
                voxelData.Colors[i] = i < colors.Count ? colors[i] : MagicaVoxel.GetDefaultColor(i);
            }
            return voxelData;
        }

        public IEnumerator<XYZ> GetEnumerator() {
            for (var x = 0; x < Size.X; ++x) {
                for (var y = 0; y < Size.Y; ++y) {
                    for (var z = 0; z < Size.Z; ++z) {
                        yield return new XYZ(x, y, z);
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
