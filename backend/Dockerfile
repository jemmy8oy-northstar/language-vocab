# Stage 1: Build .NET app
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /source

# Copy solution and project files
COPY Balenthiran.LanguageVocab.slnx .
COPY Balenthiran.LanguageVocab.WebApi/*.csproj ./Balenthiran.LanguageVocab.WebApi/
COPY Balenthiran.LanguageVocab.Abstractions/*.csproj ./Balenthiran.LanguageVocab.Abstractions/
COPY Balenthiran.LanguageVocab.DataModels/*.csproj ./Balenthiran.LanguageVocab.DataModels/
COPY Balenthiran.LanguageVocab.DomainModels/*.csproj ./Balenthiran.LanguageVocab.DomainModels/
COPY Balenthiran.LanguageVocab.Services/*.csproj ./Balenthiran.LanguageVocab.Services/
COPY Balenthiran.LanguageVocab.EntityModels/*.csproj ./Balenthiran.LanguageVocab.EntityModels/ 
COPY Balenthiran.LanguageVocab.Database/*.csproj ./Balenthiran.LanguageVocab.Database/
# Restore dependencies
RUN dotnet restore

# Copy all source files
COPY . .

# Build and publish
RUN dotnet publish Balenthiran.LanguageVocab.WebApi/Balenthiran.LanguageVocab.WebApi.csproj -c Release -o /app/publish

# Stage 2: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=build /app/publish .

# Expose port (default ASP.NET Core port is 8080 in .NET 8+)
EXPOSE 8080

ENTRYPOINT ["dotnet", "Balenthiran.LanguageVocab.WebApi.dll"]
