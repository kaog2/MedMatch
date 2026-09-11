import { Alert, Button, Paper, Stack, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router-dom';
import { api } from '../services/api';

export default function VerifyEmail() {
  const { t } = useTranslation();
  const [params] = useSearchParams();
  const token = params.get('token');
  const email = params.get('email');
  const [state, setState] = useState<'checking' | 'pending' | 'verified' | 'error'>(token ? 'checking' : 'pending');

  useEffect(() => {
    if (!token) return;
    api('/auth/verify-email', { method: 'POST', body: JSON.stringify({ token }) })
      .then(() => setState('verified'))
      .catch(() => setState('error'));
  }, [token]);

  return <Paper sx={{ p: 4, maxWidth: 560, mx: 'auto', mt: 6 }}><Stack spacing={2}>
    {state === 'pending' && <><Typography variant="h4">{t('verify.checkTitle')}</Typography><Typography color="text.secondary">{t(email ? 'verify.checkBodyEmail' : 'verify.checkBody', { email })}</Typography><Alert severity="info">{t('verify.expires')}</Alert></>}
    {state === 'checking' && <><Typography variant="h4">{t('verify.checkingTitle')}</Typography><Typography color="text.secondary">{t('verify.checkingBody')}</Typography></>}
    {state === 'verified' && <><Typography variant="h4">{t('verify.verifiedTitle')}</Typography><Alert severity="success">{t('verify.verifiedBody')}</Alert><Button component={Link} to="/login" variant="contained">{t('verify.goSignIn')}</Button></>}
    {state === 'error' && <><Typography variant="h4">{t('verify.errorTitle')}</Typography><Alert severity="error">{t('verify.errorBody')}</Alert><Button component={Link} to="/login" variant="outlined">{t('verify.backSignIn')}</Button></>}
  </Stack></Paper>;
}
