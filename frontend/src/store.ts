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

type AuthState = {
  accessToken: string | null;
  refreshToken: string | null;
  roles: Role[];
  setSession: (accessToken: string, refreshToken: string, roles: Role[]) => void;
  signOut: () => void;
};

export const useAuthStore = create<AuthState>((set) => ({
  accessToken: localStorage.getItem('medmatch_access'),
  refreshToken: localStorage.getItem('medmatch_refresh'),
  roles: loadRoles(),
  setSession: (accessToken, refreshToken, roles) => {
    localStorage.setItem('medmatch_access', accessToken);
    localStorage.setItem('medmatch_refresh', refreshToken);
    localStorage.setItem('medmatch_roles', JSON.stringify(roles));
    localStorage.removeItem('medmatch_role');
    set({ accessToken, refreshToken, roles });
  },
  signOut: () => {
    ['medmatch_access', 'medmatch_refresh', 'medmatch_roles', 'medmatch_role'].forEach((key) => localStorage.removeItem(key));
    set({ accessToken: null, refreshToken: null, roles: [] });
  },
}));
