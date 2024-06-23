@echo off
setlocal EnableDelayedExpansion

:: �o�b�`�t�@�C�����g�̃p�X���擾
set script_path=%~dp0
set script_name=%~nx0

:: �v���W�F�N�g�f�B���N�g���̎w��i�o�b�`�t�@�C�����g�̃f�B���N�g���j
set project_dir=%script_path%

:: �ꎞ�f�B���N�g���̍쐬
set temp_dir=%TEMP%\UnityProjectBackup
if exist "%temp_dir%" rd /s /q "%temp_dir%"
mkdir "%temp_dir%"

:: ���O����f�B���N�g���ƃt�@�C���̃��X�g���쐬�iexclude_patterns.txt�j
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

:: �v���W�F�N�g�S�̂��ꎞ�f�B���N�g���ɃR�s�[�i���O����f�B���N�g���ƃt�@�C�����w��j
xcopy "%project_dir%\*" "%temp_dir%\" /s /e /y /i /exclude:%temp_dir%\exclude_patterns.txt

:: �o�b�`�t�@�C�����g���ꎞ�f�B���N�g���ɃR�s�[
copy "%project_dir%\%script_name%" "%temp_dir%\%script_name%"

:: �ۑ�����v�����v�g�Ŏ���
set /p save_path="�ۑ���̃p�X�ƃt�@�C��������͂��Ă��������i��FC:\Backup\Project.zip�j: "

:: zip���k�̎��s
powershell -Command "Compress-Archive -Path '%temp_dir%\*' -DestinationPath '%save_path%'"

:: �ꎞ�f�B���N�g���̍폜
rd /s /q "%temp_dir%"

echo Unity�v���W�F�N�g�̃o�b�N�A�b�v���������܂����B
pause
