using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Voxels {
    public enum MeshType {
        Triangles=0,
        Quads=1,
    }

    public struct MeshSettings {
        public bool BackFaces;
        public bool FrontFaces;
        public bool FakeLighting;
        public bool FloorShadow;
        public MeshType MeshType;
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

        void CreateMesh(VoxelData voxelData) {
            if (settings.FloorShadow) {
                for (var x = -1; x <= voxelData.size.X; x++) {
                    for (var y = -1; y <= voxelData.size.Y; y++) {
                        RenderQuad(voxelData, new XYZ(x, y, -1), Color.Transparent, 1f, XYZ.OneZ); // Top
                    }
                }
            }
            if (settings.BackFaces || settings.FrontFaces) {
                for (var y = voxelData.size.Y - 1; y >= 0; --y) {
                    for (var x = voxelData.size.X - 1; x >= 0; --x) {
                        for (var z = 0; z < voxelData.size.Z; ++z) {
                            var i = new XYZ(x, y, z);

                            var color = voxelData.ColorOf(i);
                            if (color.A > 0) {
                                var lightLevel = settings.FakeLighting ? 0.5f : 1.0f;
                                var lightDelta = settings.FakeLighting ? 0.1f : 0.0f;

                                if (settings.BackFaces) {
                                    RenderQuad(voxelData, i, color, (lightLevel + lightDelta * 0), -XYZ.OneZ); // Bottom
                                    RenderQuad(voxelData, i, color, (lightLevel + lightDelta * 1), XYZ.OneX); // Right
                                    RenderQuad(voxelData, i, color, (lightLevel + lightDelta * 2), XYZ.OneY); // Back
                                }
                                if (settings.FrontFaces) {
                                    RenderQuad(voxelData, i, color, (lightLevel + lightDelta * 3), -XYZ.OneX); // Left
                                    RenderQuad(voxelData, i, color, (lightLevel + lightDelta * 4), -XYZ.OneY); // Front
                                    RenderQuad(voxelData, i, color, (lightLevel + lightDelta * 5), XYZ.OneZ); // Top
                                }
                            }
                        }
                    }
                }
            }
        }

        void RenderQuad(VoxelData voxelData, XYZ p, Color color, float lightLevel, XYZ faceUp) {
            // Only render quad if face it isn't hidden by voxel above it
            if (voxelData[p + faceUp].colorIndex == 0) {
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
                colors.AddRange(new[] { color, color, color, color });

                // Calculate occlusion values
                occlusion.Add(AmbientOcclusion.AOToOcclusion(a00)*lightLevel);
                occlusion.Add(AmbientOcclusion.AOToOcclusion(a01)*lightLevel);
                occlusion.Add(AmbientOcclusion.AOToOcclusion(a11)*lightLevel);
                occlusion.Add(AmbientOcclusion.AOToOcclusion(a10)*lightLevel);

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
