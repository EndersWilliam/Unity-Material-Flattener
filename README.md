# Unity Material Flattener

This editor takes an FBX with multiple materials and flattens it into a single material, then exports a new FBX. Intended to use with [Kenney assets](https://www.kenney.nl/assets).

Make sure to have the FBX Exporter package installed.

# How to use

 1. Tools -> Flatten Materials
 2. Select a folder with one or more FBX files
 3. Convert Objects
 4. Select a folder to ouput your new FBX files
 
# Why are my objects blurry?

There are a few ways to correct this depending on the intended game engine. If the engine lets you choose the filtering of a texture, change the generated texture filtering to Point and turn off compression. If it doesn't, you can either scale the generated texture up with the Texture Size option or increase the padding of the UVs, or both!

# How to install

**Install via Git URL**
 1. In your Unity project, open the  `Package Manager`  window.
 2. Click the **add**  ![](https://docs.unity3d.com/uploads/Main/PackageManagerUI-add.png) button in the status bar.
 3. The options for adding packages appear.
    
    ![Add package from git URL button](https://docs.unity3d.com/uploads/Main/PackageManagerUI-GitURLPackageButton.png)
    Add package from git URL button
    
4.  Select  **Add package from git URL**  from the add menu. A text box and an  **Add**  button appear.
5.  Enter `https://github.com/EndersWilliam/Unity-Material-Flattener.git`  in the text box and click **Add**
6.  Add the FBX Exporter preview package from the Unity Registry.

# Notes
TextureScale script script by [Eric Haines](http://wiki.unity3d.com/index.php/TextureScale).
