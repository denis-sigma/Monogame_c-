@echo off
cd /d "c:\Users\user\Desktop\Monogame"
echo Компилируем проект...
call dotnet build
echo.
echo Запускаем игру...
call dotnet run --project MyGame
pause
