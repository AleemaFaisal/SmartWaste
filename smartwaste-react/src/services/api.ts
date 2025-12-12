import type { AuthenticatedUser, LoginCredentials, Role } from "../types.ts";

const DEFAULT_API_BASE_URL = "http://localhost:5000/api";
const API_BASE_URL = (import.meta.env.VITE_API_URL as string | undefined)?.replace(/\/$/, "") ?? DEFAULT_API_BASE_URL;

interface LoginResponseSuccess {
    success: true;
    user: {
        userID: string;
        roleID: number;
        roleName: string;
    };
    message?: string;
}

interface LoginResponseFailure {
    success: false;
    message?: string;
}

type LoginResponse = LoginResponseSuccess | LoginResponseFailure;

interface LoginResult {
    role: Role;
    user: AuthenticatedUser;
    message?: string;
}

const normalizeRole = (roleName: string): Role => {
    const normalized = roleName.trim().toLowerCase();
    switch (normalized) {
        case "citizen":
            return "citizen";
        case "operator":
            return "operator";
        case "admin":
            return "admin";
        default:
            throw new Error(`Unsupported role '${roleName}'. Please contact support.`);
    }
};

export const login = async ({
    cnic,
    password,
    useEntityFramework,
}: LoginCredentials): Promise<LoginResult> => {
    const response = await fetch(`${API_BASE_URL}/auth/login`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            ...(typeof useEntityFramework === "boolean"
                ? { "X-Use-EF": useEntityFramework.toString() }
                : {}),
        },
        body: JSON.stringify({ cnic, password }),
    });

    let data: LoginResponse | null = null;
    try {
        data = (await response.json()) as LoginResponse;
    } catch (error) {
        // Non-JSON response (e.g., 500 HTML). We'll handle below.
    }

    if (!response.ok || !data) {
        throw new Error("Unable to login. Please verify credentials and try again.");
    }

    if (!data.success) {
        throw new Error(data.message ?? "Invalid CNIC or password.");
    }

    const role = normalizeRole(data.user.roleName);
    const user: AuthenticatedUser = {
        userId: data.user.userID,
        roleId: data.user.roleID,
        roleName: data.user.roleName,
    };

    return {
        role,
        user,
        message: data.message,
    };
};
