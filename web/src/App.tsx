import { Link, Routes, Route, Navigate } from "react-router-dom";
import KidsRewardsPage from "./pages/KidsRewardsPage";
import TodosPage from "./pages/TodosPage";

import Login from "./pages/Login";
import SelectKid from "./pages/SelectKid";
import RequireRole from "./components/RequireRole";

export default function App() {
  return (
    <div style={{ minHeight: "100vh", fontFamily: "system-ui" }}>
      <div
        style={{
          maxWidth: 860,
          margin: "20px auto 0",
          padding: "0 16px",
          display: "flex",
          gap: 12,
        }}
      >
        {/* Parent links */}
        <Link to="/parent/kids">Kids + Rewards</Link>
        <Link to="/parent/todos">Todos</Link>

        {/* Kid link (optional) */}
        <Link to="/kid">Kid View</Link>

        {/* Auth links */}
        <Link to="/login">Login</Link>
        <Link to="/select-kid">Select Kid</Link>
      </div>

      <Routes>
        {/* Default */}
        <Route path="/" element={<Navigate to="/login" replace />} />

        {/* Public */}
        <Route path="/login" element={<Login />} />

        {/* Parent-only: pick a kid (creates Kid token) */}
        <Route
          path="/select-kid"
          element={
            <RequireRole role="Parent">
              <SelectKid />
            </RequireRole>
          }
        />

        {/* Parent-only pages */}
        <Route
          path="/parent/kids"
          element={
            <RequireRole role="Parent">
              <KidsRewardsPage />
            </RequireRole>
          }
        />
        <Route
          path="/parent/todos"
          element={
            <RequireRole role="Parent">
              <TodosPage />
            </RequireRole>
          }
        />

        {/* Kid-only page */}
        <Route
          path="/kid"
          element={
            <RequireRole role="Kid">
              <KidsRewardsPage />
            </RequireRole>
          }
        />

        {/* Fallback */}
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </div>
  );
}
