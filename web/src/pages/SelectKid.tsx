import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { getKids } from "../api";
import type { KidProfile } from "../types";

export default function SelectKid() {
  const { auth, setAuth, enterKidMode, enterParentMode } = useAuth();
  const navigate = useNavigate();
  const [kids, setKids] = useState<KidProfile[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Run once on mount (prevents effect re-running if enterParentMode isn't memoized)
  useEffect(() => {
    (async () => {
      try {
        setError(null);
        enterParentMode(); // Ensure Parent mode before fetching kids
        const data = await getKids();
        setKids(data);
      } catch (e) {
        console.error("SelectKid: failed to load kids", e);
        setError("Failed to load kids. Please refresh and try again.");
      } finally {
        setLoading(false);
      }
    })();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function setCurrentKid(kidId: string, kidName: string) {
    // Parent selection only (no kid session)
    setAuth((prev) => ({
      ...prev,
      selectedKidId: kidId,
      selectedKidName: kidName,
      uiMode: "Parent",
      activeRole: prev.parentToken ? "Parent" : null,
    }));

    navigate(`/parent/kids/${kidId}`, { replace: true });
  }

  async function goKidMode(kidId: string) {
    try {
      setError(null);
      await enterKidMode(kidId); // calls /api/kid-session and stores kidToken
      // NOTE: If your kid route is NOT "/kid", change this to your actual route (often "/kid/:kidId")
      navigate(`/kid/${kidId}`, { replace: true });
    } catch (e) {
      console.error("SelectKid: enterKidMode failed", e);
      setError("Could not enter Kid Mode. Please try again.");
    }
  }

  return (
    <div style={{ maxWidth: 860, margin: "24px auto", padding: "0 16px" }}>
      <h2>Select Kid</h2>

      <p>
        Active role: <strong>{auth?.activeRole ?? "none"}</strong>
      </p>
      <p>
        Current kid:{" "}
        <strong>{auth?.selectedKidName ?? auth?.kidName ?? "(none)"}</strong>
      </p>

      {error && (
        <p style={{ color: "salmon", marginTop: 8 }}>
          {error}
        </p>
      )}

      {loading ? (
        <p>Loading kids...</p>
      ) : (
        <div style={{ display: "grid", gap: 10 }}>
          {kids.map((k) => (
            <div
              key={k.id}
              style={{
                display: "flex",
                gap: 8,
                alignItems: "center",
                border: "1px solid #333",
                borderRadius: 10,
                padding: 10,
              }}
            >
              <div style={{ flex: 1 }}>
                <strong>{k.displayName}</strong>
                <div style={{ fontSize: 12, opacity: 0.8 }}>{k.id}</div>
              </div>

              <button
                type="button"
                onClick={() => setCurrentKid(k.id, k.displayName)}
              >
                View as Parent
              </button>

              <button type="button" onClick={() => void goKidMode(k.id)}>
                Enter Kid Mode
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
