import { CssBaseline, ThemeProvider, createTheme } from '@mui/material';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react'; import ReactDOM from 'react-dom/client'; import { BrowserRouter } from 'react-router-dom';
import App from './App';

const client = new QueryClient();
ReactDOM.createRoot(document.getElementById('root')!).render(<React.StrictMode><QueryClientProvider client={client}><ThemeProvider theme={createTheme({ palette: { primary: { main: '#00695c' } } })}><CssBaseline /><BrowserRouter><App /></BrowserRouter></ThemeProvider></QueryClientProvider></React.StrictMode>);
