import { Link, Routes, Route, Navigate } from "react-router-dom";
import KidsRewardsPage from "./pages/KidsRewardsPage";
import TodosPage from "./pages/TodosPage";
import Login from "./pages/Login";
import RequireRole from "./components/RequireRole";
import { useAuth } from "./context/AuthContext";

export default function App() {
  const { auth, setAuth } = useAuth();

  // ✅ Auth = only Parent login exists now
  const isAuthed = !!auth?.parentToken;
  const isParentRole = auth?.activeRole === "Parent";

  // ✅ Kid vs Parent is now UI MODE (not auth role)
  const isKidMode = auth?.uiMode === "Kid";
  const isParentMode = auth?.uiMode === "Parent";

  // Option A: kidId in the URL (saved selection)
  const parentKidId = auth?.selectedKidId;

  function clearAuth() {
    setAuth({
      parentToken: null,
      activeRole: null,
      uiMode: "Kid", // ✅ safe default when logged out
      kidId: undefined,
      kidName: undefined,
      selectedKidId: undefined,
      selectedKidName: undefined,
    });
  }

function switchToParentMode() {
  if (!auth?.parentToken) return;

  const expectedPin = import.meta.env.VITE_PARENT_PIN || "1234";
  const entered = window.prompt("Enter Parent PIN:");

  if (entered === expectedPin) {
    setAuth((prev) => ({ ...prev, uiMode: "Parent" }));
  } else {
    alert("Wrong PIN. Staying in Kid Mode.");
    setAuth((prev) => ({ ...prev, uiMode: "Kid" }));
  }
}


function switchToKidMode() {
  if (!auth?.parentToken) return;
  setAuth((prev) => ({ ...prev, uiMode: "Kid" }));
}


  return (
    <div style={{ minHeight: "100vh", fontFamily: "system-ui", color: "#fff" }}>
      <div
        style={{
          maxWidth: 860,
          margin: "20px auto 0",
          padding: "0 16px",
          display: "flex",
          gap: 12,
          alignItems: "center",
        }}
      >
        {/* Only show app nav if logged in */}
        {isAuthed && (
          <>
            <Link to={parentKidId ? `/parent/kids/${parentKidId}` : "/parent/kids"}>
              Kids + Rewards
            </Link>
            <Link to="/parent/todos">Todos</Link>
          </>
        )}

        {/* Always visible */}
        <Link to="/login">Login</Link>

        {/* Mode toggle + logout */}
        <div style={{ marginLeft: "auto", display: "flex", gap: 8 }}>
          {isAuthed && (
            <>
              <button
                onClick={switchToKidMode}
                style={{
                  cursor: "pointer",
                  opacity: isKidMode ? 0.7 : 1,
                }}
              >
                Kid Mode
              </button>

              <button
                onClick={switchToParentMode}
                style={{
                  cursor: "pointer",
                  opacity: isParentMode ? 0.7 : 1,
                }}
              >
                Parent Mode
              </button>

              <button onClick={clearAuth} style={{ cursor: "pointer" }}>
                Logout
              </button>
            </>
          )}
        </div>
      </div>

      <Routes>
        {/* Default route */}
        <Route
          path="/"
          element={
            isAuthed ? (
              parentKidId ? (
                <Navigate to={`/parent/kids/${parentKidId}`} replace />
              ) : (
                <Navigate to="/parent/kids" replace />
              )
            ) : (
              <Navigate to="/login" replace />
            )
          }
        />

        <Route path="/login" element={<Login />} />

        {/* Parent-only pages (auth role check) */}
        <Route
          path="/parent/kids"
          element={
            <RequireRole role="Parent">
              <KidsRewardsPage />
            </RequireRole>
          }
        />

        <Route
          path="/parent/kids/:kidId"
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

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </div>
  );
}
