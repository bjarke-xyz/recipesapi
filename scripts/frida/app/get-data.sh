#!/usr/bin/env bash

rclone lsf r2:

file_url="$(curl -I https://frida.fooddata.dk/download --silent | grep Location | tr -d '\r\n' | sed 's/^Location: //')"
filename="/app/output/$(basename $file_url)"
if [ ! -f $filename ]
then
    curl $(echo "$file_url") --silent -o "$filename"
else
    echo "File $filename already exists, not downloading"
    exit 0
fi

# rm -f /app/output/data.*
unzip -o "$filename" -d /app/output/fridafiles
mkdir -p /app/output/final
in2csv --sheet "Data_Normalised" /app/output/fridafiles/*.xlsx > /app/output/final/data.csv
# csvjson /app/output/data.csv > /app/output/final/data.json

rclone sync /app/output/final r2:frida