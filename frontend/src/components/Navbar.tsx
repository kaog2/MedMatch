import {
  AppBar,
  Avatar,
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
  Tooltip,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { useAuthStore, hasRole } from '../store';
import { api, MatchSummary } from '../services/api';
import { useColorMode } from '../theme';
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

function SunIcon() {
  return (
    <SvgIcon viewBox="0 0 24 24">
      <circle cx="12" cy="12" r="4" fill="none" stroke="currentColor" strokeWidth="2" />
      <path fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41" />
    </SvgIcon>
  );
}

function MoonIcon() {
  return (
    <SvgIcon viewBox="0 0 24 24">
      <path fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z" />
    </SvgIcon>
  );
}

/** Up to two initials derived from an email address local part (e.g. jane.doe → JD). */
function initialsFrom(email: string): string {
  const local = email.split('@')[0] ?? '';
  const parts = local.split(/[._\-+]+/).filter(Boolean);
  const initials = parts.slice(0, 2).map((part) => part[0]?.toUpperCase() ?? '').join('');
  return initials || local.slice(0, 2).toUpperCase() || '?';
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
  const { roles, email, signOut } = useAuthStore();
  const { mode, toggleColorMode } = useColorMode();
  const navigate = useNavigate();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const [drawerOpen, setDrawerOpen] = useState(false);
  const initials = initialsFrom(email);

  const { data: matchSummary } = useQuery({
    queryKey: ['match-summary'],
    queryFn: () => api<MatchSummary>('/matches/summary'),
    enabled: hasRole(roles, 'Patient'),
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
    { to: '/recommend', label: 'Recommend', show: hasRole(roles, 'Patient') },
    { to: '/people', label: 'People', show: hasRole(roles, 'Patient') },
    { to: '/matches', label: 'Matches', show: hasRole(roles, 'Patient'), badgeCount: unreadMatches },
    { to: '/profile', label: 'Profile', show: hasRole(roles, 'Patient') },
    { to: '/clinic-patients', label: 'Patients', show: hasRole(roles, 'Clinic') },
    { to: '/admin', label: 'Admin', show: hasRole(roles, 'Admin') },
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

                <Tooltip title={mode === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}>
                  <IconButton
                    onClick={toggleColorMode}
                    aria-label="Toggle color mode"
                    sx={{
                      color: 'rgba(248,246,240,.8)',
                      '&:hover': { color: '#fff', bgcolor: 'rgba(255,255,255,.1)' },
                    }}
                  >
                    {mode === 'dark' ? <SunIcon /> : <MoonIcon />}
                  </IconButton>
                </Tooltip>

                {roles.length > 0 ? (
                  <>
                    <Tooltip title={email || 'Signed in'}>
                      <Avatar
                        sx={{
                          width: 36,
                          height: 36,
                          bgcolor: '#f2b880',
                          color: '#102a2b',
                          fontWeight: 800,
                          fontSize: '.85rem',
                          letterSpacing: '.02em',
                          border: '2px solid rgba(255,255,255,.25)',
                        }}
                      >
                        {initials}
                      </Avatar>
                    </Tooltip>
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
                  </>
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

            {/* ---- Mobile: theme toggle + hamburger ---- */}
            {isMobile && (
              <Stack direction="row" alignItems="center" spacing={0.25}>
                <IconButton
                  onClick={toggleColorMode}
                  aria-label="Toggle color mode"
                  sx={{
                    color: 'rgba(248,246,240,.8)',
                    '&:hover': { color: '#fff', bgcolor: 'rgba(255,255,255,.1)' },
                  }}
                >
                  {mode === 'dark' ? <SunIcon /> : <MoonIcon />}
                </IconButton>
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
              </Stack>
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

        {roles.length > 0 && (
          <Box sx={{ px: 2, py: 1.5, display: 'flex', alignItems: 'center', gap: 1.5 }}>
            <Avatar
              sx={{
                width: 40,
                height: 40,
                bgcolor: '#f2b880',
                color: '#102a2b',
                fontWeight: 800,
                fontSize: '.95rem',
                letterSpacing: '.02em',
                flexShrink: 0,
              }}
            >
              {initials}
            </Avatar>
            <Box sx={{ minWidth: 0 }}>
              <Typography
                sx={{
                  fontWeight: 700,
                  fontSize: '.9rem',
                  whiteSpace: 'nowrap',
                  overflow: 'hidden',
                  textOverflow: 'ellipsis',
                }}
              >
                {email || 'Signed in'}
              </Typography>
              <Typography sx={{ fontSize: '.72rem', color: 'rgba(248,246,240,.55)' }}>
                {roles.join(', ')}
              </Typography>
            </Box>
          </Box>
        )}

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
          {roles.length > 0 ? (
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
