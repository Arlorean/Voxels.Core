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
        Dictionary<XYZ,Chunk> chunks = new Dictionary<XYZ, Chunk>();

        public class Chunk {
            byte[] voxels = new byte[Size*Size*Size];

            /// <summary>
            /// Count of visible voxels in chunk.
            /// </summary>
            public int Count { get; private set; }

            public const int Size = 32;

            int IndexOf(XYZ p) {
                return p.X * (Size * Size) + p.Y * Size + p.Z;
            }

            public byte this[XYZ p] {
                get {
                    return voxels[IndexOf(p)];
                }
                set {
                    var i = IndexOf(p);
                    var oldValue = voxels[i];
                    if (oldValue != value) {
                        voxels[i] = value;

                        if (oldValue == 0) {
                            Count++;
                        }
                        else {
                            Count--;
                        }
                    }
                }
            }
        }

        public XYZ Size { get; }
        public Color[] Colors { get; set; }

        public VoxelData(XYZ size, Color[] colors=null) {
            this.Size = size;
            this.Colors = colors;
        }

        public int Count {
            get { 
                var count = 0;
                foreach (var chunk in chunks.Values) {
                    count += chunk.Count;
                }
                return count;
            }
        }

        public bool IsValid(XYZ p) {
            return (p.X >= 0 && p.X < Size.X)
                && (p.Y >= 0 && p.Y < Size.Y)
                && (p.Z >= 0 && p.Z < Size.Z);
        }

        public Voxel this[XYZ p] {
            get {
                if (IsValid(p)) {
                    var c = p / Chunk.Size;
                    if (chunks.TryGetValue(c, out var chunk)) {
                        return new Voxel(chunk[p % Chunk.Size]);
                    }
                }
                return Voxel.Empty;
            }
            set {
                if (IsValid(p)) {
                    var c = p / Chunk.Size;
                    if (!chunks.TryGetValue(c, out var chunk)) {
                        // Chunk doesn't currently exist - add one unless setting to invisible
                        if (value.IsVisible) {
                            chunks.Add(c, chunk = new Chunk());
                        }
                    }

                    if (chunk != null) {
                        chunk[p % Chunk.Size] = (byte)value.Index;

                        // Chunk now entirely invisible - remove it
                        if (chunk.Count == 0) {
                            chunks.Remove(c);
                        }
                    }
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

        Color ColorOf(Voxel voxel) {
            return Colors != null ? Colors[voxel.Index] : voxel.Color;
        }

        public Color ColorOf(XYZ p) {
            return ColorOf(this[p]);
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
