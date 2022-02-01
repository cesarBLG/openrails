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

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Simulation;
using Orts.Simulation.AIs;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Common;
using ORTS.Common;
using ORTS.Common.Input;

namespace Orts.Viewer3D.Popups
{
    public class EOTListWindow : Window
    {
        public EOTListWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 20, Window.DecorationSize.Y + (owner.Viewer.Simulator.SharedEOTData == null ?
                  owner.TextFontDefault.Height * 2 : owner.TextFontDefault.Height * (owner.Viewer.Simulator.SharedEOTData.Count + 2)), Viewer.Catalog.GetString("EOT List"))
        {
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var vbox = base.Layout(layout).AddLayoutVertical();
            if (Owner.Viewer.Simulator.SharedEOTData != null)
            {
                var colWidth = (vbox.RemainingWidth - vbox.TextHeight * 2) / 3;
                {
                    var line = vbox.AddLayoutHorizontalLineOfText();
                    line.Add(new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString("Filename")));
                    line.Add(new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString("Folder Name"), LabelAlignment.Left));
                    line.Add(new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString("Category"), LabelAlignment.Right));
                }
                vbox.AddHorizontalSeparator();
                var scrollbox = vbox.AddLayoutScrollboxVertical(vbox.RemainingWidth);
 
                foreach (var thisEOTType in Owner.Viewer.Simulator.SharedEOTData)
                {
                    var line = scrollbox.AddLayoutHorizontalLineOfText();
                    EOTLabel filename, foldername, category;
                    line.Add(filename = new EOTLabel(colWidth, line.RemainingHeight, Owner.Viewer, thisEOTType, thisEOTType.EOTName, LabelAlignment.Left));
                    line.Add(foldername = new EOTLabel(colWidth - Owner.TextFontDefault.Height, line.RemainingHeight, Owner.Viewer, thisEOTType, thisEOTType.EOTDirectory, LabelAlignment.Left));
                    line.Add(category = new EOTLabel(colWidth, line.RemainingHeight, Owner.Viewer, thisEOTType, "*", LabelAlignment.Right));
                    if (Owner.Viewer.Simulator.PlayerLocomotive?.Train != null && thisEOTType.EOTName == Owner.Viewer.Simulator.PlayerLocomotive.Train.EOTType.EOTName
                        && thisEOTType.EOTDirectory == Owner.Viewer.Simulator.PlayerLocomotive.Train.EOTType.EOTDirectory)
                    {                       
                        filename.Color = Color.Red;
                        foldername.Color = Color.Red;
                        category.Color = Color.Red;
                    }
                }
             }
            return vbox;
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull && Owner.Viewer.Simulator.SharedEOTData != null)
            {
                Layout();
            }
        }
    }

    class EOTLabel : Label
    {
        readonly Viewer Viewer;
        readonly EOTType PickedEOTTypeFromList;

        public EOTLabel(int width, int height, Viewer viewer, EOTType eotType, String eotString, LabelAlignment alignment)
            : base(width, height, eotString, alignment)
        {
            Viewer = viewer;
            PickedEOTTypeFromList = eotType;
            Click += new Action<Control, Point>(EOTListLabel_Click);
        }

        void EOTListLabel_Click(Control arg1, Point arg2)
        {
            if (PickedEOTTypeFromList.EOTName != "" && Viewer.PlayerLocomotive.AbsSpeedMpS > Simulator.MaxStoppedMpS)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Can't attach EOT if player train not stopped"));
                return;
            }
            if (PickedEOTTypeFromList.EOTName != "")
            {
                if (!(PickedEOTTypeFromList.EOTName == Viewer.Simulator.PlayerLocomotive.Train.EOTType.EOTName
                        && PickedEOTTypeFromList.EOTDirectory == Viewer.Simulator.PlayerLocomotive.Train.EOTType.EOTDirectory))
                {
                    if (PickedEOTTypeFromList.EOTName != "" && Viewer.PlayerLocomotive.Train?.EOT != null)
                    {
                        Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Player train already has a mounted EOT"));
                        return;
                    }
                    //Ask to mount EOT
                    new EOTMountCommand(Viewer.Log, true, PickedEOTTypeFromList);
                }
                else if (PickedEOTTypeFromList.EOTName == Viewer.Simulator.PlayerLocomotive.Train.EOTType.EOTName
                        && PickedEOTTypeFromList.EOTDirectory == Viewer.Simulator.PlayerLocomotive.Train.EOTType.EOTDirectory)
                {
                    new EOTMountCommand(Viewer.Log, false, PickedEOTTypeFromList);
                }
                else
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Can't mount an EOT if another one is mounted"));
                    return;
                }
            }
        }
    }
 }
