import { Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

type Role = "Parent" | "Kid";

export default function RequireRole({
  role,
  allow,
  children,
}: {
  role?: Role;
  allow?: Role[];
  children: JSX.Element;
}) {
  const { auth } = useAuth();

  const allowed: Role[] = allow ?? (role ? [role] : ["Parent", "Kid"]);
  const activeRole = auth?.activeRole;

  // No active role = not authenticated
  if (!activeRole) {
    return <Navigate to="/login" replace />;
  }

  // Active role must be explicitly allowed
  if (!allowed.includes(activeRole)) {
    return <Navigate to="/login" replace />;
  }

  return children;
}
