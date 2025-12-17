import { Navigate } from "react-router-dom";
import { ReactNode } from "react";
import { useAuth } from "../context/AuthContext";

type Role = "Parent" | "Kid";

interface RequireRoleProps {
  role: Role;
  children: ReactNode;
}

export default function RequireRole({ role, children }: RequireRoleProps) {
  const { auth } = useAuth();

  // Not logged in at all
  if (!auth.token) {
    return <Navigate to="/login" replace />;
  }

  // Logged in, but wrong role
  if (auth.role !== role) {
    return <Navigate to="/login" replace />;
  }

  // Authorized
  return <>{children}</>;
}
