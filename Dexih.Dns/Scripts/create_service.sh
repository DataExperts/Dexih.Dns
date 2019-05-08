mkdir /hoem/ubuntu/app
dotnet publish -r Release -o /home/ubuntu/app

sudo cp dexih.dns.service /etc/systemd/system
sudo systemctl daemon-reload
sudo systemctl enable dexih.dns.service
sudo systemctl start dexih.dns.service