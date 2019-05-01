FROM node:10.4-alpine AS builder

WORKDIR /usr/src/app
COPY ./src/frontend .
RUN npm ci && npm run build

FROM nginx:stable-alpine
LABEL version="1.0"

COPY ./src/frontend/nginx.conf /etc/nginx/nginx.conf

WORKDIR /usr/share/nginx/html
COPY --from=builder /usr/src/app/dist/math-quiz/ .