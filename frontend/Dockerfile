FROM node:22-alpine AS build
WORKDIR /app
COPY frontend/package*.json ./
RUN npm install
COPY frontend/ ./
RUN npm install \
 && chmod +x node_modules/.bin/* || true
ARG VITE_API_BASE_URL=/api/v1
ARG VITE_API_ORIGIN=
ARG VITE_APP_ENV=production
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_API_ORIGIN=$VITE_API_ORIGIN
ENV VITE_APP_ENV=$VITE_APP_ENV
RUN npm run build

FROM nginx:1.27-alpine AS final
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
