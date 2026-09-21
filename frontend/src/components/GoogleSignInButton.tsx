import { Alert, Box, Divider, Typography } from '@mui/material';
import { useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';

// Prefer the runtime-injected config (written by the container entrypoint from
// env vars) so the client id never has to be baked into the image. Fall back
// to the Vite build-time variable for local development.
const clientId = window.__MEDMATCH_CONFIG__?.GOOGLE_CLIENT_ID || (import.meta.env.VITE_GOOGLE_CLIENT_ID as string | undefined);

type GoogleCredentialResponse = { credential: string };
type GoogleAccounts = { id: { initialize: (options: { client_id: string; callback: (response: GoogleCredentialResponse) => void }) => void; renderButton: (element: HTMLElement, options: { theme: string; size: string; width: number }) => void } };

declare global { interface Window { google?: { accounts: GoogleAccounts } } }

export default function GoogleSignInButton({ onCredential }: { onCredential: (credential: string) => void }) {
  const { t } = useTranslation();
  const buttonRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!clientId || !buttonRef.current) return;
    const render = () => {
      if (!window.google || !buttonRef.current) return;
      window.google.accounts.id.initialize({ client_id: clientId, callback: ({ credential }) => onCredential(credential) });
      buttonRef.current.replaceChildren();
      window.google.accounts.id.renderButton(buttonRef.current, { theme: 'outline', size: 'large', width: 390 });
    };
    if (window.google) { render(); return; }
    const script = document.createElement('script');
    script.src = 'https://accounts.google.com/gsi/client'; script.async = true; script.defer = true; script.onload = render;
    document.head.appendChild(script);
    return () => { script.remove(); };
  }, [onCredential]);

  if (!clientId) return <Alert severity="info">{t('googleSignIn.notConfigured')}</Alert>;
  return <Box><Divider sx={{ my: 1 }}><Typography variant="caption" color="text.secondary">{t('googleSignIn.or')}</Typography></Divider><Box ref={buttonRef} sx={{ display: 'flex', justifyContent: 'center' }} /></Box>;
}
