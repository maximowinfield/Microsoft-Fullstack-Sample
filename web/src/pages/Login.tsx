import { useState } from "react";
import { useAuth } from "../context/AuthContext";
import { apiFetch } from "../services/api";

export default function Login() {
  const { setAuth } = useAuth();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");

  async function login() {
    try {
      const data = await apiFetch("/api/parent/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username, password }),
      });

      setAuth({ token: data.token, role: "Parent" });
    } catch (err: any) {
      alert(err.message || "Login failed");
    }
  }

  return (
    <div>
      <h2>Parent Login</h2>
      <input
        placeholder="Username"
        onChange={(e) => setUsername(e.target.value)}
      />
      <input
        type="password"
        placeholder="Password"
        onChange={(e) => setPassword(e.target.value)}
      />
      <button onClick={login}>Login</button>
    </div>
  );
}
