const API_BASE = import.meta.env.VITE_API_BASE;

export async function apiFetch(path: string, options: RequestInit = {}) {
  const url = `${API_BASE}${path}`;

  const res = await fetch(url, options);

  // Try to return JSON when possible
  const contentType = res.headers.get("content-type") || "";
  const body = contentType.includes("application/json")
    ? await res.json()
    : await res.text();

  if (!res.ok) {
    // Bubble up a readable error
    const message =
      typeof body === "string" ? body : body?.message || "Request failed";
    throw new Error(message);
  }

  return body;
}
