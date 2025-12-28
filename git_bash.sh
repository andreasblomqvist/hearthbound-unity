# Navigate to where you extracted HearthboundUnity.zip


# Initialize git (if not already done)
git init
git add -A
git commit -m "Initial commit: Unity seed-based world generation

- Converted from Godot to Unity
- Seed-based procedural terrain, villages, and forests
- AI-powered NPCs with Claude API
- Full Unity MCP integration for Cursor IDE
- Complete documentation"

# Add your new repository as remote
git branch -M main
git remote add origin https://github.com/andreasblomqvist/hearthbound-unity.git

# Push to GitHub
git push -u origin main
