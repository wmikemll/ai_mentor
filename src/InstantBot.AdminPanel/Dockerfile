FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/InstantBot.AdminPanel/InstantBot.AdminPanel.csproj",     "src/InstantBot.AdminPanel/"]
COPY ["src/InstantBot.Application/InstantBot.Application.csproj",   "src/InstantBot.Application/"]
COPY ["src/InstantBot.Domain/InstantBot.Domain.csproj",             "src/InstantBot.Domain/"]
COPY ["src/InstantBot.Infrastructure/InstantBot.Infrastructure.csproj", "src/InstantBot.Infrastructure/"]
RUN dotnet restore "src/InstantBot.AdminPanel/InstantBot.AdminPanel.csproj"
COPY . .
RUN dotnet publish "src/InstantBot.AdminPanel/InstantBot.AdminPanel.csproj" \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InstantBot.AdminPanel.dll"]
