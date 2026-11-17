# Instructor setup

Open the project in **Unity 6000.6.0f1**. For the first playtest choose **Tools → GDD 110 → Start Here → Play from Title**.

## Author a tutorial

Tutorial Authoring Tools and its Tutorial Framework dependency install when Unity resolves Packages. Use **Assets → Create → Tutorials → Tutorial** to make a tutorial. Create or add pages in its Inspector, add the tutorial to a **Tutorial Container**, then open **Tutorials → Show Tutorials Window** to preview it. Use the authoring guide for setting up a container and adding completion criteria.

[Unity Tutorial Authoring Tools guide](https://docs.unity.cn/Packages/com.unity.learn.iet-framework.authoring%402.0/manual/authoring-guide.html)

## Build for the browser

In Unity Hub, open **Installs**, locate **6000.6.0f1**, and choose **Add modules → WebGL Build Support** if it is missing. This module is installed per editor on each workstation. The project includes WebGL Publisher. Use the course's **Build WebGL for Unity Play** button to build the configured scene list, then follow [WebGL builds and sharing](03-WebGL-and-Sharing.md).

## Local instructor MCP

Coplay's MCP package is a local embedded package under `Packages/com.coplaydev.unity-mcp`. This folder is ignored by Unity Version Control. For an editable handoff, use a clean repository checkout or omit that folder when copying or archiving. Unity may record local embedded package metadata in `packages-lock.json`. Remove the `com.coplaydev.unity-mcp` entry from the copy of the lock file you distribute if it is present. The shared manifest has no MCP dependency.

For another instructor who wants it, copy just the upstream `MCPForUnity` folder from the [Coplay repository](https://github.com/CoplayDev/unity-mcp/tree/main/MCPForUnity) into `Packages/com.coplaydev.unity-mcp`. Open **Window → MCP for Unity → Toggle MCP Window** to access the bridge. The package installation and AI-client configuration are separate steps.

## Neutral scene lighting

The five playable scenes use a neutral flat Environment Lighting color and zero skybox reflection intensity. For a new scene, open **Window → Rendering → Lighting → Environment**, set **Environment Lighting → Source** to **Color**, use a neutral gray, and set **Environment Reflections → Intensity Multiplier** to zero.
