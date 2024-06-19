# cms client

Navigate to your project directory:
Open your terminal or command prompt and navigate to the root directory of your Git project.

Add the submodule:
Use the git submodule add command followed by the repository URL and the directory path where you want the submodule to be located. In your case, assuming you want the submodule in a directory called code-editor, the command would be:

```shell
git submodule add git@gitlab.shenxun.org:js/cms-client.git
```

Commit the changes:
After adding the submodule, stage and commit the changes to your main repository:

```shell
git add .
git commit -m "Added cms client module"
```

Push the changes:
If your repository is hosted remotely and you want to push the changes:
```shell
git push
```
This process will add the specified Git submodule to your project, allowing you to work with it as part of your main repository.

cloning and initializing submodule
```shell
git submodule init
git submodule update --recursive
```
