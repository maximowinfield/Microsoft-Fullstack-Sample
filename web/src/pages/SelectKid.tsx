import { useAuth } from "../context/AuthContext";

export default function SelectKid() {
  const { auth, setAuth } = useAuth();

  async function enterKidMode(kidId: string, displayName: string) {
    const res = await fetch("/api/kid-session", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${auth.token}`,
      },
      body: JSON.stringify({ kidId }),
    });

    if (!res.ok) {
      alert("Failed to enter kid mode");
      return;
    }

    const data = await res.json();
    setAuth({
      token: data.token,
      role: "Kid",
      kidName: displayName,
    });
  }

  return (
    <div>
      <h2>Select Kid</h2>

      {/* Temporary hardcoded kids (replace with API later) */}
      <button onClick={() => enterKidMode("kid-1", "Kid 1")}>
        Kid 1
      </button>

      <button onClick={() => enterKidMode("kid-2", "Kid 2")}>
        Kid 2
      </button>
    </div>
  );
}
