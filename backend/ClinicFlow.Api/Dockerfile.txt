FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["ClinicFlow.Api.csproj", "./"]
RUN dotnet restore "ClinicFlow.Api.csproj"

COPY . .
RUN dotnet build "ClinicFlow.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ClinicFlow.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:$PORT
ENTRYPOINT ["dotnet", "ClinicFlow.Api.dll"]