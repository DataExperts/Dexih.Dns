#!/usr/bin/env bash
# This script updates the dexih.api binaries 

# An error exit function
error_exit()
{
        echo "$1" 1>&2
        exit 1
}

DIRECTORY='Dns'

JSON=`curl -s https://api.github.com/repos/DataExperts/Dexih.Dns/releases/latest`
BROWSER_PATH=`echo ${JSON} | jq -r .assets[0].browser_download_url`
FILENAME=`echo ${JSON} | jq -r .assets[0].name`
wget ${BROWSER_PATH}

TEMP_DIRECTORY=`mktemp -d -t dexih.dns.XXXXX`
BACKUP_DIRECTORY="dexih_dns_`date +"%Y%m%d%H%M%S"`"

unzip -q ${FILENAME} -d ${TEMP_DIRECTORY}
mv ${DIRECTORY} ${BACKUP_DIRECTORY}
mv ${TEMP_DIRECTORY} ${DIRECTORY}
rm ${FILENAME}

sudo systemctl restart dexih.dns.service