import { createTheme, ThemeProvider } from '@mui/material';
import { createContext, useContext, useMemo, useState, type ReactNode } from 'react';

export type ColorMode = 'light' | 'dark';

const ColorModeContext = createContext<{ mode: ColorMode; toggleColorMode: () => void }>({
  mode: 'light',
  toggleColorMode: () => {},
});

export const useColorMode = () => useContext(ColorModeContext);

function buildTheme(mode: ColorMode) {
  const isDark = mode === 'dark';
  return createTheme({
    palette: {
      mode,
      primary: isDark
        ? { main: '#4db6ac', contrastText: '#0c1d1e' }
        : { main: '#00695c', contrastText: '#ffffff' },
      secondary: { main: '#f2b880' },
      background: isDark
        ? { default: '#0c1d1e', paper: '#13292a' }
        : { default: '#f3f0e9', paper: '#ffffff' },
      text: isDark
        ? { primary: '#f8f6f0', secondary: 'rgba(248,246,240,.72)' }
        : { primary: '#102a2b', secondary: 'rgba(16,42,43,.68)' },
      divider: isDark ? 'rgba(248,246,240,.14)' : 'rgba(16,42,43,.14)',
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
            borderColor: isDark ? 'rgba(248,246,240,.14)' : 'rgba(16,42,43,.14)',
            transition: 'box-shadow .2s ease, transform .2s ease',
            '&:hover': {
              boxShadow: isDark ? '0 4px 20px rgba(0,0,0,.45)' : '0 4px 20px rgba(0,0,0,.08)',
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
}

export function AppThemeProvider({ children }: { children: ReactNode }) {
  const [mode, setMode] = useState<ColorMode>(() =>
    typeof localStorage !== 'undefined' && localStorage.getItem('medmatch_theme') === 'dark' ? 'dark' : 'light',
  );

  const colorMode = useMemo(
    () => ({
      mode,
      toggleColorMode: () => {
        setMode((prev) => {
          const next = prev === 'light' ? 'dark' : 'light';
          localStorage.setItem('medmatch_theme', next);
          return next;
        });
      },
    }),
    [mode],
  );

  const theme = useMemo(() => buildTheme(mode), [mode]);

  return (
    <ColorModeContext.Provider value={colorMode}>
      <ThemeProvider theme={theme}>{children}</ThemeProvider>
    </ColorModeContext.Provider>
  );
}
