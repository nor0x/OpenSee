find . -iname "bin" | xargs rm -rf
find . -iname "obj" | xargs rm -rf


# echo "restoring nugets"
nuget restore OpenSee.Common/OpenSee.Common.csproj
nuget restore OpenSeeXF/OpenSeeXF/OpenSeeXF.csproj
nuget restore OpenSeeXF/OpenSeeXF.MacOS/OpenSeeXF.MacOS.csproj

# echo "building class libs"
dotnet build OpenSeeXF/OpenSeeXF/OpenSeeXF.csproj -c Release
dotnet build OpenSee.Common/OpenSee.Common.csproj -c Release

msbuild OpenSee.sln /target:OpenSeeXF_MacOS:Rebuild /p:Configuration=Release

echo "checking files"
cd $GITHUB_WORKSPACE
ls
cd $GITHUB_WORKSPACE/OpenSeeXF/OpenSeeXF.MacOS/bin/Release
ls

ditto -c -k --sequesterRsrc --keepParent OpenSee.app OpenSee.app.zip 
ls