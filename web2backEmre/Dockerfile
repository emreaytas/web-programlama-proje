FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# SSL sertifikası için gerekli dizini oluştur
RUN mkdir -p /https

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["webprogbackend.csproj", "./"]
RUN dotnet restore "webprogbackend.csproj"
COPY . .
RUN dotnet build "webprogbackend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "webprogbackend.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# SSL sertifikasını kopyala
COPY ./https/dev-cert.pfx /https/
ENTRYPOINT ["dotnet", "webprogbackend.dll"] 