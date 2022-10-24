FROM python:3.8-slim-buster

RUN apt update && apt upgrade -y
RUN apt install -y unzip curl

RUN pip install csvkit
RUN curl https://rclone.org/install.sh | bash

COPY ./app/get-data.sh /app/get-data.sh
COPY ./conf/rclone.conf /root/.config/rclone/rclone.conf

WORKDIR /app
CMD ["./get-data.sh"]