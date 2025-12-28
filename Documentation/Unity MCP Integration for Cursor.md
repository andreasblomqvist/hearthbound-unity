# Unity MCP Integration for Cursor

## Yes! Unity MCP exists and works with Cursor

**Repository**: https://github.com/CoderGamester/mcp-unity
**Stars**: 1.2k | **Forks**: 147
**Status**: Active (last updated 3 weeks ago)

## What It Does

The Unity MCP plugin bridges Unity Editor with AI assistants like:
- **Cursor IDE** ✓
- Claude Desktop ✓
- Windsurf IDE ✓
- Other MCP-compatible clients ✓

## Key Features

1. **Direct Unity Editor Control** - AI can interact with Unity Editor
2. **GameObject Manipulation** - Create, update, read GameObject information
3. **Script Generation** - Generate and modify C# scripts
4. **Scene Management** - Work with scenes programmatically
5. **Asset Management** - Handle Unity assets
6. **Real-time Communication** - WebSocket connection between Unity and MCP client

## Installation Steps

### 1. Install Node.js 18+
Required for running the MCP server

### 2. Add Unity Package
Via Unity Package Manager:
- Add package from git URL: `https://github.com/CoderGamester/mcp-unity.git`

### 3. Configure Cursor
Two options:
- **Option A**: Use Unity Editor UI (Tools > MCP Unity > Server Window > Configure)
- **Option B**: Manually edit Cursor MCP config

### 4. Start Server
- Open Unity Editor
- Navigate to Tools > MCP Unity > Server Window
- Click "Start Server" (runs on port 8090 by default)

### 5. Use in Cursor
- Open your Unity project in Cursor
- Cursor can now interact with Unity Editor through MCP
- AI can generate scripts, create GameObjects, modify scenes, etc.

## How It Works

```
Cursor IDE <-> MCP Protocol <-> Node.js Server <-> WebSocket <-> Unity Editor
```

1. You write prompts in Cursor
2. Cursor sends commands via MCP
3. Node.js server translates to Unity WebSocket commands
4. Unity Editor executes the commands
5. Results sent back to Cursor

## Benefits for Your Project

With Unity MCP + Cursor, you can:
- **Write C# scripts in Cursor** with full context
- **Generate terrain code** and have it immediately work in Unity
- **Create GameObjects programmatically** from Cursor
- **Test and iterate quickly** without manual Unity Editor work
- **Build entire procedural systems** code-first

## Perfect for Your Use Case

Since you want to:
- Build everything in Cursor ✓
- Work code-first (minimal editor work) ✓
- Create procedural world generation ✓
- Use seed-based generation ✓

Unity MCP is ideal because:
- You write all generation code in Cursor
- AI understands Unity context
- Can test/run directly from Cursor
- Minimal Unity Editor interaction needed
