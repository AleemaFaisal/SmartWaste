export type Role = "citizen" | "operator" | "admin";

export interface AuthenticatedUser {
    userId: string;
    roleId: number;
    roleName: string;
}

export interface LoginCredentials {
    cnic: string;
    password: string;
    expectedRole?: Role;
    useEntityFramework?: boolean;
}
