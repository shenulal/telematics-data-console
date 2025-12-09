"use client";

import { Header } from "@/components/layout/Header";
import { AuthGuard } from "@/components/layout/AuthGuard";
import { ImeiInput } from "@/components/verification/ImeiInput";
import { useAuthStore } from "@/lib/store";
import { Card, CardContent } from "@/components/ui/card";
import { Activity, CheckCircle, Clock, Shield, Info } from "lucide-react";
import { useEffect, useState } from "react";
import { technicianApi } from "@/lib/api";

interface TechnicianStats {
  verificationsToday: number;
  totalVerifications: number;
  remainingDailyLimit: number;
}

export default function VerifyPage() {
  const { user } = useAuthStore();
  const [stats, setStats] = useState<TechnicianStats | null>(null);

  const isTechnician = user?.roles?.includes("TECHNICIAN") && user?.technicianId;
  const isSuperAdmin = user?.roles?.includes("SUPERADMIN");
  const isResellerAdmin = user?.roles?.includes("RESELLER ADMIN");
  const isSupervisor = user?.roles?.includes("SUPERVISOR");
  const isAdminUser = isSuperAdmin || isResellerAdmin || isSupervisor;

  useEffect(() => {
    const fetchStats = async () => {
      // Only fetch stats for actual technicians, not admin users
      if (isTechnician && user?.technicianId) {
        try {
          const response = await technicianApi.getStats(user.technicianId);
          setStats(response.data);
        } catch (error) {
          console.error("Failed to fetch stats:", error);
        }
      }
    };
    fetchStats();
  }, [user?.technicianId, isTechnician]);

  const getAccessDescription = () => {
    if (isSuperAdmin) {
      return "As Super Admin, you can verify any IMEI number.";
    }
    if (isResellerAdmin) {
      return "As Reseller Admin, you can verify IMEIs from your technicians' allowed restrictions. If no restrictions are configured, you can verify any IMEI.";
    }
    if (isSupervisor) {
      return "As Supervisor, you can verify IMEIs from your technicians' allowed restrictions. If no restrictions are configured, you can verify any IMEI.";
    }
    return "";
  };

  return (
    <AuthGuard requiredRoles={["TECHNICIAN", "SUPERADMIN", "RESELLER ADMIN", "SUPERVISOR"]}>
      <div className="min-h-screen bg-gray-50">
        <Header />
        <main className="max-w-7xl mx-auto px-4 py-6 sm:px-6 lg:px-8">
          {/* Admin Info Banner */}
          {isAdminUser && !isTechnician && (
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6 flex items-start gap-3">
              <Shield className="h-5 w-5 text-blue-600 mt-0.5" />
              <div>
                <h3 className="font-semibold text-blue-800">Admin Verification Mode</h3>
                <p className="text-sm text-blue-700">{getAccessDescription()}</p>
              </div>
            </div>
          )}

          {/* Stats Cards - Only show for technicians */}
          {isTechnician && (
            <div className="grid grid-cols-3 gap-4 mb-6">
              <Card>
                <CardContent className="pt-4 text-center">
                  <Activity className="h-6 w-6 text-blue-600 mx-auto mb-2" />
                  <p className="text-2xl font-bold">{stats?.verificationsToday || 0}</p>
                  <p className="text-xs text-gray-500">Today</p>
                </CardContent>
              </Card>
              <Card>
                <CardContent className="pt-4 text-center">
                  <CheckCircle className="h-6 w-6 text-green-600 mx-auto mb-2" />
                  <p className="text-2xl font-bold">{stats?.totalVerifications || 0}</p>
                  <p className="text-xs text-gray-500">Total</p>
                </CardContent>
              </Card>
              <Card>
                <CardContent className="pt-4 text-center">
                  <Clock className="h-6 w-6 text-orange-600 mx-auto mb-2" />
                  <p className="text-2xl font-bold">{stats?.remainingDailyLimit ?? "∞"}</p>
                  <p className="text-xs text-gray-500">Remaining</p>
                </CardContent>
              </Card>
            </div>
          )}

          {/* Admin Quick Stats */}
          {isAdminUser && !isTechnician && (
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
              <Card className="border-l-4 border-l-purple-500">
                <CardContent className="pt-4">
                  <div className="flex items-center gap-2">
                    <Shield className="h-5 w-5 text-purple-600" />
                    <span className="font-medium text-gray-900">
                      {isSuperAdmin ? "Super Admin" : isResellerAdmin ? "Reseller Admin" : "Supervisor"}
                    </span>
                  </div>
                  <p className="text-xs text-gray-500 mt-1">Verification Role</p>
                </CardContent>
              </Card>
              <Card className="border-l-4 border-l-blue-500">
                <CardContent className="pt-4">
                  <div className="flex items-center gap-2">
                    <Info className="h-5 w-5 text-blue-600" />
                    <span className="font-medium text-gray-900">
                      {isSuperAdmin ? "All IMEIs" : "Restricted IMEIs"}
                    </span>
                  </div>
                  <p className="text-xs text-gray-500 mt-1">Access Level</p>
                </CardContent>
              </Card>
              <Card className="border-l-4 border-l-green-500">
                <CardContent className="pt-4">
                  <div className="flex items-center gap-2">
                    <CheckCircle className="h-5 w-5 text-green-600" />
                    <span className="font-medium text-gray-900">No Daily Limit</span>
                  </div>
                  <p className="text-xs text-gray-500 mt-1">Verification Quota</p>
                </CardContent>
              </Card>
            </div>
          )}

          {/* IMEI Input */}
          <ImeiInput />
        </main>
      </div>
    </AuthGuard>
  );
}

