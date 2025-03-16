FROM ollama/ollama:latest

COPY ./OllamaModelfile modelfile
COPY ./run-ollama.sh run-ollama.sh

RUN chmod +x run-ollama.sh \
    && ./run-ollama.sh 

EXPOSE 11434