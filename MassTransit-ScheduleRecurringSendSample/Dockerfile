#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MassTransit-ScheduleRecurringSendSample/MassTransit-ScheduleRecurringSendSample.csproj", "MassTransit-ScheduleRecurringSendSample/"]
RUN dotnet restore "MassTransit-ScheduleRecurringSendSample/MassTransit-ScheduleRecurringSendSample.csproj"
COPY . .
WORKDIR "/src/MassTransit-ScheduleRecurringSendSample"
RUN dotnet build "MassTransit-ScheduleRecurringSendSample.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MassTransit-ScheduleRecurringSendSample.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MassTransit-ScheduleRecurringSendSample.dll"]
