# ✨ Microsoft Full-Stack Sample  
### React + TypeScript + .NET 8 Minimal API + Docker

A clean and modern implementation of Microsoft’s full-stack ecosystem — deployed and running in the cloud.

---

## 🚀 Live Demo

| Component | URL |
|---------|-----|
| **Frontend (GitHub Pages)** | https://maximowinfield.github.io/Microsoft-Fullstack-Sample/ |
| **Backend API (Render)** | https://microsoft-fullstack-sample.onrender.com/api/todos |

> ⚠️ API may take 3-5 seconds to wake up on first request (free hosting tier)

---

## 🧰 Tech Stack

### Frontend
- React 18 + TypeScript
- Vite build tooling
- Axios for HTTP requests
- React Hooks for UI logic

### Backend
- .NET 8 Minimal API
- RESTful todo CRUD operations
- In-memory data store (easy upgrade to EF Core + SQL)

### DevOps / Hosting
- Docker Compose for full-stack local deployment
- CI/CD with GitHub Actions
- Deployed to:
  - GitHub Pages → Frontend
  - Render → API

---

## 📸 Screenshot

<img width="819" height="343" alt="image" src="https://github.com/user-attachments/assets/f7c9893c-51e7-4545-b2df-8c55986a50ae" />


---

## 🧠 Features

- Full production deployment with real API + real UI
- Create | Toggle | Delete todo items
- Modern Microsoft-based architecture
- Cloud-hosted public demo

---

## 🛠️ Running Locally

### 1️⃣ Clone this repository

```bash
git clone https://github.com/maximowinfield/Microsoft-Fullstack-Sample.git
cd Microsoft-Fullstack-Sample
```

### 2️⃣ Run using Docker Compose

Once running:

Frontend → http://localhost:5173

API health → http://localhost:8080/api/health

### 3️⃣ Run manually (without Docker)

## Start backend
```bash
cd api
dotnet run
```
## Start frontend
```bash
cd web
npm install
npm run dev
```

Then access:

Frontend → http://localhost:5173

API → http://localhost:8080/api/todos

## 🔌 API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET    | /api/health       | Health check |
| GET    | /api/todos        | Fetch todos |
| POST   | /api/todos        | Add a todo |
| PUT    | /api/todos/{id}   | Toggle completion |
| DELETE | /api/todos/{id}   | Delete a todo |




