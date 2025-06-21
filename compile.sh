## please keep me SH or atleast BASH only


## TO-DO
## ask user for GAME_DIR , cache it in ./.generated/GAME_DIRCache.txt single line	##TODO-gameDir

echo "REQUIRES: wget tar"
echo "DEBIAN EXAMPLE: sudo apt install wget tar"
scriptDir="${PWD}"	##suboptimal method, but whatever
##test -z "$scriptDir" || echo "please run script with terminal in the folder its in" && exit ##lol no echo if no terminal
buildDir="$scriptDir/ClassLibrary1"
downloadDir="$scriptDir/.downloaded"
generatedDir="$scriptDir/.generator"
dotnetDir="$scriptDir/.downloaded/dotnet"
mkdir "$downloadDir"
mkdir "$generatedDir"
mkdir "$dotnetDir"

dotnetUrl="https://builds.dotnet.microsoft.com/dotnet/Sdk/8.0.411/dotnet-sdk-8.0.411-linux-x64.tar.gz"
dotnetExecutable="$dotnetDir/dotnet"
export PATH+=":$dotnetDir"	##DEBUG ##also speeds up if already downloaded
echo $PATH
if command -v "dotnet"; then
	dotnetExecutable=dotnet
else
	echo "dotnet command not found, downloading to: $dotnetDir"
	cd  "$downloadDir"
	wget -c "$dotnetUrl" -O "$downloadDir/dotnet.tar.gz"
	cd  "$dotnetDir"
	tar --extract --file="$downloadDir/dotnet.tar.gz" -gzip
	chmod +x "$dotnetExecutable"
fi

cd "$buildDir"
##ls ##DEBUG
gameDir="/F0/steamLib/steamapps/common/OxygenNotIncluded/"	##TODO-gameDir
##echo "FORMATING"
##GAME_DIR="$gameDir" "$dotnetExecutable" format --verbosity normal ##how is normal not default, bruh
echo "BUILDING"
GAME_DIR="$gameDir" "$dotnetExecutable" build