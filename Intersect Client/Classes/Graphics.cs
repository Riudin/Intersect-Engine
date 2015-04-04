﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SFML.Graphics;
using SFML.Window;
using Color = SFML.Graphics.Color;
using Font = SFML.Graphics.Font;
using Image = SFML.Graphics.Image;
using KeyEventArgs = SFML.Window.KeyEventArgs;
using View = SFML.Graphics.View;

namespace Intersect_Client.Classes
{
    public static class Graphics
    {
        public static RenderWindow RenderWindow;

        //Screen Values
        public static int ScreenWidth;
        public static int ScreenHeight;
        public static int DisplayMode;
        public static bool FullScreen = false;
        public static bool MustReInit;
        public static int FadeStage = 1;
        public static float FadeAmt = 255f;
        public static FrmGame MyForm;
        public static List<Keyboard.Key> KeyStates = new List<Keyboard.Key>();
        public static List<Mouse.Button> MouseState = new List<Mouse.Button>();
        public static Font GameFont;
        public static int Fps;
        private static int _fpsCount;
        private static long _fpsTimer;
        private static RenderStates _renderState = new RenderStates(BlendMode.Alpha);

        //Game Textures
        public static List<Texture> Tilesets = new List<Texture>();
        public static List<Texture> Entities = new List<Texture>();
        public static List<string> EntityNames = new List<string>();

        //DayNight Stuff
        public static bool LightsChanged = true;
        public static RenderTexture NightCacheTexture;
        public static RenderTexture CurrentNightTexture;
        public static Image NightImg;
        public static Texture PlayerLightTex;
        public static float SunIntensity;

        //Player Spotlight Values
        private const float PlayerLightIntensity = .7f;
        private const int PlayerLightSize = 150;
        private const float PlayerLightScale = .6f;

        private static long _fadeTimer;


        //Init Functions
        public static void InitGraphics()
        {
            InitSfml();
            LoadEntities();
            GameFont = new Font("Arvo-Regular.ttf");

            //Load menu bg
            if (File.Exists("data\\graphics\\bg.png")){
                //menuBG = new Texture("data\\graphics\\bg.png");
            }
        }
        private static void InitSfml()
        {
            if (DisplayMode < 0 || DisplayMode >= GetValidVideoModes().Count) { DisplayMode = 0; }
            MyForm = new FrmGame();
            if (GetValidVideoModes().Any())
            {
                MyForm.Width = (int)GetValidVideoModes()[DisplayMode].Width;
                MyForm.Height = (int)GetValidVideoModes()[DisplayMode].Height;
                MyForm.Text = @"Intersect Client";
                RenderWindow = new RenderWindow(MyForm.Handle);
                if (FullScreen)
                {
                    MyForm.TopMost = true;
                    MyForm.FormBorderStyle = FormBorderStyle.None;
                    MyForm.WindowState = FormWindowState.Maximized;
                    RenderWindow.SetView(new View(new FloatRect(0, 0, (int)GetValidVideoModes()[DisplayMode].Width, (int)GetValidVideoModes()[DisplayMode].Height)));
                }
                else
                {
                    RenderWindow.SetView(new View(new FloatRect(0, 0, MyForm.ClientSize.Width, MyForm.ClientSize.Height)));
                }
                
            }
            else
            {
                MyForm.Width = 800;
                MyForm.Height = 640;
                MyForm.Text = @"Intersect Client";
                RenderWindow = new RenderWindow(MyForm.Handle);
                RenderWindow.SetView(new View(new FloatRect(0, 0, MyForm.ClientSize.Width, MyForm.ClientSize.Height)));
            }
            if (FullScreen)
            {
                ScreenWidth = (int)GetValidVideoModes()[DisplayMode].Width;
                ScreenHeight = (int)GetValidVideoModes()[DisplayMode].Height;
            }
            else
            {
                ScreenWidth = MyForm.ClientSize.Width;
                ScreenHeight = MyForm.ClientSize.Height;
            }
            RenderWindow.KeyPressed += renderWindow_KeyPressed;
            RenderWindow.KeyReleased += renderWindow_KeyReleased;
            RenderWindow.MouseButtonPressed += renderWindow_MouseButtonPressed;
            RenderWindow.MouseButtonReleased += renderWindow_MouseButtonReleased;
            Gui.InitGwen();
        }

        //GUI Input Events
        static void renderWindow_MouseButtonReleased(object sender, MouseButtonEventArgs e)
        {
            while (MouseState.Remove(e.Button)) { }
        }
        static void renderWindow_MouseButtonPressed(object sender, MouseButtonEventArgs e)
        {
            MouseState.Add(e.Button);
        }
        static void renderWindow_KeyReleased(object sender, KeyEventArgs e)
        {
            while (KeyStates.Remove(e.Code)) { }
        }
        static void renderWindow_KeyPressed(object sender, KeyEventArgs e)
        {
            KeyStates.Add(e.Code);
            if (e.Code != Keyboard.Key.Return) return;
            if (Globals.GameState != 1) return;
            if (Gui._GameGui.HasInputFocus() == false)
            {
                Gui._GameGui.FocusChat = true;
            }
        }

        
        //Game Rendering
        public static void DrawGame()
        {
            if (MustReInit)
            {
                Gui.DestroyGwen();
                RenderWindow.Close();
                MyForm.Close();
                MyForm.Dispose();
                Gui.SetupHandlers = false;
                InitSfml();
                MustReInit = false;
            }
            if (!RenderWindow.IsOpen()) return;
            RenderWindow.DispatchEvents();
            RenderWindow.Clear(Color.Black);

            if (Globals.GameState == 1 && Globals.GameLoaded)
            {
                if (LightsChanged) { InitLighting(); }
                //Render players, names, maps, etc.
                for (var i = 0; i < 9; i++)
                {
                    if (Globals.LocalMaps[i] > -1)
                    {
                        DrawMap(i); //Lower only
                    }
                }

                for (var i = 0; i < 9; i++)
                {
                    if (Globals.LocalMaps[i] <= -1) continue;
                    for (var y = 0; y < Constants.MapHeight ; y++)
                    {
                        foreach (var t in Globals.Entities)
                        {
                            if (t == null) continue;
                            if (t.CurrentMap != Globals.LocalMaps[i]) continue;
                            if (t.CurrentY == y)
                            {
                                t.Draw(i);
                            }
                        }
                        foreach (var t in Globals.Events)
                        {
                            if (t == null) continue;
                            if (t.CurrentMap != Globals.LocalMaps[i]) continue;
                            if (t.CurrentY == y)
                            {
                                t.Draw(i);
                            }
                        }
                    }
                }

                for (var i = 0; i < 9; i++)
                {
                    if (Globals.LocalMaps[i] <= -1) continue;
                    DrawMap(i, true); //Upper Layers

                    for (var y = 0; y < Constants.MapHeight; y++)
                    {
                        foreach (var t in Globals.Entities)
                        {
                            if (t == null) continue;
                            if (t.CurrentMap != Globals.LocalMaps[i]) continue;
                            if (t.CurrentY != y) continue;
                            t.DrawName(i,false);
                            t.DrawHpBar(i);
                        }
                        foreach (var t in Globals.Events)
                        {
                            if (t == null) continue;
                            if (t.CurrentMap != Globals.LocalMaps[i]) continue;
                            if (t.CurrentY == y)
                            {
                                t.DrawName(i,true);
                            }
                        }
                    }
                }
                DrawNight();
            }
                
            Gui.DrawGui();

                
            if (FadeStage != 0)
            {
                if (_fadeTimer < Environment.TickCount)
                {
                    if (FadeStage == 1)
                    {
                        FadeAmt -= 2f;
                        if (FadeAmt <= 0)
                        {
                            FadeStage = 0;
                            FadeAmt = 0f;
                        }
                    }
                    else
                    {
                        FadeAmt += 2f;
                        if (FadeAmt >= 255)
                        {
                            FadeAmt = 255f;
                        }
                    }
                    _fadeTimer = Environment.TickCount + 10;
                }
                var myShape = new RectangleShape(new Vector2f(ScreenWidth, ScreenHeight))
                {
                    FillColor = new Color(0, 0, 0, (byte) FadeAmt)
                };
                RenderWindow.Draw(myShape);
            }

            RenderWindow.Display();
            _fpsCount++;
            if (_fpsTimer < Environment.TickCount)
            {
                Fps = _fpsCount;
                _fpsCount = 0;
                _fpsTimer = Environment.TickCount + 1000;
                RenderWindow.SetTitle("Intersect Engine - FPS: " + Fps);
            }
        }
        private static void DrawMap(int index, bool upper = false)
        {
            var mapoffsetx = CalcMapOffsetX(index);
            var mapoffsety = CalcMapOffsetY(index);

            if (Globals.LocalMaps[index] > Globals.GameMaps.Count() || Globals.LocalMaps[index] < 0) return;
            if (Globals.GameMaps[Globals.LocalMaps[index]] == null) return;
            if (Globals.GameMaps[Globals.LocalMaps[index]].MapLoaded)
            {
                Globals.GameMaps[Globals.LocalMaps[index]].Draw(mapoffsetx, mapoffsety, upper);
            }
        }

        
        //Graphic Loading
        public static void LoadTilesets(string[] tilesetnames)
        {
            foreach (var t in tilesetnames)
            {
                if (t == "")
                {
                    Tilesets.Add(null);
                }
                else
                {
                    if (!File.Exists("data/graphics/tilesets/" + t))
                    {
                        Tilesets.Add(null);
                    }
                    else
                    {
                        Tilesets.Add(new Texture("data/graphics/tilesets/" + t));                    
                    }
                }
            }
        }

        public static void LoadEntities()
        {
            var entityPaths = Directory.GetFiles("data/graphics/entities/","*.png");
            foreach (var t in entityPaths)
            {
                EntityNames.Add(t.Split('/')[t.Split('/').Count() - 1].ToLower());
                Entities.Add(new Texture(t));
            }
        }

        //Lighting
        private static void InitLighting()
        {
            //If we don't have a light texture, make a base/blank one.
            if (NightCacheTexture == null)
            {
                NightCacheTexture = new RenderTexture(Constants.MapWidth * 32 * 3, Constants.MapHeight * 32 * 3);
                CurrentNightTexture = new RenderTexture(Constants.MapWidth * 32 * 3, Constants.MapHeight * 32 * 3);
                var size = CalcLightWidth(PlayerLightSize);
                var tmpLight = new Bitmap(size, size);
                var g = System.Drawing.Graphics.FromImage(tmpLight);
                var pth = new GraphicsPath();
                pth.AddEllipse(0, 0, size - 1, size - 1);
                var pgb = new PathGradientBrush(pth)
                {
                    CenterColor =
                        System.Drawing.Color.FromArgb((int) (255*PlayerLightIntensity), (int) (255*PlayerLightIntensity),
                            (int) (255*PlayerLightIntensity), (int) (255*PlayerLightIntensity)),
                    SurroundColors = new[] {System.Drawing.Color.Black},
                    FocusScales = new PointF(PlayerLightScale, PlayerLightScale)
                };
                g.FillPath(pgb, pth);
                g.Dispose();
                PlayerLightTex = TexFromBitmap(tmpLight);
            }

            //If loading maps still, dont make the texture, no point
            for (var i = 0; i < 9; i++)
            {
                if (Globals.LocalMaps[i] <= -1 || Globals.LocalMaps[i] >= Globals.GameMaps.Count()) continue;
                if (Globals.GameMaps[Globals.LocalMaps[i]] != null)
                {
                    if (Globals.GameMaps[Globals.LocalMaps[i]].MapLoaded)
                    {

                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            NightCacheTexture.Clear(new Color(30, 30, 30, 255));

            //Render each light.
            for (var z = 0; z < 9; z++)
            {
                if (Globals.LocalMaps[z] <= -1 || Globals.LocalMaps[z] >= Globals.GameMaps.Count()) continue;
                if (Globals.GameMaps[Globals.LocalMaps[z]] == null) continue;
                if (!Globals.GameMaps[Globals.LocalMaps[z]].MapLoaded) continue;
                foreach (var t in Globals.GameMaps[Globals.LocalMaps[z]].Lights)
                {
                    double w = CalcLightWidth(t.Range);
                    var x = CalcMapOffsetX(z,true) + Constants.MapWidth * 32 + (t.TileX * 32 + t.OffsetX) - (int)w / 2 + 16;
                    var y = CalcMapOffsetY(z,true) + Constants.MapHeight* 32 + (t.TileY * 32 + t.OffsetY) - (int)w / 2 + 16;
                    AddLight(x, y, (int)w, t.Intensity, t);
                }
            }
            NightCacheTexture.Display();
            NightImg = NightCacheTexture.Texture.CopyToImage();
            LightsChanged = false;
        }
        private static int CalcLightWidth(int range)
        {
            //Formula that is ~equilivant to Unity spotlight widths, this is so future Unity lighting is possible.
            int[] xVals = { 0, 5, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160, 170, 180 };
            int[] yVals = { 1, 8, 18, 34, 50, 72, 92, 114, 135, 162, 196, 230, 268, 320, 394, 500, 658, 976, 1234, 1600 };
            int w;
            var x = 0;
            while (range >= xVals[x])
            {
                x++;
            }
            if (x > yVals.Length)
            {
                w = yVals[yVals.Length - 1];
            }
            else
            {
                w = yVals[x - 1];
                w += (int)((range - xVals[x - 1]) / ((float)xVals[x] - xVals[x - 1])) * (yVals[x] - yVals[x - 1]);
            }
            return w;
        }
        private static void DrawNight()
        {
            //TODO: Calculate which areas will not be seen on screen and don't render them each frame.
            if (Globals.GameMaps[Globals.CurrentMap].IsIndoors) { return; } //Don't worry about day or night if indoors
            var rs = new RectangleShape(new Vector2f(3 * 32 * Constants.MapWidth, 3 * 32 * Constants.MapHeight));
            CurrentNightTexture.Clear(Color.Transparent);
            RenderTexture(NightCacheTexture.Texture, 0, 0, CurrentNightTexture); //Draw our cached map lights
            
            //Draw the light around the player (if any)
            if (PlayerLightTex != null)
            {
                var tmpSprite = new Sprite(PlayerLightTex)
                {
                    Position =
                        new Vector2f(
                            (int)
                                Math.Ceiling(Globals.Entities[Globals.MyIndex].GetCenterPos(4).X - PlayerLightTex.Size.X/2 +
                                             Constants.MapWidth*32),
                            (int)
                                Math.Ceiling(Globals.Entities[Globals.MyIndex].GetCenterPos(4).Y - PlayerLightTex.Size.Y/2 +
                                             Constants.MapHeight*32))
                };
                RenderTexture(PlayerLightTex, (int)
                                Math.Ceiling(Globals.Entities[Globals.MyIndex].GetCenterPos(4).X - PlayerLightTex.Size.X / 2 +
                                             Constants.MapWidth * 32), (int)
                                Math.Ceiling(Globals.Entities[Globals.MyIndex].GetCenterPos(4).Y - PlayerLightTex.Size.Y / 2 +
                                             Constants.MapHeight * 32),CurrentNightTexture,BlendMode.Add);
            }
            rs.FillColor = new Color(255, 255, 255, (byte)(SunIntensity * 255));    //Draw a rectangle, the opacity indicates if it is day or night.
            CurrentNightTexture.Draw(rs, new RenderStates(BlendMode.Add));
            CurrentNightTexture.Display();
            RenderTexture(CurrentNightTexture.Texture,CalcMapOffsetX(0),CalcMapOffsetY(0),RenderWindow,BlendMode.Multiply);
        }
        private static void AddLight(int x1, int y1, int size, double intensity, LightObj light)
        {
            Bitmap tmpLight;
            //If not cached, create a radial gradent for the light.
            if (light.Graphic == null)
            {
                tmpLight = new Bitmap(size, size);
                var g = System.Drawing.Graphics.FromImage(tmpLight);
                var pth = new GraphicsPath();
                pth.AddEllipse(0, 0, size - 1, size - 1);
                var pgb = new PathGradientBrush(pth)
                {
                    CenterColor = System.Drawing.Color.FromArgb((int) (255*intensity), 255, 255, 255),
                    SurroundColors = new[] {System.Drawing.Color.Transparent},
                    FocusScales = new PointF(0.8f, 0.8f)
                };
                g.FillPath(pgb, pth);
                g.Dispose();
                light.Graphic = tmpLight;
            }
            else
            {
                tmpLight = light.Graphic;
            }

            var tmpSprite = new Sprite(TexFromBitmap(tmpLight)) {Position = new Vector2f(x1, y1)};
            NightCacheTexture.Draw(tmpSprite, new RenderStates(BlendMode.Add));
        }

        //Helper Functions
        private static Texture TexFromBitmap(Bitmap bmp)
        {
            var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return new Texture(ms);
        }
        public static List<VideoMode> GetValidVideoModes()
        {
            var myList = new List<VideoMode>();
            for (var i = 0; i < VideoMode.FullscreenModes.Length; i++)
            {
                if (VideoMode.FullscreenModes[i].BitsPerPixel == 32)
                {
                    myList.Add(VideoMode.FullscreenModes[i]);
                }
            }
            myList.Reverse();
            return myList;
        }
        public static int CalcMapOffsetX(int i, bool ignorePlayerOffset = false)
        {
            if (i < 3)
            {
                if (ignorePlayerOffset)
                {
                    return ((-Constants.MapWidth * 32) + ((i) * (Constants.MapWidth * 32)));
                }
                return ((-Constants.MapWidth * 32) + ((i) * (Constants.MapWidth * 32))) + (ScreenWidth / 2) - Globals.Entities[Globals.MyIndex].CurrentX * 32 - (int)Math.Ceiling(Globals.Entities[Globals.MyIndex].OffsetX);
            }
            if (i < 6)
            {
                if (ignorePlayerOffset)
                {
                    return ((-Constants.MapWidth * 32) + ((i - 3) * (Constants.MapWidth * 32)));
                }
                return ((-Constants.MapWidth * 32) + ((i - 3) * (Constants.MapWidth * 32))) + (ScreenWidth / 2) - Globals.Entities[Globals.MyIndex].CurrentX * 32 - (int)Math.Ceiling(Globals.Entities[Globals.MyIndex].OffsetX);
            }
            if (ignorePlayerOffset)
            {
                return ((-Constants.MapWidth * 32) + ((i - 6) * (Constants.MapWidth * 32)));
            }
            return ((-Constants.MapWidth * 32) + ((i - 6) * (Constants.MapWidth * 32))) + (ScreenWidth / 2) - Globals.Entities[Globals.MyIndex].CurrentX * 32 - (int)Math.Ceiling(Globals.Entities[Globals.MyIndex].OffsetX);
        }
        public static int CalcMapOffsetY(int i, bool ignorePlayerOffset = false)
        {
            if (i < 3)
            {
                if (ignorePlayerOffset)
                {
                    return -Constants.MapHeight * 32;
                }
                return -Constants.MapHeight * 32 + (ScreenHeight / 2) - Globals.Entities[Globals.MyIndex].CurrentY * 32 - (int)Math.Ceiling(Globals.Entities[Globals.MyIndex].OffsetY);
            }
            if (i < 6)
            {
                if (ignorePlayerOffset)
                {
                    return 0;
                }
                return 0 + (ScreenHeight / 2) - Globals.Entities[Globals.MyIndex].CurrentY * 32 - (int)Math.Ceiling(Globals.Entities[Globals.MyIndex].OffsetY);
            }
            if (ignorePlayerOffset)
            {
                return Constants.MapHeight * 32;
            }
            return Constants.MapHeight * 32 + (ScreenHeight / 2) - Globals.Entities[Globals.MyIndex].CurrentY * 32 - (int)Math.Ceiling(Globals.Entities[Globals.MyIndex].OffsetY);
        }


        //Rendering Functions
        public static void RenderTexture(Texture tex, int x, int y, RenderTarget renderTarget)
        {
            var destRectangle = new Rectangle(x, y, (int)tex.Size.X, (int)tex.Size.Y);
            var srcRectangle = new Rectangle(0, 0, (int)tex.Size.X, (int)tex.Size.Y);
            RenderTexture(tex,srcRectangle,destRectangle,renderTarget);
        }
        public static void RenderTexture(Texture tex, int x, int y, RenderTarget renderTarget, BlendMode blendMode)
        {
            var destRectangle = new Rectangle(x, y, (int)tex.Size.X, (int)tex.Size.Y);
            var srcRectangle = new Rectangle(0, 0, (int)tex.Size.X, (int)tex.Size.Y);
            RenderTexture(tex, srcRectangle, destRectangle, renderTarget,blendMode);
        }
        public static void RenderTexture(Texture tex, int dx, int dy,int sx,int sy, int w, int h, RenderTarget renderTarget)
        {
            var destRectangle = new Rectangle(dx, dy, w, h);
            var srcRectangle = new Rectangle(sx, sy,w, h);
            RenderTexture(tex, srcRectangle, destRectangle, renderTarget);
        }
        public static void RenderTexture(Texture tex,Rectangle srcRectangle, Rectangle targetRect,RenderTarget renderTarget, BlendMode blendMode = BlendMode.Alpha)
        {
            var vertexCache = new Vertex[4];
            var u1 = (float)srcRectangle.X / tex.Size.X;
            var v1 = (float)srcRectangle.Y / tex.Size.Y;
            var u2 = (float)srcRectangle.Right / tex.Size.X;
            var v2 = (float)srcRectangle.Bottom / tex.Size.Y;


            u1 *= tex.Size.X;
            v1 *= tex.Size.Y;
            u2 *= tex.Size.X;
            v2 *= tex.Size.Y;

            _renderState.BlendMode = blendMode;

            if (_renderState.Texture == null || _renderState.Texture != tex)
            {
                // enable the new texture
                _renderState.Texture = tex;
            }

            var right = targetRect.X + targetRect.Width;
            var bottom = targetRect.Y + targetRect.Height;

            vertexCache[0] = new Vertex(new Vector2f(targetRect.X, targetRect.Y), new Vector2f(u1, v1));
            vertexCache[1] = new Vertex(new Vector2f(right, targetRect.Y), new Vector2f(u2, v1));
            vertexCache[2] = new Vertex(new Vector2f(right, bottom), new Vector2f(u2, v2));
            vertexCache[3] = new Vertex(new Vector2f(targetRect.X, bottom), new Vector2f(u1, v2));
            renderTarget.Draw(vertexCache, 0, 4, PrimitiveType.Quads, _renderState);
            renderTarget.ResetGLStates();
        }
    }
}