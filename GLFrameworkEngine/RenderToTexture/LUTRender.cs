﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class LUTRender
    {
        static Dictionary<string, int> lutCache = new Dictionary<string, int>();

        static int ID = -1;

        public static int CreateTextureRender(int textureID, int width, int height)
        {
            if (lutCache.ContainsKey(textureID.ToString()))
                return lutCache[textureID.ToString()];

            var shader = GlobalShaders.GetShader("LUT_DISPLAY");

            Framebuffer frameBuffer = new Framebuffer(FramebufferTarget.Framebuffer, width, height, PixelInternalFormat.Rgba16f, 1);
            frameBuffer.Bind();

            GLH.Disable(EnableCap.Blend);

            shader.Enable();

            GLH.ActiveTexture(TextureUnit.Texture1);
            GLH.BindTexture(TextureTarget.Texture3D, textureID);
            shader.SetInt("dynamic_texture_array", 1);

            GLH.ClearColor(0, 0, 0, 0);
            GLH.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GLH.Viewport(0, 0, width, height);

            //Draw the texture onto the framebuffer
            ScreenQuadRender.Draw();

            //Disable shader and textures
            GLH.UseProgram(0);
            GLH.BindTexture(TextureTarget.Texture3D, 0);

            var image = (GLTexture2D)frameBuffer.Attachments[0];
            ID = image.ID;

            lutCache.Add(textureID.ToString(), ID);
            return ID;
        }
    }
}
