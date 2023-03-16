FROM mcr.microsoft.com/devcontainers/java:0-17-bullseye

RUN sudo apt-get update && \
    sudo apt-get -y install python3-pip && \
    pip3 install awscli awscli-local

USER vscode
RUN curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 7.0
ENV PATH="${PATH}:/home/vscode/.dotnet:/home/vscode/.dotnet/tools"