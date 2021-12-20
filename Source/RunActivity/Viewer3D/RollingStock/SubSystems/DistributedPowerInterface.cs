// COPYRIGHT 2014 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team. 

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Formats.Msts;
using Orts.Simulation.RollingStocks;
using Orts.Viewer3D.Popups;
using Orts.Viewer3D.RollingStock.SubSystems.ETCS;
using ORTS.Common;
using ORTS.Scripting.Api.ETCS;
using System;
using System.Collections.Generic;
using System.Linq;
using static Orts.Viewer3D.RollingStock.SubSystems.DistributedPowerInterface;

namespace Orts.Viewer3D.RollingStock.SubSystems
{
    public class DistributedPowerInterface
    {
        public readonly MSTSLocomotive Locomotive;
        public readonly Viewer Viewer;
        public IList<DPIWindow> Windows = new List<DPIWindow>();
        float PrevScale = 1;
        public ETCSStatus ETCSStatus { get; private set; }

        bool Active;
        public float Scale { get; private set; }
        public float MipMapScale { get; private set; }
        readonly int Height = 480;
        readonly int Width = 640;

        public readonly DPDefaultWindow DPDefaultWindow;

        public readonly DriverMachineInterfaceShader Shader;

        // Color RGB values are from ETCS specification
        public static readonly Color ColorGrey = new Color(195, 195, 195);
        public static readonly Color ColorMediumGrey = new Color(150, 150, 150);
        public static readonly Color ColorDarkGrey = new Color(85, 85, 85);
        public static readonly Color ColorYellow = new Color(223, 223, 0);
        public static readonly Color ColorOrange = new Color(234, 145, 0);
        public static readonly Color ColorRed = new Color(191, 0, 2);
        public static readonly Color ColorBackground = new Color(8, 8, 8); // almost black
        public static readonly Color ColorPASPlight = new Color(41, 74, 107);
        public static readonly Color ColorPASPdark = new Color(33, 49, 74);
        public static readonly Color ColorShadow = new Color(8, 24, 57);
        public static readonly Color ColorWhite = new Color(255, 255, 255);

        // Some DPIs use black for the background and white for borders, instead of blue scale
        public readonly bool BlackWhiteTheme = false;

        public Texture2D ColorTexture { get; private set; }

        public bool Blinker2Hz { get; private set; }
        public bool Blinker4Hz { get; private set; }
        float BlinkerTime;

        public float CurrentTime => (float)Viewer.Simulator.ClockTime;

        /// <summary>
        /// True if the screen is sensitive
        /// </summary>
        public bool IsTouchScreen = true;
        /// <summary>
        /// Controls the layout of the DMI screen depending.
        /// Must be true if there are physical buttons to control the DMI, even if it is a touch screen.
        /// If false, the screen must be tactile.
        /// </summary>
        public bool IsSoftLayout;
        public DPIWindow ActiveWindow;
        DPIButton ActiveButton;
        public DistributedPowerInterface(float height, float width, MSTSLocomotive locomotive, Viewer viewer, CabViewControl control)
        {
            Viewer = viewer;
            Locomotive = locomotive;
            Scale = Math.Min(width / Width, height / Height);
            if (Scale < 0.5) MipMapScale = 2;
            else MipMapScale = 1;

            Shader = new DriverMachineInterfaceShader(Viewer.GraphicsDevice);
            DPDefaultWindow = new DPDefaultWindow(this, control);
            DPDefaultWindow.Visible = true;

            AddToLayout(DPDefaultWindow, Point.Zero);
            ActiveWindow = DPDefaultWindow;
        }
        public void ShowSubwindow(DPISubwindow window)
        {
            AddToLayout(window, new Point(window.FullScreen ? 0 : 334, 15));
        }
        public void AddToLayout(DPIWindow window, Point position)
        {
            window.Position = position;
            window.Parent = ActiveWindow;
            ActiveWindow = window;
            Windows.Add(window);
        }
        public Texture2D LoadTexture(string name)
        {
            string path;
            if (MipMapScale == 2)
                path = System.IO.Path.Combine(Viewer.ContentPath, "ETCS", "mipmap-2", name);
            else
                path = System.IO.Path.Combine(Viewer.ContentPath, "ETCS", name);
            return SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, path);
        }
        public void PrepareFrame(float elapsedSeconds)
        {
            ETCSStatus currentStatus = Locomotive.TrainControlSystem.ETCSStatus;
            ETCSStatus = currentStatus;
            Active = currentStatus != null && currentStatus.DMIActive;
            if (!Active) return;

            BlinkerTime += elapsedSeconds;
            BlinkerTime -= (int)BlinkerTime;
            Blinker2Hz = BlinkerTime < 0.5;
            Blinker4Hz = BlinkerTime < 0.25 || (BlinkerTime > 0.5 && BlinkerTime < 0.75);

            foreach (var area in Windows)
            {
                area.PrepareFrame(currentStatus);
            }
        }
        public void SizeTo(float width, float height)
        {
            Scale = Math.Min(width / Width, height / Height);

            if (Math.Abs(1f - PrevScale / Scale) > 0.1f)
            {
                PrevScale = Scale;
                if (Scale < 0.5) MipMapScale = 2;
                else MipMapScale = 1;
                foreach (var area in Windows)
                {
                    area.ScaleChanged();
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Point position)
        {
            if (ColorTexture == null)
            {
                ColorTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                ColorTexture.SetData(new[] { Color.White });
            }
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearWrap, null, null, null); // TODO: Handle brightness via DMI shader
            if (!Active) return;
            foreach (var area in Windows)
            {
                area.Draw(spriteBatch, new Point(position.X + (int)(area.Position.X * Scale), position.Y + (int)(area.Position.Y * Scale)));
            }
        }

        public void HandleMouseInput(bool pressed, int x, int y)
        {
            DPIButton pressedButton = null;
            if (ActiveButton != null)
            {
                if (!ActiveButton.Enabled)
                {
                    ActiveButton.Pressed = false;
                    ActiveButton = null;
                }
                else if (ActiveButton.SensitiveArea(ActiveWindow.Position).Contains(x, y))
                {
                    if (ActiveButton.UpType)
                    {
                        if (ActiveButton.DelayType && ActiveButton.FirstPressed + 2 > CurrentTime)
                        {
                            ActiveButton.Pressed = ((int)((CurrentTime - ActiveButton.FirstPressed) * 4)) % 2 == 0;
                        }
                        else
                        {
                            ActiveButton.Pressed = true;
                            if (!pressed)
                            {
                                pressedButton = ActiveButton;
                            }
                        }
                    }
                    else
                    {
                        ActiveButton.Pressed = false;
                        if (ActiveButton.FirstPressed + 1.5 < CurrentTime)
                        {
                            if (ActiveButton.LastPressed + 0.3 < CurrentTime)
                            {
                                pressedButton = ActiveButton;
                                ActiveButton.Pressed = true;
                                ActiveButton.LastPressed = CurrentTime;
                            }
                        }
                    }
                }
                else
                {
                    ActiveButton.FirstPressed = CurrentTime;
                    ActiveButton.Pressed = false;
                }
            }
            else if (pressed)
            {
                foreach (var area in ActiveWindow.SubAreas)
                {
                    if (!(area is DPIButton)) continue;
                    var b = (DPIButton)area;
                    b.Pressed = false;
                    if (b.SensitiveArea(ActiveWindow.Position).Contains(x, y))
                    {
                        ActiveButton = b;
                        ActiveButton.Pressed = true;
                        ActiveButton.FirstPressed = CurrentTime;
                        if (!b.UpType && b.Enabled) pressedButton = ActiveButton;
                        break;
                    }
                }
            }
            if (!pressed && ActiveButton != null)
            {
                ActiveButton.Pressed = false;
                ActiveButton = null;
            }
            pressedButton?.PressedAction();
        }
        public void ExitWindow(DPIWindow window)
        {
            var windows = new List<DPIWindow>(Windows);
            windows.Remove(window);
            Windows = windows;
            if (window.Parent == null) ActiveWindow = DPDefaultWindow;
            else ActiveWindow = window.Parent;
        }
    }
    public class DPDefaultWindow : DPIWindow
    {
 //       PlanningWindow PlanningWindow;
        MessageArea MessageArea;
 //       TargetDistance TargetDistance;
 //       TTIandLSSMArea TTIandLSSMArea;
        MenuBar MenuBar;
        public DPDefaultWindow(DistributedPowerInterface dpi, CabViewControl control) : base(dpi, 640, 480)
        {
            var param = (control as CVCScreen).CustomParameters;
            int maxSpeed = 400;
            if (param.ContainsKey("maxspeed")) int.TryParse(param["maxspeed"], out maxSpeed);
            int maxVisibleSpeed = maxSpeed;
            if (param.ContainsKey("maxvisiblespeed")) int.TryParse(param["maxvisiblespeed"], out maxVisibleSpeed);
//            PlanningWindow = new PlanningWindow(dpi);
//           TTIandLSSMArea = new TTIandLSSMArea(dpi);
 //           TargetDistance = new TargetDistance(dpi);
//           MessageArea = new MessageArea(dpi);
//            MenuBar = new MenuBar(dpi);
//            TargetDistance.Layer = -1;
//            TTIandLSSMArea.Layer = -1;
//           MessageArea.Layer = -1;
            /*            AddToLayout(PlanningWindow, new Point(334, DPI.IsSoftLayout ? 0 : 15));
                        AddToLayout(PlanningWindow.ButtonScaleDown, new Point(334, DPI.IsSoftLayout ? 0 : 15));
                        AddToLayout(PlanningWindow.ButtonScaleUp, new Point(334, 285 + (DPI.IsSoftLayout ? 0 : 15)));
                        AddToLayout(TTIandLSSMArea, new Point(0, DPI.IsSoftLayout ? 0 : 15));
                        AddToLayout(TargetDistance, new Point(0, 54 + (DPI.IsSoftLayout ? 0 : 15)));
                        AddToLayout(MessageArea, new Point(54, DPI.IsSoftLayout ? 350 : 365));
                        AddToLayout(MessageArea.ButtonScrollUp, new Point(54+234, DPI.IsSoftLayout ? 350 : 365));
                        AddToLayout(MessageArea.ButtonScrollDown, new Point(54+234, MessageArea.Height / 2 + (DPI.IsSoftLayout ? 350 : 365)));*/
            /*           foreach (int i in Enumerable.Range(0, MenuBar.Buttons.Count))
                       {
                           AddToLayout(MenuBar.Buttons[i], new Point(580, 15 + 50*i));
                       }*/
            DPISubwindow FixedText = new DPISubwindow("Distributed Power Operation", true, dpi);
            AddToLayout(FixedText, new Point(0, 0));
            DPITable DPITable = new DPITable(fullTable:true, fullScreen:true, dpi:dpi);
            AddToLayout(DPITable, new Point(0, 76));
        }
    }

    public class DPIArea
    {
        public Point Position;
        public readonly DistributedPowerInterface DPI;
        protected Texture2D ColorTexture => DPI.ColorTexture;
        public float Scale => DPI.Scale;
        public int Height;
        public int Width;
        protected List<RectanglePrimitive> Rectangles = new List<RectanglePrimitive>();
        protected List<TextPrimitive> Texts = new List<TextPrimitive>();
        protected List<TexturePrimitive> Textures = new List<TexturePrimitive>();
        public int Layer;
        protected bool FlashingFrame;
        public Color BackgroundColor = Color.Transparent;
        public bool Pressed;
        public bool Visible;
        public class TextPrimitive
        {
            public Point Position;
            public Color Color;
            public WindowTextFont Font;
            public string Text;

            public TextPrimitive(Point position, Color color, string text, WindowTextFont font)
            {
                Position = position;
                Color = color;
                Text = text;
                Font = font;
            }

            public void Draw(SpriteBatch spriteBatch, Point position)
            {
                Font.Draw(spriteBatch, position, Text, Color);
            }
        }
        public struct TexturePrimitive
        {
            public readonly Texture2D Texture;
            public readonly Vector2 Position;
            public TexturePrimitive(Texture2D texture, Vector2 position)
            {
                Texture = texture;
                Position = position;
            }
            public TexturePrimitive(Texture2D texture, float x, float y)
            {
                Texture = texture;
                Position = new Vector2(x, y);
            }
        }
        public struct RectanglePrimitive
        {
            public readonly float X;
            public readonly float Y;
            public readonly float Width;
            public readonly float Height;
            public readonly bool DrawAsInteger;
            public Color Color;
        }
        public DPIArea(DistributedPowerInterface dpi)
        {
            DPI = dpi;
        }
        public DPIArea(DistributedPowerInterface dpi, int width, int height)
        {
            DPI = dpi;
            Width = width;
            Height = height;
        }
        public virtual void Draw(SpriteBatch spriteBatch, Point drawPosition)
        {
            if (BackgroundColor != Color.Transparent) DrawRectangle(spriteBatch, drawPosition, 0, 0, Width, Height, BackgroundColor);

            foreach (var r in Rectangles)
            {
                if (r.DrawAsInteger) DrawIntRectangle(spriteBatch, drawPosition, r.X, r.Y, r.Width, r.Height, r.Color);
                else DrawRectangle(spriteBatch, drawPosition, r.X, r.Y, r.Width, r.Height, r.Color);
            }
            foreach(var text in Texts)
            {
                int x = drawPosition.X + (int)Math.Round(text.Position.X * Scale);
                int y = drawPosition.Y + (int)Math.Round(text.Position.Y * Scale);
                text.Draw(spriteBatch, new Point(x, y));
            }
            foreach(var tex in Textures)
            {
                DrawSymbol(spriteBatch, tex.Texture, drawPosition, tex.Position.Y, tex.Position.Y);
            }
            if (FlashingFrame && DPI.Blinker4Hz)
            {
                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, 2, Height, ColorYellow);
                DrawIntRectangle(spriteBatch, drawPosition, Width - 2, 0, 2, Height, ColorYellow);
                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, Width, 2, ColorYellow);
                DrawIntRectangle(spriteBatch, drawPosition, 0, Height - 2, Width, 2, ColorYellow);
            }
            else if (DPI.BlackWhiteTheme)
            {
                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, 1, Height, Color.White);
                DrawIntRectangle(spriteBatch, drawPosition, Width - 1, 0, 1, Height, Color.White);
                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, Width, 1, Color.White);
                DrawIntRectangle(spriteBatch, drawPosition, 0, Height - 1, Width, 1, Color.White);
            }
            else if (this is DPIButton && (this as DPIButton).ShowButtonBorder)
            {
                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, 1, Height, Color.Black);
                DrawIntRectangle(spriteBatch, drawPosition, Width - 1, 0, 1, Height, ColorShadow);
                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, Width, 1, Color.Black);
                DrawIntRectangle(spriteBatch, drawPosition, 0, Height - 1, Width, 1, ColorShadow);

                if (!Pressed)
                {
                    DrawIntRectangle(spriteBatch, drawPosition, 1, 1, 1, Height - 2, ColorShadow);
                    DrawIntRectangle(spriteBatch, drawPosition, Width - 2, 1, 1, Height - 2, Color.Black);
                    DrawIntRectangle(spriteBatch, drawPosition, 1, 1, Width - 2, 1, ColorShadow);
                    DrawIntRectangle(spriteBatch, drawPosition, 1, Height - 2, Width - 2, 1, Color.Black);
                }
            }
            else if (Layer < 0)
            {
                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, 1, Height, Color.Black);
                DrawIntRectangle(spriteBatch, drawPosition, Width - 1, 0, 1, Height, ColorShadow);
                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, Width, 1, Color.Black);
                DrawIntRectangle(spriteBatch, drawPosition, 0, Height - 1, Width, 1, ColorShadow);
            }
        }
        public virtual void PrepareFrame(ETCSStatus status) { }

        public void DrawRectangle(SpriteBatch spriteBatch, Point drawPosition, float x, float y, float width, float height, Color color)
        {
            spriteBatch.Draw(ColorTexture, new Vector2(drawPosition.X + x * Scale, drawPosition.Y + y * Scale), null, color, 0f, Vector2.Zero, new Vector2(width * Scale, height * Scale), SpriteEffects.None, 0);
        }
        public void DrawIntRectangle(SpriteBatch spriteBatch, Point drawPosition, float x, float y, float width, float height, Color color)
        {
            spriteBatch.Draw(ColorTexture, new Rectangle(drawPosition.X + (int)(x * Scale), drawPosition.Y + (int)(y * Scale), Math.Max((int)(width * Scale), 1), Math.Max((int)(height * Scale), 1)), null, color);
        }
        public void DrawSymbol(SpriteBatch spriteBatch, Texture2D texture, Point origin, float x, float y)
        {
            spriteBatch.Draw(texture, new Vector2(origin.X + x * Scale, origin.Y + y * Scale), null, Color.White, 0, Vector2.Zero, Scale * DPI.MipMapScale, SpriteEffects.None, 0);
        }
        public WindowTextFont GetFont(float size, bool bold=false)
        {
            return DPI.Viewer.WindowManager.TextManager.GetExact("Arial", GetScaledFontSize(size), bold ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular);
        }
        /// <summary>
        /// Get scaled font size, increasing it if result is small
        /// </summary>
        /// <param name="requiredSize"></param>
        /// <returns></returns>
        public float GetScaledFontSize(float requiredSize)
        {
            float size = requiredSize * Scale;
            if (size < 5) return size * 1.2f;
            return size;
        }
        public virtual void ScaleChanged() { }
    }
    public class DPIWindow : DPIArea
    {
        public DPIWindow Parent;
        public List<DPIArea> SubAreas = new List<DPIArea>();
        public bool FullScreen;
        protected DPIWindow(DistributedPowerInterface dpi, int width, int height) : base(dpi, width, height)
        {
        }
        public override void PrepareFrame(ETCSStatus status)
        {
            if (!Visible) return;
            base.PrepareFrame(status);
            foreach(var area in SubAreas)
            {
                area.PrepareFrame(status);
            }
        }
        public override void Draw(SpriteBatch spriteBatch, Point drawPosition)
        {
            if (!Visible) return;
            base.Draw(spriteBatch, drawPosition);
            foreach(var area in SubAreas)
            {
                if (area.Visible) area.Draw(spriteBatch, new Point((int)Math.Round(drawPosition.X + area.Position.X * Scale), (int)Math.Round(drawPosition.Y + area.Position.Y * Scale)));
            }
        }
        public void AddToLayout(DPIArea area, Point position)
        {
            area.Position = position;
            area.Visible = true;
            SubAreas.Add(area);
        }
        public override void ScaleChanged()
        {
            base.ScaleChanged();
            foreach (var area in SubAreas)
            {
                area.ScaleChanged();
            }
        }
    }
    public class DPISubwindow : DPIWindow
    {
        public string WindowTitle { get; private set; }
        TextPrimitive WindowTitleText;
        WindowTextFont WindowTitleFont;
        readonly int FontHeightWindowTitle = 20;
        protected readonly DPIIconButton CloseButton;
        public DPISubwindow(string title, bool fullScreen, DistributedPowerInterface dpi) : base(dpi, fullScreen ? 640 : 306, 450)
        {
            WindowTitle = title;
            FullScreen = fullScreen;
            CloseButton = new DPIIconButton("NA_11.bmp", "NA_12.bmp", Viewer.Catalog.GetString("Close"), true, () => dpi.ExitWindow(this), 82, 50, dpi);
            CloseButton.Enabled = true;
            BackgroundColor = DPI.BlackWhiteTheme ? Color.Black : ColorBackground;
            SetFont();
            AddToLayout(CloseButton, new Point(0, fullScreen ? 440 : 396));
        }
        public override void ScaleChanged()
        {
            base.ScaleChanged();
            SetFont();
        }
        void SetFont()
        {
            WindowTitleFont = GetFont(FontHeightWindowTitle);
            SetTitle(WindowTitle);
        }
        public override void Draw(SpriteBatch spriteBatch, Point drawPosition)
        {
            if (!Visible) return;
            base.Draw(spriteBatch, drawPosition);
//            DrawRectangle(spriteBatch, drawPosition, 0, 0, FullScreen ? 334 : 306, 24, Color.Black);
            int x = drawPosition.X + (int)Math.Round(WindowTitleText.Position.X * Scale);
            int y = drawPosition.Y + (int)Math.Round(WindowTitleText.Position.Y * Scale);
            WindowTitleText.Draw(spriteBatch, new Point(x, y));
        }
        public void SetTitle(string s)
        {
            WindowTitle = s;
            int length = (int)(WindowTitleFont.MeasureString(s));
            int x = (int)((640 - length) / 2 * Scale);
            WindowTitleText = new TextPrimitive(new Point(x, (int)((FontHeightWindowTitle + 3) * Scale)), ColorWhite, WindowTitle, WindowTitleFont);
        }
    }

    public class DPITable : DPIWindow
    {
        public DistributedPowerInterface DPI;
        private const int NumberOfRowsFull = 9;
        private const int NumberOfRowsPartial = 6;
        private const int NumberOfColumns = 7;
        public const string Fence = "\u2590";
        public string[] TableRows = new string[NumberOfRowsFull];
        TextPrimitive[,] TableText = new TextPrimitive[NumberOfRowsFull, NumberOfColumns];
        TextPrimitive[,] TableSymbol = new TextPrimitive[NumberOfRowsFull, NumberOfColumns];
        WindowTextFont TableTextFont;
        WindowTextFont TableSymbolFont;
        readonly int FontHeightTableText = 32;
        readonly int FontHeightTableSymbol = 38;
        readonly int RowHeight = 34;
        readonly int ColLength = 88;
        public bool FullTable = true;

        // Change text color
        readonly Dictionary<string, Color> ColorCodeCtrl = new Dictionary<string, Color>
        {
            { "!!!", Color.OrangeRed },
            { "!!?", Color.Orange },
            { "!??", Color.White },
            { "?!?", Color.Black },
            { "???", Color.Yellow },
            { "??!", Color.Green },
            { "?!!", Color.PaleGreen },
            { "$$$", Color.LightSkyBlue},
            { "%%%", Color.Cyan}
        };

        /// <summary>
        /// A Train Dpu row with data fields.
        /// </summary>
        public struct ListLabel
        {
            public string FirstCol;
            public int FirstColWidth;
            public List<string> LastCol;
            public List<int> LastColWidth;
            public List<string> SymbolCol;
            public bool ChangeColWidth;
            public string KeyPressed;
        }

        public List<ListLabel> TempListToLabel = new List<ListLabel>();// used when listtolabel is changing

        protected string[] FirstColumn = { "ID", "Throttle", "Load", "BP", "Flow", "Remote", "ER", "BC", "MR" };

        public DPITable(bool fullTable, bool fullScreen, DistributedPowerInterface dpi) : base(dpi, 640,  fullScreen? 404 : 260)
        {
            DPI = dpi;
            FullScreen = fullScreen;
            FullTable = fullTable;
            BackgroundColor = DPI.BlackWhiteTheme ? Color.Black : ColorBackground;
            SetFont();
            string text = "";
            for (int iRow = 0; iRow < (fullTable ? NumberOfRowsFull : NumberOfRowsPartial); iRow++)
            {
                for (int iCol = 0; iCol < NumberOfColumns; iCol++)
                {
//                    text = iCol.ToString() + "--" + iRow.ToString();
                    TableText[iRow, iCol] = new TextPrimitive(new Point(20 + ColLength * iCol, (iRow + 1) * (FontHeightTableText - 8)), Color.White, text, TableTextFont);
                    TableSymbol[iRow, iCol] = new TextPrimitive(new Point(10 + ColLength * iCol, (iRow + 1) * (FontHeightTableText - 8)), Color.Green, text, TableSymbolFont);
                }
            }
        }

        public override void ScaleChanged()
        {
            base.ScaleChanged();
            SetFont();
        }
        void SetFont()
        {
            TableTextFont = GetFont(FontHeightTableText);
            TableSymbolFont = GetFont(FontHeightTableSymbol);
        }
        public override void Draw(SpriteBatch spriteBatch, Point drawPosition)
        {
            if (!Visible) return;
            base.Draw(spriteBatch, drawPosition);
            for (int iRow = 0; iRow < NumberOfRowsFull; iRow++)
                for (int iCol = 0; iCol < NumberOfColumns; iCol++)
                {
                    //            DrawRectangle(spriteBatch, drawPosition, 0, 0, FullScreen ? 334 : 306, 24, Color.Black);
                    int x = drawPosition.X + (int)Math.Round(TableText[iRow, iCol].Position.X * Scale);
                    int y = drawPosition.Y + (int)Math.Round(TableText[iRow, iCol].Position.Y * Scale);
                    TableText[iRow, iCol].Draw(spriteBatch, new Point(x, y));
                    x = drawPosition.X + (int)Math.Round(TableSymbol[iRow, iCol].Position.X * Scale);
                    y = drawPosition.Y + (int)Math.Round(TableSymbol[iRow, iCol].Position.Y * Scale);
                    TableSymbol[iRow, iCol].Draw(spriteBatch, new Point(x, y));
                }
        }

        public override void PrepareFrame(ETCSStatus etcsstatus)
        {
            int tRIndex = 0;
            var tableRow = TableRows[tRIndex];
            tableRow = "";
            var locomotive = DPI.Locomotive;
            var train = locomotive.Train;
            var multipleUnitsConfiguration = locomotive.GetMultipleUnitsConfiguration();
            int dieselLocomotivesCount = 0;

//            var firstCol = label.FirstCol;
            var firstColWidth = 0;
 //           var lastCol = label.LastCol;
            List<int> lastColWidth = new List<int>();
//            var symbolCol = label.SymbolCol;

            if (locomotive != null)
            {
                int numberOfDieselLocomotives = 0;
                int maxNumberOfEngines = 0;
                for (var i = 0; i < train.Cars.Count; i++)
                {
                    if (train.Cars[i] is MSTSDieselLocomotive)
                    {
                        numberOfDieselLocomotives++;
                        maxNumberOfEngines = Math.Max(maxNumberOfEngines, (train.Cars[i] as MSTSDieselLocomotive).DieselEngines.Count);
                    }
                }
                if (numberOfDieselLocomotives > 0)
                {
                    var dieselLoco = MSTSDieselLocomotive.GetDpuHeader(true, numberOfDieselLocomotives, maxNumberOfEngines).Replace("\t", "");
                    string[] dieselLocoHeader = dieselLoco.Split('\n');
                    string[,] tempStatus = new string[numberOfDieselLocomotives, dieselLocoHeader.Length];
                    var k = 0;
                    var dpUnitId = 0;
                    var dpUId = -1;
                    var i = 0;
                    for (i = 0; i < train.Cars.Count; i++)
                    {
                        if (train.Cars[i] is MSTSDieselLocomotive)
                        {
                            if (dpUId != (train.Cars[i] as MSTSLocomotive).DPUnitID)
                            {
                                var status = (train.Cars[i] as MSTSDieselLocomotive).GetDpuStatus(true).Split('\t');
                                var fence = ((dpUnitId != (dpUnitId = train.Cars[i].RemoteControlGroup)) ? "| " : "  ");
                                tempStatus[k, 0] = fence + status[0].Split('(').First();
                                for (var j = 1; j < status.Length; j++)
                                {
                                    // fence
                                    tempStatus[k, j] = fence + status[j].Split(' ').First();
                                }
                                dpUId = (train.Cars[i] as MSTSLocomotive).DPUnitID;
                                k++;
                            }
                        }
                    }

                    dieselLocomotivesCount = k;// only leaders loco group

                    for (i = 0; i < dieselLocoHeader.Count(); i++)
                    {
//                        lastCol = new List<string>();
 //                       symbolCol = new List<string>();

                        for (int j = 0; j < dieselLocomotivesCount; j++)
                        {
                            //                           symbolCol.Add(tempStatus[i, j] != null && tempStatus[i, j].Contains("|") ? Symbols.Fence + ColorCode[Color.Green] : "");
                            var text = tempStatus[j, i].Replace('|', ' ');
                            var colorFirstColEndsWith = ColorCodeCtrl.Keys.Any(text.EndsWith) ? ColorCodeCtrl[text.Substring(text.Length - 3)] : Color.White;
                            TableText[i, j + 1].Text = (colorFirstColEndsWith == Color.White) ? text : text.Substring(0, text.Length - 3); ;
                            TableText[i, j + 1].Color = colorFirstColEndsWith;
                            TableSymbol[i, j + 1].Text = (tempStatus[j, i] != null && tempStatus[j, i].Contains("|")) ? Fence : "";
                        }

                        TableText[i, 0].Text = dieselLocoHeader[i];
                    }
                }
            }
        }
    }
    public class DPIButton : DPIArea
    {
        public Rectangle SensitiveArea(Point WindowPosition) => new Rectangle(WindowPosition.X + Position.X - ExtendedSensitiveArea.X, WindowPosition.Y + Position.Y - ExtendedSensitiveArea.Y, Width + ExtendedSensitiveArea.Width + ExtendedSensitiveArea.X, Height + ExtendedSensitiveArea.Height + ExtendedSensitiveArea.Y);
        public Rectangle ExtendedSensitiveArea;
        public Action PressedAction = null;
        public string ConfirmerCaption;
        public readonly string DisplayName;
        public bool Enabled;
        public bool PressedEffect;
        public readonly bool UpType;
        public bool DelayType;
        public bool ShowButtonBorder;
        public float FirstPressed;
        public float LastPressed;
        public DPIButton(string displayName, bool upType, DistributedPowerInterface dpi, bool showButtonBorder) : base(dpi)
        {
            DisplayName = displayName;
            Enabled = false;
            UpType = upType;
            ShowButtonBorder = showButtonBorder;
        }
        public DPIButton(string displayName, bool upType, Action pressedAction, int width, int height, DistributedPowerInterface dpi, bool showButtonBorder=false) : base(dpi, width, height)
        {
            DisplayName = displayName;
            Enabled = false;
            UpType = upType;
            PressedAction = pressedAction;
            ShowButtonBorder = showButtonBorder;
        }
    }
    public class DPITextButton : DPIButton
    {
        string[] Caption;
        WindowTextFont CaptionFont;
        int FontHeightButton = 12;
        TextPrimitive[] CaptionText;
        public DPITextButton(string caption, string displayName, bool upType, Action pressedAction, int width, int height, DistributedPowerInterface dpi, int fontHeight = 12) :
            base(displayName, upType, pressedAction, width, height, dpi, true)
        {
            Caption = caption.Split('\n');
            CaptionText = new TextPrimitive[Caption.Length];
            ConfirmerCaption = caption;
            FontHeightButton = fontHeight;
            SetFont();
            SetText();
        }
        void SetText()
        {
            foreach (int i in Enumerable.Range(0, Caption.Length))
            {
                int fontWidth = (int)(CaptionFont.MeasureString(Caption[i]) / Scale);
                CaptionText[i] = new TextPrimitive(new Point((Width - fontWidth) / 2, (Height - FontHeightButton) / 2 + FontHeightButton * (2 * i - Caption.Length + 1)), Color.White, Caption[i], CaptionFont);
            }
        }
        public override void ScaleChanged()
        {
            base.ScaleChanged();
            SetFont();
            SetText();
        }
        void SetFont()
        {
            CaptionFont = GetFont(FontHeightButton);
        }
        public override void PrepareFrame(ETCSStatus status)
        {
            base.PrepareFrame(status);
            foreach (var text in CaptionText)
                text.Color = Enabled ? ColorGrey : ColorDarkGrey;
        }
        public override void Draw(SpriteBatch spriteBatch, Point drawPosition)
        {
            base.Draw(spriteBatch, drawPosition);
            foreach (var text in CaptionText)
            {
                int x = drawPosition.X + (int)Math.Round(text.Position.X * Scale);
                int y = drawPosition.Y + (int)Math.Round(text.Position.Y * Scale);
                text.Draw(spriteBatch, new Point(x, y));
            }
        }
    }
    public class DPIIconButton : DPIButton
    {
        readonly string DisabledSymbol;
        readonly string EnabledSymbol;
        TexturePrimitive DisabledTexture;
        TexturePrimitive EnabledTexture;
        public DPIIconButton(string enabledSymbol, string disabledSymbol, string displayName, bool upType , Action pressedAction, int width, int height, DistributedPowerInterface dpi) :
            base(displayName, upType, pressedAction, width, height, dpi, true)
        {
            DisabledSymbol = disabledSymbol;
            EnabledSymbol = enabledSymbol;
            SetIcon();
        }
        void SetIcon()
        {
            Texture2D tex1 = DPI.LoadTexture(EnabledSymbol);
            Texture2D tex2 = DPI.LoadTexture(DisabledSymbol);
            EnabledTexture = new TexturePrimitive(tex1, new Vector2((Width - tex1.Width * DPI.MipMapScale) / 2, (Height - tex1.Height * DPI.MipMapScale) / 2));
            DisabledTexture = new TexturePrimitive(tex2, new Vector2((Width - tex2.Width * DPI.MipMapScale) / 2, (Height - tex2.Height * DPI.MipMapScale) / 2));
        }
        public override void ScaleChanged()
        {
            base.ScaleChanged();
            SetIcon();
        }
        public override void Draw(SpriteBatch spriteBatch, Point drawPosition)
        {
            base.Draw(spriteBatch, drawPosition);
            var tex = Enabled ? EnabledTexture : DisabledTexture;
            DrawSymbol(spriteBatch, tex.Texture, drawPosition, tex.Position.X, tex.Position.Y);
        }
    }
    public class DPITextLabel : DPIArea
    {
        string[] Caption;
        WindowTextFont CaptionFont;
        int FontHeightButton = 12;
        TextPrimitive[] CaptionText;
        public DPITextLabel(string caption, int width, int height, DistributedPowerInterface dpi) :
            base(dpi, width, height)
        {
            Caption = caption.Split('\n');
            CaptionText = new TextPrimitive[Caption.Length];
            SetFont();
            SetText();
        }
        void SetText()
        {
            foreach (int i in Enumerable.Range(0, Caption.Length))
            {
                int fontWidth = (int)(CaptionFont.MeasureString(Caption[i]) / Scale);
                CaptionText[i] = new TextPrimitive(new Point((Width - fontWidth) / 2, (Height - FontHeightButton) / 2 + FontHeightButton * (2 * i - Caption.Length + 1)), ColorGrey, Caption[i], CaptionFont);
            }
        }
        public override void ScaleChanged()
        {
            base.ScaleChanged();
            SetFont();
            SetText();
        }
        void SetFont()
        {
            CaptionFont = GetFont(FontHeightButton);
        }
        public override void Draw(SpriteBatch spriteBatch, Point drawPosition)
        {
            base.Draw(spriteBatch, drawPosition);
            foreach (var text in CaptionText)
            {
                int x = drawPosition.X + (int)Math.Round(text.Position.X * Scale);
                int y = drawPosition.Y + (int)Math.Round(text.Position.Y * Scale);
                text.Draw(spriteBatch, new Point(x, y));
            }
        }
    }

    public class DistributedPowerInterfaceRenderer : CabViewControlRenderer, ICabViewMouseControlRenderer
    {
        DistributedPowerInterface DPI;
        bool Zoomed = false;
        protected Rectangle DrawPosition;
        [CallOnThread("Loader")]
        public DistributedPowerInterfaceRenderer(Viewer viewer, MSTSLocomotive locomotive, CVCScreen control, CabShader shader)
            : base(viewer, locomotive, control, shader)
        {
            Position.X = (float)Control.PositionX;
            Position.Y = (float)Control.PositionY;
            DPI = new DistributedPowerInterface((int)Control.Height, (int)Control.Width, locomotive, viewer, control);
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (!IsPowered)
                return;

            base.PrepareFrame(frame, elapsedTime);
            var xScale = Viewer.CabWidthPixels / 640f;
            var yScale = Viewer.CabHeightPixels / 480f;
            DrawPosition.X = (int)(Position.X * xScale) - Viewer.CabXOffsetPixels + Viewer.CabXLetterboxPixels;
            DrawPosition.Y = (int)(Position.Y * yScale) + Viewer.CabYOffsetPixels + Viewer.CabYLetterboxPixels;
            DrawPosition.Width = (int)(Control.Width * xScale);
            DrawPosition.Height = (int)(Control.Height * yScale);
            if (Zoomed)
            {
                DrawPosition.Width = 640;
                DrawPosition.Height = 480;
                DPI.SizeTo(DrawPosition.Width, DrawPosition.Height);
                DrawPosition.X -= 320;
                DrawPosition.Y -= 240;
                DPI.DPDefaultWindow.BackgroundColor = ColorBackground;
            }
            else
            {
                DPI.SizeTo(DrawPosition.Width, DrawPosition.Height);
                DPI.DPDefaultWindow.BackgroundColor = Color.Transparent;
            }
            DPI.PrepareFrame(elapsedTime.ClockSeconds);
        }

        public bool IsMouseWithin()
        {
            int x = (int)((UserInput.MouseX - DrawPosition.X) / DPI.Scale);
            int y = (int)((UserInput.MouseY - DrawPosition.Y) / DPI.Scale);
            if (UserInput.IsMouseRightButtonPressed && new Rectangle(0, 0, 640, 480).Contains(x, y)) Zoomed = !Zoomed;
            foreach (var area in DPI.ActiveWindow.SubAreas)
            {
                if (!(area is DPIButton)) continue;
                var b = (DPIButton)area;
                if (b.SensitiveArea(DPI.ActiveWindow.Position).Contains(x, y) && b.Enabled) return true;
            }
            return false;
        }

        public void HandleUserInput()
        {
            DPI.HandleMouseInput(UserInput.IsMouseLeftButtonDown, (int)((UserInput.MouseX - DrawPosition.X) / DPI.Scale), (int)((UserInput.MouseY - DrawPosition.Y) / DPI.Scale));
        }

        public string GetControlName()
        {
            int x = (int)((UserInput.MouseX - DrawPosition.X) / DPI.Scale);
            int y = (int)((UserInput.MouseY - DrawPosition.Y) / DPI.Scale);
            foreach (var area in DPI.ActiveWindow.SubAreas)
            {
                if (!(area is DPIButton)) continue;
                var b = (DPIButton)area;
                if (b.SensitiveArea(DPI.ActiveWindow.Position).Contains(x, y)) return b.DisplayName;
            }
            return "";
        }
        public string GetControlLabel()
        {
            return GetControlName();
        }
        public override void Draw(GraphicsDevice graphicsDevice)
        {
            DPI.Draw(CabShaderControlView.SpriteBatch, new Point(DrawPosition.X, DrawPosition.Y));
            CabShaderControlView.SpriteBatch.End();
            CabShaderControlView.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, DepthStencilState.Default, null, Shader);
        }
    }
}