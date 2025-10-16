FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# �������� ����� ������ � ���������
COPY ./publish/ ./

# ���� ��� �� ���-����������, ����� ������ ������ ����
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "TwitchChatBot.dll"]
