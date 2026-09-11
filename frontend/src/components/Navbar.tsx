import {
  AppBar,
  Badge,
  Box,
  Button,
  Container,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemText,
  Stack,
  SvgIcon,
  Toolbar,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { useAuthStore } from '../store';
import { api, MatchSummary } from '../services/api';
import MedMatchIcon from './MedMatchIcon';

function MenuIcon() {
  return (
    <SvgIcon viewBox="0 0 24 24">
      <path fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" d="M4 6h16M4 12h16M4 18h16" />
    </SvgIcon>
  );
}

function CloseIcon() {
  return (
    <SvgIcon viewBox="0 0 24 24">
      <path fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
    </SvgIcon>
  );
}

/** Pill-style navigation link for the desktop navbar. */
function NavPill({ to, label, badgeCount }: { to: string; label: string; badgeCount?: number }) {
  return (
    <Button
      component={Link}
      to={to}
      sx={{
        color: 'rgba(248,246,240,.85)',
        textTransform: 'none',
        fontWeight: 500,
        fontSize: '.875rem',
        px: 2,
        py: 0.75,
        borderRadius: '9999px',
        border: '1px solid transparent',
        transition: 'all .15s ease',
        display: 'inline-flex',
        alignItems: 'center',
        gap: 0.8,
        '&:hover': {
          bgcolor: 'rgba(255,255,255,.08)',
          borderColor: 'rgba(255,255,255,.1)',
          color: '#fff',
        },
      }}
    >
      <span>{label}</span>
      {typeof badgeCount === 'number' && badgeCount > 0 && (
        <Box
          component="span"
          sx={{
            bgcolor: '#b06f42',
            color: '#fff',
            fontSize: '.68rem',
            fontWeight: 800,
            borderRadius: '9999px',
            px: 0.8,
            py: 0.1,
            lineHeight: 1.2,
          }}
        >
          {badgeCount > 99 ? '99+' : badgeCount}
        </Box>
      )}
    </Button>
  );
}

export default function Navbar() {
  const { role, signOut } = useAuthStore();
  const navigate = useNavigate();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const [drawerOpen, setDrawerOpen] = useState(false);

  const { data: matchSummary } = useQuery({
    queryKey: ['match-summary'],
    queryFn: () => api<MatchSummary>('/matches/summary'),
    enabled: role === 'Patient',
    refetchInterval: 15000,
  });
  const unreadMatches = matchSummary?.unreadCount ?? 0;

  const handleSignOut = () => {
    signOut();
    navigate('/');
    setDrawerOpen(false);
  };

  // Shared nav items for desktop and mobile
  const navItems: { to: string; label: string; show: boolean; badgeCount?: number }[] = [
    { to: '/clinics', label: 'Care providers', show: true },
    { to: '/people', label: 'People', show: role === 'Patient' },
    { to: '/matches', label: 'Matches', show: role === 'Patient', badgeCount: unreadMatches },
    { to: '/profile', label: 'Profile', show: role === 'Patient' },
    { to: '/clinic-patients', label: 'Patients', show: role === 'Clinic' },
    { to: '/admin', label: 'Admin', show: role === 'Admin' },
  ];

  return (
    <>
      <AppBar
        position="sticky"
        elevation={0}
        sx={{
          bgcolor: '#102a2b',
          borderBottom: '1px solid rgba(255,255,255,.12)',
          backdropFilter: 'blur(12px)',
          borderRadius: 0,
          width: '100%',
        }}
      >
        <Container maxWidth="lg" disableGutters sx={{ px: { xs: 2, md: 3 } }}>
          <Toolbar disableGutters sx={{ height: { xs: 64, md: 72 }, gap: 1 }}>
            {/* ---- Logo / Brand ---- */}
            <Box
              component={Link}
              to="/"
              sx={{
                display: 'flex',
                alignItems: 'center',
                gap: 1.25,
                textDecoration: 'none',
                color: 'inherit',
                mr: 'auto',
                '&:hover .icon-wrapper': {
                  bgcolor: 'rgba(0,105,92,.25)',
                  borderColor: 'rgba(0,105,92,.5)',
                },
              }}
            >
              <Box
                className="icon-wrapper"
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  width: 38,
                  height: 38,
                  borderRadius: '10px',
                  bgcolor: 'rgba(0,105,92,.15)',
                  border: '1px solid rgba(0,105,92,.3)',
                  transition: 'all .2s ease',
                }}
              >
                <MedMatchIcon sx={{ fontSize: 22, color: '#00695c' }} />
              </Box>
              <Box>
                <Typography
                  variant="h6"
                  sx={{
                    fontWeight: 800,
                    fontSize: '1.25rem',
                    letterSpacing: '-.02em',
                    lineHeight: 1,
                    color: '#fff',
                  }}
                >
                  MedMatch
                </Typography>
                <Typography
                  sx={{
                    fontSize: '.6rem',
                    fontWeight: 600,
                    textTransform: 'uppercase',
                    letterSpacing: '.12em',
                    color: 'rgba(94,199,183,.55)',
                    mt: 0.25,
                  }}
                >
                  Health Network
                </Typography>
              </Box>
            </Box>

            {/* ---- Desktop nav ---- */}
            {!isMobile && (
              <Stack direction="row" alignItems="center" spacing={0.5}>
                {navItems
                  .filter((item) => item.show)
                  .map((item) => (
                    <NavPill key={item.to} to={item.to} label={item.label} badgeCount={item.badgeCount} />
                  ))}

                <Divider
                  orientation="vertical"
                  flexItem
                  sx={{ mx: 1, borderColor: 'rgba(255,255,255,.1)', alignSelf: 'center', height: 20 }}
                />

                {role ? (
                  <Button
                    onClick={handleSignOut}
                    sx={{
                      color: '#fff',
                      textTransform: 'none',
                      fontWeight: 600,
                      fontSize: '.875rem',
                      px: 2.5,
                      py: 0.75,
                      borderRadius: '9999px',
                      border: '1px solid rgba(255,255,255,.65)',
                      transition: 'all .15s ease',
                      '&:hover': {
                        borderColor: '#fff',
                        bgcolor: 'rgba(255,255,255,.1)',
                      },
                    }}
                  >
                    Sign out
                  </Button>
                ) : (
                  <Button
                    component={Link}
                    to="/login"
                    sx={{
                      color: '#fff',
                      textTransform: 'none',
                      fontWeight: 600,
                      fontSize: '.875rem',
                      px: 2.5,
                      py: 0.75,
                      borderRadius: '9999px',
                      border: '1px solid rgba(255,255,255,.65)',
                      transition: 'all .15s ease',
                      '&:hover': {
                        borderColor: '#fff',
                        bgcolor: 'rgba(255,255,255,.1)',
                      },
                    }}
                  >
                    Sign in
                  </Button>
                )}
              </Stack>
            )}

            {/* ---- Mobile hamburger ---- */}
            {isMobile && (
              <IconButton
                onClick={() => setDrawerOpen(true)}
                sx={{
                  color: 'rgba(248,246,240,.8)',
                  '&:hover': { color: '#fff', bgcolor: 'rgba(255,255,255,.1)' },
                }}
                aria-label="Open navigation menu"
              >
                <Badge badgeContent={unreadMatches} color="error" variant="dot" invisible={unreadMatches === 0}>
                  <MenuIcon />
                </Badge>
              </IconButton>
            )}
          </Toolbar>
        </Container>
      </AppBar>

      {/* ---- Mobile Drawer ---- */}
      <Drawer
        anchor="right"
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        PaperProps={{
          sx: {
            bgcolor: '#0d2223',
            color: '#f8f6f0',
            width: 280,
            borderLeft: '1px solid rgba(255,255,255,.08)',
          },
        }}
      >
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', px: 2, py: 1.5 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <MedMatchIcon sx={{ fontSize: 20, color: '#00695c' }} />
            <Typography sx={{ fontWeight: 800, fontSize: '1rem', letterSpacing: '-.02em' }}>MedMatch</Typography>
          </Box>
          <IconButton onClick={() => setDrawerOpen(false)} sx={{ color: 'rgba(248,246,240,.6)' }}>
            <CloseIcon />
          </IconButton>
        </Box>

        <Divider sx={{ borderColor: 'rgba(255,255,255,.08)' }} />

        <List sx={{ px: 1, py: 1.5 }}>
          {navItems
            .filter((item) => item.show)
            .map((item) => (
              <ListItemButton
                key={item.to}
                component={Link}
                to={item.to}
                onClick={() => setDrawerOpen(false)}
                sx={{
                  borderRadius: '12px',
                  mb: 0.5,
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  '&:hover': { bgcolor: 'rgba(255,255,255,.07)' },
                }}
              >
                <ListItemText
                  primary={item.label}
                  primaryTypographyProps={{ fontWeight: 500, fontSize: '.95rem' }}
                />
                {typeof item.badgeCount === 'number' && item.badgeCount > 0 && (
                  <Box
                    component="span"
                    sx={{
                      bgcolor: '#b06f42',
                      color: '#fff',
                      fontSize: '.7rem',
                      fontWeight: 800,
                      borderRadius: '9999px',
                      px: 1,
                      py: 0.2,
                      lineHeight: 1.2,
                    }}
                  >
                    {item.badgeCount > 99 ? '99+' : item.badgeCount}
                  </Box>
                )}
              </ListItemButton>
            ))}
        </List>

        <Divider sx={{ borderColor: 'rgba(255,255,255,.08)', mx: 2 }} />

        <Box sx={{ px: 2, py: 2 }}>
          {role ? (
            <Button
              fullWidth
              onClick={handleSignOut}
              sx={{
                color: '#fff',
                textTransform: 'none',
                fontWeight: 600,
                py: 1.25,
                borderRadius: '9999px',
                border: '1px solid rgba(255,255,255,.65)',
                '&:hover': { borderColor: '#fff', bgcolor: 'rgba(255,255,255,.1)' },
              }}
            >
              Sign out
            </Button>
          ) : (
            <Button
              fullWidth
              component={Link}
              to="/login"
              onClick={() => setDrawerOpen(false)}
              sx={{
                color: '#fff',
                textTransform: 'none',
                fontWeight: 600,
                py: 1.25,
                borderRadius: '9999px',
                border: '1px solid rgba(255,255,255,.65)',
                '&:hover': { borderColor: '#fff', bgcolor: 'rgba(255,255,255,.1)' },
              }}
            >
              Sign in
            </Button>
          )}
        </Box>
      </Drawer>
    </>
  );
}
