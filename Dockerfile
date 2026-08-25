# --- build stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, cached separately from source copy.
COPY OmniHogar.slnx ./
COPY src/OmniHogar.Domain/OmniHogar.Domain.csproj src/OmniHogar.Domain/
COPY src/OmniHogar.Application/OmniHogar.Application.csproj src/OmniHogar.Application/
COPY src/OmniHogar.Infrastructure/OmniHogar.Infrastructure.csproj src/OmniHogar.Infrastructure/
COPY src/OmniHogar.WebApi/OmniHogar.WebApi.csproj src/OmniHogar.WebApi/
RUN dotnet restore src/OmniHogar.WebApi/OmniHogar.WebApi.csproj

COPY src/ src/
RUN dotnet publish src/OmniHogar.WebApi/OmniHogar.WebApi.csproj -c Release -o /app --no-restore

# --- dev stage (hot reload, source bind-mounted over /src at runtime) ---
FROM build AS dev
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
CMD ["dotnet", "watch", "run", "--no-launch-profile", "--project", "src/OmniHogar.WebApi/OmniHogar.WebApi.csproj"]

# --- runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "OmniHogar.WebApi.dll"]
