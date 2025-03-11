FROM ollama/ollama:latest
EXPOSE 11434

COPY OllamaModelfile modelfile
RUN nohup bash -c "ollama serve &" && sleep 5 && ollama create mannix/llama3.1-8b-lexi-petunio -f /modelfile