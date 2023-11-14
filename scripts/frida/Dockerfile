FROM python:3.12-slim

RUN apt update && apt upgrade -y
RUN apt install -y unzip curl

RUN pip install csvkit

COPY ./app/get-data.sh /app/get-data.sh

WORKDIR /app
CMD ["./get-data.sh"]