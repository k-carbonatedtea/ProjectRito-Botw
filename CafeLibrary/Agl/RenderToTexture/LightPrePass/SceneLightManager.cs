﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Diagnostics;
using GLFrameworkEngine;

namespace AGraphicsLibrary
{
    class SceneLightManager
    {
        public static Vector3 PointPosition;

        public static void DrawSceneLights(Camera camera, GLTexture gbuffer, GLTexture linearDepth)
        {
            return;

            var shader = GlobalShaders.GetShader("LIGHTPREPASS");
            shader.Enable();

            GLH.ActiveTexture(TextureUnit.Texture0 + 1);
            gbuffer.Bind();
            shader.SetInt("normalsTexture", 1);

            GLH.ActiveTexture(TextureUnit.Texture0 + 2);
            linearDepth.Bind();
            shader.SetInt("depthTexture", 2);

            int programID = shader.program;

            var projectionMatrixInverse = camera.ProjectionMatrix.Inverted();
            var viewMatrixInverse = camera.ViewMatrix.Inverted();
            var mtxProjView = camera.ProjectionMatrix * camera.ViewMatrix;

            shader.SetMatrix4x4("mtxProjInv", ref projectionMatrixInverse);
            shader.SetMatrix4x4("mtxViewInv", ref viewMatrixInverse);
            shader.SetVector3("cameraPosition", camera.TargetPosition);


            float projectionA = camera.ZFar / (camera.ZFar - camera.ZNear);
            float projectionB = (-camera.ZFar * camera.ZNear) / (camera.ZFar - camera.ZNear);
            shader.SetFloat("projectionA", projectionA);
            shader.SetFloat("projectionB", projectionB);
            shader.SetFloat("z_range", camera.ZFar - camera.ZNear);
            shader.SetFloat("fov_x", camera.Fov);
            shader.SetFloat("fov_y", camera.Fov);

            PointLight[] pointLights = new PointLight[32];
            for (int i = 0; i < 32; i++)
            {
                pointLights[i] = new PointLight();
                if (i == 0)
                {
                    pointLights[i].Position = PointPosition;
                    pointLights[i].Color = new Vector4(1, 0, 0, 1);
                }

                GLH.Uniform4(GLH.GetUniformLocation(programID, $"pointLights[{i}].uColor"), pointLights[i].Color);
                GLH.Uniform3(GLH.GetUniformLocation(programID, $"pointLights[{i}].uPosition"), pointLights[i].Position);
            }

            GLH.ClearColor(0, 0, 0, 0);
            GLH.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            ScreenQuadRender.Draw();
        }

        class PointLight
        {
            public Vector3 Position { get; set; }
            public Vector4 Color { get; set; }
        }
    }
}
