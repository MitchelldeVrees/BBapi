# Build context is the frontend/ folder (see docker-compose.yml), so this Dockerfile
# can see the whole Angular workspace: the dma-order-intake library and the
# dma-order-intake-demo app that consumes it.

FROM node:22-alpine AS build
WORKDIR /workspace

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# The library must be built first: the workspace tsconfig path mapping for
# "dma-order-intake" points at its dist/ output, not its source.
RUN npx ng build dma-order-intake --configuration production
RUN npx ng build dma-order-intake-demo --configuration production

FROM nginx:alpine AS final
COPY --from=build /workspace/dist/dma-order-intake-demo/browser /usr/share/nginx/html
COPY apps/dma-order-intake-demo/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
