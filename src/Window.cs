using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;

namespace Shaderer.src
{
    public class Window : GameWindow
    {
        public ImGuiController controller;

        public ImFontPtr mainFont;

        public string? currentSavePath = null;

        string vertCode;

        string fragCode;

        Dictionary<string, int> icons = new Dictionary<string, int>();

        const int VIEWPORT_WIDTH = 800, VIEWPORT_HEIGHT = 450;

        Viewport viewport;

        int shaderProgram = -1;

        List<string> errorList = new List<string>();

        bool wireframeMode = false;

        

        Dictionary<string, uint> syntaxTokens = new Dictionary<string, uint>();

        
        Matrix4 projection;

        Matrix4 view;

        Vector3 camPos;

        float time;

        float camTime = 0f;

        bool animateCam;

        bool showQuitWindow;

        bool showAboutWindow;

        public Window(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings{ClientSize = new Vector2i(width, height), Title = title}){
            controller = new ImGuiController(width,height);

            Unload += WindowUnload;
            RenderFrame += WindowRender;
            UpdateFrame += WindowUpdate;
            Load += WindowLoad;
            Resize += WindowResize;
            MouseWheel += WindowScroll;
            TextInput += WindowText;

            ImageResult icon = ImageResult.FromStream(File.OpenRead("icons/logo.png"), ColorComponents.RedGreenBlueAlpha);

            OpenTK.Windowing.Common.Input.Image image = new OpenTK.Windowing.Common.Input.Image(icon.Width, icon.Height, icon.Data);
            Icon = new OpenTK.Windowing.Common.Input.WindowIcon([image]);
            
            CenterWindow();
            Run();
        }

        public void LoadFile(string path){
            if(File.Exists(path)){
                JsonDocument file = JsonDocument.Parse(File.OpenRead(path));

                JsonElement vert, frag;
                if(file.RootElement.TryGetProperty("VertexCode", out vert)){
                    Debug.WriteLine("vertex code found!");
                    vertCode = vert.GetString();
                }

                if(file.RootElement.TryGetProperty("FragmentCode", out frag)){
                    Debug.WriteLine("fragment code found!");
                    fragCode = frag.GetString();
                }

                CompileShader();
            }
        }

        public void SetWireframe(bool b){
            wireframeMode = b;
            if(wireframeMode){
                GL.Disable(EnableCap.CullFace);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            }
            else{
                GL.Enable(EnableCap.CullFace);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            }
        }

        public uint Color4ToUint(Color4 col){
            return ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(col.R, col.G, col.B, col.A));
        }

        public void CompileShader(){
            errorList.Clear();

            int vertex, fragment;
            string infoLog;

            if(shaderProgram != -1){
                GL.DeleteShader(shaderProgram);
            }

            shaderProgram = GL.CreateProgram();

            vertex = GL.CreateShader(ShaderType.VertexShader);

            GL.ShaderSource(vertex, vertCode);

            GL.CompileShader(vertex);

            infoLog = GL.GetShaderInfoLog(vertex);

            if(infoLog != string.Empty) errorList.Add(infoLog);

            fragment = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragment, fragCode);
            GL.CompileShader(fragment);

            infoLog = GL.GetShaderInfoLog(fragment);
            if(infoLog != string.Empty) errorList.Add(infoLog);

            GL.AttachShader(shaderProgram, vertex);
            GL.AttachShader(shaderProgram, fragment);
            GL.LinkProgram(shaderProgram);

            infoLog = GL.GetProgramInfoLog(shaderProgram);
            if(infoLog != string.Empty) errorList.Add(infoLog);

            GL.DeleteShader(vertex);
            GL.DeleteShader(fragment);
        }

        public int LoadTexture(string path, bool flip_y, TextureMinFilter minFilter, TextureMagFilter magFilter){
            int texID = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, texID);

            StbImage.stbi_set_flip_vertically_on_load(flip_y? 1: 0);

            ImageResult image = ImageResult.FromStream(File.OpenRead(path), ColorComponents.RedGreenBlueAlpha);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);

            return texID;
        }

        private void WindowText(TextInputEventArgs args)
        {
            controller.PressChar((char)args.Unicode);
        }

        private void WindowScroll(MouseWheelEventArgs args)
        {
            controller.MouseScroll(args.Offset);
        }

        private void WindowResize(ResizeEventArgs args)
        {
            controller.WindowResized(args.Width, args.Height);
            GL.Viewport(0,0,ClientSize.X, ClientSize.Y);
        }

        private void WindowLoad()
        {
            GL.ClearColor(Color4.LimeGreen);
            mainFont = ImGui.GetIO().Fonts.AddFontFromFileTTF("fonts/Hack-Regular.ttf", 20f);
            
            controller.RecreateFontDeviceTexture();

            syntaxTokens.Add("#version", Color4ToUint(Color4.LimeGreen));
            syntaxTokens.Add("uniform", Color4ToUint(Color4.Purple));

            //data types
            syntaxTokens.Add("float", Color4ToUint(Color4.Cyan));
            syntaxTokens.Add("int", Color4ToUint(Color4.Cyan));
            syntaxTokens.Add("vec2", Color4ToUint(Color4.Cyan));
            syntaxTokens.Add("vec3", Color4ToUint(Color4.Cyan));
            syntaxTokens.Add("vec4", Color4ToUint(Color4.Cyan));
            syntaxTokens.Add("sampler2D", Color4ToUint(Color4.Cyan));
            syntaxTokens.Add("mat4", Color4ToUint(Color4.Cyan));

            //load icons
            icons.Add("logo", LoadTexture("icons/logo.png", false, TextureMinFilter.Nearest, TextureMagFilter.Nearest));
            icons.Add("float", LoadTexture("icons/Float.png", true, TextureMinFilter.Nearest, TextureMagFilter.Nearest));
            icons.Add("int", LoadTexture("icons/Int.png", true, TextureMinFilter.Nearest, TextureMagFilter.Nearest));
            icons.Add("vec2", LoadTexture("icons/Vec2.png", true, TextureMinFilter.Nearest, TextureMagFilter.Nearest));
            icons.Add("vec3", LoadTexture("icons/Vec3.png", true, TextureMinFilter.Nearest, TextureMagFilter.Nearest));
            icons.Add("vec4", LoadTexture("icons/Vec4.png", true, TextureMinFilter.Nearest, TextureMagFilter.Nearest));
            icons.Add("mat3", LoadTexture("icons/Mat3.png", true, TextureMinFilter.Nearest, TextureMagFilter.Nearest));
            icons.Add("mat4", LoadTexture("icons/Mat4.png", true, TextureMinFilter.Nearest, TextureMagFilter.Nearest));
            icons.Add("texture", LoadTexture("icons/Texture.png", true, TextureMinFilter.Nearest, TextureMagFilter.Nearest));
            icons.Add("warning", LoadTexture("icons/warning.png", true, TextureMinFilter.Nearest, TextureMagFilter.Nearest));

            LoadFile("exampleShaders/default.shader");

            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);

            SetWireframe(wireframeMode);

            viewport = new Viewport(VIEWPORT_WIDTH, VIEWPORT_HEIGHT, Color4.CornflowerBlue);

            
        }

        private void WindowUpdate(FrameEventArgs args)
        {
            float delta = (float)args.Time;
            time += delta;
            if(animateCam){
                camTime += delta;
                
            }

            camPos.X = MathF.Sin(camTime) * 4f;
            camPos.Z = MathF.Cos(camTime) * 4f;
            
            controller.Update(this, delta);
        }

        private void WindowRender(FrameEventArgs args)
        {
            float delta = (float)args.Time;

            

            GL.Clear(ClearBufferMask.ColorBufferBit);

            ImGui.PushFont(mainFont);

            uint dock = ImGui.DockSpaceOverViewport();
            

            if(ImGui.BeginMainMenuBar()){
                if(ImGui.BeginMenu("File")){
                    if(ImGui.MenuItem("New Shader")){
                        vertCode = "";
                        fragCode = "";
                    }

                    if(ImGui.MenuItem("Open Shader...")){
                        //open new shader
                    }

                    if(ImGui.MenuItem("Save")){
                        //save to file if a path already exists
                    }

                    if(ImGui.MenuItem("Save As...")){
                        //save to a new path
                    }

                    if(ImGui.MenuItem("Export Shader...")){
                        //Export to .vert and .frag
                    }

                    if(ImGui.MenuItem("Exit Shaderer...")){
                        showQuitWindow = true;
                        
                    }

                    ImGui.EndMenu();
                }

                if(ImGui.BeginMenu("Edit")){
                    

                    ImGui.EndMenu();
                }

                if(ImGui.BeginMenu("Help")){
                    if(ImGui.MenuItem("About Shaderer...")){
                        //spawn about window
                        showAboutWindow = true;
                    }
                }
            }
            

            if(ImGui.Begin("Vertex Shader")){               
                var textBegin = ImGui.GetCursorScreenPos();

                //ImGui.PushStyleColor(ImGuiCol.Text, Color4ToUint(Color4.White));
                ImGui.InputTextMultiline("##vshader code", ref vertCode, 10000, ImGui.GetWindowSize() * new System.Numerics.Vector2(1.0f, 0.9f), ImGuiInputTextFlags.AllowTabInput);
                //ImGui.PopStyleColor();
            }

            if(ImGui.Begin("Fragment Shader")){
                ImGui.InputTextMultiline("##fshader code", ref fragCode, 10000, ImGui.GetWindowSize() * new System.Numerics.Vector2(1.0f, 0.9f), ImGuiInputTextFlags.AllowTabInput);
            }

            if(ImGui.Begin("Uniforms")){
                
            }

            if(ImGui.Begin("Preview")){
                if(ImGui.Button("Compile")){
                    //compile shaders
                    CompileShader();
                }

                if(ImGui.BeginCombo("Mesh", viewport.previewMesh.ToString())){
                    foreach(int i in Enum.GetValues(typeof(Viewport.PREVIEW_MESH))){
                        if(ImGui.Selectable(Enum.GetNames(typeof(Viewport.PREVIEW_MESH))[i])){
                            viewport.previewMesh = (Viewport.PREVIEW_MESH)i;
                        }
                    }
                    ImGui.EndCombo();
                }

                if(ImGui.Button("Animate cam: " + (animateCam? "ON" : "OFF"))){
                    animateCam = !animateCam;
                }

                if(ImGui.Button("Reset cam")){
                    camTime = 0f;
                    camPos.Y = 0f;
                }

                if(ImGui.Button("Wireframe: " + (wireframeMode? "ON" : "OFF"))){
                    SetWireframe(!wireframeMode);
                }

                

                if(errorList.Count > 0){
                    ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1,0,0,1));
                    ImGui.TextWrapped("COMPILATION ERRORS DETECTED");

                    foreach(string error in errorList){
                        ImGui.TextWrapped(error);
                    }

                    ImGui.PopStyleColor();
                }
                else{
                    ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0,1,0,1));
                    ImGui.TextWrapped("COMPILATION SUCCESSFUL");
                    ImGui.PopStyleColor();

                    float hFac = ImGui.GetWindowWidth() / VIEWPORT_WIDTH;
                    GL.Viewport(0,0,VIEWPORT_WIDTH, VIEWPORT_HEIGHT);
                    GL.UseProgram(shaderProgram);

                    //apply uniforms
                    projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45f), ((float)VIEWPORT_WIDTH / (float)VIEWPORT_HEIGHT), 0.1f, 100f);
                    view = Matrix4.LookAt(camPos, Vector3.Zero, Vector3.UnitY);

                    GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "projection"), false, ref projection);
                    GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "view"), false, ref view);
                    GL.Uniform1(GL.GetUniformLocation(shaderProgram, "time"), time);
                    
                    var imageBounds1 = ImGui.GetCursorScreenPos();
                    
                    ImGui.Image(viewport.GetViewportTexture(shaderProgram), new System.Numerics.Vector2(VIEWPORT_WIDTH * hFac, VIEWPORT_HEIGHT * hFac), new System.Numerics.Vector2(0.0f, 1.0f),new System.Numerics.Vector2(1.0f, 0.0f));

                    if(ImGui.IsMouseDragging(ImGuiMouseButton.Left)){
                        var mPos = ImGui.GetMousePos();

                        if(mPos.X > imageBounds1.X && mPos.X < imageBounds1.X + (VIEWPORT_WIDTH * hFac) && mPos.Y > imageBounds1.Y && mPos.Y < imageBounds1.Y + (VIEWPORT_HEIGHT * hFac)){
                            //move the camera
                            camTime -= MouseState.Delta.X * 0.01f;
                            camPos.Y = MathHelper.Clamp(camPos.Y + MouseState.Delta.Y * 0.1f, -2.0f, 2.0f);
                        }
                    }


                    GL.Viewport(0,0,ClientSize.X, ClientSize.Y);
                }
            }

            if(showAboutWindow && ImGui.Begin("About Shaderer", ref showAboutWindow, ImGuiWindowFlags.NoDocking)){
                ImGui.SetCursorPos(new System.Numerics.Vector2(ImGui.GetWindowWidth() / 2f - 50, ImGui.GetCursorPosY()));
                ImGui.Image(icons["logo"], new System.Numerics.Vector2(100,100));
                ImGui.NewLine();
                ImGui.TextWrapped("Shaderer is a real-time GLSL editor I made in a few weeks. It uses OpenTK and DearIMGUI as backend.");
                ImGui.TextWrapped("The project is open-source so feel free to inspect the code or add to it yourself!");
                if(ImGui.Button("View the project on GitHub!")){
                    Process p = new Process();
                    p.StartInfo.FileName = "https://github.com/MatthewRoxby/Shaderer";
                    p.StartInfo.UseShellExecute = true;
                    p.Start();
                }
            }

            if(showQuitWindow) ImGui.OpenPopup("Quit Confirmation");

            if(ImGui.BeginPopupModal("Quit Confirmation", ref showQuitWindow, ImGuiWindowFlags.AlwaysAutoResize)){
                ImGui.Text("Are you sure you want to quit?");
                if(ImGui.Button("Yes")){
                    Close();
                }
                
                if(ImGui.Button("No")){
                    showQuitWindow = false;
                }
            }

            

            ImGui.PopFont();

            controller.Render();

            SwapBuffers();
        }

        private void WindowUnload()
        {
            viewport.Dispose();
            MeshLoader.CleanUp();
            GL.DeleteProgram(shaderProgram);
            controller.Dispose();
        }
    }
}