"use client";

import { useAuthStore } from "@/lib/store";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";

interface AuthGuardProps {
  children: React.ReactNode;
  requiredRoles?: string[];
  requiredPermissions?: string[];
}

export function AuthGuard({
  children,
  requiredRoles,
  requiredPermissions,
}: AuthGuardProps) {
  const { isAuthenticated, user, hasRole, hasPermission } = useAuthStore();
  const router = useRouter();
  const [isHydrated, setIsHydrated] = useState(false);
  const [isAuthorized, setIsAuthorized] = useState(false);

  // Wait for Zustand to hydrate from localStorage
  useEffect(() => {
    setIsHydrated(true);
  }, []);

  useEffect(() => {
    // Don't check auth until store is hydrated
    if (!isHydrated) return;

    if (!isAuthenticated) {
      router.push("/login");
      return;
    }

    // Check roles
    if (requiredRoles && requiredRoles.length > 0) {
      const hasRequiredRole = requiredRoles.some((role) => hasRole(role));
      if (!hasRequiredRole) {
        router.push("/unauthorized");
        return;
      }
    }

    // Check permissions
    if (requiredPermissions && requiredPermissions.length > 0) {
      const hasRequiredPermission = requiredPermissions.some((perm) =>
        hasPermission(perm)
      );
      if (!hasRequiredPermission) {
        router.push("/unauthorized");
        return;
      }
    }

    setIsAuthorized(true);
  }, [isHydrated, isAuthenticated, user, requiredRoles, requiredPermissions, router, hasRole, hasPermission]);

  if (!isHydrated || !isAuthorized) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-100">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return <>{children}</>;
}

