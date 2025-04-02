#!/bin/

if [ ! -d "../ComfUI" ]; then
    git clone https://github.com/mothcrown/docker-comfyui-petunio.git ComfyUI
else
    cd ComfyUI && git pull
fi