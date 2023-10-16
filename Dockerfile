FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /app

COPY . ./

RUN dotnet publish "RecipesAPI.API/RecipesAPI.API.csproj" -c Release -o /app/publish \
    --runtime linux-x64 \
    --self-contained true \
    /p:PublishSingleFile=true

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine

WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /app/scripts/frida/output/final/frida.csv /data/frida.csv
ENTRYPOINT ["./RecipesAPI.API"]