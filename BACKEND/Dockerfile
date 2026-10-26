# 1. Compilación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de proyecto y restaurar dependencias
COPY *.csproj ./
RUN dotnet restore

# Copiar el resto del código y compilar en modo Release
COPY . ./
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# 2. Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Reemplaza 'NombreDeTuApi.dll' por el nombre real de tu archivo compilado (el nombre de tu proyecto .csproj)
ENTRYPOINT ["dotnet", "FichajeApp.dll"]