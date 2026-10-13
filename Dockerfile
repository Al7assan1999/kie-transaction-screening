FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Screening.Api/Screening.Api.csproj src/Screening.Api/
RUN dotnet restore src/Screening.Api/Screening.Api.csproj
COPY src/ src/
RUN dotnet publish src/Screening.Api -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Screening.Api.dll"]
