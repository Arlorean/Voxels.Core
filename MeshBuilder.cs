using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Voxels {
    public enum MeshType {
        Triangles=0,
        Quads=1,
    }

    public struct MeshSettings {
        float yaw;
        float pitch;
        public float Yaw {
            get => yaw;
            set {
                yaw = Clamp180(value);
            }
        }
        public float Pitch {
            get => pitch;
            set {
                pitch = Clamp180(value);
            }
        }
        public bool FakeLighting;
        public bool FloorShadow;
        public MeshType MeshType;

        /// <summary>
        /// How to limit angles in (-180,180) range
        /// https://stackoverflow.com/a/47680296/256627
        /// </summary>
        static float Clamp180(float angle) {
            angle %= 360;
            return angle > 180 ? angle - 360 : angle;
        }
    }

    public class MeshBuilder {
        List<XYZ> vertices = new List<XYZ>();
        List<XYZ> normals = new List<XYZ>();
        List<Color> colors = new List<Color>();
        List<float> occlusion = new List<float>();
        List<int> faces = new List<int>();
        MeshSettings settings;

        public XYZ[] Vertices { get { return vertices.ToArray(); } }
        public XYZ[] Normals { get { return normals.ToArray(); } }
        public Color[] Colors { get { return colors.ToArray(); } }
        public float[] Occlusion { get { return occlusion.ToArray(); } }
        public int[] Faces { get { return faces.ToArray(); } }

        public MeshBuilder(VoxelData voxelData, MeshSettings settings) {
            this.settings = settings;
            CreateMesh(voxelData);
        }

        /// <summary>
        /// Enumerate voxels so that they can be rendered back to front without a Z Buffer.
        /// </summary>
        IEnumerable<XYZ> GetOrderedVoxels(VoxelData voxelData, List<XYZ> faces) {
            var xRange = Enumerable.Range(0, voxelData.Size.X);
            var yRange = Enumerable.Range(0, voxelData.Size.Y);
            var zRange = Enumerable.Range(0, voxelData.Size.Z);

            if (settings.Yaw >= 90) {
                xRange = xRange.Reverse();
                faces.AddRange(new[] { -XYZ.OneX, XYZ.OneY });
            }
            else
            if (settings.Yaw >= 0) {
                xRange = xRange.Reverse();
                yRange = yRange.Reverse();
                faces.AddRange(new[] { -XYZ.OneX, -XYZ.OneY });
            }
            else
            if (settings.Yaw >= -90) {
                yRange = yRange.Reverse();
                faces.AddRange(new[] { XYZ.OneX, -XYZ.OneY });
            }
            else {
                faces.AddRange(new[] { XYZ.OneX, XYZ.OneY });
            }

            if (settings.Pitch >= 90) {
                faces.AddRange(new[] { XYZ.OneZ });
            }
            else
            if (settings.Pitch >= 0) {
                faces.AddRange(new[] { -XYZ.OneZ });
            }
            else
            if (settings.Pitch >= -90) {
                faces.AddRange(new[] { XYZ.OneZ });
            }
            else {
                faces.AddRange(new[] { -XYZ.OneZ });
            }

            foreach (var y in yRange) {
                foreach (var x in xRange) {
                    foreach (var z in zRange) {
                        yield return new XYZ(x, y, z);
                    }
                }
            }
        }

        void CreateMesh(VoxelData voxelData) {
            if (settings.FloorShadow) {
                for (var x = -1; x <= voxelData.Size.X; x++) {
                    for (var y = -1; y <= voxelData.Size.Y; y++) {
                        RenderQuad(voxelData, new XYZ(x, y, -1), Color.Transparent, 1f, XYZ.OneZ); // Top
                    }
                }
            }
            var faces = new List<XYZ>();
            foreach (var i in GetOrderedVoxels(voxelData, faces)) {
                var color = voxelData.ColorOf(i);
                if (color.A > 0) {
                    var lightLevel = settings.FakeLighting ? 0.5f : 1.0f;
                    var lightDelta = settings.FakeLighting ? 0.1f : 0.0f;

                    if (faces.Contains(XYZ.OneZ)) {
                        RenderQuad(voxelData, i, color, (lightLevel + lightDelta * 5), XYZ.OneZ); // Top
                    }
                    if (faces.Contains(-XYZ.OneZ)) {
                        RenderQuad(voxelData, i, color, (lightLevel + lightDelta * 0), -XYZ.OneZ); // Bottom
                    }
                    if (faces.Contains(XYZ.OneX)) {
                        RenderQuad(voxelData, i, color, (lightLevel + lightDelta * 1), XYZ.OneX); // Right
                    }
                    if (faces.Contains(-XYZ.OneX)) {
                        RenderQuad(voxelData, i, color, (lightLevel + lightDelta * 3), -XYZ.OneX); // Left
                    }
                    if (faces.Contains(XYZ.OneY)) {
                        RenderQuad(voxelData, i, color, (lightLevel + lightDelta * 2), XYZ.OneY); // Back
                    }
                    if (faces.Contains(-XYZ.OneY)) {
                        RenderQuad(voxelData, i, color, (lightLevel + lightDelta * 4), -XYZ.OneY); // Front
                    }
                }
            }
        }

        void RenderQuad(VoxelData voxelData, XYZ p, Color color, float lightLevel, XYZ faceUp) {
            // Only render quad if face it isn't hidden by voxel above it
            if (voxelData[p + faceUp].Index == 0) {
                // Calculate adjacent voxels
                var adjacent = new XYZ[] { XYZ.Zero, XYZ.Zero, XYZ.Zero, XYZ.Zero };
                if (faceUp == -XYZ.OneX) { adjacent = new[] { XYZ.OneY, XYZ.OneZ, -XYZ.OneY, -XYZ.OneZ }; }
                if (faceUp == -XYZ.OneY) { adjacent = new[] { XYZ.OneZ, XYZ.OneX, -XYZ.OneZ, -XYZ.OneX }; }
                if (faceUp == -XYZ.OneZ) { adjacent = new[] { XYZ.OneX, XYZ.OneY, -XYZ.OneX, -XYZ.OneY }; }
                if (faceUp == XYZ.OneX) { adjacent = new[] { XYZ.OneZ, XYZ.OneY, -XYZ.OneZ, -XYZ.OneY }; }
                if (faceUp == XYZ.OneY) { adjacent = new[] { XYZ.OneX, XYZ.OneZ, -XYZ.OneX, -XYZ.OneZ }; }
                if (faceUp == XYZ.OneZ) { adjacent = new[] { XYZ.OneY, XYZ.OneX, -XYZ.OneY, -XYZ.OneX }; }

                // Calculate ambient occlusion vertex colors
                var a00 = AmbientOcclusion.CalculateAO(voxelData, p, adjacent[0], adjacent[1], faceUp);
                var a01 = AmbientOcclusion.CalculateAO(voxelData, p, adjacent[1], adjacent[2], faceUp);
                var a11 = AmbientOcclusion.CalculateAO(voxelData, p, adjacent[2], adjacent[3], faceUp);
                var a10 = AmbientOcclusion.CalculateAO(voxelData, p, adjacent[3], adjacent[0], faceUp);

                // Ignore shadow face there is no occlusion
                if (p.Z == -1 && (a00 + a01 + a11 + a10) == 12) {
                    return;
                }

                // Store vertex colors
                colors.AddRange(new[] { color*lightLevel, color*lightLevel, color*lightLevel, color*lightLevel });

                // Calculate occlusion values
                occlusion.Add(AmbientOcclusion.AOToOcclusion(a00));
                occlusion.Add(AmbientOcclusion.AOToOcclusion(a01));
                occlusion.Add(AmbientOcclusion.AOToOcclusion(a11));
                occlusion.Add(AmbientOcclusion.AOToOcclusion(a10));

                // Calculate quad vertices based on adjacent voxels
                var v0 = p + (adjacent[0] + adjacent[1] + faceUp + XYZ.One) / 2;
                var v1 = p + (adjacent[1] + adjacent[2] + faceUp + XYZ.One) / 2;
                var v2 = p + (adjacent[2] + adjacent[3] + faceUp + XYZ.One) / 2;
                var v3 = p + (adjacent[3] + adjacent[0] + faceUp + XYZ.One) / 2;
                var n = vertices.Count; 
                vertices.AddRange(new[] { v0, v1, v2, v3 });

                // Populate Normals
                normals.AddRange(new[] { faceUp, faceUp, faceUp, faceUp });

                // Add face primitives for MeshType
                var i0 = (n + 0);
                var i1 = (n + 1);
                var i2 = (n + 2);
                var i3 = (n + 3);
                switch (settings.MeshType) {
                case MeshType.Triangles:
                    // Change quad -> triangle face orientation based on ambient occlusion
                    if (a00 + a11 <= a01 + a10) {
                        faces.AddRange(new[] { i1, i2, i3, i3, i0, i1 });
                    }
                    else {
                        faces.AddRange(new[] { i0, i1, i2, i2, i3, i0 });
                    }
                    break;
                case MeshType.Quads:
                    faces.AddRange(new [] { i0, i1, i2, i3 });
                    break;
                }
            }
        }
    }
}
