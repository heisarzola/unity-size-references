# Size References
On scene (editor-only) game objects with real world scales to help designing realistic sized scenes.

Inspired after [Samuel Bernou's Blender plugin](https://github.com/Pullusb/real_scale_references), which is also the source where the models were taken from.

# Tool Preview
![Tool Preview](https://imgur.com/0E1kl7V.gif)

# Controls
Everything is controlled with the draggable tab:

![image](https://user-images.githubusercontent.com/34221560/146625139-392ad070-9f07-4078-99a0-71033e1f6c19.png) Allows you to show/hide most of the controls.

![image](https://user-images.githubusercontent.com/34221560/146625177-303e2651-6504-4a78-9cc0-21c4c41efafd.png) Hides/Shows the model in the scene.

![image](https://user-images.githubusercontent.com/34221560/146625194-547f0f3f-916a-4c17-93df-10aa5147aa83.png) Allows you to cycle through all model options, where clicking the named button, allows you to focus on the model. Clicking it again reselects what you already had (if anything).

![image](https://user-images.githubusercontent.com/34221560/146625222-60420f9e-ff58-4cb4-9ff3-a447154ce6cb.png) Click it to set the model wherever you want on the scene. It raycasts to the first object you hover over, so something in the scene (like ground) is needed first. Once you start moving the model, you can left click to confirm, or right click to return the model where it was. If you right click the button, the position is reset to zero.

![image](https://user-images.githubusercontent.com/34221560/146625271-54ea4ff9-6d99-45af-a5fc-c200f6e3f59b.png) Shows/hides the gizmo that allows you to rotate the model. If you right click the button, the rotation is reset.

![image](https://user-images.githubusercontent.com/34221560/146625285-e166636d-060b-4d99-85c3-b4f28adc0ba1.png) Allows you to edit the color (or more specifically, the emission) of the models.

# How To Install
This repo was made with Unity's Package Manager in mind, so all you need to do is open it, click here:

![image](https://user-images.githubusercontent.com/34221560/146624892-9b7b9da2-c870-4188-a5f6-c22ce0a5f746.png)

And type out `https://github.com/heisarzola/unity-tools-core.git`.

Once you do that, repeat this process, but now with `https://github.com/heisarzola/unity-size-references.git` as the target. 

After both packages have downloaded, you're done.

# Known Limitations
This project has been marked public, in case someone wants to contribute with expanding the current functionality. As the following limitations currently exist:

* Only one model can be seen at a time, and the number of options, while reasonable, is still limited.
* All controls need for you to interact with the floating scene tooltip for the most part, and all functions with the mouse (no keyboard shortcuts).
* Without monitors to test it on, it's possible that displays with unusual DPIs like retina displays might behave incorrectly.
* This was tested with the newer versions of Unity in mind, there is no Unity 5.X support or similar out of the box.