# Create Unity materials from Substance Painter

1. In Painter, open **File → Export Textures**. Choose **Unity Universal Render Pipeline (Metallic Standard)** and export PNG or TIFF files. For classroom web builds, start with 1024 or 2048 textures.
2. Copy the export into its own folder under **Assets**, such as `Assets/My Game/Art/Robot Textures`. Keep the files for a set together and keep the export suffixes.
3. Right-click the folder in Unity's **Project** window and choose **GDD 110 → Create Materials from Substance Textures**. You can also use the button in **Tools → GDD 110 → Start Here** and choose the folder.
4. Unity creates and selects one URP Lit `.mat` per texture set. Drag each material onto the matching mesh slot under **Mesh Renderer** or **Skinned Mesh Renderer → Materials**. For a reusable FBX, assign the materials in its importer's **Materials** remap list and click **Apply**.

For example, `Robot_Body_BaseColor.png`, `Robot_Body_Normal.png`, and `Robot_Body_MetallicSmoothness.png` produce `Robot_Body.mat`. **AlbedoTransparency** also works for the color map. If the export includes **AmbientOcclusion** or **Emission**, the tool connects those too. It sets the normal and linear data import settings and reads smoothness from the packed metallic map's alpha channel. For cutouts, enable **Alpha Clipping** on the material. These materials start as opaque.

To revise the paint, export into the same texture filenames and replace them in Unity. The material retains its references. Running the generator again creates new materials with unique filenames. If Unity reports missing or duplicate maps, check that each texture set uses the same filename prefix and has one file per map. For raw roughness, separate metallic maps, DirectX normals, or UDIM exports, first produce a matching URP Metallic Standard set.

[Adobe Painter export presets](https://experienceleague.adobe.com/en/docs/substance-3d-painter/using/export/output-templates/default-output-templates/default-presets)
