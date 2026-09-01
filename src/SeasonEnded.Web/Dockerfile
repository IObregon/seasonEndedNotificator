FROM node:22-alpine AS build
WORKDIR /source

COPY src/SeasonEnded.Web/package*.json ./
RUN npm ci

COPY src/SeasonEnded.Web/ ./
RUN npm run build

FROM nginx:1.29-alpine

RUN apk add --no-cache openssl wget && \
    mkdir -p /etc/nginx/certs && \
    openssl req -x509 -nodes -days 365 \
      -newkey rsa:2048 \
      -keyout /etc/nginx/certs/key.pem \
      -out /etc/nginx/certs/cert.pem \
      -subj "/CN=localhost"

COPY src/SeasonEnded.Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /source/dist /usr/share/nginx/html
EXPOSE 80 443
