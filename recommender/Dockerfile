# syntax=docker/dockerfile:1

# using ubuntu as base image because some of the dependencies cannot be installed with the official python image idk why
FROM ubuntu:latest

RUN apt-get update -y \
    && apt-get install -y python3-pip

COPY . ./recommender
WORKDIR ./recommender
RUN pip install --no-cache-dir -r requirements.txt

ENV FLASK_APP app.py
ENV PYTHONPATH "${PYTHONPATH}:/recommender"

# production needs WSGI for stableness, security and efficiency
CMD exec gunicorn --bind 0.0.0.0:5000 --workers 1 --threads 8 --timeout 0 wsgi:app