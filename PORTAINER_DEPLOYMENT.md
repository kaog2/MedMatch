# Portainer Deployment

This deployment uses Harbor images and Nginx Proxy Manager.

## 1. Prepare the Docker host

The Portainer endpoint, Harbor image containers, and Nginx Proxy Manager must run on the same Docker host.

Create the external network once, if it does not already exist:

```bash
docker network create nginx_proxi_default
```

In Portainer, verify that the Nginx Proxy Manager container is attached to `nginx_proxi_default`.

## 2. Add Harbor to Portainer

In Portainer:

1. Open **Registries**.
2. Choose **Add registry**.
3. Select **Custom registry**.
4. Registry URL: `harbor.your-domain.example`.
5. Enter a Harbor robot account or deployment user with pull permission on the `medmatch` project.
6. Save the registry.

Do not put the Harbor password in Git or in the Compose file.

## 3. Create the Stack from Git

In Portainer:

1. Open **Stacks** and select **Add stack**.
2. Name it `medmatch`.
3. Select **Git repository**.
4. Enter the repository URL and branch.
5. Compose path:

```text
docker-compose.harbor.yml
```

6. Configure Git authentication if the repository is private.
7. Enable automatic updates only after the first manual deployment succeeds.

## 4. Add environment variables in Portainer

Use the Stack's **Environment variables** section. Add each key/value separately. Do not paste `env.dev`; it contains development credentials and invalid-certificate settings.

Required production values:

```text
ASPNETCORE_ENVIRONMENT=Production
DATABASE_NAME=medmatch
DATABASE_USER=<production database user>
DATABASE_PASSWORD=<long production database password>
JWT_SECRET=<long random production secret>
JWT_ISSUER=medmatch
JWT_AUDIENCE=medmatch-client
FRONTEND_URL=https://your-domain.example
GOOGLE_CLIENT_ID=<Google web client ID>
SMTP_HOST=mail.your-domain.example
SMTP_PORT=587
SMTP_USERNAME=noreply@your-domain.example
SMTP_PASSWORD=<Mailcow mailbox password>
SMTP_FROM_EMAIL=noreply@your-domain.example
SMTP_FROM_NAME=MedMatch
SMTP_ALLOW_INVALID_CERTIFICATE=false
HARBOR_REGISTRY=harbor.your-domain.example
HARBOR_PROJECT=medmatch
IMAGE_TAG=2026.09.09.1
```

Optional values have defaults in `docker-compose.harbor.yml`, but setting them explicitly is recommended:

```text
DATABASE_NAME=medmatch
JWT_ISSUER=medmatch
JWT_AUDIENCE=medmatch-client
SMTP_PORT=587
SMTP_FROM_NAME=MedMatch
HARBOR_REGISTRY=harbor.your-domain.example
HARBOR_PROJECT=medmatch
IMAGE_TAG=latest
```

Never set `SMTP_ALLOW_INVALID_CERTIFICATE=true` in production. Renew the Mailcow certificate instead.

## 5. Deploy

Click **Deploy the stack**. Portainer should pull:

```text
harbor.your-domain.example/medmatch/backend:<IMAGE_TAG>
harbor.your-domain.example/medmatch/frontend:<IMAGE_TAG>
```

The stack exposes no host ports for the application containers. They are reachable through the shared Docker network only.

## 6. Configure Nginx Proxy Manager

Create a Proxy Host for the public hostname, for example `medmatch.your-domain.example`:

- Scheme: `http`
- Forward hostname/IP: `medmatch-frontend`
- Forward port: `80`
- Enable Websockets Support if needed
- Request an SSL certificate through Let's Encrypt
- Enable **Force SSL**

The frontend image serves the SPA and its internal Nginx proxies `/api` and `/health` to `medmatch-backend:8080`. Nginx Proxy Manager only needs to forward the public host to `medmatch-frontend:80`. The repository Nginx is not used in this deployment.

Both containers must be attached to `nginx_proxi_default`.

## 7. Verify

Check in Portainer:

- `medmatch-frontend` is running.
- `medmatch-backend` is running.
- `medmatch-postgres` is healthy.
- Backend logs show migrations completed.
- `https://your-domain.example` loads the frontend.
- `https://your-domain.example/health` returns the API health response if routed publicly.
- Password registration sends email through Mailcow.
- Google sign-in works with the production Google authorized JavaScript origin.

When publishing a new release:

1. Build and push a new version tag with `scripts/push-harbor.ps1`.
2. Change `IMAGE_TAG` in Portainer.
3. Redeploy the stack.
4. Keep the previous tag available for rollback.

Without `-Tag`, the script generates a UTC version tag and also updates `latest`:

```powershell
.\scripts\push-harbor.ps1
```

You can provide an explicit version tag:

```powershell
.\scripts\push-harbor.ps1 -Tag "2026.09.09.2"
```

Version tags are preserved. Only the `latest` convenience tag moves forward.
