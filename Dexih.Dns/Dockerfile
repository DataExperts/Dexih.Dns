FROM microsoft/dotnet:2.0-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.0-sdk AS build
WORKDIR /src
COPY Dexih.Dns.sln ./
COPY Dexih.Dns/Dexih.Dns.csproj Dexih.Dns/
RUN dotnet restore -nowarn:msb3202,nu1503
COPY . .
WORKDIR /src/Dexih.Dns
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .

ENV ROOT_IP_ADDRESS=104.18.32.48
ENV ROOT_DOMAIN=dexih.com
ENV DNS_IP_ADDRESS=60.224.193.14
ENV DNS_EMAIL=gholland.dataexpertsgroup.com
ENV DNS_TTL=300

ENTRYPOINT ["dotnet", "Dexih.Dns.dll"]
