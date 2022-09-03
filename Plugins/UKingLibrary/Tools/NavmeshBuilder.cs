﻿using GLFrameworkEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using MapStudio.UI;
using HKX2;
using HKX2Builders;
using OpenTK.Graphics;

namespace UKingLibrary
{
    public class NavmeshBuilder
    {
        private static IMapLoader ActiveLoader
        {
            get
            {
                return UKingEditor.ActiveUkingEditor.ActiveMapLoader;
            }
        }

        public static void Build()
        {
            foreach (var navmeshLoader in ActiveLoader.Navmesh)
            {
                Vector3 min;
                Vector3 max;

                if (ActiveLoader is FieldMapLoader)
                {
                    min = navmeshLoader.Origin;
                    min.Y = float.MinValue;
                    max = navmeshLoader.Origin + new Vector3(250);
                    max.Y = float.MaxValue;
                }
                else
                {
                    min = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                    max = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                }

                DrawingHelper.VerticesIndices<Vector3> mergedMesh = GetMergedMesh(navmeshLoader.Origin, min, max, 5f);

                var root = hkaiNavMeshBuilder.BuildRoot(UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig, mergedMesh.Vertices.Select(v => new System.Numerics.Vector3(v.X, v.Y, v.Z)).ToList(), mergedMesh.Indices);

                navmeshLoader.Replace(root);
            }
        }

        /// <summary>
        /// Gets a simple merged mesh from all the collision data in the current loader/scene.
        /// </summary>
        private static DrawingHelper.VerticesIndices<Vector3> GetMergedMesh(Vector3 navmeshOrigin, Vector3 sampleMin, Vector3 sampleMax, float terrSamplePadding)
        {
            DrawingHelper.VerticesIndices<Vector3> res = new DrawingHelper.VerticesIndices<Vector3>();
            int vtxBase = 0;
            foreach (var collisionLoader in ActiveLoader.BakedCollision)
            {
                foreach (var render in collisionLoader.ShapeRenders)
                {
                    if (!(render.Transform.Position.X / GLContext.PreviewScale > sampleMin.X &&
                        render.Transform.Position.Y / GLContext.PreviewScale > sampleMin.Y &&
                        render.Transform.Position.Z / GLContext.PreviewScale > sampleMin.Z &&
                        render.Transform.Position.X / GLContext.PreviewScale <= sampleMax.X &&
                        render.Transform.Position.Y / GLContext.PreviewScale <= sampleMax.Y &&
                        render.Transform.Position.Z / GLContext.PreviewScale <= sampleMax.Z))
                        continue;

                    Matrix4 renderTransform = render.Transform.TransformMatrix;
                    renderTransform.Transpose();

                    Matrix4 navmeshTransform = Matrix4.CreateTranslation(navmeshOrigin * GLContext.PreviewScale);
                    navmeshTransform.Transpose();

                    res.Vertices.AddRange(render.Vertices.Select(x => (navmeshTransform.Inverted() * renderTransform * new Vector4(x.Position, 1f)).Xyz / GLContext.PreviewScale));

                    foreach (int index in render.Indices)
                    {
                        res.Indices.Add(index + vtxBase);
                    }

                    vtxBase = res.Vertices.Count;
                }
            }

            if (ActiveLoader is FieldMapLoader)
            {
                foreach (var render in ((FieldMapLoader)ActiveLoader).Terrain.TerrainRenders)
                {
                    // Must be greatest to smallest index
                    SortedSet<int> removedVertices = new SortedSet<int>();
                    int[] indexMapping = Enumerable.Repeat(-1, render.Vertices.Length).ToArray();

                    for (int i = 0; i < render.Vertices.Length; i++)
                    {
                        Matrix4 renderTransform = render.Transform.TransformMatrix;
                        renderTransform.Transpose();

                        Vector4 pos = (renderTransform * new Vector4(render.Vertices[i].Position, 1f));

                        if (!(pos.X / GLContext.PreviewScale >= sampleMin.X - terrSamplePadding &&
                        pos.Y / GLContext.PreviewScale >= sampleMin.Y - terrSamplePadding &&
                        pos.Z / GLContext.PreviewScale >= sampleMin.Z - terrSamplePadding &&
                        pos.X / GLContext.PreviewScale <= sampleMax.X + terrSamplePadding &&
                        pos.Y / GLContext.PreviewScale <= sampleMax.Y + terrSamplePadding &&
                        pos.Z / GLContext.PreviewScale <= sampleMax.Z + terrSamplePadding))
                        {
                            removedVertices.Add(i);
                            continue;
                        }

                        Matrix4 navmeshTransform = Matrix4.CreateTranslation(navmeshOrigin * GLContext.PreviewScale);
                        navmeshTransform.Transpose();

                        indexMapping[i] = res.Vertices.Count;
                        res.Vertices.Add((navmeshTransform.Inverted() * pos).Xyz / GLContext.PreviewScale);
                    }

                    List<int> indices = Rendering.TerrainRender.Indices.ToList();

                    // Optimize a little bit
                    if (removedVertices.Count == render.Vertices.Length)
                        continue;

                    for (int i = 0; i < indices.Count; i += 3)
                    {
                        if (removedVertices.Contains(indices[i + 0]))
                            continue;
                        if (removedVertices.Contains(indices[i + 1]))
                            continue;
                        if (removedVertices.Contains(indices[i + 2]))
                            continue;

                        res.Indices.Add(indexMapping[indices[i + 0]]);
                        res.Indices.Add(indexMapping[indices[i + 1]]);
                        res.Indices.Add(indexMapping[indices[i + 2]]);
                    }

                    vtxBase = res.Vertices.Count;
                }
            }

            return res;
        }
    }
}