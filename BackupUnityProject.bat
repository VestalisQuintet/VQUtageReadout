@echo off
setlocal EnableDelayedExpansion

:: バッチファイル自身のパスを取得
set script_path=%~dp0
set script_name=%~nx0

:: プロジェクトディレクトリの指定（バッチファイル自身のディレクトリ）
set project_dir=%script_path%

:: 一時ディレクトリの作成
set temp_dir=%TEMP%\UnityProjectBackup
if exist "%temp_dir%" rd /s /q "%temp_dir%"
mkdir "%temp_dir%"

:: 除外するディレクトリとファイルのリストを作成（exclude_patterns.txt）
(
    echo Library
    echo Temp
    echo obj
    echo Build
    echo Builds
    echo Logs
    echo UserSettings
    echo MemoryCaptures
    echo Recordings
    echo .csproj
    echo .unityproj
    echo .sln
    echo .suo
    echo .tmp
    echo .user
    echo .userprefs
    echo .pidb
    echo .booproj
    echo .svd
    echo .pdb
    echo .mdb
    echo .opendb
    echo .VC.db
    echo .apk
    echo .aab
    echo .unitypackage
    echo .app
    echo sysinfo.txt
    echo crashlytics-build.properties
    echo Assets\AddressableAssetsData\*\*.bin*
    echo Assets\StreamingAssets\aa.meta
    echo Assets\StreamingAssets\aa\
    echo voicewavout
) > "%temp_dir%\exclude_patterns.txt"

:: プロジェクト全体を一時ディレクトリにコピー（除外するディレクトリとファイルを指定）
xcopy "%project_dir%\*" "%temp_dir%\" /s /e /y /i /exclude:%temp_dir%\exclude_patterns.txt

:: バッチファイル自身を一時ディレクトリにコピー
copy "%project_dir%\%script_name%" "%temp_dir%\%script_name%"

:: 保存先をプロンプトで質問
set /p save_path="保存先のパスとファイル名を入力してください（例：C:\Backup\Project.zip）: "

:: zip圧縮の実行
powershell -Command "Compress-Archive -Path '%temp_dir%\*' -DestinationPath '%save_path%'"

:: 一時ディレクトリの削除
rd /s /q "%temp_dir%"

echo Unityプロジェクトのバックアップが完了しました。
pause
