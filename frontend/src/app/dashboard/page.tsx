"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useAuthStore } from "@/lib/store";
import { dashboardApi } from "@/lib/api";
import { AuthGuard } from "@/components/layout/AuthGuard";
import { Header } from "@/components/layout/Header";
import { ArrowRight, ExternalLink } from "lucide-react";

// Dashboard types
interface DailyVerification {
  date: string;
  count: number;
}

interface HourlyVerification {
  hour: number;
  hourLabel: string;
  count: number;
}

interface VerificationSummary {
  totalVerificationsToday: number;
  totalVerificationsThisWeek: number;
  totalVerificationsThisMonth: number;
  totalVerificationsAllTime: number;
  uniqueDevicesToday: number;
  uniqueDevicesThisWeek: number;
  uniqueDevicesThisMonth: number;
  uniqueDevicesAllTime: number;
  averageVerificationsPerDay: number;
}

interface TechnicianSummary {
  technicianId: number;
  name: string;
  resellerId: number | null;
  resellerName: string;
  verificationsToday: number;
  verificationsThisWeek: number;
  verificationsThisMonth: number;
  lastVerificationAt: string | null;
}

interface ResellerSummary {
  resellerId: number;
  companyName: string;
  technicianCount: number;
  verificationsThisMonth: number;
}

interface RecentVerification {
  verificationId: number;
  deviceId: number;
  verifiedAt: string;
}

interface SuperAdminDashboard {
  totalResellers: number;
  activeResellers: number;
  totalUsers: number;
  activeUsers: number;
  totalTechnicians: number;
  activeTechnicians: number;
  verificationSummary: VerificationSummary;
  topResellers: ResellerSummary[];
  topTechnicians: TechnicianSummary[];
  verificationTrend: DailyVerification[];
  hourlyBreakdown: HourlyVerification[];
}

interface ResellerAdminDashboard {
  resellerId: number;
  resellerName: string;
  totalTechnicians: number;
  activeTechnicians: number;
  totalUsers: number;
  verificationSummary: VerificationSummary;
  topTechnicians: TechnicianSummary[];
  verificationTrend: DailyVerification[];
  hourlyBreakdown: HourlyVerification[];
}

interface SupervisorDashboard {
  totalTechnicians: number;
  activeTechnicians: number;
  verificationSummary: VerificationSummary;
  technicianStats: TechnicianSummary[];
  verificationTrend: DailyVerification[];
  hourlyBreakdown: HourlyVerification[];
}

interface TechnicianDashboard {
  technicianId: number;
  technicianName: string;
  dailyLimit: number;
  verificationSummary: VerificationSummary;
  remainingToday: number;
  lastVerificationAt: string | null;
  recentVerifications: RecentVerification[];
  verificationTrend: DailyVerification[];
  hourlyBreakdown: HourlyVerification[];
}

type DashboardData = SuperAdminDashboard | ResellerAdminDashboard | SupervisorDashboard | TechnicianDashboard;

export default function DashboardPage() {
  const { user } = useAuthStore();
  const [dashboard, setDashboard] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const userRole = user?.roles?.[0] || "";

  useEffect(() => {
    const fetchDashboard = async () => {
      try {
        setLoading(true);
        const response = await dashboardApi.getDashboard();
        setDashboard(response.data);
        setError(null);
      } catch (err: unknown) {
        const errorMessage = err instanceof Error ? err.message : "Failed to load dashboard";
        setError(errorMessage);
      } finally {
        setLoading(false);
      }
    };

    if (user) {
      fetchDashboard();
    }
  }, [user]);

  const formatDate = (dateStr: string | null) => {
    if (!dateStr) return "Never";
    return new Date(dateStr).toLocaleString();
  };

  const renderStatCard = (title: string, value: number | string, subtitle?: string, color?: string, href?: string) => {
    const cardContent = (
      <>
        <div className="flex items-start justify-between">
          <h3 className="text-sm font-medium text-gray-500">{title}</h3>
          {href && <ArrowRight className="h-4 w-4 text-gray-400" />}
        </div>
        <p className="text-3xl font-bold text-gray-900 mt-2">{value}</p>
        {subtitle && <p className="text-sm text-gray-500 mt-1">{subtitle}</p>}
      </>
    );

    if (href) {
      return (
        <Link href={href} className={`bg-white rounded-lg shadow p-6 border-l-4 ${color || "border-blue-500"} hover:shadow-lg transition-shadow cursor-pointer block`}>
          {cardContent}
        </Link>
      );
    }

    return (
      <div className={`bg-white rounded-lg shadow p-6 border-l-4 ${color || "border-blue-500"}`}>
        {cardContent}
      </div>
    );
  };

  const renderTrendChart = (trend: DailyVerification[]) => {
    const maxCount = Math.max(...trend.map((d) => d.count), 1);
    return (
      <div className="bg-white rounded-lg shadow p-6">
        <h3 className="text-lg font-semibold mb-4">Verification Trend (Daily)</h3>
        <div className="flex items-end space-x-1 h-40">
          {trend.map((day, idx) => (
            <div key={idx} className="flex-1 flex flex-col items-center">
              <div
                className="w-full bg-blue-500 rounded-t"
                style={{ height: `${(day.count / maxCount) * 100}%`, minHeight: day.count > 0 ? "4px" : "0" }}
                title={`${new Date(day.date).toLocaleDateString()}: ${day.count}`}
              />
            </div>
          ))}
        </div>
        <div className="flex justify-between text-xs text-gray-500 mt-2">
          <span>{trend.length > 0 ? new Date(trend[0].date).toLocaleDateString() : ""}</span>
          <span>{trend.length > 0 ? new Date(trend[trend.length - 1].date).toLocaleDateString() : ""}</span>
        </div>
      </div>
    );
  };

  const renderHourlyChart = (hourly: HourlyVerification[]) => {
    if (!hourly || hourly.length === 0) return null;
    const maxCount = Math.max(...hourly.map((h) => h.count), 1);
    return (
      <div className="bg-white rounded-lg shadow p-6">
        <h3 className="text-lg font-semibold mb-4">Today&apos;s Hourly Breakdown</h3>
        <div className="flex items-end space-x-1 h-32">
          {hourly.map((hour, idx) => (
            <div key={idx} className="flex-1 flex flex-col items-center">
              <div
                className="w-full bg-green-500 rounded-t"
                style={{ height: `${(hour.count / maxCount) * 100}%`, minHeight: hour.count > 0 ? "4px" : "0" }}
                title={`${hour.hourLabel}: ${hour.count}`}
              />
            </div>
          ))}
        </div>
        <div className="flex justify-between text-xs text-gray-500 mt-2">
          <span>12 AM</span>
          <span>6 AM</span>
          <span>12 PM</span>
          <span>6 PM</span>
          <span>11 PM</span>
        </div>
      </div>
    );
  };

  const renderVerificationSummaryCards = (summary: VerificationSummary) => (
    <>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {renderStatCard("Today", summary.totalVerificationsToday, `${summary.uniqueDevicesToday} unique devices`, "border-orange-500")}
        {renderStatCard("This Week", summary.totalVerificationsThisWeek, `${summary.uniqueDevicesThisWeek} unique devices`, "border-blue-500")}
        {renderStatCard("This Month", summary.totalVerificationsThisMonth, `${summary.uniqueDevicesThisMonth} unique devices`, "border-purple-500")}
        {renderStatCard("All Time", summary.totalVerificationsAllTime, `${summary.uniqueDevicesAllTime} unique devices`, "border-green-500")}
      </div>
      <div className="bg-white rounded-lg shadow p-4">
        <div className="flex items-center justify-between">
          <span className="text-sm text-gray-500">Average Verifications Per Day</span>
          <span className="text-lg font-semibold text-gray-900">{summary.averageVerificationsPerDay.toFixed(1)}</span>
        </div>
      </div>
    </>
  );

  const renderTechnicianTable = (technicians: TechnicianSummary[], title: string, showReseller: boolean = false) => (
    <div className="bg-white rounded-lg shadow p-6">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold">{title}</h3>
        <Link href="/admin/technicians" className="flex items-center gap-1 text-sm text-green-600 hover:text-green-800">
          View All <ExternalLink className="h-4 w-4" />
        </Link>
      </div>
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Name</th>
              {showReseller && <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Reseller</th>}
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Today</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">This Week</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">This Month</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Last Activity</th>
              <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Action</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {technicians.map((tech) => (
              <tr key={tech.technicianId} className="hover:bg-gray-50">
                <td className="px-4 py-3 whitespace-nowrap text-sm font-medium text-gray-900">{tech.name}</td>
                {showReseller && (
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">
                    <span className="px-2 py-1 bg-purple-100 text-purple-800 rounded-full text-xs">{tech.resellerName}</span>
                  </td>
                )}
                <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{tech.verificationsToday}</td>
                <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{tech.verificationsThisWeek}</td>
                <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{tech.verificationsThisMonth}</td>
                <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{formatDate(tech.lastVerificationAt)}</td>
                <td className="px-4 py-3 whitespace-nowrap text-sm text-right">
                  <Link href={`/admin/technicians?id=${tech.technicianId}`} className="text-green-600 hover:text-green-800">
                    View <ArrowRight className="h-3 w-3 inline" />
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );

  const renderSuperAdminDashboard = (data: SuperAdminDashboard) => (
    <div className="space-y-6">
      {/* System Overview */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {renderStatCard("Total Resellers", data.totalResellers, `${data.activeResellers} active`, "border-purple-500", "/admin/resellers")}
        {renderStatCard("Total Technicians", data.totalTechnicians, `${data.activeTechnicians} active`, "border-green-500", "/admin/technicians")}
        {renderStatCard("Total Users", data.totalUsers, `${data.activeUsers} active`, "border-blue-500", "/admin/users")}
        {renderStatCard("Avg/Day", data.verificationSummary.averageVerificationsPerDay.toFixed(1), "verifications", "border-teal-500")}
      </div>

      {/* Verification Summary */}
      <h3 className="text-lg font-semibold text-gray-700">Verification Summary</h3>
      {renderVerificationSummaryCards(data.verificationSummary)}

      {/* Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {renderTrendChart(data.verificationTrend)}
        {renderHourlyChart(data.hourlyBreakdown)}
      </div>

      {/* Top Resellers Summary */}
      <div className="bg-white rounded-lg shadow p-6">
        <div className="flex items-center justify-between mb-4">
          <div>
            <h3 className="text-lg font-semibold">Reseller Performance Summary</h3>
            <p className="text-sm text-gray-500">All technicians are organized under their respective resellers</p>
          </div>
          <Link href="/admin/resellers" className="flex items-center gap-1 text-sm text-purple-600 hover:text-purple-800">
            View All <ExternalLink className="h-4 w-4" />
          </Link>
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Reseller Company</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Technicians</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Verifications (Month)</th>
                <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Action</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {data.topResellers.map((reseller) => (
                <tr key={reseller.resellerId} className="hover:bg-gray-50">
                  <td className="px-4 py-3 whitespace-nowrap text-sm font-medium text-gray-900">
                    <span className="px-2 py-1 bg-purple-100 text-purple-800 rounded-full">{reseller.companyName}</span>
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{reseller.technicianCount}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{reseller.verificationsThisMonth}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-right">
                    <Link href={`/admin/resellers?id=${reseller.resellerId}`} className="text-purple-600 hover:text-purple-800">
                      View <ArrowRight className="h-3 w-3 inline" />
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Top Technicians with Reseller Info */}
      {renderTechnicianTable(data.topTechnicians, "Top Technicians (by Reseller)", true)}
    </div>
  );

  const renderResellerAdminDashboard = (data: ResellerAdminDashboard) => (
    <div className="space-y-6">
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-4">
        <h2 className="text-xl font-semibold text-blue-800">{data.resellerName}</h2>
      </div>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {renderStatCard("Total Technicians", data.totalTechnicians, `${data.activeTechnicians} active`, "border-green-500", "/admin/technicians")}
        {renderStatCard("Total Users", data.totalUsers, undefined, "border-blue-500", "/admin/users")}
        {renderStatCard("Avg/Day", data.verificationSummary.averageVerificationsPerDay.toFixed(1), "verifications", "border-teal-500")}
      </div>

      {/* Verification Summary */}
      <h3 className="text-lg font-semibold text-gray-700">Verification Summary</h3>
      {renderVerificationSummaryCards(data.verificationSummary)}

      {/* Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {renderTrendChart(data.verificationTrend)}
        {renderHourlyChart(data.hourlyBreakdown)}
      </div>

      {renderTechnicianTable(data.topTechnicians, "Technician Performance")}
    </div>
  );

  const renderSupervisorDashboard = (data: SupervisorDashboard) => (
    <div className="space-y-6">
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {renderStatCard("Total Technicians", data.totalTechnicians, `${data.activeTechnicians} active`, "border-green-500", "/admin/technicians")}
        {renderStatCard("Verifications Today", data.verificationSummary.totalVerificationsToday, `${data.verificationSummary.uniqueDevicesToday} unique devices`, "border-orange-500")}
        {renderStatCard("Avg/Day", data.verificationSummary.averageVerificationsPerDay.toFixed(1), "verifications", "border-teal-500")}
      </div>

      {/* Verification Summary */}
      <h3 className="text-lg font-semibold text-gray-700">Verification Summary</h3>
      {renderVerificationSummaryCards(data.verificationSummary)}

      {/* Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {renderTrendChart(data.verificationTrend)}
        {renderHourlyChart(data.hourlyBreakdown)}
      </div>

      {renderTechnicianTable(data.technicianStats, "Technician Performance")}
    </div>
  );

  const renderTechnicianDashboard = (data: TechnicianDashboard) => {
    const verificationsToday = data.verificationSummary.totalVerificationsToday;
    return (
    <div className="space-y-6">
      <div className="bg-green-50 border border-green-200 rounded-lg p-4 mb-4 flex items-center justify-between">
        <h2 className="text-xl font-semibold text-green-800">Welcome, {data.technicianName}</h2>
        <Link href="/verify" className="flex items-center gap-2 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors">
          Start Verification <ArrowRight className="h-4 w-4" />
        </Link>
      </div>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {renderStatCard("Today", `${verificationsToday} / ${data.dailyLimit}`, `${data.remainingToday} remaining`, "border-orange-500")}
        {renderStatCard("This Week", data.verificationSummary.totalVerificationsThisWeek, `${data.verificationSummary.uniqueDevicesThisWeek} unique devices`, "border-blue-500")}
        {renderStatCard("This Month", data.verificationSummary.totalVerificationsThisMonth, `${data.verificationSummary.uniqueDevicesThisMonth} unique devices`, "border-purple-500")}
        {renderStatCard("All Time", data.verificationSummary.totalVerificationsAllTime, `${data.verificationSummary.uniqueDevicesAllTime} unique devices`, "border-green-500", "/history")}
      </div>
      <div className="bg-white rounded-lg shadow p-6">
        <h3 className="text-lg font-semibold mb-2">Daily Progress</h3>
        <div className="w-full bg-gray-200 rounded-full h-4">
          <div
            className={`h-4 rounded-full ${data.remainingToday === 0 ? "bg-red-500" : "bg-green-500"}`}
            style={{ width: `${Math.min((verificationsToday / data.dailyLimit) * 100, 100)}%` }}
          />
        </div>
        <p className="text-sm text-gray-500 mt-2">
          {verificationsToday} of {data.dailyLimit} verifications completed today
        </p>
      </div>

      {/* Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {renderTrendChart(data.verificationTrend)}
        {renderHourlyChart(data.hourlyBreakdown)}
      </div>

      <div className="bg-white rounded-lg shadow p-6">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-semibold">Recent Verifications</h3>
          <Link href="/history" className="flex items-center gap-1 text-sm text-blue-600 hover:text-blue-800">
            View All History <ExternalLink className="h-4 w-4" />
          </Link>
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">ID</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Device ID</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Verified At</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {data.recentVerifications.map((v) => (
                <tr key={v.verificationId} className="hover:bg-gray-50">
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">#{v.verificationId}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm font-mono text-gray-900">{v.deviceId}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{formatDate(v.verifiedAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
  };

  const renderDashboard = () => {
    if (!dashboard) return null;

    if (userRole === "SUPERADMIN") {
      return renderSuperAdminDashboard(dashboard as SuperAdminDashboard);
    } else if (userRole === "RESELLER ADMIN") {
      return renderResellerAdminDashboard(dashboard as ResellerAdminDashboard);
    } else if (userRole === "SUPERVISOR") {
      return renderSupervisorDashboard(dashboard as SupervisorDashboard);
    } else if (userRole === "TECHNICIAN") {
      return renderTechnicianDashboard(dashboard as TechnicianDashboard);
    }

    return <div className="text-center text-gray-500">No dashboard available for your role.</div>;
  };

  return (
    <AuthGuard>
      <div className="min-h-screen bg-gray-100">
        <Header />
        <main className="max-w-7xl mx-auto py-6 px-4 sm:px-6 lg:px-8">
          <div className="mb-6">
            <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
            <p className="text-sm text-gray-500">
              {userRole === "SUPERADMIN" && "System Overview"}
              {userRole === "RESELLER ADMIN" && "Reseller Overview"}
              {userRole === "SUPERVISOR" && "Team Overview"}
              {userRole === "TECHNICIAN" && "My Performance"}
            </p>
          </div>

          {loading && (
            <div className="flex justify-center items-center h-64">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
            </div>
          )}

          {error && (
            <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-4">
              {error}
            </div>
          )}

          {!loading && !error && renderDashboard()}
        </main>
      </div>
    </AuthGuard>
  );
}

