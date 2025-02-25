|---------------------------------------------------------------------------------------------------------------------------|

  Wiskered is a small independent game development studio and tools for game developers and other 3D graphics professionals.

|---------------------------------------------------------------------------------------------------------------------------|

* Unity Asset Store: https://assetstore.unity.com/publishers/46701
* Wiskered Discord: https://discord.gg/NqcZJE5HYF
* Wiskered Twitter: https://twitter.com/wiskered
* Phil Twitter: https://twitter.com/Phil_Wiskered
* Reddit: https://www.reddit.com/user/Wiskered_Team
* YouTube: https://www.youtube.com/c/HORDBRUN_GAMEDEV
* Instagram: https://instagram.com/gamedev_hordbrun
* Itch.io: https://wiskered.itch.io/

* Email Wiskered Team: wiskered@gmail.com
* Email Unity Asset Publisher: philunitypublisher@gmail.com

|--------------------------------------------------------------------|
|            TerraForge - Terrain Generator for Unity                |
|--------------------------------------------------------------------|

|Introduction|
--------------

TerraForge is a versatile and user-friendly terrain generator tool designed for Unity, empowering developers to effortlessly create stunning and realistic landscapes. With a focus on simplicity and efficiency, TerraForge enables users to customize terrains, experiment with various noise algorithms, and produce captivating outdoor environments for games, simulations, or virtual experiences.

---------------------------------------------------------------------|
---------------------------------------------------------------------|

|Getting Started|
-----------------

To use TerraForge in your Unity project, follow these steps:

1. Importing TerraForge:
------------------------
* Obtain the TerraForge package from the Unity Asset Store or any other trusted source.
* Open your Unity project and import the package by going to "Assets" > "Import Package" > "Custom Package," and then selecting the TerraForge package.


2. Creating a New Terrain:
--------------------------
* In the Unity scene, create a new terrain by going to "GameObject" > "3D Object" > "Terrain."


3. Attaching HeightsGenerator Component:
----------------------------------------
* Select the newly created terrain GameObject in the Hierarchy window.
* In the Inspector window, click on "Add Component" and search for "HeightsGenerator."
* Click on "HeightsGenerator" to attach it to the terrain.


4. Customizing the Terrain:
---------------------------
* Now, you can customize the terrain generation options and noise settings in the HeightsGenerator component.


5. Generating the Landscape:
----------------------------
* After adjusting the settings, you have two options for generating the landscape:
* Click the "Generate" button in the HeightsGenerator component to apply the settings and create a new version of the terrain.
* If you have the "AutoUpdate" option enabled in the HeightsGenerator, the terrain will be automatically generated whenever you make changes to the settings.


6. Quality Improvement without Shape Change:
--------------------------------------------
* If you wish to enhance the quality of the terrain without altering the shape, uncheck the "Randomize" and "AutoUpdate" options.
* Customize the terrain settings such as resolutions, materials, etc.


7. Save Generation Preset (Optional):
-------------------------------------
* If you want to reuse the current generation settings, you can save them as a preset for future use.


8. Editing Terrain:
-------------------
* To make edits to the terrain, you need to disable the HeightsGenerator component temporarily. This allows you to freely sculpt and modify the terrain as desired.

---------------------------------------------------------------------|
---------------------------------------------------------------------|

|HeightsGenerator Component Parameters|
---------------------------------------

The HeightsGenerator component provides several parameters that allow you to customize the terrain generation. Here is a brief explanation of each parameter:

1. Generation Options:
-------------------
* Randomize: When checked, this option randomizes the terrain generation by setting a new seed value each time the terrain is generated.
* AutoUpdate: If enabled, the terrain will be automatically updated whenever you make changes to the HeightsGenerator settings.

2. Terrain Settings:
-----------------
* NoiseType: The type of noise algorithm used for terrain generation (e.g., OpenSimplex2).
* FractalType: The type of fractal algorithm used for noise generation (e.g., Ridged).
* Size: The number of tiles (chunks) used to create the terrain. Higher values result in larger terrains.
* Width, Height, Depth: The dimensions of the terrain in Unity units.
* Scale: The scale factor applied to the noise algorithm, affecting the amplitude of generated features.

3. Noise Settings:
---------------
* Octaves: The number of noise octaves used for terrain generation. More octaves create finer details.
* Lacunarity: The frequency multiplier between successive noise octaves.
* Persistance: The amplitude multiplier between successive noise octaves.
* Seed: The seed value used to initialize the random number generator for noise generation.
* HeightCurve: An animation curve that maps noise values to terrain heights, allowing for custom height distributions.

4. Falloff Settings:
-----------------
* UseFalloffMap: When enabled, a falloff map is used to soften the edges of the generated terrain.
* FalloffDirection: The direction of the falloff gradient.
* FalloffRange: The range of the falloff gradient.

---------------------------------------------------------------------|
---------------------------------------------------------------------|

|Terrain Shader - Custom/TerraForgeTerrainShader|
-------------------------------------------------

The provided terrain shader allows for smooth blending between different layers of the terrain. The shader works with four different texture layers, each with its own properties such as color, height, and transition width. The shader includes the following properties for each texture layer:

* Texture: The texture for the specific layer.
* Color: The color tint for the layer.
* Tile Size X, Tile Size Y: The tiling factor for the texture on the X and Y axes.
* Height and Transition Width: Parameters for controlling the height and smoothness of the transition between layers.

---------------------------------------------------------------------|
---------------------------------------------------------------------|

|Terrain URP and HDRP Shaders|
-------------------------------------------------

*If you are using URP or HDRP, you must reconfigure the materials to the appropriate shader:
TerraForge\TerraForgeShaders\URP\... or TerraForge\TerraForgeShaders\HDRP\...

---------------------------------------------------------------------|
---------------------------------------------------------------------|