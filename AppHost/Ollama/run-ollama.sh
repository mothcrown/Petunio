#!/bin/bash

echo "Starting Ollama server..."
ollama serve &


echo "Waiting for Ollama server to be active..."
while [ "$(ollama list | grep 'NAME')" == "" ]; do
  sleep 1
done


ollama pull nomic-embed-text
ollama pull huihui_ai/phi4-abliterated
ollama create huihui_ai/phi4-abliterated-petunio -f /modelfile