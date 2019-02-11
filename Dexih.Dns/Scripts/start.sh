#!/usr/bin/env bash

sudo ./env.sh
sudo ./Dexih.Dns &> log`date +%Y%m%d_%H%M%S`.txt &
disown