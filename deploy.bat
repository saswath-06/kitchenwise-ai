@echo off
echo 🚀 KitchenWise Quick Deployment
echo ===============================
echo.

echo 📱 Publishing Desktop Application...
dotnet publish KitchenWise.Desktop -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish/desktop

echo.
echo 🌐 Publishing API...
dotnet publish KitchenWise.Api -c Release -o ./publish/api

echo.
echo ✅ Deployment Complete!
echo.
echo 📂 Published files:
echo   Desktop: .\publish\desktop\KitchenWise.Desktop.exe
echo   API:     .\publish\api\
echo.
echo 📖 See DEPLOYMENT_GUIDE.md for configuration instructions
echo.
pause
