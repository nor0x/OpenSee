echo "restoring nugets"
nuget restore OpenSee.Common/OpenSee.Common.csproj
nuget restore OpenSeeXF/OpenSeeXF/OpenSeeXF.csproj
nuget restore OpenSeeXF/OpenSeeXF.MacOS/OpenSeeXF.MacOS.csproj

echo "building class libs"
dotnet build OpenSeeXF/OpenSeeXF/OpenSeeXF.csproj -c Release
dotnet build OpenSee.Common/OpenSee.Common.csproj -c Release


#Preview Version of VS4Mac
#/Applications/Visual\ Studio\ \(Preview\).app/Contents/MacOS/vstool build -t:Build -c:"Release" ~/Documents/dev/git/own/OpenSee/OpenSee.sln

/Applications/Visual\ Studio.app/Contents/MacOS/vstool build -t:Build -c:"Release" $GITHUB_WORKSPACE/OpenSee.sln

cd $GITHUB_WORKSPACE/OpenSeeXF/OpenSeeXF.MacOS/bin/Release
ditto -c -k --sequesterRsrc --keepParent OpenSee.app OpenSee.app.zip 
