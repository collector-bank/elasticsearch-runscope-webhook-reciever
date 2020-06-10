#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Runscope.WebHook.Receiver.Api/Runscope.WebHook.Receiver.Api.csproj", "Runscope.WebHook.Receiver.Api/"]
RUN dotnet restore "Runscope.WebHook.Receiver.Api/Runscope.WebHook.Receiver.Api.csproj"
COPY . .
WORKDIR "/src/Runscope.WebHook.Receiver.Api"
RUN dotnet build "Runscope.WebHook.Receiver.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Runscope.WebHook.Receiver.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Runscope.WebHook.Receiver.Api.dll"]