#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ENV GOOGLE_APPLICATION_CREDENTIALS=/app/ocr-test-268205-1a43299b2f58.json

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
ENV http_proxy=http://srvwlproxy.evertrust.com.tw:6588
ENV https_proxy=http://srvwlproxy.evertrust.com.tw:6588
WORKDIR /src
COPY ["ImgTextApi/ImgTextApi.csproj", "ImgTextApi/"]
RUN dotnet restore "ImgTextApi/ImgTextApi.csproj"
COPY . .
WORKDIR "/src/ImgTextApi"
RUN dotnet build "ImgTextApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ImgTextApi.csproj" -c Release -o /app/publish
ENV http_proxy=
ENV https_proxy=

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ImgTextApi.dll"]