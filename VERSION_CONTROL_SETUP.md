# Version Control System Setup Guide

## Git & GitHub Configuration

### 1. Initialize Git Repository

```bash
# Navigate to project root
cd "C:\Users\HP 840\Desktop\Learning\Best Programming Practices\WasteCollectionSystem(1)\WasteCollectionSystem\WasteCollectionSystem\WasteCollectionSystem"

# Initialize Git repository
git init

# Add all files
git add .

# Create initial commit
git commit -m "Initial commit: Waste Collection System with Docker, Design Patterns, and Testing"
```

### 2. Create GitHub Repository

1. Go to [GitHub.com](https://github.com)
2. Click "New repository"
3. Name it: `WasteCollectionSystem` or `GreenTrack`
4. **DO NOT** initialize with README (we already have one)
5. Click "Create repository"

### 3. Connect Local Repository to GitHub

**✅ CURRENTLY CONFIGURED:**
- Remote URL: `https://github.com/MUCYO-DAVID/WasteCollectionSystemBestp-exam.git`
- Repository is already connected and configured

```bash
# Verify remote (already configured)
git remote -v

# If you need to change remote URL:
git remote set-url origin https://github.com/MUCYO-DAVID/WasteCollectionSystemBestp-exam.git

# Push to GitHub (use appropriate branch)
git push -u origin main
# OR if on feature branch:
git push -u origin <branch-name>
```

### 4. Configure Git in Visual Studio / VS Code

#### Visual Studio:
1. Go to **Tools** → **Options** → **Source Control** → **Git Global Settings**
2. Set your name and email:
   ```
   git config --global user.name "Your Name"
   git config --global user.email "your.email@example.com"
   ```
3. Go to **View** → **Team Explorer**
4. Click **Settings** → **Repository Settings**
5. Verify remote URL is set correctly

#### VS Code:
1. Install "Git" extension (usually pre-installed)
2. Open Command Palette (Ctrl+Shift+P)
3. Type "Git: Clone" to clone, or "Git: Initialize Repository" if starting fresh
4. Use Source Control panel (Ctrl+Shift+G) to commit and push

### 5. Daily Workflow

```bash
# Check status
git status

# Add changes
git add .

# Commit with descriptive message
git commit -m "Add feature: [describe what you did]"

# Push to GitHub
git push origin main

# Pull latest changes (if working in team)
git pull origin main
```

### 6. Link to Issue Tracker (Jira/Trello)

#### Option A: Jira Integration
1. Create Jira account at [jira.com](https://www.atlassian.com/software/jira)
2. Create project in Jira
3. Link issues in commit messages:
   ```bash
   git commit -m "PROJ-123: Fix payment processing bug"
   ```

#### Option B: GitHub Issues (Built-in)
1. Go to your GitHub repository
2. Click "Issues" tab
3. Create issues for bugs/features
4. Reference in commits:
   ```bash
   git commit -m "Fix #5: Resolve database connection issue"
   ```

### 7. Verify Configuration

Run these commands to verify:

```bash
# Check Git is initialized
git status

# Check remote is configured
git remote -v

# Check user configuration
git config --list
```

### 8. .gitignore File

The project already has a `.gitignore` file that excludes:
- `bin/` and `obj/` folders
- User-specific files
- Sensitive configuration files

**Important**: Never commit `appsettings.json` with real secrets. Use `appsettings.Development.example.json` as a template.

### 9. Branching Strategy (Optional)

For team projects:
```bash
# Create feature branch
git checkout -b feature/payment-integration

# Work on feature, commit changes
git add .
git commit -m "Add MTN MoMo payment integration"

# Switch back to main
git checkout main

# Merge feature
git merge feature/payment-integration
```

### 10. Evidence for Examiner

Show the examiner:
1. GitHub repository URL
2. Commit history (multiple commits showing progress)
3. IDE Git integration (Visual Studio Team Explorer or VS Code Source Control)
4. Issue tracker link (if using Jira/Trello)

