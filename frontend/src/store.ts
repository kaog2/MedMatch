import { create } from 'zustand';

type Role = 'Patient' | 'Clinic' | 'Doctor' | 'Admin';
type AuthState = { accessToken: string | null; refreshToken: string | null; role: Role | null; setSession: (accessToken: string, refreshToken: string, role: Role) => void; signOut: () => void };
export const useAuthStore = create<AuthState>((set) => ({
  accessToken: localStorage.getItem('medmatch_access'), refreshToken: localStorage.getItem('medmatch_refresh'), role: localStorage.getItem('medmatch_role') as Role | null,
  setSession: (accessToken, refreshToken, role) => { localStorage.setItem('medmatch_access', accessToken); localStorage.setItem('medmatch_refresh', refreshToken); localStorage.setItem('medmatch_role', role); set({ accessToken, refreshToken, role }); },
  signOut: () => { ['medmatch_access', 'medmatch_refresh', 'medmatch_role'].forEach((key) => localStorage.removeItem(key)); set({ accessToken: null, refreshToken: null, role: null }); },
}));
