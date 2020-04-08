Open Rails, Monogame version (unofficial) README - Release NewYear - Rev.57
April 8th, 2020

Please note that the installation and use of Open Rails software, even of its unofficial versions, is governed by the Open Rails End User License Agreement. 

INSTALLATION
- the requirements for installation of the official Open Rails version apply, with the precisions of next lines
- XNA 3.1 Redistributable is not needed
- you must have at least a Windows Vista computer. Windows XP is not supported
- start openrails simply by clicking on Openrails.exe
- don't try to update the pack by using the link on the upper right side of the main menu window: 
you would return to the official OR version.

RELEASE NOTES
This unofficial version has been derived from the latest official Open Rails unstable revision U2020.04.06-1151 (which already includes Monogame)
and from the latest official OpenRails testing revision X1.3.1-124.
It includes some features not (yet) available in the Open Rails unstable official version, that is:
- addition of track sounds in the sound debug window (by dennisat)
- F5 HUD scrolling (by mbm_or)
- checkbox in General Options tab to enable or disable watchdog
- increase of remote horn sound volume level
- when car is selected through the F9 window, the car's brake line in the extended brake HUD is highlighted in yellow (by mbm_or)
- improved distance management in roadcar camera
- signal script parser (by perpetualKid): reduces CPU time needed for signal management
- true 64-bit management, allowing to use more than 4 GB of memory, if available, in Win64 systems (mainly by perpetualKid)
- general options checkbox for optional run at 32 bit on Win64 (to avoid slight train shaking bug)
- translatable Train Driving Info window (see http://www.elvastower.com/forums/index.php?/topic/33401-f5-hud-scrolling/page__view__findpost__p__251671 and following posts), by mbm_OR
- extended Raildriver setup, as present in OR Ultimate (by perpetualKid)
- Simple Control and Physics option as default
- analog clocks showing game time, see http://www.elvastower.com/forums/index.php?/topic/29546-station-clocks/ , by jonas
- bug fix for bad mouse pointing in full screen mode and for issues with Fast full screen-Alt-Tab mode
- operation as webserver, enabling HUD and Train Driving info on web clients like PCs, tablets or smartphones, by mbm_OR, cjakeman and BillC
- support for AWS train control system
- panto commands and animations now swapped when in rear cab
- support for Italian train control systems SCMT and TCS
- improvements in Activity Evaluation of station stops, by mbm_OR
- NEW: tentative correction to reduce transfer flickering at short distance
- NEW: ORTS_SIGNED_TRACTION_BRAKING control (see http://www.elvastower.com/forums/index.php?/topic/33883-signed-traction-braking-cabview-control/ )
- NEW: enable script search for distance signals in backward direction. 



CREDITS
This unofficial version couldn't have been created without following contributions:
- the whole Open Rails Development Team and Open Rails Management Team, that have generated the official Open Rails version
- the Monogame Development Team
- Peter Gulyas, who created the first Monogame version of Open Rails
- perpetualKid
- Dennis A T (dennisat)
- Mauricio (mbm_OR)
- Peter Newell (steamer_ctn)
- Rob Roeterdink (roeter)
- jonas
- Carlo Santucci

- all those who contributed with ideas and provided contents for testing and pointed to malfunctions.

DISCLAIMER
No testing on a broad base of computer configurations and of contents has been done. Therefore, in addition
to the disclaimers valid also for the official Open Rails version, 
the above named persons keep no responsibility, including on malfunctions, damages, losses of data or time.
It is reminded that Open Rails is distributed WITHOUT ANY WARRANTY, and without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

