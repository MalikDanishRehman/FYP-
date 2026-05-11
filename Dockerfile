# Build and publish the Blazor / ASP.NET Core web app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY AI_Driven_Water_Supply.Presentation.sln ./
COPY AI_Driven_Water_Supply.Presentation/AI_Driven_Water_Supply.Presentation.csproj AI_Driven_Water_Supply.Presentation/
COPY AI_Driven_Water_Supply.Application/AI_Driven_Water_Supply.Application.csproj AI_Driven_Water_Supply.Application/
COPY AI_Driven_Water_Supply.Domain/AI_Driven_Water_Supply.Domain.csproj AI_Driven_Water_Supply.Domain/
COPY AI_Driven_Water_Supply.Infrastructure/AI_Driven_Water_Supply.Infrastructure.csproj AI_Driven_Water_Supply.Infrastructure/

RUN dotnet restore AI_Driven_Water_Supply.Presentation/AI_Driven_Water_Supply.Presentation.csproj

COPY AI_Driven_Water_Supply.Presentation/ AI_Driven_Water_Supply.Presentation/
COPY AI_Driven_Water_Supply.Application/ AI_Driven_Water_Supply.Application/
COPY AI_Driven_Water_Supply.Domain/ AI_Driven_Water_Supply.Domain/
COPY AI_Driven_Water_Supply.Infrastructure/ AI_Driven_Water_Supply.Infrastructure/

RUN dotnet publish AI_Driven_Water_Supply.Presentation/AI_Driven_Water_Supply.Presentation.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "AI_Driven_Water_Supply.Presentation.dll"]
