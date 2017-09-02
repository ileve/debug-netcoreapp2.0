# `setsln` utility for debugging asp.net core 2.0 with Visual Studio 2017 15.3

This utility is based on my answer in stackoverflow in the following link:

https://stackoverflow.com/questions/41656571/debugging-asp-net-core-1-1-mvc-source-code-in-visual-studio-2017-rc

it automates the steps 4 - 7 and specifically:
* modifies our solution file appropriately in order to include automatically all the required projects from the asp.net core open source code.
* modifies our `{our_project_name}.csproj` by adding all the required project references
* modifies all the `\*.csproj` files inside the `src\` folder of the asp.net core open source code. It actually automates step 4.

Before the modification of our solution and project files, a backup is created of the two files. Thus if want to revert back our existing project to its initial state, we just delete the modified solution and project files and rename the two respective files with `.backup` extension.
***
## Program usage
First we have to follow the steps 1 - 3 described in my answer in `stackoverflow`. Then unzip the files. Now the next step is to specify all the required files for the proper modifications. In the `ourslnpath.txt`, we write our solution and project files, with full directory paths, which we want to be modified properly for debugging the source code. In `srcslnpaths.txt` we write all the solution files \(`*.sln`\) of the open source code. For example we might want to debug both `MVC` and `Routing` source code. We download and unzip them and then we specify the full path of the files `MVC.sln` and `Routing.sln`.   We open the command prompt and we go to the directory where we built the executable. We just type `setsln`. If we receive no messages, then all the required changes have been applied successfully. 

We are now ready to open our solution files and all the projects in the `src\` folder will be included automatically with all the proper project references inside the `{our_project_name}.csproj`. 
