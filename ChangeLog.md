# Anisotropic Part Resizer Change Log

## v1.5.0.1 / 2022-04-01

* Compiled for KSP 1.12.3
* Internal project changes for CI/CD

## v1.5.0 / 2022-02-14

* AssemblyVersion
* csproj: added NIGHTBUILD flag to debug configuration
* update_attach_nodes: fixed possible NRE
* update_attach_nodes:performance: extracted partTransform into local variable
* AF+RF
* PartUpdaterBase: added OnInit to separate OnStart-only logic from OnLoad+OnStart
* PartUpdaterBase: made SaveDefaults protected rather than public
* PartUpdaterBase: made Init protected rather than public
* KSP: reference changed 1.10.1 => 1.11.1

## v1.4.1 / 2020-07-31

* AssemblyVersion: 1.4.1
* RF
* AnisotropicPartResizer: on rescale first break compound parts, then Rescale
* AnisotropicPartResizer: have to update PAW after rescaling
* AnisotropicPartResizer: improved OnStart logic
* Using FloatFieldWatchers for aspect and size to avoid unnesessary rescaling
* ResourceUpdater: disabling the updater when base part doesn't have resources
* ModuleUpdater: fixed duplication of module pairs on part cloning
* PartUpdater: disable updaters if not initialized properly
* PartUpdater: initialize updaters only once OnStart
* KSP refs: 1.10.0
* Changed references to KSP-1.9.1

## v1.4.0 / 2019-12-22

* AssemblyVersion: 1.4.0
* Retreive aspect/size fields once, then use them
* Updating PAW caused it to freeze when a part is in a symmetry group
* Using BaseField.OnValueModified instead of UI_Control.onFiledChanged
* MagneticDamperUpdater: scale everything as quad*aspect for better playability
* Added ATMagneticDamperUpdater
* Refactoring+AF
* ModuleUpdater.Init should dispose of the used enumerators explicitly.
* Removed obsolete static AnisotropicResizable.unequal
* AnisotropicPartResizer.Rescale updates PAW
* Set caps for breakingForce/Torque to 50k to be closer to current stock values
* Fixed AnizotropicResizable.GetModuleMass/Cost calculations
* ResourcesUpdater is IPartCostModifier and only handles resources present in prefab
* In Scale treating aspect like size: with respect to original aspect
* Fixed NRE in JettisonUpdater

## v1.3.0.1 / 2019-11-07

* Set new UnityEngine.Module dlls as non-private: don't copy them to the output
* Added required Unity-2019 Module dlls
* Changed target framework to .NET-4.5
* REFS KSP-1.8.1
* REFS KSP-1.7.3
* Using KSPField.onFieldChange instead of Update to rescale the model

## v1.3.0 / 2019-05-26

* AssemblyVersion: 1.3
* Commented out debug logging
* Formatting
* Moved create_updaters() call inside SaveDefaults where it should be
* Moved PropsUpdater and NodesUpdater code to AnisotropicPartResizer
* Refactoring
* Removed unneeded usings
* REFS: KSP-1.7.0
* Changed references to KSP-1.6.0

## v1.2.0.4 / 2018-10-11

* Fixed issue AT_Utils#7: no need to move parts on first update.
* Only udpate drag cubes in flight
* Changed version to 1.2.0.4
* Added ControlSurfaceUpdater (thanks @Fengist)
* Removed obsolete 'using AT_Utils'
* Only update DragCubes in flight
* Changed references to KSP-1.4.5
* Changed references to KSP-1.4.3
* Converted tabs to spaces.
* Changed references to KSP-1.4.1
* Changed version 1.2.0.3
* Changed references to KSP-1.3.1
* Added UpdateDragCube method to use on size/shape change.

## v1.2.0.2 / 2017-06-04

* Changed version to 1.2.0.2
* Changed references to KSP-1.3
* Fixed the "Coroutine couldn't be started" bug.

## v1.2.0.1 / 2017-02-06

* Changed version to 1.2.0.1
* Added nightbuilds.
* Fixed MassDisplay update on resize. Closed #184 in Hangar.
* Fixed limits for ScienceCareer mode.
* Moved to fully-manual versioning to avoid unpredicted revision number changes.

## v1.2.0 / 2016-12-19

* Updaters: del BaseConverter; add AsteroidDrill, ResourceHarvester, ResourceConverter.
* Updated references to 1.2.2
* Fixed Load/OnStart double-rescale problem.
* Added prefab_model property.
* Added abstract prepare_model method to resize part model on Load.
* Changed references to 1.2.1

## v1.1.0 / 2016-10-21

* Now I can =)
* Updated to KSP-1.2 API.
* Added Scale.volume = cube*aspect.
* Changed version to 1.1.*
* Fixed the bug that caused the PartUpdater constructor function to return null sometimes.
* UpdaterRegistrator refactored.
* Added ResourceConverterUpdater for BaseConverter.
* Added CoMOffset update, moved DragCubes update to PropsUpdater.
* Added DragCubeUpdater.
* Removed Part.GetModule<T> extension.
* Using automatic build number to prevent incompatibility issues.
* Converted the Resizable and its derivatives to IPartMassModifier interface.
* Moved ScaleVector static method to Scale class.
* Moved dll-s into Plugins subfolder.
* Fixed debug logging.
* Fixed output directory.
* Removed debug logging.
* Moved under the AT_Utils.
* Added link to AT_Utils to readme.
* Fixed limits initialization.
* Added Globals to define absolute max/min values.
* Moved classes to separate files. Reworked configuration framework. WIP.
* Update README.md
* Initial commit
* Fixed part resizer TechFlow initialization; still needs reworking, though.
* Changed namespace to AT_Utils.
* Initial commit.
