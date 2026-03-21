FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY AiManual.API/*.csproj ./AiManual.API/
RUN dotnet restore AiManual.API/AiManual.API.csproj

COPY . .
RUN dotnet publish AiManual.API/AiManual.API.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "AiManual.API.dll"]
