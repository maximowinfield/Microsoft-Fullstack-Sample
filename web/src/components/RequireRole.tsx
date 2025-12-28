// src/components/RequireRole.tsx
import React from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

type Role = "Parent" | "Kid";

export default function RequireRole({
  role,
  allow,
  children,
}: {
  role?: Role;
  allow?: Role[];
  children: React.ReactElement;
}) {
  const { auth } = useAuth();
  const location = useLocation();

  const required: Role[] = allow ?? (role ? [role] : []);
  const activeRole = auth?.activeRole ?? null;

  // ✅ Determine if user is authenticated for their activeRole
  const hasParentAuth = !!auth?.parentToken;
  const hasKidAuth = !!auth?.kidToken;

  // ✅ If no auth at all, go login
  if (!hasParentAuth && !hasKidAuth) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  // ✅ If route requires a role, enforce it
  if (required.length > 0 && (!activeRole || !required.includes(activeRole))) {
    // If they are authed but wrong role, send them home so App redirects correctly
    return <Navigate to="/" replace />;
  }

  // ✅ Extra safety: if role says Kid but kidToken missing, don’t allow kid pages
  if (activeRole === "Kid" && !hasKidAuth) {
    return <Navigate to="/" replace />;
  }

  // ✅ Extra safety: if role says Parent but parentToken missing, don’t allow parent pages
  if (activeRole === "Parent" && !hasParentAuth) {
    return <Navigate to="/" replace />;
  }

  return children;
}
