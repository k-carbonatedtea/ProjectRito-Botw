﻿using System.Collections.Generic;
using System.Linq;
using GLFrameworkEngine;
using Toolbox.Core.ViewModels;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using HKX2;
using HKX2Builders;
using HKX2Builders.Extensions;


namespace UKingLibrary.Collision
{
    public class HavokMeshShapeRender : EditableObject
    {
        RenderMesh<HavokMeshShapeVertex> ShapeMesh;

        public HavokMeshShapeRender(NodeBase parent) : base(parent)
        {
        }

        public override void DrawModel(GLContext context, Pass pass)
        {
            if (pass == Pass.TRANSPARENT)
            {
                var shader = GlobalShaders.GetShader("HAVOK_SHAPE");
                context.CurrentShader = shader;
                shader.SetTransform(GLConstants.ModelMatrix, new GLTransform());

                GL.Enable(EnableCap.CullFace);
                GL.Enable(EnableCap.Blend);
                ShapeMesh.Draw(context);
            }
        }

        public void LoadShape(hkpBvCompressedMeshShape shape)
        {
            // Obtain mesh data
            MeshContainer mesh = shape.ToMesh();

            // Get vertices in a good format for this
            HavokMeshShapeVertex[] vertices = new HavokMeshShapeVertex[mesh.Vertices.Count];
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                vertices[i] = new HavokMeshShapeVertex()
                {
                    Position = new Vector3(mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z)
                };
            }

            // Get indices in a good format for this.
            // We're also gonna triangulate our quad data while we're at it:
            List<int> indices = new List<int>(mesh.Primitives.Count * 6); // 6 because for each quad we'll have 6 indices once triangulated.
            for (int i = 0; i < mesh.Primitives.Count; i++)
            {
                indices.Add(mesh.Primitives[i][0]);
                indices.Add(mesh.Primitives[i][1]);
                indices.Add(mesh.Primitives[i][2]);

                indices.Add(mesh.Primitives[i][2]);
                indices.Add(mesh.Primitives[i][3]);
                indices.Add(mesh.Primitives[i][0]);
            }

            // Oftentimes the mesh quads will have duplicate data to turn them into effective triangles.
            // Since we triangulated this, we might have a few corrupted triangles (effective lines) that we need to get rid of.
            for (int i = indices.Count - 1; i >= 0; i-= 3)
            {
                bool removeTri = false;
                if (indices[i] == indices[i - 1])
                    removeTri = true;
                else if (indices[i] == indices[i - 2])
                    removeTri = true;
                else if (indices[i - 1] == indices[i - 2])
                    removeTri= true;

                if (removeTri)
                    indices.RemoveRange(i - 2, 3);
            }

            // Set misc data
            var normals = DrawingHelper.CalculateNormals(vertices.Select(x => x.Position).ToList(), indices.ToList());
            for (int i = 0; i < vertices.Count(); i++)
            {
                vertices[i].Normal = normals[i];
                vertices[i].VertexColor = new Vector4(0, 0.5f, 1, 0.5f);
            }


            ShapeMesh = new RenderMesh<HavokMeshShapeVertex>(vertices, indices.ToArray(), OpenTK.Graphics.OpenGL.PrimitiveType.Triangles);
        }

        public struct HavokMeshShapeVertex
        {
            [RenderAttribute("vPosition", VertexAttribPointerType.Float, 0)]
            public Vector3 Position;

            [RenderAttribute("vNormalWorld", VertexAttribPointerType.Float, 12)]
            public Vector3 Normal;

            [RenderAttribute("vVertexColor", VertexAttribPointerType.Float, 24)]
            public Vector4 VertexColor;
        }
    }
}