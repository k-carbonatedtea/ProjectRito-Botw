﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace GLFrameworkEngine
{
    /// <summary>
    /// Represents a quad renderer for drawning frame buffers.
    /// </summary>
    public class ScreenQuadRender
    {
        static VertexBufferObject vao;

        static int Length;

        public static void Init()
        {
            if (Length == 0)
            {
                int buffer = GLH.GenBuffer();
                vao = new VertexBufferObject(buffer);
                vao.AddAttribute(0, 2, VertexAttribPointerType.Float, false, 16, 0);
                vao.AddAttribute(1, 2, VertexAttribPointerType.Float, false, 16, 8);
                vao.Initialize();

                Vector2[] positions = new Vector2[4]
                {
                    new Vector2(-1.0f, 1.0f),
                    new Vector2(-1.0f, -1.0f),
                    new Vector2(1.0f, 1.0f),
                    new Vector2(1.0f, -1.0f),
                };

                Vector2[] texCoords = new Vector2[4]
                {
                    new Vector2(0.0f, 1.0f),
                    new Vector2(0.0f, 0.0f),
                    new Vector2(1.0f, 1.0f),
                    new Vector2(1.0f, 0.0f),
                };

                List<float> list = new List<float>();
                for (int i = 0; i < 4; i++)
                {
                    list.Add(positions[i].X);
                    list.Add(positions[i].Y);
                    list.Add(texCoords[i].X);
                    list.Add(texCoords[i].Y);
                }

                Length = 4;

                float[] data = list.ToArray();

                GLH.BindBuffer(BufferTarget.ArrayBuffer, buffer);
                GLH.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * data.Length, data, BufferUsageHint.StaticDraw);
                GLH.BindBuffer(BufferTarget.ArrayBuffer, 0);
            }
        }

        public static void Draw(ShaderProgram shader, int textureID)
        {
            Init();

            GLH.MatrixMode(MatrixMode.Modelview);
            GLH.LoadIdentity();

            GLH.ActiveTexture(TextureUnit.Texture1);
            GLH.BindTexture(TextureTarget.Texture2D, textureID);
            shader.SetInt("screenTexture", 1);

            GLH.Enable(EnableCap.CullFace);
            GLH.Enable(EnableCap.DepthTest);
            GLH.CullFace(CullFaceMode.Back);

            vao.Enable(shader);
            vao.Use();
            GLH.DrawArrays(PrimitiveType.TriangleStrip, 0, Length);
        }

        public static void Draw()
        {
            Init();

            GLH.MatrixMode(MatrixMode.Modelview);
            GLH.LoadIdentity();

            vao.Enable(null);
            vao.Use();
            GLH.DrawArrays(PrimitiveType.TriangleStrip, 0, Length);
        }
    }
}
