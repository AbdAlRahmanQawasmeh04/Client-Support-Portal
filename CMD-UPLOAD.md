# Upload from Windows CMD

Run these in the prepared project root containing Backend, Frontend and README.md. They are for a **new local repository and empty GitHub repository**, not for replacing an existing repository history.

```cmd
git init -b main
git status --short
git add .
git diff --cached --stat
```

Review the staged list. It must not include local appsettings, credentials, databases, App_Data, node_modules, bin or obj. The default .gitignore excludes these.

```cmd
git commit -m "Add support portal frontend, backend and documentation"
```

If Git asks for identity, configure your actual name and email locally in this repository, then repeat the commit. Do not guess someone else's identity.

Create an empty GitHub repository. Replace the example URL below with the real repository URL before running it:

```cmd
git remote add origin https://github.com/YOUR_ACCOUNT/YOUR_REPOSITORY.git
git push -u origin main
```

Authenticate through Git's sign-in flow when prompted. Do not put access tokens or passwords in the remote URL or this file. If the remote already has commits, stop and inspect it instead of force-pushing.

After uploading, refresh GitHub and verify the README images, both project folders and absence of sensitive files. This document does not mean an upload has happened.
