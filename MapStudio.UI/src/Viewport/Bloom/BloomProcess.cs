﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using GLFrameworkEngine;

namespace MapStudio.UI
{
    public class BloomProcess
    {
        public static void Draw(GLTexture brightnessTexture, Framebuffer brightnessBuffer,
            GLContext glControl, int Width, int Height)
        {
            brightnessBuffer.Bind();
            GLH.Viewport(0, 0, glControl.Width, glControl.Height);

            //Clear out the buffer
            GLH.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GLH.BindTexture(TextureTarget.Texture2D, 0);

            var shader = GlobalShaders.GetShader("BLUR");
            glControl.CurrentShader = shader;

            int amount = 8;
            shader.SetVector2("iResolution ", new Vector2(glControl.Width, glControl.Height));

            for (int i = 0; i < amount; i++)
            {
                brightnessBuffer.Bind();
                GLH.Viewport(0, 0, glControl.Width, glControl.Height);

                var radius = (amount - i - 1) * 1;
                shader.SetVector2("direction", i % 2 == 0 ? new Vector2(radius, 0) : new Vector2(0, radius));

                if (i == 0)
                    DrawBlur(glControl, brightnessTexture);
                else
                    DrawBlur(glControl, (GLTexture)brightnessBuffer.Attachments[0]);

                GLH.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            }
            GLH.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        static void DrawBlur(GLContext control, GLTexture texture)
        {
            GLH.ActiveTexture(TextureUnit.Texture0);
            texture.Bind();
            control.CurrentShader.SetInt("image", 0);

            GLFrameworkEngine.ScreenQuadRender.Draw();

            GLH.BindTexture(TextureTarget.Texture2D, 0);
        }
    }
}
