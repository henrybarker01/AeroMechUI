# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for layer-cached restore
COPY ["Aeromech.UI.Web/AeroMech.UI.Web.csproj", "Aeromech.UI.Web/"]
COPY ["AeroMech.Data/AeroMech.Data.csproj", "AeroMech.Data/"]
COPY ["AeroMech.Models/AeroMech.Models.csproj", "AeroMech.Models/"]
RUN dotnet restore "Aeromech.UI.Web/AeroMech.UI.Web.csproj"

COPY Aeromech.UI.Web/ Aeromech.UI.Web/
COPY AeroMech.Data/ AeroMech.Data/
COPY AeroMech.Models/ AeroMech.Models/
RUN dotnet publish "Aeromech.UI.Web/AeroMech.UI.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Native deps for QuestPDF (SkiaSharp) + fonts for PDF rendering
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 fonts-liberation \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "AeroMech.UI.Web.dll"]
