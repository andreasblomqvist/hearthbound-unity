# Unity MCP Integration Guide for Cursor

This guide explains how to connect your Hearthbound Unity project to Cursor IDE using the Unity MCP (Model Context Protocol) server. This enables a powerful code-first workflow where you can control the Unity Editor directly from Cursor.

## What is Unity MCP?

Unity MCP is a bridge that allows AI assistants like Cursor to interact with your Unity project. You can ask Cursor to:

-   Create and modify C# scripts.
-   Generate GameObjects and prefabs.
-   Change scene properties.
-   Run Unity menu items.
-   And much more, all through natural language prompts!

## Step 1: Install Node.js

The MCP server runs on Node.js. You must have **Node.js 18 or later** installed.

1.  Go to the [Node.js download page](https://nodejs.org/en/download/).
2.  Download and run the installer for your operating system.
3.  Verify the installation by opening a terminal or command prompt and running:
    ```shell
    node --version
    ```

## Step 2: Install the Unity MCP Package

1.  Open your `HearthboundUnity` project in the Unity Editor.
2.  Go to **Window > Package Manager**.
3.  Click the **+** icon in the top-left corner and select **Add package from git URL...**.
4.  Enter the following URL:
    ```
    https://github.com/CoderGamester/mcp-unity.git
    ```
5.  Click **Add**. Unity will download and install the package.

## Step 3: Configure Cursor

Now, you need to tell Cursor how to find and run the Unity MCP server.

1.  In the Unity Editor, go to **Tools > MCP Unity > Server Window**.
2.  This window shows you the exact configuration needed for your AI client (Cursor).
3.  Click the **Configure** button for your AI LLM client.
4.  Confirm the installation with the given popup.

**Manual Configuration (Alternative):**

If the automatic configuration doesn't work, you can do it manually:

1.  In Cursor, open the Command Palette (**Ctrl+Shift+P** or **Cmd+Shift+P**).
2.  Type `Configure MCP Servers` and press Enter.
3.  This will open a `mcp_servers.json` file.
4.  In the Unity Editor, go to **Tools > MCP Unity > Server Window**.
5.  Copy the JSON configuration block shown in the window.
6.  Paste this block into your `mcp_servers.json` file in Cursor.

It should look something like this:

```json
{
    "mcp-unity": {
        "command": "node",
        "args": [
            "/path/to/your/project/Library/PackageCache/com.codergamester.mcp-unity@.../Server~/build/index.js"
        ]
    }
}
```

> **Important**: The path in `args` must be the **absolute path** to the `index.js` file inside your project's `Library` folder. The Unity MCP window provides the correct path for you to copy.

## Step 4: Start the MCP Server

1.  In the Unity Editor, go to **Tools > MCP Unity > Server Window**.
2.  Click the **Start Server** button.
3.  The server will start, and you should see a log message in the Unity Console confirming that the WebSocket server is running (usually on port `8090`).

## Step 5: Use MCP in Cursor

Now you're ready to control Unity from Cursor!

1.  Open your `HearthboundUnity` project folder in Cursor.
2.  Open any C# script or create a new one.
3.  In the chat, you can now use prompts that interact with Unity.

### Example Prompts:

-   **Create a script:**
    > "Create a new C# script called `PlayerHealth` that has a `TakeDamage` function."

-   **Modify a GameObject:**
    > "In the current scene, find the GameObject named `[PLAYER]` and add a `Rigidbody` component to it."

-   **Run a menu item:**
    > "Execute the menu item `GameObject/Create Empty` to create a new empty GameObject."

-   **Get scene information:**
    > "List all the GameObjects in the current scene hierarchy."

-   **Generate code based on context:**
    > "In my `TerrainGenerator.cs` script, add a new function to create rivers."

## How It Works

1.  You type a prompt in Cursor.
2.  Cursor sends the command to the Node.js MCP server.
3.  The MCP server sends a message to the Unity Editor via a WebSocket connection.
4.  The Unity MCP package receives the message and executes the corresponding action (e.g., creates a script, modifies a component).
5.  The result is sent back to Cursor.

## Troubleshooting

-   **Server not starting:** Make sure Node.js is installed and in your system's PATH.
-   **Cursor can't connect:** Ensure the MCP server is running in Unity (check the Server Window). Also, verify that the path in your `mcp_servers.json` is correct.
-   **Commands failing:** Check the Unity Console for any error messages from the MCP package.
-   **Firewall issues:** Make sure your firewall is not blocking the connection on port `8090` (or whichever port you configure).

With this setup, you can now enjoy a seamless, code-first workflow for building your Hearthbound world, leveraging the power of AI directly within your development environment.
