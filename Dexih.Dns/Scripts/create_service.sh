sudo cp dexih.dns.service /etc/systemd/system
sudo systemctl daemon-reload
sudo systemctl enable dexih-dns.service
sudo systemctl start dexih-dns.service