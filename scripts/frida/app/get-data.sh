#!/usr/bin/env bash

file_url="$(curl -I https://frida.fooddata.dk/download --silent | grep Location | tr -d '\r\n' | sed 's/^Location: //')"
filename="/app/output/tmp/$(basename $file_url)"
if [ ! -f $filename ]
then
    curl $(echo "$file_url") --silent -o "$filename"
else
    echo "File $filename already exists, not downloading"
    exit 0
fi

mkdir -p /app/output/final
mkdir -p /app/output/tmp
unzip -o "$filename" -d /app/output/tmp/fridafiles
in2csv --sheet "Data_Normalised" /app/output/tmp/fridafiles/*.xlsx > /app/output/final/frida.csv
