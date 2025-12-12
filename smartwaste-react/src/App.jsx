// import { useState } from 'react'
// import { BrowserRouter, Routes, Route } from "react-router-dom";
// import OperatorDashboard from "./pages/OperatorDashboard";
// import Login from './pages/Login'
// import Register from './pages/Register'
// import CitizenDashboard from './pages/CitizenDashboard'
// import './App.css'

// function App() {
//   const [user, setUser] = useState(null)
//   const [showRegister, setShowRegister] = useState(false)
//   const [useEF, setUseEF] = useState(true) // true = EF, false = SP

//   const handleLoginSuccess = (userData) => {
//     setUser(userData)
//   }

//   const handleLogout = () => {
//     setUser(null)
//   }

//   const toggleImplementation = () => {
//     setUseEF(!useEF)
//   }

//   const handleShowRegister = () => {
//     setShowRegister(true)
//   }

//   const handleBackToLogin = () => {
//     setShowRegister(false)
//   }

//   const handleRegisterSuccess = () => {
//     setShowRegister(false)
//   }

//   return (
//     <>
//       {!user ? (
//         showRegister ? (
//           <Register
//             onRegisterSuccess={handleRegisterSuccess}
//             onBackToLogin={handleBackToLogin}
//           />
//         ) : (
//           <Login
//             onLoginSuccess={handleLoginSuccess}
//             onShowRegister={handleShowRegister}
//           />
//         )
//       ) : (
//         <CitizenDashboard
//           user={user}
//           onLogout={handleLogout}
//           useEF={useEF}
//           onToggleImplementation={toggleImplementation}
//         />
//       )}
//     </>
//   )
// }

// export default App

import { useState, useEffect } from "react";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import OperatorDashboard from "./pages/OperatorDashboard";
import Login from "./pages/Login";
import Register from "./pages/Register";
import CitizenDashboard from "./pages/CitizenDashboard";
import { setBackendPreference } from "./services/api";
import "./App.css";

function App() {
  const [user, setUser] = useState(null);

  // "Engine" Toggle: true = Entity Framework, false = Stored Procedures
  const [useEF, setUseEF] = useState(true);

  // Toggle Function passed to Dashboards
  const toggleImplementation = () => {
    const newValue = !useEF;
    setUseEF(newValue);
    // Save to localStorage
    localStorage.setItem("useEF", newValue ? "true" : "false");
    // CRITICAL: Update api.js global variable
    setBackendPreference(newValue);
    console.log(`[App] Toggle clicked - New mode: ${newValue ? "EF" : "SP"}`);
  };

  // Initialize localStorage and api.js on load
  useEffect(() => {
    localStorage.setItem("useEF", useEF ? "true" : "false");
    setBackendPreference(useEF);
    console.log(`[App] Initialized with mode: ${useEF ? "EF" : "SP"}`);
  }, []);

  return (
    <BrowserRouter>
      <div className="app-container">
        {/* Global "Engine" Status Bar (Optional but cool for the project) */}
        <div
          style={{
            background: "#333",
            color: "white",
            padding: "5px",
            textAlign: "center",
            fontSize: "12px",
          }}
        >
          Current Backend Mode:{" "}
          <strong>
            {useEF ? "🟢 Entity Framework" : "🔵 Stored Procedures"}
          </strong>
        </div>

        <Routes>
          {/* 1. PUBLIC ROUTES */}
          <Route
            path="/"
            element={
              !user ? (
                <Login onLoginSuccess={setUser} />
              ) : (
                <Navigate
                  to={user.role === "Operator" ? "/operator" : "/citizen"}
                />
              )
            }
          />
          <Route
            path="/login"
            element={
              !user ? (
                <Login onLoginSuccess={setUser} />
              ) : (
                <Navigate
                  to={user.role === "Operator" ? "/operator" : "/citizen"}
                />
              )
            }
          />
          <Route
            path="/register"
            element={!user ? <Register /> : <Navigate to="/" />}
          />

          {/* 2. PROTECTED ROUTES */}
          <Route
            path="/citizen"
            element={
              user && user.role === "Citizen" ? (
                <CitizenDashboard
                  user={user}
                  onLogout={() => setUser(null)}
                  useEF={useEF}
                  onToggleImplementation={toggleImplementation}
                />
              ) : (
                <Navigate to="/" />
              )
            }
          />

          <Route
            path="/operator"
            element={
              // Check if user exists AND is an Operator
              user && (user.role === "Operator" || user.role === "Admin") ? (
                <OperatorDashboard
                  user={user}
                  onLogout={() => setUser(null)}
                  useEF={useEF}
                  onToggleImplementation={toggleImplementation}
                />
              ) : (
                <Navigate to="/" />
              )
            }
          />

          {/* Fallback */}
          <Route path="*" element={<Navigate to="/" />} />
        </Routes>
      </div>
    </BrowserRouter>
  );
}

export default App;
