CHERNOBYL ABANDONED - Soviet Control Room & Props
==================================================

98 game-ready, PBR-textured props for abandoned Soviet / Chernobyl-style
scenes: control room consoles and wall panels, furniture, industrial parts,
small props, architecture pieces and outdoor exclusion-zone elements.


FOLDER LAYOUT
-------------
<Category>/SM_<AssetName>/
    SM_<AssetName>.fbx
    T_<AssetName>_BaseColor.png                  sRGB
    T_<AssetName>_Normal.png                     tangent space, OpenGL (Y+)  -> Blender, Godot, Unity
    T_<AssetName>_Normal_DirectX_Unreal.png      tangent space, DirectX (Y-) -> Unreal Engine
    T_<AssetName>_Roughness.png                  linear
    T_<AssetName>_Metallic.png                   linear
    T_<AssetName>_ORM_Unreal.png                 linear, R = AO (white), G = Roughness, B = Metallic
    T_<AssetName>_MetallicSmoothness_Unity.png   linear, RGB = Metallic, A = Smoothness (URP/HDRP Lit)
    T_<AssetName>_Emissive.png                   only on assets with glowing parts

Preview/overview.png - every asset at a glance.


FBX DETAILS
-----------
- Units: meters (1 unit = 1 m). Real-world scale.
- Axes: Y up, -Z forward. The front of each asset faces -Y in Blender.
- One UV channel (UV0), non-overlapping (glass faces excluded), used by all textures.
- Most assets use one material slot. Glass parts (windows, gauge covers,
  screens, jars, bottles) are a separate material slot named M_Glass_*.
  Give it a transparent material, suggested values:
    Clear glass:  color (0.70, 0.72, 0.70), roughness 0.08, opacity 0.20
    Green glass:  color (0.45, 0.60, 0.48), roughness 0.08, opacity 0.40
- Triangle counts are listed below (LOD0). No LODs inside the FBX files;
  the Unreal Engine project contains 4 generated LODs per mesh.


UNREAL ENGINE 5.8 PROJECT (separate download)
---------------------------------------------
- 98 static meshes, 2 master materials (PBR + translucent glass),
  99 material instances, 389 textures.
- 4 LODs per mesh, simple box collision, Nanite off.
- Maps: Overview (all assets laid out) and Demo_ControlRoom.
- Open ChernobylAbandoned.uproject with Unreal Engine 5.8, or migrate the
  Content/ChernobylAbandoned folder into your own project.


HOW THIS PACK WAS MADE
----------------------
All models and textures were generated procedurally (Blender scripts),
with the help of an AI assistant that wrote the generation code.


ASSET LIST (triangles, texture resolution)
------------------------------------------

ControlRoom (20)
  SM_Button_AZ5_Emergency                  6,562 tris   512 px
  SM_Button_Push                             150 tris   512 px
  SM_ComputerTerminal_CRT                  1,190 tris   1024 px
  SM_ControlConsole_A                      2,956 tris   2048 px
  SM_ControlConsole_B_AZ5                  8,908 tris   2048 px
  SM_ControlConsole_Short                  2,720 tris   1024 px
  SM_Gauge_Round                             166 tris   512 px
  SM_IndicatorLamp                            26 tris   512 px
  SM_Knob_Rotary                             178 tris   512 px
  SM_RelayCabinet_Closed                   1,104 tris   2048 px
  SM_RelayCabinet_Open                     1,104 tris   2048 px
  SM_Switch_Toggle                            62 tris   512 px
  SM_Telephone_Rotary                      5,366 tris   512 px
  SM_WallPanel_Annunciator                 2,050 tris   2048 px
  SM_WallPanel_Gauges                      3,970 tris   2048 px
  SM_WallPanel_KnifeSwitches               4,428 tris   2048 px
  SM_WallPanel_Looted                      6,722 tris   2048 px
  SM_WallPanel_PipeDiagram                 4,186 tris   2048 px
  SM_WallPanel_Recorders                   3,602 tris   2048 px
  SM_WallPanel_RodDisplay                 18,218 tris   2048 px

Furniture (13)
  SM_Bookshelf_Wooden                        840 tris   2048 px
  SM_Chair_Wooden                            108 tris   1024 px
  SM_FilingCabinet                           224 tris   1024 px
  SM_Fridge_Soviet                           464 tris   1024 px
  SM_KindergartenBed_Metal                 1,892 tris   1024 px
  SM_Lockers_Triple                          582 tris   2048 px
  SM_OfficeDesk_Soviet                       120 tris   1024 px
  SM_OperatorChair                           252 tris   1024 px
  SM_SchoolDesk_Soviet                       156 tris   1024 px
  SM_Shelf_Metal_Stocked                     988 tris   2048 px
  SM_Stool_Wooden                            220 tris   1024 px
  SM_TV_CRT_Soviet                           500 tris   1024 px
  SM_Wardrobe_Wooden                       1,100 tris   2048 px

Industrial (17)
  SM_Cable_Coil                            4,876 tris   1024 px
  SM_Cable_Hanging                         1,036 tris   1024 px
  SM_CableTray_2m                          3,616 tris   2048 px
  SM_Crate_Military_Large                  1,978 tris   1024 px
  SM_Crate_Military_Small                  1,894 tris   1024 px
  SM_Duct_Round_2m                           276 tris   2048 px
  SM_FireHoseCabinet                       7,384 tris   1024 px
  SM_GasCylinder_Propane                     924 tris   1024 px
  SM_JunctionBox                             198 tris   1024 px
  SM_OilDrum_Green                        34,180 tris   1024 px
  SM_OilDrum_Red                          34,180 tris   1024 px
  SM_OilDrum_Rusty                        33,044 tris   1024 px
  SM_OilDrum_Yellow_Radioactive           34,282 tris   1024 px
  SM_Pipe_Elbow90                          2,180 tris   1024 px
  SM_Pipe_Straight_2m                        596 tris   2048 px
  SM_Pipe_Tee                                940 tris   1024 px
  SM_Pipe_Valve_Handwheel                  4,240 tris   1024 px

Props (26)
  SM_Binder_Folder                            50 tris   512 px
  SM_Books_Stack                             288 tris   512 px
  SM_Bottle_Glass                            288 tris   glass only
  SM_Bucket_Enamel                         1,620 tris   512 px
  SM_Calendar_1986_April                  20,988 tris   512 px
  SM_Clipboard                                26 tris   512 px
  SM_Debris_Plaster_A                        108 tris   512 px
  SM_Debris_Plaster_B                        108 tris   512 px
  SM_Debris_Plaster_C                        108 tris   512 px
  SM_DeskFan                              11,092 tris   512 px
  SM_DeskLamp                                748 tris   512 px
  SM_Dosimeter_DP5                           872 tris   512 px
  SM_FireExtinguisher_OU                   1,052 tris   1024 px
  SM_FirstAidBox                             636 tris   512 px
  SM_GlassJar_3L                             508 tris   512 px
  SM_HardHat                                 704 tris   512 px
  SM_Kettle_Enamel                           776 tris   512 px
  SM_Mug_Enamel                              476 tris   512 px
  SM_Paper_Sheet                              64 tris   512 px
  SM_Paper_Stack                              98 tris   512 px
  SM_Poster_GloryToLabour                  2,304 tris   1024 px
  SM_Poster_PeacefulAtom                  16,666 tris   1024 px
  SM_Radio_WallSpeaker                     1,360 tris   512 px
  SM_Sign_HighVoltage                      1,478 tris   512 px
  SM_Sign_Radiation                        4,134 tris   1024 px
  SM_WallClock_Stopped_0123                2,210 tris   512 px

Architecture (13)
  SM_BulkheadLamp_Cage                     4,100 tris   512 px
  SM_CeilingGrid_Section                     108 tris   1024 px
  SM_CeilingLamp_Shade                       980 tris   1024 px
  SM_CeilingTile                              12 tris   1024 px
  SM_CeilingTile_Broken                       28 tris   1024 px
  SM_Door_Wooden_Frame                        96 tris   2048 px
  SM_Floor_Terrazzo_2m                         2 tris   2048 px
  SM_FluorescentLight                        148 tris   1024 px
  SM_FluorescentLight_Hanging                304 tris   1024 px
  SM_Radiator_CastIron                     1,540 tris   1024 px
  SM_VentGrille                              168 tris   1024 px
  SM_Wall_Segment_2m                          12 tris   2048 px
  SM_Window_Broken                           144 tris   1024 px

Outdoor (9)
  SM_ConcreteBlock_FBS                     1,924 tris   1024 px
  SM_ConcreteFence_PO2                     1,752 tris   2048 px
  SM_ConcreteSlab_Broken                     564 tris   2048 px
  SM_Fence_Wire_Segment                    5,710 tris   2048 px
  SM_ManholeCover                         14,968 tris   1024 px
  SM_RoadBarrier_Boom                        192 tris   2048 px
  SM_StreetLamp_Soviet                       406 tris   2048 px
  SM_UtilityPole_Concrete                  1,028 tris   2048 px
  SM_ZoneSign_Exclusion                    3,102 tris   2048 px

Total: 98 assets, 352,988 triangles.
