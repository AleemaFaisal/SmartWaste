import { Navigate, Route, Routes } from "react-router-dom";
import { useState } from "react";
import LoginPage from "./pages/LoginPage.tsx";
import CitizenPortalPage from "./pages/CitizenPortalPage.tsx";
import OperatorPortalPage from "./pages/OperatorPortalPage.tsx";
import AdminPortalPage from "./pages/AdminPortalPage.tsx";
import { login } from "./services/api.ts";
import type { AuthenticatedUser, LoginCredentials, Role } from "./types.ts";

const App = () => {
  const [role, setRole] = useState<Role | null>(null);
  const [user, setUser] = useState<AuthenticatedUser | null>(null);
  const [authError, setAuthError] = useState<string | null>(null);
  const [authLoading, setAuthLoading] = useState(false);

  const handleLogin = async (credentials: LoginCredentials) => {
    setAuthLoading(true);
    setAuthError(null);

    try {
      const result = await login(credentials);
      if (result.role === "admin") {
        setRole(null);
        setUser(null);
        setAuthError(
          "The admin portal is still under development. Please sign in as a citizen or operator."
        );
        return;
      }

      if (
        credentials.expectedRole &&
        credentials.expectedRole !== result.role
      ) {
        const friendlyRole = result.role === "citizen" ? "Citizen" : "Operator";
        setRole(null);
        setUser(null);
        setAuthError(
          `This CNIC is registered as a ${friendlyRole}. Switch the portal selection to continue.`
        );
        return;
      }

      setRole(result.role);
      setUser(result.user);
    } catch (error) {
      const message =
        error instanceof Error
          ? error.message
          : "Unable to login. Please try again.";

      setAuthError(message);
      throw error;
    } finally {
      setAuthLoading(false);
    }
  };

  const handleLogout = () => {
    setRole(null);
    setUser(null);
    setAuthError(null);
  };

  return (
    <Routes>
      <Route
        path="/"
        element={
          role ? (
            <Navigate to={`/${role}`} replace />
          ) : (
            <LoginPage
              onLogin={handleLogin}
              loading={authLoading}
              error={authError}
            />
          )
        }
      />
      <Route
        path="/citizen"
        element={
          role === "citizen" ? (
            <CitizenPortalPage onLogout={handleLogout} />
          ) : (
            <Navigate to="/" replace />
          )
        }
      />
      <Route
        path="/operator"
        element={
          role === "operator" ? (
            <OperatorPortalPage onLogout={handleLogout} />
          ) : (
            <Navigate to="/" replace />
          )
        }
      />
      <Route
        path="/admin"
        element={
          role === "admin" ? (
            <AdminPortalPage onLogout={handleLogout} />
          ) : (
            <Navigate to="/" replace />
          )
        }
      />
      <Route
        path="*"
        element={<Navigate to={role ? `/${role}` : "/"} replace />}
      />
    </Routes>
  );
};

export default App;
