FROM node:22-alpine AS build
WORKDIR /source

COPY src/SeasonEnded.Web/package*.json ./
RUN npm ci

COPY src/SeasonEnded.Web/ ./
RUN npm run build

FROM nginx:1.29-alpine
COPY src/SeasonEnded.Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /source/dist /usr/share/nginx/html
EXPOSE 8080
