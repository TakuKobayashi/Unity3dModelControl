# What is This
This is the utility tools to increase work efficiency in Unity. 

It has the following features. 

 * The file exporter
   * Convert 3D Models(fbx, obj, etc...) to prefabs.
   * Dissosiate AnimationClip files from 3D Models.
   * Capture 3D Models image and export file.
   * Register the asset file references to asset, csv, or json file.
 * Automaticaly attach colliders from 3D Models mesh info.

# Install

Since unitypackage is in release tag, install it.
https://github.com/TakuKobayashi/Unity3dModelControl/releases

# Usage
After import, select Tools on the Menu bar, and select the FileExportEditor or AttachColliderEditor. And then open the Editors Window.

### FileExportEditor
Input the items and press the execute button to execute the process.
About each item is explained below.

 * Export Mode: Select from the 4 types of features described above to use
 * Search Root Directory: Execute processing to all files under the inputed Path.
 * Export Directory: Exports the result of executing processing to the inputed directory. The exporting file name has the same name as the file to be processed.
 * Distribute with the parent directory?: When exporting files, it is often that you want to export in directories structure following to a your own rule. At that time, if this checkbox is checked and executed, data will be exported with the same directory structure rule.
 * Refer hierarchy parent number: This item comes out when you check "Distribute with the parent directory?". From the target file, create a directory with the same name as the parent directory of the hierarchy which you make, and export the file to the distributed directory in that directory.
 * Search File Extention: This item comes out when you select "Convert To Prefab" or "Dissociate Animation Clip". You can execute only the format of the 3d model file that you have select.
 * Capture Image Width, Height: This item comes out when you select "Capture Scene Image".You can input the size of the image you want to export as Thumbnail.
 * Export File Extention:This item comes out when you select "Capture Scene Image".You can input the file type of the image you want to export as Thumbnail.
 * Execute:The file is export with the your settings.

