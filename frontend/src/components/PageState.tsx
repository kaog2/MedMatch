import { Alert, CircularProgress, Stack } from '@mui/material';
import { useTranslation } from 'react-i18next';
export function Loading() { return <Stack alignItems="center" sx={{ py: 6 }}><CircularProgress /></Stack>; }
export function ErrorState({ error }: { error: unknown }) { const { t } = useTranslation(); return <Alert severity="error">{error instanceof Error ? error.message : t('common.somethingWentWrong')}</Alert>; }
