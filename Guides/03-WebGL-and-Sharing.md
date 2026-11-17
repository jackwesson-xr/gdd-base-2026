# WebGL build and classroom sharing

1. In Unity Hub, confirm **WebGL Build Support** is installed for **6000.6.0f1**.
2. Stop Play Mode. Choose **Tools → GDD 110 → WebGL → Build for Unity Play** and select an empty output directory outside Assets.
3. Test the build over HTTP. Confirm the title starts, the intro fades into the Playground, and the exit portal reaches Win. Click inside the browser game and press a controller button before testing gamepad input.
4. ZIP the **contents** of the output directory so `index.html` is at the ZIP root beside `Build/` and `TemplateData/`. Upload the build ZIP to Unity Play.

Scene 0 contains the playable opening. Scene 1 is an optional preview that goes straight into the intro. Scene 2 is the lab and Scene 3 is Win. Keep the configured scene order for the exit portal.

To share the **editable** course, grant students repository access or give them a separate project archive containing Assets (with all .meta files), Packages, and ProjectSettings. Omit the local Coplay MCP package folder from any editable archive. Unity regenerates Library and other local caches. See the [instructor setup guide](07-Instructor-Setup.md) for the exact folder and lock file details. The WebGL ZIP is only for playing. Review third-party asset redistribution terms before making the project public.
