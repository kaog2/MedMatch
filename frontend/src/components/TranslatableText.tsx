import { Box, Button, Typography } from '@mui/material';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { translateText } from '../services/api';

type Props = {
  text: string;
  variant?: 'body1' | 'body2' | 'caption';
  sx?: object;
};

export default function TranslatableText({ text, variant = 'body1', sx }: Props) {
  const { t, i18n } = useTranslation();
  const [translated, setTranslated] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [failed, setFailed] = useState(false);

  const target = (i18n.resolvedLanguage ?? i18n.language ?? 'en').split('-')[0];

  const toggle = async () => {
    if (translated) {
      setTranslated(null);
      return;
    }
    setLoading(true);
    setFailed(false);
    try {
      setTranslated(await translateText(text, target));
    } catch {
      setFailed(true);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box>
      <Typography variant={variant} sx={sx}>
        {translated ?? text}
      </Typography>
      <Button size="small" onClick={toggle} disabled={loading} sx={{ mt: 0.25, textTransform: 'none' }}>
        {loading ? t('translate.translating') : translated ? t('translate.showOriginal') : t('translate.translate')}
      </Button>
      {failed && (
        <Typography variant="caption" color="error" sx={{ display: 'block', mt: 0.5 }}>
          {t('translate.error')}
        </Typography>
      )}
    </Box>
  );
}
