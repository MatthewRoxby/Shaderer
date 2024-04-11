using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Quic;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Shaderer.src
{
    public class Viewport
    {
        Color4 clearColour;
        int width, height;
        int framebuffer;
        int framebufferColour, framebufferDepth;

        Mesh[] meshes;

        public enum PREVIEW_MESH{
            QUAD = 0,
            CUBE = 1,
            SPHERE = 2,
            
            SUZANNE = 3
        }

        public PREVIEW_MESH previewMesh = PREVIEW_MESH.QUAD;


        public Viewport(int width, int height, Color4 clear){
            this.width = width;
            this.height = height;
            this.clearColour = clear;
            framebuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
            framebufferColour = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, framebufferColour);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, framebufferColour, 0);

            framebufferDepth = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, framebufferDepth);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent24, width, height, 0, PixelFormat.DepthComponent, PixelType.UnsignedByte, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, framebufferDepth, 0);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            meshes = new Mesh[]{
                MeshLoader.FromFile("models/quad.obj"),
                MeshLoader.FromFile("models/cube.obj"),
                MeshLoader.FromFile("models/sphere.obj"),
                MeshLoader.FromFile("models/suzanne.obj")
            };
        }


        public int GetViewportTexture(int shaderProgram){

            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
            GL.ClearColor(clearColour);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);


            Mesh selectedMesh = meshes[(int)previewMesh];
            GL.BindVertexArray(selectedMesh.VAO);

            
            if(selectedMesh.elements){
                GL.DrawElements(selectedMesh.drawType, selectedMesh.drawCount, DrawElementsType.UnsignedInt, 0);
            }
            else{
                GL.DrawArrays(selectedMesh.drawType, 0, selectedMesh.drawCount);
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            return framebufferColour;
        }

        public void Dispose(){
            GL.DeleteTexture(framebufferColour);
            GL.DeleteTexture(framebufferDepth);
            GL.DeleteFramebuffer(framebuffer);
        }


    }
}