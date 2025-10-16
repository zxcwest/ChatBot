FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# копируем файлы сборки в контейнер
COPY ./publish/ ./

# если это не веб-приложение, можно убрать строки ниже
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "TwitchChatBot.dll"]
