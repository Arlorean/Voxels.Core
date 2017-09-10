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
        public readonly XYZ size;
        readonly Voxel[] voxels;
        readonly Color[] colors;

        public VoxelData(XYZ size, Color[] colors) {
            this.size = size;
            this.voxels = new Voxel[size.Volume];
            this.colors = colors;
        }

        public int Count {
            get { return voxels.Count(v => v.IsVisible); }
        }

        public bool IsValid(XYZ p) {
            return (p.X >= 0 && p.X < size.X)
                && (p.Y >= 0 && p.Y < size.Y)
                && (p.Z >= 0 && p.Z < size.Z);
        }

        public Voxel this[XYZ p] {
            get {
                if (IsValid(p)) {
                    return voxels[p.X * (size.Y * size.Z) + p.Y * size.Z + p.Z];
                }
                return Voxel.Empty;
            }
            set {
                if (IsValid(p)) {
                    voxels[p.X * (size.Y * size.Z) + p.Y * size.Z + p.Z] = value;
                }
                else {
                    throw new ArgumentOutOfRangeException("p", p, "point not in voxel data set.");
                }
            }
        }

        public Color[] Colors { get { return colors; } }

        public Color ColorOf(Voxel voxel) {
            return colors != null ? colors[voxel.Index] : voxel.Color;
        }
        public Color ColorOf(XYZ p) {
            return ColorOf(this[p]);
        }

        public VoxelData CreatePalette() {
            var colors = new List<Color>() { Color.Transparent };
            var voxelData = new VoxelData(this.size, new Color[256]);
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
            for (var x = 0; x < size.X; ++x) {
                for (var y = 0; y < size.Y; ++y) {
                    for (var z = 0; z < size.Z; ++z) {
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
