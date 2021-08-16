// COPYRIGHT 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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

using Microsoft.Xna.Framework.Graphics;
using Orts.Simulation.Physics;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using Orts.Simulation.RollingStocks.SubSystems.Brakes;
using Orts.Simulation.RollingStocks;
using System.Text;
using ORTS.Common.Input;
using Microsoft.Xna.Framework;
using System.IO;

namespace Orts.Viewer3D.Popups
{
    public class TrainDrivingWindow : Window
    {
        bool DynBrakeSetup = false;
        bool ResizeWindow = false;
        bool UpdateDataEnded = false;
        double StartTime;
        int FirstColIndex = 0;//first string that does not fit
        int FirstColLenght = 0;
        int FirstColOverFlow = 0;
        int LastColLenght = 0;
        int LastColOverFlow = 0;
        int LinesCount = 0;

        bool ctrlAIFiremanOn = false; //AIFireman On
        bool ctrlAIFiremanOff = false;//AIFireman Off
        bool ctrlAIFiremanReset = false;//AIFireman Reset
        double clockAIFireTime; //AIFireman reset timing

        bool grateLabelVisible = false;// Grate label visible
        double clockGrateTime; // Grate hide timing

        bool wheelLabelVisible = false;// Wheel label visible
        double clockWheelTime; // Wheel hide timing

        bool doorsLabelVisible = false; // Doors label visible
        double clockDoorsTime; // Doors hide timing

        public bool StandardHUD = true;// Standard text
        public bool TrainDrivingUpdating = false;

        int WindowHeightMin = 0;
        int WindowHeightMax = 0;
        int WindowWidthMin = 0;
        int WindowWidthMax = 0;
        int maxFirstColWidth = 0;
        int maxLastColWidth = 0;

        char expandWindow;
        string keyPressed;// display a symbol when a control key is pressed.
        string gradientIndicator;
        public int OffSetX = 0;
        const int TextSize = 15;
        public int keyPresLenght;

        Label indicator;
        LabelMono indicatorMono;
        Label ExpandWindow;
        Label LabelFontToBold;
        public static bool MonoFont;
        public static bool FontToBold;

        public struct ListLabel
        {
            public string FirstCol { get; set; }
            public int FirstColWidth { get; set; }
            public string LastCol { get; set; }
            public int LastColWidth { get; set; }
            public string SymbolCol { get; set; }
            public bool ChangeColWidth { get; set; }
            public string keyPressed { get; set; }
        }
        public List<ListLabel> ListToLabel = new List<ListLabel>();
        public List<ListLabel> TempListToLabel = new List<ListLabel>();// used when listtolabel is changing
        // Change text color
        readonly Dictionary<string, Color> ColorCode = new Dictionary<string, Color>
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

        readonly Dictionary<string, string> FirstColToAbbreviated = new Dictionary<string, string>()
        {
            [Viewer.Catalog.GetString("AI Fireman")] = Viewer.Catalog.GetString("AIFR"),
            [Viewer.Catalog.GetString("Autopilot")] = Viewer.Catalog.GetString("AUTO"),
            [Viewer.Catalog.GetString("Battery switch")] = Viewer.Catalog.GetString("BATT"),
            [Viewer.Catalog.GetString("Boiler pressure")] = Viewer.Catalog.GetString("PRES"),
            [Viewer.Catalog.GetString("Boiler water glass")] = Viewer.Catalog.GetString("WATR"),
            [Viewer.Catalog.GetString("Boiler water level")] = Viewer.Catalog.GetString("LEVL"),
            [Viewer.Catalog.GetString("CCStatus")] = Viewer.Catalog.GetString("CCST"),
            [Viewer.Catalog.GetString("Circuit breaker")] = Viewer.Catalog.GetString("CIRC"),
            [Viewer.Catalog.GetString("Cylinder cocks")] = Viewer.Catalog.GetString("CCOK"),
            [Viewer.Catalog.GetString("Direction")] = Viewer.Catalog.GetString("DIRC"),
            [Viewer.Catalog.GetString("Doors open")] = Viewer.Catalog.GetString("DOOR"),
            [Viewer.Catalog.GetString("Dynamic brake")] = Viewer.Catalog.GetString("BDYN"),
            [Viewer.Catalog.GetString("Electric train supply")] = Viewer.Catalog.GetString("TSUP"),
            [Viewer.Catalog.GetString("Engine brake")] = Viewer.Catalog.GetString("BLOC"),
            [Viewer.Catalog.GetString("Engine")] = Viewer.Catalog.GetString("ENGN"),
            [Viewer.Catalog.GetString("Fire mass")] = Viewer.Catalog.GetString("FIRE"),
            [Viewer.Catalog.GetString("Fixed gear")] = Viewer.Catalog.GetString("GEAR"),
            [Viewer.Catalog.GetString("Fuel levels")] = Viewer.Catalog.GetString("FUEL"),
            [Viewer.Catalog.GetString("Gear")] = Viewer.Catalog.GetString("GEAR"),
            [Viewer.Catalog.GetString("Gradient")] = Viewer.Catalog.GetString("GRAD"),
            [Viewer.Catalog.GetString("Grate limit")] = Viewer.Catalog.GetString("GRAT"),
            [Viewer.Catalog.GetString("Master key")] = Viewer.Catalog.GetString("MAST"),
            [Viewer.Catalog.GetString("MaxAccel")] = Viewer.Catalog.GetString("MACC"),
            [Viewer.Catalog.GetString("Pantographs")] = Viewer.Catalog.GetString("PANT"),
            [Viewer.Catalog.GetString("Power")] = Viewer.Catalog.GetString("POWR"),
            [Viewer.Catalog.GetString("Regulator")] = Viewer.Catalog.GetString("REGL"),
            [Viewer.Catalog.GetString("Replay")] = Viewer.Catalog.GetString("RPLY"),
            [Viewer.Catalog.GetString("Retainers")] = Viewer.Catalog.GetString("RETN"),
            [Viewer.Catalog.GetString("Reverser")] = Viewer.Catalog.GetString("REVR"),
            [Viewer.Catalog.GetString("Sander")] = Viewer.Catalog.GetString("SAND"),
            [Viewer.Catalog.GetString("Speed")] = Viewer.Catalog.GetString("SPED"),
            [Viewer.Catalog.GetString("Steam usage")] = Viewer.Catalog.GetString("STEM"),
            [Viewer.Catalog.GetString("Target")] = Viewer.Catalog.GetString("TARG"),
            [Viewer.Catalog.GetString("Throttle")] = Viewer.Catalog.GetString("THRO"),
            [Viewer.Catalog.GetString("Time")] = Viewer.Catalog.GetString("TIME"),
            [Viewer.Catalog.GetString("Traction cut-off relay")] = Viewer.Catalog.GetString("TRAC"),
            [Viewer.Catalog.GetString("Train brake")] = Viewer.Catalog.GetString("BTRN"),
            [Viewer.Catalog.GetString("Wheel")] = Viewer.Catalog.GetString("WHEL")
        };

        readonly Dictionary<string, string> LastColToAbbreviated = new Dictionary<string, string>()
        {
            [Viewer.Catalog.GetString("(absolute)")] = Viewer.Catalog.GetString("(Abs.)"),
            [Viewer.Catalog.GetString("apply Service")] = Viewer.Catalog.GetString("Apply"),
            [Viewer.Catalog.GetString("Apply Quick")] = Viewer.Catalog.GetString("ApplQ"),
            [Viewer.Catalog.GetString("Apply Slow")] = Viewer.Catalog.GetString("ApplS"),
            [Viewer.Catalog.GetString("coal")] = Viewer.Catalog.GetString("c"),
            [Viewer.Catalog.GetString("Emergency Braking Push Button")] = Viewer.Catalog.GetString("EmerBPB"),
            [Viewer.Catalog.GetString("Lap Self")] = Viewer.Catalog.GetString("LapS"),
            [Viewer.Catalog.GetString("Minimum Reduction")] = Viewer.Catalog.GetString("MRedc"),
            [Viewer.Catalog.GetString("(safe range)")] = Viewer.Catalog.GetString("(safe)"),
            [Viewer.Catalog.GetString("skid")] = Viewer.Catalog.GetString("Skid"),
            [Viewer.Catalog.GetString("slip warning")] = Viewer.Catalog.GetString("Warning"),
            [Viewer.Catalog.GetString("slip")] = Viewer.Catalog.GetString("Slip"),
            [Viewer.Catalog.GetString("water")] = Viewer.Catalog.GetString("w")
        };

        public TrainDrivingWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 10, Window.DecorationSize.Y + owner.TextFontDefault.Height * 10, Viewer.Catalog.GetString("Train Driving Info"))
        {
            WindowHeightMin = Location.Height;
            WindowHeightMax = Location.Height + owner.TextFontDefault.Height * 20;
            WindowWidthMin = Location.Width;
            WindowWidthMax = Location.Width + owner.TextFontDefault.Height * 20;
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(StandardHUD);
            outf.Write(Location.X);
            outf.Write(Location.Y);
            outf.Write(Location.Width);
            outf.Write(Location.Height);
            outf.Write(clockAIFireTime);
            outf.Write(ctrlAIFiremanOn);
            outf.Write(ctrlAIFiremanOff);
            outf.Write(ctrlAIFiremanReset);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            Rectangle LocationRestore;
            StandardHUD = inf.ReadBoolean();
            LocationRestore.X = inf.ReadInt32();
            LocationRestore.Y = inf.ReadInt32();
            LocationRestore.Width = inf.ReadInt32();
            LocationRestore.Height = inf.ReadInt32();
            clockAIFireTime = inf.ReadDouble();
            ctrlAIFiremanOn = inf.ReadBoolean();
            ctrlAIFiremanOff = inf.ReadBoolean();
            ctrlAIFiremanReset = inf.ReadBoolean();

            // Display window
            SizeTo(LocationRestore.Width, LocationRestore.Height);
            MoveTo(LocationRestore.X, LocationRestore.Y);
        }

        protected internal override void Initialize()
        {
            base.Initialize();
            // Reset window size
            UpdateWindowSize();
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            // Display main HUD data
            var vbox = base.Layout(layout).AddLayoutVertical();
            if (ListToLabel.Count > 0)
            {
                var colWidth = ListToLabel.Max(x => x.FirstColWidth) + (StandardHUD ? 15 : 20);
                var TimeHboxPositionY = 0;
                foreach (var data in ListToLabel.ToList())
                {
                    if (data.FirstCol.Contains(Viewer.Catalog.GetString("NwLn")))
                    {
                        var hbox = vbox.AddLayoutHorizontalLineOfText();
                        hbox.Add(new Label(colWidth * 2, hbox.RemainingHeight, " "));
                    }
                    else if (data.FirstCol.Contains("Sprtr"))
                    {
                        vbox.AddHorizontalSeparator();
                    }
                    else
                    {
                        var hbox = vbox.AddLayoutHorizontalLineOfText();
                        var FirstCol = data.FirstCol;
                        var LastCol = data.LastCol;
                        var SymbolCol = data.SymbolCol;

                        if (ColorCode.Keys.Any(FirstCol.EndsWith) || ColorCode.Keys.Any(LastCol.EndsWith) || ColorCode.Keys.Any(data.keyPressed.EndsWith) || ColorCode.Keys.Any(data.SymbolCol.EndsWith))
                        {
                            var colorFirstColEndsWith = ColorCode.Keys.Any(FirstCol.EndsWith) ? ColorCode[FirstCol.Substring(FirstCol.Length - 3)] : Color.White;
                            var colorLastColEndsWith = ColorCode.Keys.Any(LastCol.EndsWith) ? ColorCode[LastCol.Substring(LastCol.Length - 3)] : Color.White;
                            var colorKeyPressed = ColorCode.Keys.Any(data.keyPressed.EndsWith) ? ColorCode[data.keyPressed.Substring(data.keyPressed.Length - 3)] : Color.White;
                            var colorSymbolCol = ColorCode.Keys.Any(data.SymbolCol.EndsWith) ? ColorCode[data.SymbolCol.Substring(data.SymbolCol.Length - 3)] : Color.White;

                            // Erase the color code at the string end
                            FirstCol = ColorCode.Keys.Any(FirstCol.EndsWith) ? FirstCol.Substring(0, FirstCol.Length - 3) : FirstCol;
                            LastCol = ColorCode.Keys.Any(LastCol.EndsWith) ? LastCol.Substring(0, LastCol.Length - 3) : LastCol;
                            keyPressed = ColorCode.Keys.Any(data.keyPressed.EndsWith) ? data.keyPressed.Substring(0, data.keyPressed.Length - 3) : data.keyPressed;
                            SymbolCol = ColorCode.Keys.Any(data.SymbolCol.EndsWith) ? data.SymbolCol.Substring(0, data.SymbolCol.Length - 3) : data.SymbolCol;

                            // Apply color to FirstCol
                            if (StandardHUD)
                            {   // Apply color to FirstCol
                                hbox.Add(indicator = new Label(TextSize, hbox.RemainingHeight, keyPressed, LabelAlignment.Center));
                                indicator.Color = colorKeyPressed;
                                hbox.Add(indicator = new Label(colWidth, hbox.RemainingHeight, FirstCol));
                                indicator.Color = colorFirstColEndsWith;
                            }
                            else
                            {   // Use constant width font
                                hbox.Add(indicator = new Label(TextSize, hbox.RemainingHeight, keyPressed, LabelAlignment.Center));
                                indicator.Color = colorKeyPressed;
                                hbox.Add(indicatorMono = new LabelMono(colWidth, hbox.RemainingHeight, FirstCol));
                                indicatorMono.Color = colorFirstColEndsWith;
                            }

                            if (data.keyPressed != null && data.keyPressed != "")
                            {
                                hbox.Add(indicator = new Label(-TextSize, 0, TextSize, hbox.RemainingHeight, keyPressed, LabelAlignment.Right));
                                indicator.Color = colorKeyPressed;
                            }

                            if (data.SymbolCol != null && data.SymbolCol != "")
                            {
                                hbox.Add(indicator = new Label(-(TextSize + 3), 0, TextSize, hbox.RemainingHeight, SymbolCol, LabelAlignment.Right));
                                indicator.Color = colorSymbolCol;
                            }

                            // Apply color to LastCol
                            hbox.Add(indicator = new Label(colWidth, hbox.RemainingHeight, LastCol));
                            indicator.Color = colorFirstColEndsWith == Color.White ? colorLastColEndsWith : colorFirstColEndsWith;
                        }
                        else
                        {   // blanck space
                            keyPressed = "";
                            hbox.Add(indicator = new Label(TextSize, hbox.RemainingHeight, keyPressed, LabelAlignment.Center));
                            indicator.Color = Color.White; // Default color

                            //Avoids troubles when the Main Scale (Windows DPI settings) is not set to 100%
                            if (LastCol.Contains(':')) TimeHboxPositionY = hbox.Position.Y;

                            if (StandardHUD)
                            {
                                hbox.Add(indicator = new Label(colWidth, hbox.RemainingHeight, FirstCol));
                                indicator.Color = Color.White; // Default color
                            }
                            else
                            {
                                hbox.Add(indicatorMono = new LabelMono(colWidth, hbox.RemainingHeight, FirstCol));
                                indicatorMono.Color = Color.White; // Default color
                            }

                            // Font to bold, clickable label
                            if (hbox.Position.Y == TimeHboxPositionY && LastCol.Contains(':')) // Time line.
                            {
                                hbox.Add(LabelFontToBold = new Label(Owner.TextFontDefault.MeasureString(LastCol) - (StandardHUD ? 5 : 3), hbox.RemainingHeight, LastCol));
                                LabelFontToBold.Color = Color.White;
                                LabelFontToBold.Click += new Action<Control, Point>(FontToBold_Click);
                            }
                            else
                            {
                                hbox.Add(indicator = new Label(colWidth, hbox.RemainingHeight, LastCol));
                                indicator.Color = Color.White; // Default color
                            }
                        }

                        // Clickable symbol
                        if (hbox.Position.Y == TimeHboxPositionY)
                        {
                            hbox.Add(ExpandWindow = new Label(hbox.RemainingWidth - TextSize, 0, TextSize, hbox.RemainingHeight, expandWindow.ToString(), LabelAlignment.Right));
                            ExpandWindow.Color = Color.Yellow;
                            ExpandWindow.Click += new Action<Control, Point>(ExpandWindow_Click);
                        }
                        // Separator line
                        if (data.FirstCol.Contains("Sprtr"))
                        {
                            hbox.AddHorizontalSeparator();
                        }
                    }
                }
            }// close
            return vbox;
        }

        void FontToBold_Click(Control arg1, Point arg2)
        {
            FontToBold = FontToBold ? false : true;
        }

        void ExpandWindow_Click(Control arg1, Point arg2)
        {
            StandardHUD = StandardHUD ? false : true;
            //UpdateData();
            UpdateWindowSize();
        }

        private void UpdateWindowSize()
        {
            UpdateData();
            ModifyWindowSize();
        }

        /// <summary>
        /// Modify window size
        /// </summary>
        private void ModifyWindowSize()
        {
            if (ListToLabel.Count > 0)
            {
                var textwidth = Owner.TextFontDefault.Height;
                FirstColLenght = ListToLabel.Max(x => x.FirstColWidth);
                LastColLenght = ListToLabel.Max(x => x.LastColWidth);

                // Valid rows
                var rowCount = ListToLabel.Where(x => !string.IsNullOrWhiteSpace(x.FirstCol.ToString()) || !string.IsNullOrWhiteSpace(x.LastCol.ToString())).Count() - 1;
                var desiredHeight = FontToBold ? Owner.TextFontDefaultBold.Height * rowCount
                    : Owner.TextFontDefault.Height * rowCount;

                var desiredWidth = FirstColLenght + LastColLenght + 45;// interval between firstcol and lastcol

                var newHeight = (int)MathHelper.Clamp(desiredHeight, (StandardHUD ? WindowHeightMin : 100), WindowHeightMax);
                var newWidth = (int)MathHelper.Clamp(desiredWidth, (StandardHUD ? WindowWidthMin : 100), WindowWidthMax);

                // Move the dialog up if we're expanding it, or down if not; this keeps the center in the same place.
                var newTop = Location.Y + (Location.Height - newHeight) / 2;

                // Display window
                SizeTo(newWidth, newHeight);
                MoveTo(Location.X, newTop);
            }
        }

        /// <summary>
        /// Display info according to the full text window or the slim text window
        /// </summary>
        /// <param name="firstkeyactivated"></param>
        /// <param name="firstcol"></param>
        /// <param name="lastcol"></param>
        /// <param name="symbolcol"></param>
        /// <param name="changecolwidth"></param>
        /// <param name="lastkeyactivated"></param>
        private void InfoToLabel(string firstkeyactivated, string firstcol, string lastcol, string symbolcol, bool changecolwidth, string lastkeyactivated)
        {
            if (!UpdateDataEnded)
            {
                if (!StandardHUD)
                {
                    foreach (var code in FirstColToAbbreviated)
                    {
                        if (firstcol.Contains(code.Key))
                        {
                            firstcol = firstcol.Replace(code.Key, code.Value).TrimEnd();
                        }
                    }
                    foreach (var code in LastColToAbbreviated)
                    {
                        if (lastcol.Contains(code.Key))
                        {
                            lastcol = lastcol.Replace(code.Key, code.Value).TrimEnd();
                        }
                    }
                }

                var firstColWidth = 0;
                var lastColWidth = 0;

                if (!firstcol.Contains("Sprtr"))
                {

                    if (ColorCode.Keys.Any(firstcol.EndsWith))
                    {
                        var tempFirstCol = firstcol.Substring(0, firstcol.Length - 3);
                        firstColWidth = !StandardHUD ? Owner.TextFontMonoSpacedBold.MeasureString(tempFirstCol.TrimEnd())
                            : FontToBold ? Owner.TextFontDefaultBold.MeasureString(tempFirstCol.TrimEnd())
                            : Owner.TextFontDefault.MeasureString(tempFirstCol.TrimEnd());
                    }
                    else
                    {
                        firstColWidth = !StandardHUD ? Owner.TextFontMonoSpacedBold.MeasureString(firstcol.TrimEnd())
                            : FontToBold ? Owner.TextFontDefaultBold.MeasureString(firstcol.TrimEnd())
                            : Owner.TextFontDefault.MeasureString(firstcol.TrimEnd());
                    }

                    if (ColorCode.Keys.Any(lastcol.EndsWith))
                    {
                        var tempLastCol = lastcol.Substring(0, lastcol.Length - 3);
                        lastColWidth = FontToBold ? Owner.TextFontDefaultBold.MeasureString(tempLastCol.TrimEnd())
                            : Owner.TextFontDefault.MeasureString(tempLastCol.TrimEnd());
                    }
                    else
                    {
                        lastColWidth = FontToBold ? Owner.TextFontDefaultBold.MeasureString(lastcol.TrimEnd())
                            : Owner.TextFontDefault.MeasureString(lastcol.TrimEnd());
                    }

                    //Set a minimum value for LastColWidth to avoid overlap between time value and clickable symbol
                    if (ListToLabel.Count == 1)
                    {
                        lastColWidth = ListToLabel.First().LastColWidth + 15;// time value + clickable symbol
                    }

                    // Avoids text overlapping
                    firstColWidth = firstColWidth + 5;// avoids the symbol/keypressed from overlapping with the text
                }
                ListToLabel.Add(new ListLabel
                {
                    FirstCol = firstcol,
                    FirstColWidth = firstColWidth,
                    LastCol = lastcol,
                    LastColWidth = lastColWidth,
                    SymbolCol = symbolcol,
                    ChangeColWidth = changecolwidth,
                    keyPressed = keyPressed
                });

                //ResizeWindow, when the string spans over the right boundary of the window
                if (!ResizeWindow)
                {
                    if (maxFirstColWidth < firstColWidth) FirstColOverFlow = maxFirstColWidth;
                    if (maxLastColWidth < lastColWidth) LastColOverFlow = maxLastColWidth;
                    ResizeWindow = true;
                }
            }
            else
            {
                if (this.Visible)// Avoids conflict with WebApi data updating
                {
                    // Detect Autopilot is on to avoid flickering when slim window is displayed
                    var AutopilotOn = Owner.Viewer.PlayerLocomotive.Train.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING ? true : false;

                    //ResizeWindow, when the string spans over the right boundary of the window
                    maxFirstColWidth = ListToLabel.Max(x => x.FirstColWidth);
                    maxLastColWidth = ListToLabel.Max(x => x.LastColWidth);

                    if (!ResizeWindow & (FirstColOverFlow != maxFirstColWidth || (!AutopilotOn && LastColOverFlow != maxLastColWidth)))
                    {
                        LastColOverFlow = maxLastColWidth;
                        FirstColOverFlow = maxFirstColWidth;
                        ResizeWindow = true;
                    }
                }
            }
        }

        private void UpdateData()
        {   //Update data
            var arrowUp = '\u25B2';  // ▲
            var smallArrowUp = '\u25B3';  // △
            var arrowDown = '\u25BC';// ▼
            var smallArrowDown = '\u25BD';// ▽
            var end = '\u25AC';// Black rectangle ▬
            var endLower = '\u2596';// block ▖
            var arrowToRight = '\u25BA'; // ►
            var smallDiamond = '\u25C6'; // ●

            var playerTrain = Owner.Viewer.PlayerLocomotive.Train;
            var trainBrakeStatus = Owner.Viewer.PlayerLocomotive.GetTrainBrakeStatus();
            var dynamicBrakePercent = Owner.Viewer.PlayerLocomotive.DynamicBrakePercent;
            var dynamicBrakeStatus = Owner.Viewer.PlayerLocomotive.GetDynamicBrakeStatus();
            var engineBrakeStatus = Owner.Viewer.PlayerLocomotive.GetEngineBrakeStatus();
            var locomotive = Owner.Viewer.PlayerLocomotive as MSTSLocomotive;
            var locomotiveStatus = Owner.Viewer.PlayerLocomotive.GetStatus();
            var locomotiveSteam = Owner.Viewer.PlayerLocomotive as MSTSSteamLocomotive;
            var combinedControlType = locomotive.CombinedControlType == MSTSLocomotive.CombinedControl.ThrottleDynamic ? true : false;
            var showMUReverser = Math.Abs(playerTrain.MUReverserPercent) != 100f;
            var showRetainers = playerTrain.RetainerSetting != RetainerSetting.Exhaust;
            var stretched = playerTrain.Cars.Count > 1 && playerTrain.NPull == playerTrain.Cars.Count - 1;
            var bunched = !stretched && playerTrain.Cars.Count > 1 && playerTrain.NPush == playerTrain.Cars.Count - 1;
            var playerTrainInfo = Owner.Viewer.PlayerTrain.GetTrainInfo();
            expandWindow = '\u23FA';// ⏺ toggle window

            keyPressed = "";
            ListToLabel.Clear();
            UpdateDataEnded = false;

            if (!StandardHUD)
            {
                var newBrakeStatus = new StringBuilder(trainBrakeStatus);
                trainBrakeStatus = newBrakeStatus
                      .Replace(Viewer.Catalog.GetString("bar"), string.Empty)
                      .Replace(Viewer.Catalog.GetString("inHg"), string.Empty)
                      .Replace(Viewer.Catalog.GetString("kgf/cm²"), string.Empty)
                      .Replace(Viewer.Catalog.GetString("kPa"), string.Empty)
                      .Replace(Viewer.Catalog.GetString("psi"), string.Empty)
                      .Replace(Viewer.Catalog.GetString("lib./pal."), string.Empty)//cs locales
                      .Replace(Viewer.Catalog.GetString("pal.rtuti"), string.Empty)
                      .ToString();
            }

            // First Block
            // Client and server may have a time difference.
            keyPressed = "";
            InfoToLabel(keyPressed, Viewer.Catalog.GetString("Time"), FormatStrings.FormatTime(Owner.Viewer.Simulator.ClockTime + (MultiPlayer.MPManager.IsClient() ? Orts.MultiPlayer.MPManager.Instance().serverTimeDifference : 0)), "", false, keyPressed);
            if (Owner.Viewer.Simulator.IsReplaying)
            {
                InfoToLabel(keyPressed, Viewer.Catalog.GetString("Replay"), FormatStrings.FormatTime(Owner.Viewer.Log.ReplayEndsAt - Owner.Viewer.Simulator.ClockTime), "", false, keyPressed);
                keyPressed = "";
            }

            // Speed
            keyPressed = "";
            var speedColor = "";
            if (locomotive.SpeedMpS < playerTrainInfo.allowedSpeedMpS - 1f)
                speedColor = "!??";// White
            else if (locomotive.SpeedMpS < playerTrainInfo.allowedSpeedMpS)
                speedColor = "?!!";// PaleGreen
            else if (locomotive.SpeedMpS < playerTrainInfo.allowedSpeedMpS + 5f)
                speedColor = "!!?";// Orange
            else
                speedColor = "!!!";// Red
            InfoToLabel(keyPressed, Viewer.Catalog.GetString("Speed"), FormatStrings.FormatSpeedDisplay(Owner.Viewer.PlayerLocomotive.SpeedMpS, Owner.Viewer.PlayerLocomotive.IsMetric) + speedColor, "", false, keyPressed);

            // Gradient info
            if (StandardHUD)
            {
                float gradient = -playerTrainInfo.currentElevationPercent;
                if (gradient < -0.00015f)
                {
                    var c = '\u2198';
                    gradientIndicator = $"{gradient:F1}%{c + "$$$"}";// LightSkyBlue
                }
                else if (gradient > 0.00015f)
                {
                    var c = '\u2197';
                    gradientIndicator = $"{gradient:F1}%{c + "???"}";// Yellow
                }
                else
                    gradientIndicator = $"{gradient:F1}%";

                InfoToLabel(keyPressed, Viewer.Catalog.GetString("Gradient"), gradientIndicator, "", false, keyPressed);
                keyPressed = "";
            }

            // Separator
            InfoToLabel(keyPressed, "Sprtr", "", "", false, keyPressed);
            keyPressed = "";

            // Second block
            // Direction
            {
                UserCommand? reverserCommand = GetPressedKey(UserCommand.ControlBackwards, UserCommand.ControlForwards);
                if (reverserCommand == UserCommand.ControlBackwards || reverserCommand == UserCommand.ControlForwards)
                {
                    bool moving = Math.Abs(playerTrain.SpeedMpS) > 1;
                    bool nonSteamEnd = locomotive.EngineType != TrainCar.EngineTypes.Steam && locomotive.Direction == Direction.N && (locomotive.ThrottlePercent >= 1 || moving);
                    bool steamEnd = locomotive is MSTSSteamLocomotive steamLocomotive2 && steamLocomotive2.CutoffController.MaximumValue == Math.Abs(playerTrain.MUReverserPercent / 100);

                    if (reverserCommand != null && (nonSteamEnd || steamEnd))
                        keyPressed = end.ToString() + "???";
                    else if (reverserCommand == UserCommand.ControlBackwards)
                        keyPressed = arrowDown.ToString() + "???";
                    else if (reverserCommand == UserCommand.ControlForwards)
                        keyPressed = arrowUp.ToString() + "???";
                    else
                        keyPressed = "";
                }
                var reverserIndicator = showMUReverser ? $"{Round(Math.Abs(playerTrain.MUReverserPercent))}% " : "";
                InfoToLabel(keyPressed, Viewer.Catalog.GetString(locomotive.EngineType == TrainCar.EngineTypes.Steam ? "Reverser" : "Direction"),
                    $"{reverserIndicator}{FormatStrings.Catalog.GetParticularString("Reverser", GetStringAttribute.GetPrettyName(locomotive.Direction))}", "", false, keyPressed);
                keyPressed = "";
            }

            // Throttle
            {
                UserCommand? throttleCommand = GetPressedKey(UserCommand.ControlThrottleDecrease, UserCommand.ControlThrottleIncrease);
                bool upperLimit = throttleCommand == UserCommand.ControlThrottleIncrease && locomotive.ThrottleController.MaximumValue == locomotive.ThrottlePercent / 100;
                bool lowerLimit = throttleCommand == UserCommand.ControlThrottleDecrease && locomotive.ThrottlePercent == 0;

                if (locomotive.DynamicBrakePercent < 1 && (upperLimit || lowerLimit))
                {
                    keyPressed = end.ToString() + "???";
                }
                else if (locomotive.DynamicBrakePercent > -1)
                {
                    keyPressed = endLower.ToString() + "???";
                }
                else if (throttleCommand == UserCommand.ControlThrottleIncrease)
                {
                    keyPressed = arrowUp.ToString() + "???";
                }
                else if (throttleCommand == UserCommand.ControlThrottleDecrease)
                {
                    keyPressed = arrowDown.ToString() + "???";
                }
                else
                    keyPressed = "";

                InfoToLabel(keyPressed, Viewer.Catalog.GetString(locomotive is MSTSSteamLocomotive ? "Regulator" : "Throttle"), locomotive.ThrottlePercent.ToString("0") + "%", "", false, keyPressed);
                keyPressed = "";
            }

            // Cylinder Cocks
            if (locomotive is MSTSSteamLocomotive steamLocomotive)
            {
                if (steamLocomotive.CylinderCocksAreOpen)
                {
                    keyPressed = arrowToRight.ToString() + "???";
                    InfoToLabel(keyPressed, Viewer.Catalog.GetString("Cylinder cocks"), Viewer.Catalog.GetString("Open") + "!!?", "", false, keyPressed);
                }
                else{
                    keyPressed = "";
                    InfoToLabel(keyPressed, Viewer.Catalog.GetString("Cylinder cocks"), Viewer.Catalog.GetString("Closed") + "!??", "", false, keyPressed);
                }
            }

            // Sander
            keyPressed = UserInput.IsDown(UserCommand.ControlSander) || UserInput.IsDown(UserCommand.ControlSanderToggle) ? arrowDown.ToString() + "???" : " ";
            if (locomotive.GetSanderOn())
            {
                var sanderBlocked = locomotive is MSTSLocomotive && Math.Abs(playerTrain.SpeedMpS) > locomotive.SanderSpeedOfMpS;
                keyPressed = sanderBlocked ? "" : arrowToRight.ToString() + "???";
                InfoToLabel(keyPressed, Viewer.Catalog.GetString("Sander"), sanderBlocked ? Viewer.Catalog.GetString("Blocked") + "!!!": Viewer.Catalog.GetString("On") + "!!?", "", StandardHUD ? true : false, keyPressed);
            }
            else
            {
                keyPressed = "";
                InfoToLabel(keyPressed, Viewer.Catalog.GetString("Sander"), Viewer.Catalog.GetString("Off"), "", false, keyPressed);
            }

            InfoToLabel("", "Sprtr", "", "", false, keyPressed);

            // Train Brake multi-lines
            // TO DO: A better algorithm
            //steam loco
            keyPressed = UserInput.IsDown(UserCommand.ControlTrainBrakeDecrease) ? arrowDown.ToString() + "???" : UserInput.IsDown(UserCommand.ControlTrainBrakeIncrease) ? arrowUp.ToString() + "???" : "";

            var brakeInfoValue = "";
            var index = 0;

            if (trainBrakeStatus.Contains(Viewer.Catalog.GetString("EQ")))
            {
                brakeInfoValue = trainBrakeStatus.Substring(0, trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("EQ"))).TrimEnd();
                InfoToLabel(keyPressed, Viewer.Catalog.GetString("Train brake"), brakeInfoValue + "%%%", "", false, keyPressed);
                keyPressed = "";
                index = trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("EQ"));
                brakeInfoValue = trainBrakeStatus.Substring(index, trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("BC")) - index).TrimEnd();

                InfoToLabel(keyPressed, "", brakeInfoValue, "", false, keyPressed);
                keyPressed = "";
                if (trainBrakeStatus.Contains(Viewer.Catalog.GetString("EOT")))
                {
                    var IndexOffset = Viewer.Catalog.GetString("EOT").Length + 1;
                    index = trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("BC"));
                    brakeInfoValue = trainBrakeStatus.Substring(index, trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("EOT")) - index).TrimEnd();
                    keyPressed = "";
                    InfoToLabel(keyPressed, "", brakeInfoValue, "", false, keyPressed);
                    keyPressed = "";
                    index = trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("EOT")) + IndexOffset;
                    brakeInfoValue = trainBrakeStatus.Substring(index, trainBrakeStatus.Length - index).TrimStart();
                    keyPressed = "";
                    InfoToLabel(keyPressed, "", brakeInfoValue, "", false, keyPressed);
                    keyPressed = "";
                }
                else
                {
                    index = trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("BC"));
                    brakeInfoValue = trainBrakeStatus.Substring(index, trainBrakeStatus.Length - index).TrimEnd();
                    keyPressed = "";
                    InfoToLabel(keyPressed, "", brakeInfoValue, "", false, keyPressed);
                    keyPressed = "";
                }
            }
            else if (trainBrakeStatus.Contains(Viewer.Catalog.GetString("Lead")))
            {
                var IndexOffset = Viewer.Catalog.GetString("Lead").Length + 1;
                brakeInfoValue = trainBrakeStatus.Substring(0, trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("Lead"))).TrimEnd();
                InfoToLabel(keyPressed, Viewer.Catalog.GetString("Train brake"), brakeInfoValue + "%%%", "", false, keyPressed);

                keyPressed = "";
                index = trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("Lead")) + IndexOffset;
                if (trainBrakeStatus.Contains(Viewer.Catalog.GetString("EOT")))
                {
                    brakeInfoValue = trainBrakeStatus.Substring(index, trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("EOT")) - index).TrimEnd();
                    InfoToLabel(keyPressed, "", brakeInfoValue, "", false, keyPressed);

                    keyPressed = "";
                    index = trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("EOT")) + IndexOffset;
                    brakeInfoValue = trainBrakeStatus.Substring(index, trainBrakeStatus.Length - index).TrimEnd();
                    InfoToLabel(keyPressed, "", brakeInfoValue, "", false, keyPressed);
                }
                else
                {
                    brakeInfoValue = trainBrakeStatus.Substring(index, trainBrakeStatus.Length - index).TrimEnd();
                    InfoToLabel(keyPressed, "", brakeInfoValue, "", false, keyPressed);
                }
            }
            else if (trainBrakeStatus.Contains(Viewer.Catalog.GetString("BC")))
            {
                brakeInfoValue = trainBrakeStatus.Substring(0, trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("BC"))).TrimEnd();
                InfoToLabel(keyPressed, Viewer.Catalog.GetString("Train brake"), brakeInfoValue + "%%%", "", false, keyPressed);

                keyPressed = "";
                index = trainBrakeStatus.IndexOf(Viewer.Catalog.GetString("BC"));
                brakeInfoValue = trainBrakeStatus.Substring(index, trainBrakeStatus.Length - index).TrimEnd();

                keyPressed = "";
                InfoToLabel(keyPressed, "", brakeInfoValue, "", false, keyPressed);
                keyPressed = "";
            }

            keyPressed = "";
            if (showRetainers)
                InfoToLabel(keyPressed, Viewer.Catalog.GetString("Retainers"), (playerTrain.RetainerPercent + " " + Viewer.Catalog.GetString(GetStringAttribute.GetPrettyName(playerTrain.RetainerSetting))), "", false, keyPressed);

            keyPressed = "";
            if ((Owner.Viewer.PlayerLocomotive as MSTSLocomotive).EngineBrakeFitted) // ideally this test should be using "engineBrakeStatus != null", but this currently does not work, as a controller is defined by default
            {
            }
            keyPressed = UserInput.IsDown(UserCommand.ControlEngineBrakeDecrease) ? arrowDown.ToString() + "???" : UserInput.IsDown(UserCommand.ControlEngineBrakeIncrease) ? arrowUp.ToString() + "???" : "";
            if (engineBrakeStatus.Contains(Viewer.Catalog.GetString("BC")))
            {
                InfoToLabel(keyPressed, Viewer.Catalog.GetString("Engine brake"), engineBrakeStatus.Substring(0, engineBrakeStatus.IndexOf("BC")) + "%%%", "", false, keyPressed);
                keyPressed = "";
                index = engineBrakeStatus.IndexOf(Viewer.Catalog.GetString("BC"));
                brakeInfoValue = engineBrakeStatus.Substring(index, engineBrakeStatus.Length - index).TrimEnd();
                InfoToLabel(keyPressed, Viewer.Catalog.GetString(""), brakeInfoValue + "!??", "", false, keyPressed);
            }
            else
                InfoToLabel(keyPressed, Viewer.Catalog.GetString("Engine brake"), engineBrakeStatus + "%%%", "", false, keyPressed);

            keyPressed = "";
            if ( dynamicBrakeStatus != null && locomotive.IsLeadLocomotive())
            {
                if (!DynBrakeSetup && (UserInput.IsDown(UserCommand.ControlDynamicBrakeIncrease) && dynamicBrakePercent == 0)
                    || (combinedControlType && UserInput.IsDown(UserCommand.ControlThrottleDecrease) && Owner.Viewer.PlayerLocomotive.ThrottlePercent == 0 && dynamicBrakeStatus == "0%"))
                {
                    StartTime = locomotive.DynamicBrakeCommandStartTime + locomotive.DynamicBrakeDelayS;
                    DynBrakeSetup = true;
                    keyPressed = arrowToRight.ToString() + "???";
                    InfoToLabel(keyPressed, Viewer.Catalog.GetString("Dynamic brake"), Viewer.Catalog.GetString("Setup") + "%%%", "", false, keyPressed);
                }
                else if (DynBrakeSetup && StartTime < Owner.Viewer.Simulator.ClockTime)
                {
                    DynBrakeSetup = false;
                    InfoToLabel(keyPressed, Viewer.Catalog.GetString("Dynamic brake"), dynamicBrakePercent + "% " + "%%%", "", false, keyPressed);
                }
                else if (DynBrakeSetup && StartTime > Owner.Viewer.Simulator.ClockTime)
                {
                    keyPressed = arrowToRight.ToString() + "???";
                    InfoToLabel(keyPressed, Viewer.Catalog.GetString("Dynamic brake"), Viewer.Catalog.GetString("Setup") + "%%%", "", false, keyPressed);
                }
                else if (!DynBrakeSetup && dynamicBrakePercent > -1)
                {
                    if (combinedControlType)
                    {
                        keyPressed = UserInput.IsDown(UserCommand.ControlThrottleIncrease) || UserInput.IsDown(UserCommand.ControlDynamicBrakeDecrease) ? arrowDown.ToString() + "???"
                           : UserInput.IsDown(UserCommand.ControlThrottleDecrease) || UserInput.IsDown(UserCommand.ControlDynamicBrakeIncrease) ? arrowUp.ToString() + "???"
                           : "";
                    }
                    else
                    {
                        keyPressed = UserInput.IsDown(UserCommand.ControlDynamicBrakeDecrease) ? arrowDown.ToString() + "???"
                            : UserInput.IsDown(UserCommand.ControlDynamicBrakeIncrease) ? arrowUp.ToString() + "???"
                            : "";
                    }
                    InfoToLabel(keyPressed, Viewer.Catalog.GetString("Dynamic brake"), dynamicBrakeStatus + "%%%", "", false, keyPressed);
                }
                else if (dynamicBrakeStatus == "" && dynamicBrakePercent < 0)
                {
                    InfoToLabel(keyPressed, Viewer.Catalog.GetString("Dynamic brake"), Viewer.Catalog.GetString("Off"), "", false, keyPressed);
                }
            }
            keyPressed = "";
            InfoToLabel(keyPressed, "Sprtr", "", "", false, keyPressed);

            if (locomotiveStatus != null)
            {
                foreach (var data in locomotiveStatus.Split('\n').Where((string d) => !string.IsNullOrWhiteSpace(d)))
                {
                    var parts = data.Split(new[] { " = " }, 2, StringSplitOptions.None);
                    var HeatColor = "!??"; // Color.White
                    keyPressed = "";
                    if (!StandardHUD && Viewer.Catalog.GetString(parts[0]).StartsWith(Viewer.Catalog.GetString("Steam usage")))
                    {
                    }
                    else if (Viewer.Catalog.GetString(parts[0]).StartsWith(Viewer.Catalog.GetString("Boiler pressure")))
                    {
                        MSTSSteamLocomotive steamloco = (MSTSSteamLocomotive)Owner.Viewer.PlayerLocomotive;
                        var bandUpper = steamloco.PreviousBoilerHeatOutBTUpS * 1.025f; // find upper bandwidth point
                        var bandLower = steamloco.PreviousBoilerHeatOutBTUpS * 0.975f; // find lower bandwidth point - gives a total 5% bandwidth

                        if (steamloco.BoilerHeatInBTUpS > bandLower && steamloco.BoilerHeatInBTUpS < bandUpper) HeatColor = smallDiamond.ToString() + "!??";
                        else if (steamloco.BoilerHeatInBTUpS < bandLower) HeatColor = smallArrowDown.ToString() + "%%%"; // Color.Cyan
                        else if (steamloco.BoilerHeatInBTUpS > bandUpper) HeatColor = smallArrowUp.ToString() + "!!?"; // Color.Orange

                        keyPressed = "";
                        InfoToLabel(keyPressed, Viewer.Catalog.GetString("Boiler pressure"), Viewer.Catalog.GetString(parts[1]), HeatColor, false, keyPressed);
                    }
                    else if (!StandardHUD && Viewer.Catalog.GetString(parts[0]).StartsWith(Viewer.Catalog.GetString("Fuel levels")))
                    {
                        keyPressed = "";
                        InfoToLabel(keyPressed, parts[0].EndsWith("?") || parts[0].EndsWith("!") ? Viewer.Catalog.GetString(parts[0].Substring(0, parts[0].Length - 3)) : Viewer.Catalog.GetString(parts[0]), (parts.Length > 1 ? Viewer.Catalog.GetString(parts[1].Replace(" ", string.Empty)) : ""), "", false, keyPressed);
                    }
                    else if (parts[0].StartsWith(Viewer.Catalog.GetString("Gear")))
                    {
                        keyPressed = UserInput.IsDown(UserCommand.ControlGearDown) ? arrowDown.ToString() + "???" : UserInput.IsDown(UserCommand.ControlGearUp) ? arrowUp.ToString() + "???" : "";
                        InfoToLabel(keyPressed, Viewer.Catalog.GetString(parts[0]), (parts.Length > 1 ? Viewer.Catalog.GetString(parts[1]) : ""), "", false, keyPressed);
                        keyPressed = "";
                    }
                    else if (parts.Contains(Viewer.Catalog.GetString("Pantographs")))
                    {
                        keyPressed = UserInput.IsDown(UserCommand.ControlPantograph1) ? parts[1].StartsWith(Viewer.Catalog.GetString("Up")) ? arrowUp.ToString() + "???" : arrowDown.ToString() + "???" : "";
                        InfoToLabel(keyPressed, Viewer.Catalog.GetString(parts[0]), (parts.Length > 1 ? Viewer.Catalog.GetString(parts[1]) : ""), "", false, keyPressed);
                        keyPressed = "";
                    }
                    else if (parts.Contains(Viewer.Catalog.GetString("Engine")))
                    {
                        keyPressed = "";
                        InfoToLabel(keyPressed, Viewer.Catalog.GetString(parts[0]), (parts.Length > 1 ? Viewer.Catalog.GetString(parts[1]) + "!??" : ""), "", false, keyPressed);
                        keyPressed = "";
                    }
                    else
                    {
                        InfoToLabel("", parts[0].EndsWith("?") || parts[0].EndsWith("!") ? Viewer.Catalog.GetString(parts[0].Substring(0, parts[0].Length - 3)) : Viewer.Catalog.GetString(parts[0]), (parts.Length > 1 ? Viewer.Catalog.GetString(parts[1]) : ""), "", false, keyPressed);
                    }
                }
            }

            keyPressed = "";
            InfoToLabel(keyPressed, "Sprtr", "", "", true, keyPressed);

            // Control Cruise
            if ((Owner.Viewer.PlayerLocomotive as MSTSLocomotive).CruiseControl != null)
            {
                var cc = (Owner.Viewer.PlayerLocomotive as MSTSLocomotive).CruiseControl;
                InfoToLabel(keyPressed, Viewer.Catalog.GetString("CCStatus"), cc.SpeedRegMode.ToString() + "%%%", "", false, keyPressed);
                if (cc.SpeedRegMode == Simulation.RollingStocks.SubSystems.CruiseControl.SpeedRegulatorMode.Auto)
                {
                    InfoToLabel(keyPressed, Viewer.Catalog.GetString("Target"), 
                        FormatStrings.FormatSpeedDisplay(cc.SelectedSpeedMpS, Owner.Viewer.PlayerLocomotive.IsMetric) + "%%%", "", false, keyPressed);
                    InfoToLabel(keyPressed, Viewer.Catalog.GetString("MaxAccel"), (cc.SpeedRegulatorMaxForcePercentUnits ? cc.SelectedMaxAccelerationPercent.ToString("0") + "% " :
                        Math.Round((cc.MaxForceSelectorIsDiscrete ? (int)cc.SelectedMaxAccelerationStep : cc.SelectedMaxAccelerationStep) * 100 / cc.SpeedRegulatorMaxForceSteps).ToString("0") + "% ") + "%%%", "", false, keyPressed);
                }
                keyPressed = "";
                InfoToLabel(keyPressed, "Sprtr", "", "", true, keyPressed);
            }

            keyPressed = "";
            if (StandardHUD)
                InfoToLabel(keyPressed, Viewer.Catalog.GetString("FPS"), Owner.Viewer.RenderProcess.FrameRate.SmoothedValue.ToString("F0"), "", false, keyPressed);

            // Messages
            // Autopilot
            keyPressed = "";

            if (Owner.Viewer.PlayerLocomotive.Train.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING)
            {
                keyPressed = UserInput.IsDown(UserCommand.GameAutopilotMode) ? arrowUp.ToString() + "???" : "";
                InfoToLabel(keyPressed, Viewer.Catalog.GetString("Autopilot"), Viewer.Catalog.GetString("On") + "???", "", false, keyPressed);
            }
            else if (Owner.Viewer.PlayerLocomotive.Train.TrainType != Train.TRAINTYPE.AI_PLAYERHOSTING)
            {
                keyPressed = UserInput.IsDown(UserCommand.GameAutopilotMode) ? arrowDown.ToString() + "???" : "";
                InfoToLabel(keyPressed, Viewer.Catalog.GetString("Autopilot"), Viewer.Catalog.GetString("Off"), "", false, keyPressed);
            }
            else
                InfoToLabel("", Viewer.Catalog.GetString("Autopilot"), Viewer.Catalog.GetString("Off"), "", false, keyPressed);

            //AI Fireman
            if (locomotive is MSTSSteamLocomotive)
            {
                keyPressed = "";
                if (UserInput.IsDown(UserCommand.ControlAIFireOn))
                {
                    ctrlAIFiremanReset = ctrlAIFiremanOff = false;
                    ctrlAIFiremanOn = true;
                    keyPressed = arrowToRight.ToString() + "???";
                }
                else if (UserInput.IsDown(UserCommand.ControlAIFireOff))
                {
                    ctrlAIFiremanReset = ctrlAIFiremanOn = false;
                    ctrlAIFiremanOff = true;
                    keyPressed = arrowToRight.ToString() + "???";
                }
                else if (UserInput.IsDown(UserCommand.ControlAIFireReset))
                {
                    ctrlAIFiremanOn = ctrlAIFiremanOff = false;
                    ctrlAIFiremanReset = true;
                    clockAIFireTime = Owner.Viewer.Simulator.ClockTime;
                    keyPressed = arrowToRight.ToString() + "???";
                }

                // waiting time to hide the reset label
                if (ctrlAIFiremanReset && clockAIFireTime + 3 < Owner.Viewer.Simulator.ClockTime)
                    ctrlAIFiremanReset = false;

                if (ctrlAIFiremanOn || ctrlAIFiremanOff || ctrlAIFiremanReset)
                {
                    InfoToLabel(keyPressed, Viewer.Catalog.GetString("AI Fireman") + "!??", ctrlAIFiremanOn ? Viewer.Catalog.GetString("On") + "!??" : ctrlAIFiremanOff ? Viewer.Catalog.GetString("Off") + "!??" : ctrlAIFiremanReset ? Viewer.Catalog.GetString("Reset") + "%%%" : "-", "", false, keyPressed);
                }
            }

            // Grate limit
            keyPressed = "";
            if (locomotive.GetType() == typeof(MSTSSteamLocomotive))
            {
                MSTSSteamLocomotive steamloco = (MSTSSteamLocomotive)Owner.Viewer.PlayerLocomotive;
                if (steamloco.IsGrateLimit && steamloco.GrateCombustionRateLBpFt2 > steamloco.GrateLimitLBpFt2)
                {
                    grateLabelVisible = true;
                    clockGrateTime = Owner.Viewer.Simulator.ClockTime;
                    InfoToLabel("", Viewer.Catalog.GetString("Grate limit"), Viewer.Catalog.GetString("Exceeded") + "!!!", "", false, keyPressed);
                }
                else
                {
                    // delay to hide the grate label
                    if (grateLabelVisible && clockGrateTime + 3 < Owner.Viewer.Simulator.ClockTime)
                        grateLabelVisible = false;

                    if (grateLabelVisible)
                    {
                        InfoToLabel("", Viewer.Catalog.GetString("Grate limit") + "!??", Viewer.Catalog.GetString("Normal") + "!??", "", false, keyPressed);
                    }
                }
            }

            // Wheel
            if (playerTrain.IsWheelSlip || playerTrain.IsWheelSlipWarninq || playerTrain.IsBrakeSkid)
            {
                wheelLabelVisible = true;
                clockWheelTime = Owner.Viewer.Simulator.ClockTime;
            }
            keyPressed = "";
            if (playerTrain.IsWheelSlip)
                InfoToLabel("", Viewer.Catalog.GetString("Wheel"), Viewer.Catalog.GetString("slip") + "!!!", "", false, keyPressed);
            else if (playerTrain.IsWheelSlipWarninq)
                InfoToLabel("", Viewer.Catalog.GetString("Wheel"), Viewer.Catalog.GetString("slip warning") + "???", "", false, keyPressed);
            else if (playerTrain.IsBrakeSkid)
                InfoToLabel("", Viewer.Catalog.GetString("Wheel"), Viewer.Catalog.GetString("skid") + "!!!", "", false, keyPressed);
            else
            {
                // delay to hide the wheel label
                if (wheelLabelVisible && clockWheelTime + 3 < Owner.Viewer.Simulator.ClockTime)
                    wheelLabelVisible = false;

                if (wheelLabelVisible)
                {
                    InfoToLabel("", Viewer.Catalog.GetString("Wheel") + "!??", Viewer.Catalog.GetString("Normal") + "!??", "", false, keyPressed);
                }
            }

            // Doors
            keyPressed = "";
            var wagon = (MSTSWagon) locomotive;
            if (wagon.DoorLeftOpen || wagon.DoorRightOpen)
            {
                var status = new List<string>();
                bool flipped = locomotive.GetCabFlipped();
                doorsLabelVisible = true;
                clockDoorsTime = Owner.Viewer.Simulator.ClockTime;
                if (wagon.DoorLeftOpen)
                    status.Add(flipped ? Viewer.Catalog.GetString("Right"): Viewer.Catalog.GetString("Left"));
                if (wagon.DoorRightOpen)
                    status.Add(flipped ? Viewer.Catalog.GetString("Left") : Viewer.Catalog.GetString("Right"));

                InfoToLabel(" ", Viewer.Catalog.GetString("Doors open"), string.Join(" ", status) + (locomotive.AbsSpeedMpS > 0.1f ? "!!!" : "???"), "", false, keyPressed);
            }
            else
            {
                // delay to hide the doors label
                if (doorsLabelVisible && clockDoorsTime + 3 < Owner.Viewer.Simulator.ClockTime)
                    doorsLabelVisible = false;

                if (doorsLabelVisible)
                {
                    InfoToLabel(" ", Viewer.Catalog.GetString("Doors open") + "!??", Viewer.Catalog.GetString("Closed") + "!??", "", false, keyPressed);
                }
            }

            // Ctrl + F Firing to manual
            if (UserInput.IsDown(UserCommand.ControlFiring))
            {
                ResizeWindow = true;
            }

            UpdateDataEnded = true;
            keyPressed = "";
            InfoToLabel(keyPressed, "", "", "", true, keyPressed);
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            var MovingCurrentWindow = UserInput.IsMouseLeftButtonDown &&
                   UserInput.MouseX >= Location.X && UserInput.MouseX <= Location.X + Location.Width &&
                   UserInput.MouseY >= Location.Y && UserInput.MouseY <= Location.Y + Location.Height ?
                   true : false;

            // Avoid to updateFull when the window is moving
            if (!MovingCurrentWindow && !TrainDrivingUpdating && updateFull)
            {
                TrainDrivingUpdating = true;
                UpdateData();
                TrainDrivingUpdating = false;

                // Ctrl + F (FiringIsManual)
                if (ResizeWindow || LinesCount != ListToLabel.Count())
                {
                    ResizeWindow = false;
                    UpdateWindowSize();
                    LinesCount = ListToLabel.Count();
                }

                //Update Layout
                Layout();
            }
        }

        private static string Round(float x) => $"{Math.Round(x):F0}";

        private static UserCommand? GetPressedKey(params UserCommand[] keysToTest) => keysToTest
            .Where((UserCommand key) => UserInput.IsDown(key))
            .FirstOrDefault();
    }
}