FROM ollama/ollama:latest
EXPOSE 11434

COPY OllamaModelfile modelfile
RUN nohup bash -c "ollama serve &" && sleep 5 && ollama create huihui_ai/phi4-abliterated-petunio -f /modelfile