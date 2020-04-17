Open Rails, Monogame version (unofficial) README - Release NewYear - Rev.58
April 17th, 2020

Please note that the installation and use of Open Rails software, even of its unofficial versions, is governed by the Open Rails End User License Agreement. 

INSTALLATION
- the requirements for installation of the official Open Rails version apply, with the precisions of next lines
- XNA 3.1 Redistributable is not needed
- you must have at least a Windows Vista computer. Windows XP is not supported
- start openrails simply by clicking on Openrails.exe
- don't try to update the pack by using the link on the upper right side of the main menu window: 
you would return to the official OR version.

RELEASE NOTES
This unofficial version has been derived from the latest official Open Rails unstable revision U2020.04.17-0451 (which already includes Monogame)
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
- general options checkbox for optional run at 32 bit on Win64 (consumes less memory, recommended for computers with 4 GB RAM)
- translatable Train Driving Info window (see http://www.elvastower.com/forums/index.php?/topic/33401-f5-hud-scrolling/page__view__findpost__p__251671 and following posts), by mbm_OR
- extended Raildriver setup, as present in OR Ultimate (by perpetualKid)
- analog clocks showing game time, see http://www.elvastower.com/forums/index.php?/topic/29546-station-clocks/ , by jonas
- bug fix for bad mouse pointing in full screen mode and for issues with Fast full screen-Alt-Tab mode
- operation as webserver, enabling HUD and Train Driving info on web clients like PCs, tablets or smartphones, by mbm_OR, cjakeman and BillC
- support for AWS train control system
- panto commands and animations now swapped when in rear cab
- scripted TCS: support for Italian train control systems SCMT and TCS
- improvements in Activity Evaluation of station stops, by mbm_OR
- correction to reduce transfer flickering at short distance
- ORTS_SIGNED_TRACTION_BRAKING control (see http://www.elvastower.com/forums/index.php?/topic/33883-signed-traction-braking-cabview-control/ )
- NEW: water flickering reduced in many cases
- NEW: allow passing a red signal for shunting through dispatcher window, see https://trello.com/c/I19VVKlz/450-manual-callon-from-dispatcher-window , by césarbl
- NEW: optional etterbox cab2D + improved window shading, see http://www.elvastower.com/forums/index.php?/topic/33908-letterboxing-for-2d-cabs/page__view__findpost__p__257696 , byYoRyan
- NEW: Fix memory usage indication that was clamped to 4 GB, see http://www.elvastower.com/forums/index.php?/topic/33907-64-bit-openrails-consumes-more-memory/page__view__findpost__p__257699
- NEW: option to skip saving commands (reduces save time on long activities), see http://www.elvastower.com/forums/index.php?/topic/33907-64-bit-openrails-consumes-more-memory/page__view__findpost__p__257687
- NEW: skip warning messages related to ruler token, that is introduced by TSRE5
- NEW: scripted TCS: added hooks to get data about INFO signals (to emulate beacons)



CREDITS
This unofficial version couldn't have been created without following contributions:
- the whole Open Rails Development Team and Open Rails Management Team, that have generated the official Open Rails version
- the Monogame Development Team
- Peter Gulyas, who created the first Monogame version of Open Rails
- perpetualKid
- Dennis A T (dennisat)
- Mauricio (mbm_OR)
- cjakeman
- BillC
- Peter Newell (steamer_ctn)
- Rob Roeterdink (roeter)
- jonas
- YoRyan
- césarbl
- Carlo Santucci

- all those who contributed with ideas and provided contents for testing and pointed to malfunctions.

DISCLAIMER
No testing on a broad base of computer configurations and of contents has been done. Therefore, in addition
to the disclaimers valid also for the official Open Rails version, 
the above named persons keep no responsibility, including on malfunctions, damages, losses of data or time.
It is reminded that Open Rails is distributed WITHOUT ANY WARRANTY, and without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

