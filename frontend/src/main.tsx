import { CssBaseline, ThemeProvider, createTheme } from '@mui/material';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import App from './App';

const theme = createTheme({
  palette: {
    primary: { main: '#00695c' },
    secondary: { main: '#f2b880' },
    background: { default: '#f3f0e9', paper: '#fff' },
  },
  typography: {
    fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif',
    h1: { fontFamily: '"Manrope", sans-serif', fontWeight: 800, letterSpacing: '-.055em' },
    h2: { fontFamily: '"Manrope", sans-serif', fontWeight: 800, letterSpacing: '-.05em' },
    h3: { fontFamily: '"Manrope", sans-serif', fontWeight: 800, letterSpacing: '-.04em' },
    h4: { fontFamily: '"Manrope", sans-serif', fontWeight: 800, letterSpacing: '-.03em' },
    h5: { fontFamily: '"Manrope", sans-serif', fontWeight: 800, letterSpacing: '-.02em' },
    h6: { fontFamily: '"Manrope", sans-serif', fontWeight: 800, letterSpacing: '-.02em' },
    button: { textTransform: 'none' as const },
  },
  shape: { borderRadius: 12 },
  components: {
    MuiAppBar: {
      styleOverrides: {
        root: {
          borderRadius: 0,
        },
      },
    },
    MuiButton: {
      styleOverrides: {
        root: { borderRadius: 9999, fontWeight: 600 },
        contained: { boxShadow: 'none', '&:hover': { boxShadow: 'none' } },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          borderRadius: 12,
          boxShadow: 'none',
          border: '1px solid',
          borderColor: 'rgba(0,0,0,.12)',
          transition: 'box-shadow .2s ease, transform .2s ease',
          '&:hover': {
            boxShadow: '0 4px 20px rgba(0,0,0,.08)',
          },
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: { borderRadius: 12 },
      },
    },
  },
});

const client = new QueryClient();

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <QueryClientProvider client={client}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </ThemeProvider>
    </QueryClientProvider>
  </React.StrictMode>,
);
