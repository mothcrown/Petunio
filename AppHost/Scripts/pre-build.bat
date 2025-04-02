@echo off
if not exist ComfyUI (
    git clone https://github.com/mothcrown/docker-comfyui-petunio.git ComfyUI
    powershell -command "(Get-Content ComfyUI\init.sh -Raw).Replace(\"`r`n\",\"`n\") | Set-Content ComfyUI\init.sh -Force"
) else (
    cd ComfyUI
    git pull
    cd ..
)