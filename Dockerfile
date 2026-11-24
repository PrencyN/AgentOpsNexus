FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY services/AgentOpsNexus.AgentApi/AgentOpsNexus.AgentApi.csproj \
    services/AgentOpsNexus.AgentApi/

RUN dotnet restore \
    services/AgentOpsNexus.AgentApi/AgentOpsNexus.AgentApi.csproj

COPY services/AgentOpsNexus.AgentApi/ \
    services/AgentOpsNexus.AgentApi/

RUN dotnet publish \
    services/AgentOpsNexus.AgentApi/AgentOpsNexus.AgentApi.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

RUN addgroup \
      --system \
      agentops \
    && adduser \
      --system \
      --ingroup agentops \
      agentops

COPY --from=build \
    --chown=agentops:agentops \
    /app/publish \
    .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV DOTNET_EnableDiagnostics=0

EXPOSE 8080

USER agentops

ENTRYPOINT ["dotnet", "AgentOpsNexus.AgentApi.dll"]
