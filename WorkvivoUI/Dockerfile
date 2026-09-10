# syntax=docker/dockerfile:1

# The web image: build the Angular bundle, then serve it as static files.
#
# Nothing from the build survives into the runtime image - no Node, no sources, no
# node_modules. What ships is nginx and a directory of hashed assets.

FROM node:22-alpine AS build
WORKDIR /src

# The lockfile first, so a source change does not re-run the install.
COPY package.json package-lock.json ./

# `npm ci` rather than `npm install`: it installs exactly the lockfile and fails if
# the two disagree, which is what makes the build reproducible.
RUN npm ci

COPY . .

RUN npm run build


FROM nginx:1.27-alpine AS runtime

# The default config serves /usr/share/nginx/html and 404s on any client route.
# ours rewrites unknown paths to index.html, which is what a single-page app needs.
COPY nginx.conf /etc/nginx/conf.d/default.conf

COPY --from=build /src/dist/WorkvivoUI/browser /usr/share/nginx/html

EXPOSE 8080
