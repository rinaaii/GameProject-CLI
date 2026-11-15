# Publish this project to GitHub

The project is ready to publish, but a GitHub repository must be created under the student's own account. Do not publish API keys or personal data. The project does not require an API key.

1. Sign in to GitHub and create a **public** repository named `GameProject-CLI` (do not initialize it with another README if you plan to push this folder as-is).
2. Open a terminal in the extracted `GameProject_refactored` folder.
3. Run:

```bash
git init
git add .
git commit -m "Complete GameProject console interface"
git branch -M main
git remote add origin https://github.com/YOUR-USERNAME/GameProject-CLI.git
git push -u origin main
```

4. Replace `YOUR-USERNAME` with your GitHub username.
5. Open the repository in a browser and copy its URL. Submit that URL and the design document link (`GameProject/docs/design-document.md`) to the instructor.

Before publishing, run the build and test commands from `GameProject/README.md` and verify the demo scenarios in the design document.
