.. _cruisecontrol:

**************
Cruise Control
**************

General
=======

OR NewYear MG features a draft version of Cruise Control (CC), which has been in the 
foremost part developed by Jindrich.

To equip an electric or diesel locomotive with CC, following steps 
must be performed:


  1) In the .eng file the required entries must be provided
  2) in the .cvf file the required cab controls must be added
  3) in the .sms files the required sound stream entries must be added.

To drive a locomotive that has been equipped with CC following 
driver interfaces are available:

  1) Keyboard commands
  2) Mouse
  3) HUD and Train Driving window.

The CC feature includes also the management of a very versatile specific controller,
called Multi Position Controller (MPC). 

A paragraph is devoted to each of the above topics.

Operation Modes
===============

The CC Speed Regulator can be in 4 different states (or modes), that is:

1) *Manual*, when the automatic cruise control is disabled and the driver 
   controls the speed through throttle and brakes as if there were no CC.
2) *Auto*, when the automatic cruise control is enabled, and therefore 
   the speed is automatically controlled
3) *Testing*, not implemented at the moment
4) *AVV*, not implemented at the moment.

Switching between Manual and Auto mode can be configured to occur either 
by a specific cabview control (*ORTS_SELECTED_SPEED_REGULATOR_MODE*) or 
when certain conditions, defined in the .eng file parameters, are met, 
or by keyboard commands.
One of such parameters is e.g. *ForceRegulatorAutoWhenNonZeroSpeedSelected*.

The CC Speed Selector can be in 4 different states (or modes), that is:

1) *Parking*
2) *Neutral*
3) *On*
4) *Start*.

Switching betwenn Speed Selector Modes can be configured to occur either 
by a specific cabview control (*ORTS_SELECTED_SPEED_MODE*) or through 
keyboard commands.

The Cruise Controller can be of three types:

1) *None*
2) *Full*
3) *SpeedOnly*

The type selection occurs through .eng parameter *ControllerCruiseControlLogic*.
Types *None* and *Full* work the same way. *None* is default, meaning the full 
Cruise Controller features are active. 

Parametrizing the .eng file
===========================

In the *Engine* section of the .eng file the CC parameters must be 
declared within an *ORTSCruiseControl* block, this way::

  ORTSCruiseControl(
    CCparameter1 (value)
    CCparameter2 (value)
    ...
    CCparametern (value)
    Options ("Option1", "Option2", ... "Optionn")
    ForceStepsThrottleTable ("IntValue1", "IntValue2",...,"IntValuen")
    AccelerationTable ("FloatValue1", "FloatValue2",...,"FloatValuen")
  )


A list of the available .eng file CC parameters may be found in sheet
*eng parameters* `in this file
<CC_Parameters.xls>`_ .
The list of the available parameters for the Options() block may be 
found in sheet *Options* of the same file.
The presence of ForceStepsThrottleTable and AccelerationTable is 
optional.

Multi Position Controller (MPC)
-------------------------------

It is possible to manage a CC also without a MPC, in case the throttle 
controller is used for CC. However in general a MPC is needed.

The Multi Position Controller(s) (more than one can be defined) is 
defined in the .eng file too with an *ORTSMultiPositionController* block, 
this way::

  ORTSMultiPositionController (
    Positions (
      Position ( PositionType1 PositionFlag1 "PositionName1" )
      Position ( PositionType2 PositionFlag2 "PositionName2" )
      ...
      Position ( PositionTypen PositionFlagn "PositionNamen" )
    )
    ControllerID ( ID )
    ControllerBinding ( "Controller Linked" )
    CanControlTrainBrake ( Boolean )
  )

The list of the available PositionTypes may be found in sheet 
*MPC Position types* `in the above file
<CC_Parameters.xls>`_.

The list of the available PositionFlags may be found in sheet 
*MPC Position flags* `in the same above file
<CC_Parameters.xls>`_.

PositionNames are arbitrary strings.

The ControllerID is an integer, which must be unique for every 
defined MPC.

The ControllerBinding parameter defines to which function the 
MPC is connected. Controllers linked may be either "Throttle" or 
"SelectedSpeed".

The boolean parameter *CanControlTrainBrake*, which is false by 
default, is optional.

Cruise Control Cabview Controls
===============================

The list of the available cabview controls may be found in sheet 
*Cabview Controls* `in the usual file <CC_Parameters.xls>`_.

Restricted Speed Zone
---------------------

Strictly this is not a Cruise Control function.
When the driver sets the cabview control ORTS_RESTRICTED_SPEED_ZONE_ACTIVE, 
the Cruise Control sets to true and displays a boolean variable.
This boolean variable remains true until the full length of the train has 
passed the point where the driver set the cabview control. When the variable 
returns false, also a sound trigger is activated.

This feature helps the train driver to identify when the full length of the train 
has passed a restricted speed zone, so that he can again increase speed of the train.

Cruise Control Sound Triggers
=============================

The list of the available sound triggers may be found in sheet 
*Sound Triggers* `in the usual file <CC_Parameters.xls>`_.


Cruise Control Keyboard commands
================================

The list of the available keyboard commands may be found in sheet 
*Keyboard Commands* `in the usual file <CC_Parameters.xls>`_.

Keys listed in the sheet are valid for English keyboard.


Cruise Control commands through Mouse
=====================================

The Cabview Controls that may be activated by mouse are flagged with a *Y* 
in sheet *Cabview Controls* `in the usual file <CC_Parameters.xls>`_.

HUD and Train Driving window info about CC
==========================================

Following info is displayed both in the main 
HUD and in the Train Driving window:

1) Speed regulator mode (*Manual* or *Auto*). 
   If the mode is *Auto*, the following further info is displayed:
2) Target speed (speed set)
3) Max Acceleration in percentage

Here below a picture of the HUD with CC info is shown

.. image:: images/cruisecontrol-mainhud.png
  :align: center
  :scale: 80%

Here a picture of the Train Driving window with CC 
info is shown:

.. image:: images/cruisecontrol-traindriverwindow.png
  :align: center
  :scale: 80%

Sample files of a CC equipped electric locomotive
=================================================

The E464 is the Italian electric locomotive that has 
been built in the highest number of exemplars.

Here below is a picture of the E464 cabview:

.. image:: images/cruisecontrol-samplecab.png
  :align: center
  :scale: 80%

Following relevant items are circled in the picture:

1) Manual throttle-dynamic brake combined control lever 
   (CP_HANDLE COMBINED_CONTROL in cvf file)
2) CC maximum acceleration lever (ORTS_SELECTED_SPEED_MAXIMUM_ACCELERATION LEVER 
   in cvf file)
3) Multi position controller lever used to set the target speed 
   (ORTS_MULTI_POSITION_CONTROLLER TWO_STATE in cvf file); it has 
   four positions: unstable target speed increase position, stable 
   neutral position, unstable target speed decrease position, and 
   unstable target speed to zero position
4) Target speed digital display (ORTS_SELECTED_SPEED DIGITAL in 
   cvf file)

Switching from manual to auto mode and vice-versa occurs only when levers 1 
and 2 are in the zero position and lever 3 is in the neutral position. If 
at that point lever 1 is moved, CC switches to (or remains in) manual mode. 
If at that point lever 2 is moved, CC switches to (or remains in) auto mode.

The cvf file for the E464 equipped with CC (and also with customized TCS can be 
found in ``Documentation\SampleFiles\Manual\e464_V2SCMT_SCMTscript_alias_CC.zip``.

The eng file for the E464 can be found in 
``Documentation\SampleFiles\Manual\Fs-E464-390.zip``.


