import { createContext, useContext, useState } from "react";

type Role = "Parent" | "Kid";

interface AuthState {
  token: string | null;
  role: Role | null;
  kidName?: string;
}

const AuthContext = createContext<any>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [auth, setAuth] = useState<AuthState>({ token: null, role: null });

  return (
    <AuthContext.Provider value={{ auth, setAuth }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => useContext(AuthContext);
