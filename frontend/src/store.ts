import { create } from 'zustand';

export type Role = 'Patient' | 'Clinic' | 'Doctor' | 'Admin';
export const hasRole = (roles: Role[] | null | undefined, role: Role) => !!roles?.includes(role);

function loadRoles(): Role[] {
  const raw = localStorage.getItem('medmatch_roles');
  if (raw) {
    try { return JSON.parse(raw) as Role[]; } catch { return []; }
  }
  const legacy = localStorage.getItem('medmatch_role') as Role | null;
  return legacy ? [legacy] : [];
}

/** Extract the email claim from a JWT without verifying the signature (display only). */
function decodeEmail(token: string | null): string {
  if (!token) return '';
  try {
    const payload = token.split('.')[1];
    if (!payload) return '';
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
    const json = JSON.parse(atob(base64.padEnd(Math.ceil(base64.length / 4) * 4, '=')));
    return typeof json.email === 'string' ? json.email : '';
  } catch {
    return '';
  }
}

type AuthState = {
  accessToken: string | null;
  refreshToken: string | null;
  roles: Role[];
  email: string;
  setSession: (accessToken: string, refreshToken: string, roles: Role[]) => void;
  signOut: () => void;
};

export const useAuthStore = create<AuthState>((set) => ({
  accessToken: localStorage.getItem('medmatch_access'),
  refreshToken: localStorage.getItem('medmatch_refresh'),
  roles: loadRoles(),
  email: decodeEmail(localStorage.getItem('medmatch_access')),
  setSession: (accessToken, refreshToken, roles) => {
    localStorage.setItem('medmatch_access', accessToken);
    localStorage.setItem('medmatch_refresh', refreshToken);
    localStorage.setItem('medmatch_roles', JSON.stringify(roles));
    localStorage.removeItem('medmatch_role');
    set({ accessToken, refreshToken, roles, email: decodeEmail(accessToken) });
  },
  signOut: () => {
    ['medmatch_access', 'medmatch_refresh', 'medmatch_roles', 'medmatch_role'].forEach((key) => localStorage.removeItem(key));
    set({ accessToken: null, refreshToken: null, roles: [], email: '' });
  },
}));
