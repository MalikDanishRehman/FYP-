# 1. Base Image - Yeh hamara live server ka environment hoga jahan app chalegi
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Kyunke project mein Python (AI) hai, toh base server par Python bhi install kar rahe hain
RUN apt-get update && \
    apt-get install -y python3 python3-pip && \
    rm -rf /var/lib/apt/lists/*

# 2. Build Image - Jahan hamara C# code compile (build) hoga
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Saari files ko container ke andar copy karo
COPY . .

# Dependencies (NuGet) restore karo aur project build karo
RUN dotnet restore "AI_Driven_Water_Supply.Presentation/AI_Driven_Water_Supply.Presentation.csproj"
WORKDIR "/src/AI_Driven_Water_Supply.Presentation"
RUN dotnet build "AI_Driven_Water_Supply.Presentation.csproj" -c Release -o /app/build

# 3. Publish Image - Extra kachra nikal kar sirf zaroori files bachaana
FROM build AS publish
RUN dotnet publish "AI_Driven_Water_Supply.Presentation.csproj" -c Release -o /app/publish

# 4. Final Image - Ab isko live karne ke liye ready karna
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Jab container start ho toh yeh .NET app chala de
ENTRYPOINT ["dotnet", "AI_Driven_Water_Supply.Presentation.dll"]
