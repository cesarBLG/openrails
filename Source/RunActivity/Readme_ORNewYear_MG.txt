Open Rails, Monogame version (unofficial) README - Release NewYear - Rev.108
October 15th, 2021

Please note that the installation and use of Open Rails software, even of its unofficial versions, is governed by the Open Rails End User License Agreement. 

INSTALLATION
- the requirements for installation of the official Open Rails version apply, with the precisions of next lines
- XNA 3.1 Redistributable is not needed
- you must have at least a Windows Vista computer. Windows XP is not supported
- start openrails simply by clicking on Openrails.exe.


RELEASE NOTES
This unofficial version has been derived from the latest official OpenRails Release candidate 1.4-rc6, and from the latest official Testing release
T1.3.1-2088 

This unofficial version includes the cruise control software written by Jindrich with final adaptations by myself.

This version includes some features not (yet) available in the Open Rails testing official version, that is:
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
- extended Raildriver setup (by perpetualKid)
- panto commands and animations now swapped when in rear cab
- correction to reduce transfer flickering at short distance
- allow passing a red signal for shunting through dispatcher window, see https://trello.com/c/I19VVKlz/450-manual-callon-from-dispatcher-window , by césarbl
- Fix memory usage indication that was clamped to 4 GB, see http://www.elvastower.com/forums/index.php?/topic/33907-64-bit-openrails-consumes-more-memory/page__view__findpost__p__257699
- option to skip saving commands (reduces save time on long activities), see http://www.elvastower.com/forums/index.php?/topic/33907-64-bit-openrails-consumes-more-memory/page__view__findpost__p__257687
- skip warning messages related to ruler token, that is introduced by TSRE5
- General Option to reduce memory usage
- track gauge can be changed over the whole route, see http://www.elvastower.com/forums/index.php?/topic/34022-adjusting-track-gauge/
- re-introduced advanced coupling, by steamer_ctn
- bug fix for https://bugs.launchpad.net/or/+bug/1895391 Calculation of reversal point distance failing
- bug fix for http://www.elvastower.com/forums/index.php?/topic/34572-new-player-trains-dont-show-train-brakes-in-hud/
- bug fix for http://www.elvastower.com/forums/index.php?/topic/34633-unhandled-exception-overflow-in-win7/page__view__findpost__p__265463 , by mbm_OR
- tentative support for RAIN textures for objects, terrain and transfers
- dynamic terrain and scenery loading strategy (useful for slower computers)
- enable customized tooltips for cabview controls, by adding a Label ( "string" ) line in the cabview control block in the .cvf file, by Jindrich
- commands to selectively throw facing or trailing switches, see http://www.elvastower.com/forums/index.php?/topic/34991-opposite-switch-throwing-with-g-key/page__view__findpost__p__270291
- locomotive and train elevator, see http://www.elvastower.com/forums/index.php?/topic/35082-locomotive-and-train-elevator/#entry271012
- preliminary bug fix for http://www.elvastower.com/forums/index.php?/topic/35112-problem-with-tcs-scripts-and-timetable-mode/
- max fog distance increased to 300 km
- first cruise control implementation, by Jindrich and slightly adapted by me
- added AIFireman info in web and main display TrainDrivingInfo, by mbm_OR
- tentative improvement to sound deactivation in long player trains, see http://www.elvastower.com/forums/index.php?/topic/35244-problem-in-locomotive-sound/
- camera following detached wagons in hump yard operation by pressing Ctrl key while clicking with mouse on coupler in Train Operations Window to uncouple wagon
- bug fix that didn't check for null label text, see http://www.elvastower.com/forums/index.php?/topic/32640-or-newyear-mg/page__view__findpost__p__273496
- re-introduced bug fix for missing shapes http://www.elvastower.com/forums/index.php?/topic/32640-or-newyear-mg/page__view__findpost__p__272585
- Cruise control: support for proportional set speed controller, independent from throttle controller
- Cruise control: support for Combined control where the throttle section can be used also as max force selector
- Fix bug in stating whether tractive or braking force of ORTS_SIGNED_TRACTION_TOTAL_BRAKING
- Cruise control: add variable to adjust dynamic braking force change speed
- Cruise control: manage running in reverse
- cabview controls ODOMETER, ODOMETER_RESET and ODOMETER_DIRECTION, see https://blueprints.launchpad.net/or/+spec/odometer-cabview-control 
- Cruise control: improved support for auto/manual switch, including support for ON/OFF control and additional parameters for .eng file
- NEW: Cruise control: keyboard command to set selected speed at zero
- NEW: 2 cabview controls for generic items, see http://www.elvastower.com/forums/index.php?/topic/35572-cabview-controls-for-generic-items/
- NEW: fix bug in .csproj file, by perpetualKid
- NEW: Cruise control: allow optional setting of speed also in manual mode.

Moreover following items have been added:
- fix for https://bugs.launchpad.net/or/+bug/1945886 Impossible to start trains upnill with dynamic brake blending(PR #512 by Serana)
- code of PR #515 (Correct issue with compressor not turning off), by steamer_ctn
- code pf PR #514 (Locales not to be translated), by mbm_OR.

The Monogame related code intentionally coincides only partly with the code of the OR official testing version.

CREDITS
This unofficial version couldn't have been created without following contributions:
- the whole Open Rails Development Team and Open Rails Management Team, that have generated the official Open Rails version
- the Monogame Development Team
- Peter Gulyas, who created the first Monogame version of Open Rails
- perpetualKid
- Jindrich
- Dennis A T (dennisat)
- Mauricio (mbm_OR)
- cjakeman
- BillC
- Peter Newell (steamer_ctn)
- Rob Roeterdink (roeter)
- jonas
- YoRyan
- césarbl
- Paolo
- Weter
- Carlo Santucci

- all those who contributed with ideas and provided contents for testing and pointed to malfunctions.

DISCLAIMER
No testing on a broad base of computer configurations and of contents has been done. Therefore, in addition
to the disclaimers valid also for the official Open Rails version, 
the above named persons keep no responsibility, including on malfunctions, damages, losses of data or time.
It is reminded that Open Rails is distributed WITHOUT ANY WARRANTY, and without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

