import { Alert, CircularProgress, Stack } from '@mui/material';
export function Loading() { return <Stack alignItems="center" sx={{ py: 6 }}><CircularProgress /></Stack>; }
export function ErrorState({ error }: { error: unknown }) { return <Alert severity="error">{error instanceof Error ? error.message : 'Something went wrong.'}</Alert>; }
