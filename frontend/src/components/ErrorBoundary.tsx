import React, { Component, type ErrorInfo, type ReactNode } from 'react';
import { Box, Typography, Button, Container, Paper } from '@mui/material';
import { logger } from '../services/telemetry';

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends Component<Props, State> {
  public state: State = {
    hasError: false,
    error: null,
  };

  public static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    logger.error('React Component ErrorBoundary caught crash', error, {
      componentStack: errorInfo.componentStack,
    });
  }

  private handleReset = () => {
    this.setState({ hasError: false, error: null });
    window.location.reload();
  };

  public render() {
    if (this.state.hasError) {
      return (
        <Container maxWidth="sm" sx={{ mt: 8 }}>
          <Paper sx={{ p: 4, textAlign: 'center', borderRadius: 2 }}>
            <Typography variant="h5" color="error" gutterBottom fontWeight="bold">
              Something went wrong
            </Typography>
            <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
              An unexpected error occurred. The incident has been recorded for diagnostics.
            </Typography>
            {this.state.error && (
              <Box
                sx={{
                  p: 2,
                  mb: 3,
                  bgcolor: 'action.hover',
                  borderRadius: 1,
                  fontFamily: 'monospace',
                  fontSize: '0.85rem',
                  textAlign: 'left',
                  overflowX: 'auto',
                }}
              >
                {this.state.error.message}
              </Box>
            )}
            <Button variant="contained" color="primary" onClick={this.handleReset}>
              Reload Application
            </Button>
          </Paper>
        </Container>
      );
    }

    return this.props.children;
  }
}

