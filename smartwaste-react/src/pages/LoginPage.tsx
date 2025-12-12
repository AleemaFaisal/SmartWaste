import { ChangeEvent, FormEvent, useState } from "react";
import type { LoginCredentials, Role } from "../types.ts";

interface LoginPageProps {
  onLogin: (credentials: LoginCredentials) => Promise<void>;
  loading: boolean;
  error: string | null;
}

const roles: Array<{ label: string; value: Role; blurb: string }> = [
  {
    label: "Citizen",
    value: "citizen",
    blurb: "Track pickups and pricing insights.",
  },
  {
    label: "Operator",
    value: "operator",
    blurb: "Manage routes and collection tasks.",
  },
  {
    label: "Admin",
    value: "admin",
    blurb: "Oversee analytics and pricing controls.",
  },
];

const CNIC_PATTERN = /^\d{5}-\d{7}-\d{1}$/;

const formatCnic = (value: string) => {
  const digits = value.replace(/\D/g, "").slice(0, 13);
  const parts = [
    digits.slice(0, 5),
    digits.slice(5, 12),
    digits.slice(12, 13),
  ].filter(Boolean);
  return parts.join("-");
};

const LoginPage = ({ onLogin, loading, error }: LoginPageProps) => {
  const [selectedRole, setSelectedRole] = useState<Role>("citizen");
  const [cnic, setCnic] = useState("");
  const [password, setPassword] = useState("");
  const [cnicTouched, setCnicTouched] = useState(false);
  const [passwordTouched, setPasswordTouched] = useState(false);

  const cnicValid = CNIC_PATTERN.test(cnic);
  const passwordValid = password.trim().length >= 6;

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setCnicTouched(true);
    setPasswordTouched(true);

    if (!cnicValid || !passwordValid) {
      return;
    }

    try {
      await onLogin({
        cnic,
        password,
        expectedRole: selectedRole,
      });
      setPassword("");
    } catch (error) {
      // Allow the user to correct credentials without clearing input
      console.debug("Login failed", error);
    }
  };

  const handleCnicChange = (event: ChangeEvent<HTMLInputElement>) => {
    const formatted = formatCnic(event.target.value);
    setCnic(formatted);
  };

  return (
    <div className="page">
      <div className="card card--narrow stack stack--lg">
        <div className="stack" style={{ gap: 12, textAlign: "center" }}>
          <div style={{ fontSize: "2rem" }} aria-hidden>
            🗑️💡
          </div>
          <h1 className="title">SmartWaste</h1>
          <p className="subtitle">Login to continue to your workspace</p>
        </div>

        <form className="stack" onSubmit={handleSubmit}>
          {error ? (
            <p
              className="field__message field__message--error"
              role="alert"
              style={{ textAlign: "center" }}
            >
              {error}
            </p>
          ) : null}

          <div className="field">
            <label className="label" htmlFor="cnic">
              CNIC (13-digit Pakistani ID)
            </label>
            <input
              id="cnic"
              className={`control ${
                cnicTouched && !cnicValid ? "control--error" : ""
              }`}
              value={cnic}
              onChange={handleCnicChange}
              onBlur={() => setCnicTouched(true)}
              placeholder="12345-1234567-1"
              inputMode="numeric"
              maxLength={15}
              aria-invalid={cnicTouched && !cnicValid}
              required
            />
            <p
              className={`field__message ${
                cnicTouched && !cnicValid ? "field__message--error" : ""
              }`}
            >
              {cnicTouched && !cnicValid
                ? "Enter CNIC in 12345-1234567-1 format."
                : "Example: 35202-1234567-1"}
            </p>
          </div>

          <div className="field">
            <label className="label" htmlFor="password">
              Password
            </label>
            <input
              id="password"
              className={`control ${
                passwordTouched && !passwordValid ? "control--error" : ""
              }`}
              type="password"
              value={password}
              onChange={(event: ChangeEvent<HTMLInputElement>) =>
                setPassword(event.target.value)
              }
              onBlur={() => setPasswordTouched(true)}
              placeholder="Enter your password"
              minLength={6}
              autoComplete="current-password"
              aria-invalid={passwordTouched && !passwordValid}
              required
            />
            <p
              className={`field__message ${
                passwordTouched && !passwordValid ? "field__message--error" : ""
              }`}
            >
              {passwordTouched && !passwordValid
                ? "Password must be at least 6 characters."
                : "Use your SmartWaste portal password."}
            </p>
          </div>

          <div className="field">
            <span className="label">Choose a portal</span>
            <div className="choice-list">
              {roles.map((role) => (
                <label
                  className={`choice-card ${
                    selectedRole === role.value ? "choice-card--active" : ""
                  }`}
                  key={role.value}
                >
                  <input
                    className="choice-card__radio"
                    type="radio"
                    name="role"
                    value={role.value}
                    checked={selectedRole === role.value}
                    onChange={() => setSelectedRole(role.value)}
                  />
                  <div className="choice-card__header">
                    <span className="choice-card__label">{role.label}</span>
                    {selectedRole === role.value ? (
                      <span className="chip" style={{ fontSize: "0.75rem" }}>
                        Selected
                      </span>
                    ) : null}
                  </div>
                  <span className="choice-card__meta">{role.blurb}</span>
                </label>
              ))}
            </div>
          </div>

          <button className="btn" type="submit" disabled={loading}>
            {loading ? "Signing in..." : "Continue"}
          </button>
        </form>
      </div>
    </div>
  );
};

export default LoginPage;
