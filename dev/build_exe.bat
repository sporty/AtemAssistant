
pyinstaller --onefile ^
    --name atem_assistant --icon images\icon.ico ^
    --add-binary="atem_sdk\AtemSDK\x64\Release\TestAtem.exe;.\atem_sdk\AtemSDK\x64\Release" ^
    python\atem_assistant\cli.py
