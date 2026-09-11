#!/bin/sh
set -e

# Generate the runtime config from environment variables so that sensitive
# values (e.g. the Google OAuth client id) are never baked into the image.
GOOGLE_CLIENT_ID="${GOOGLE_CLIENT_ID:-}"

cat > /usr/share/nginx/html/config.js <<EOF
window.__MEDMATCH_CONFIG__ = {
  GOOGLE_CLIENT_ID: "${GOOGLE_CLIENT_ID}",
  API_URL: ""
};
EOF

exec "$@"
