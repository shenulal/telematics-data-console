import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function formatDate(date: string | Date): string {
  return new Date(date).toLocaleDateString("en-US", {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function formatCoordinate(value?: number): string {
  if (value === undefined || value === null) return "N/A";
  return value.toFixed(6);
}

export function getStatusColor(status: number): string {
  switch (status) {
    case 1:
      return "bg-green-100 text-green-800";
    case 0:
      return "bg-red-100 text-red-800";
    case 2:
      return "bg-yellow-100 text-yellow-800";
    case 3:
      return "bg-orange-100 text-orange-800";
    case 4:
      return "bg-gray-200 text-gray-600";
    default:
      return "bg-gray-100 text-gray-800";
  }
}

export function getStatusText(status: number): string {
  switch (status) {
    case 1:
      return "ACTIVE";
    case 0:
      return "INACTIVE";
    case 2:
      return "SUSPENDED";
    case 3:
      return "LOCKED";
    case 4:
      return "DELETED";
    default:
      return "UNKNOWN";
  }
}

// User Role constants
export const USER_ROLES = {
  SUPERADMIN: "SUPERADMIN",
  RESELLER_ADMIN: "RESELLER ADMIN",
  TECHNICIAN: "TECHNICIAN",
  SUPERVISOR: "SUPERVISOR",
} as const;

// Helper function to check if user has a specific role (handles variations)
export function hasRoleVariant(roles: string[] | undefined, targetRole: string): boolean {
  if (!roles) return false;
  const normalizedTarget = targetRole.toUpperCase().replace(/\s+/g, '');
  return roles.some(r => r.toUpperCase().replace(/\s+/g, '') === normalizedTarget);
}

// Check if user is Super Admin (handles variations like "SUPERADMIN", "Super Admin", "Admin")
export function isSuperAdmin(roles: string[] | undefined): boolean {
  if (!roles) return false;
  return roles.some(r => {
    const normalized = r.toUpperCase().replace(/\s+/g, '');
    return normalized === 'SUPERADMIN' || normalized === 'ADMIN';
  });
}

// Check if user is Reseller Admin (handles variations)
export function isResellerAdmin(roles: string[] | undefined): boolean {
  if (!roles) return false;
  return roles.some(r => {
    const normalized = r.toUpperCase().replace(/\s+/g, '');
    return normalized === 'RESELLERADMIN' || normalized === 'RESELLER';
  });
}

// Check if user is Supervisor
export function isSupervisor(roles: string[] | undefined): boolean {
  return hasRoleVariant(roles, 'SUPERVISOR');
}

// Check if user is Technician
export function isTechnician(roles: string[] | undefined): boolean {
  return hasRoleVariant(roles, 'TECHNICIAN');
}

// User Status constants
export const USER_STATUS = {
  INACTIVE: 0,
  ACTIVE: 1,
  SUSPENDED: 2,
  LOCKED: 3,
  DELETED: 4,
} as const;

