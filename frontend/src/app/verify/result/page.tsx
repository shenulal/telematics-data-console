"use client";

import { Header } from "@/components/layout/Header";
import { AuthGuard } from "@/components/layout/AuthGuard";
import { DeviceDataDisplay } from "@/components/verification/DeviceDataDisplay";

export default function VerifyResultPage() {
  return (
    <AuthGuard requiredRoles={["TECHNICIAN", "SUPERADMIN", "RESELLER ADMIN"]}>
      <div className="min-h-screen bg-gray-50">
        <Header />
        <main className="max-w-7xl mx-auto px-4 py-6 sm:px-6 lg:px-8">
          <DeviceDataDisplay />
        </main>
      </div>
    </AuthGuard>
  );
}

