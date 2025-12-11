"use client";

import { Header } from "@/components/layout/Header";
import { AuthGuard } from "@/components/layout/AuthGuard";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useEffect, useState, useCallback } from "react";
import { auditApi } from "@/lib/api";
import { formatDate } from "@/lib/utils";
import { FileText, Search, ChevronLeft, ChevronRight, Download, RefreshCw } from "lucide-react";

interface AuditLog {
  auditId: number;
  userId?: number;
  username?: string;
  action: string;
  entityType: string;
  entityId?: string;
  oldValues?: string;
  newValues?: string;
  ipAddress?: string;
  createdAt: string;
}

interface AuditUser {
  userId: number;
  username: string;
  fullName?: string;
}

interface PagedResult {
  items: AuditLog[];
  totalCount: number;
  page: number;
  pageSize: number;
}

const formatDateForInput = (date: Date): string => {
  return date.toISOString().split("T")[0];
};

export default function AuditPage() {
  const [logs, setLogs] = useState<PagedResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const pageSize = 25;

  // Filter states
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [actionFilter, setActionFilter] = useState("");
  const [userFilter, setUserFilter] = useState<number | "">("");
  const [usernameSearch, setUsernameSearch] = useState("");

  // Filter options from API
  const [actions, setActions] = useState<string[]>([]);
  const [users, setUsers] = useState<AuditUser[]>([]);

  // Load filter options on mount
  useEffect(() => {
    const loadFilterOptions = async () => {
      try {
        const [actionsRes, usersRes] = await Promise.all([
          auditApi.getActions(),
          auditApi.getUsers(),
        ]);
        setActions(actionsRes.data);
        setUsers(usersRes.data);
      } catch (error) {
        console.error("Failed to load filter options:", error);
      }
    };
    loadFilterOptions();
  }, []);

  const fetchLogs = useCallback(async () => {
    setLoading(true);
    try {
      const response = await auditApi.getLogs({
        action: actionFilter || undefined,
        userId: userFilter || undefined,
        username: usernameSearch || undefined,
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
        page,
        pageSize,
      });
      setLogs(response.data);
    } catch (error) {
      console.error("Failed to fetch audit logs:", error);
    } finally {
      setLoading(false);
    }
  }, [actionFilter, userFilter, usernameSearch, fromDate, toDate, page, pageSize]);

  useEffect(() => {
    fetchLogs();
  }, [fetchLogs]);

  const handleSearch = () => {
    setPage(1);
    fetchLogs();
  };

  const handleReset = () => {
    setFromDate("");
    setToDate("");
    setActionFilter("");
    setUserFilter("");
    setUsernameSearch("");
    setPage(1);
  };

  const handleExport = async () => {
    try {
      const response = await auditApi.getLogs({
        action: actionFilter || undefined,
        userId: userFilter || undefined,
        username: usernameSearch || undefined,
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
        page: 1,
        pageSize: 10000,
      });

      const data = response.data.items || [];
      const headers = ["Time", "User", "Action", "Entity Type", "Entity ID", "IP Address"];
      const csvRows = [headers.join(",")];

      data.forEach((log: AuditLog) => {
        const row = [
          log.createdAt ? new Date(log.createdAt).toLocaleString() : "",
          log.username || "",
          log.action,
          log.entityType,
          log.entityId || "",
          log.ipAddress || "",
        ];
        csvRows.push(row.map((v) => `"${v}"`).join(","));
      });

      const csvContent = csvRows.join("\n");
      const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
      const link = document.createElement("a");
      link.setAttribute("href", URL.createObjectURL(blob));
      link.setAttribute("download", `audit_logs_${new Date().toISOString().split("T")[0]}.csv`);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    } catch (error) {
      console.error("Export failed:", error);
    }
  };

  const getActionColor = (action: string) => {
    if (action.includes("LOGIN") || action.includes("Login")) return "bg-blue-100 text-blue-800";
    if (action.includes("CREATE") || action.includes("Create")) return "bg-green-100 text-green-800";
    if (action.includes("UPDATE") || action.includes("Update")) return "bg-yellow-100 text-yellow-800";
    if (action.includes("DELETE") || action.includes("Delete")) return "bg-red-100 text-red-800";
    if (action.includes("DENIED") || action.includes("Denied")) return "bg-red-100 text-red-800";
    if (action.includes("FAILED") || action.includes("Failed")) return "bg-orange-100 text-orange-800";
    if (action.includes("ACCESS") || action.includes("Access")) return "bg-purple-100 text-purple-800";
    if (action.includes("VERIFICATION")) return "bg-teal-100 text-teal-800";
    return "bg-gray-100 text-gray-800";
  };

  const totalPages = logs ? Math.ceil(logs.totalCount / pageSize) : 1;

  return (
    <AuthGuard requiredRoles={["SUPERADMIN"]}>
      <div className="min-h-screen bg-gray-50">
        <Header />
        <main className="max-w-7xl mx-auto px-4 py-6 sm:px-6 lg:px-8">
          <Card>
            <CardHeader>
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
                <CardTitle className="flex items-center gap-2">
                  <FileText className="h-5 w-5" />
                  Audit Logs
                </CardTitle>
                <Button
                  variant="outline"
                  onClick={handleExport}
                  disabled={!logs || logs.items.length === 0}
                  className="flex items-center gap-2"
                >
                  <Download className="h-4 w-4" />
                  Export to CSV
                </Button>
              </div>
            </CardHeader>
            <CardContent className="space-y-6">
              {/* Filters Section */}
              <div className="bg-gray-50 rounded-lg p-4">
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-4">
                  {/* Date From */}
                  <div>
                    <Label htmlFor="fromDate" className="text-gray-700">Date From</Label>
                    <Input
                      id="fromDate"
                      type="date"
                      value={fromDate}
                      onChange={(e) => setFromDate(e.target.value)}
                      className="mt-1"
                    />
                  </div>

                  {/* Date To */}
                  <div>
                    <Label htmlFor="toDate" className="text-gray-700">Date To</Label>
                    <Input
                      id="toDate"
                      type="date"
                      value={toDate}
                      onChange={(e) => setToDate(e.target.value)}
                      className="mt-1"
                    />
                  </div>

                  {/* Action Filter */}
                  <div>
                    <Label htmlFor="actionFilter" className="text-gray-700">Action</Label>
                    <select
                      id="actionFilter"
                      className="mt-1 w-full border rounded-md px-3 py-2 text-sm bg-white"
                      value={actionFilter}
                      onChange={(e) => setActionFilter(e.target.value)}
                    >
                      <option value="">All Actions</option>
                      {actions.map((action) => (
                        <option key={action} value={action}>
                          {action}
                        </option>
                      ))}
                    </select>
                  </div>

                  {/* User Filter */}
                  <div>
                    <Label htmlFor="userFilter" className="text-gray-700">User</Label>
                    <select
                      id="userFilter"
                      className="mt-1 w-full border rounded-md px-3 py-2 text-sm bg-white"
                      value={userFilter}
                      onChange={(e) => setUserFilter(e.target.value ? Number(e.target.value) : "")}
                    >
                      <option value="">All Users</option>
                      {users.map((user) => (
                        <option key={user.userId} value={user.userId}>
                          {user.username} {user.fullName ? `(${user.fullName})` : ""}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>

                {/* Username Search and Buttons */}
                <div className="flex flex-col sm:flex-row gap-4 items-end">
                  <div className="flex-1">
                    <Label htmlFor="usernameSearch" className="text-gray-700">Search by Username</Label>
                    <Input
                      id="usernameSearch"
                      type="text"
                      placeholder="Type username to search..."
                      value={usernameSearch}
                      onChange={(e) => setUsernameSearch(e.target.value)}
                      className="mt-1"
                    />
                  </div>
                  <div className="flex gap-2">
                    <Button onClick={handleSearch} disabled={loading} className="flex items-center gap-2">
                      <Search className="h-4 w-4" />
                      Search
                    </Button>
                    <Button variant="outline" onClick={handleReset} className="flex items-center gap-2">
                      <RefreshCw className="h-4 w-4" />
                      Reset
                    </Button>
                  </div>
                </div>
              </div>

              {/* Results Summary */}
              {!loading && logs && (
                <div className="text-sm text-gray-600">
                  Found {logs.totalCount} audit log{logs.totalCount !== 1 ? "s" : ""}
                </div>
              )}

              {/* Table */}
              {loading ? (
                <div className="flex justify-center py-8">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                </div>
              ) : logs?.items.length === 0 ? (
                <div className="text-center py-8 text-gray-500">
                  <FileText className="h-12 w-12 mx-auto mb-4 opacity-50" />
                  <p>No audit logs found for the selected filters</p>
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Time</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">User</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Action</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Entity</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">IP Address</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y">
                      {logs?.items.map((log) => (
                        <tr key={log.auditId} className="hover:bg-gray-50">
                          <td className="px-4 py-3 text-xs whitespace-nowrap">{formatDate(log.createdAt)}</td>
                          <td className="px-4 py-3">{log.username || "-"}</td>
                          <td className="px-4 py-3">
                            <span className={`px-2 py-1 rounded text-xs font-medium ${getActionColor(log.action)}`}>
                              {log.action}
                            </span>
                          </td>
                          <td className="px-4 py-3">
                            <span className="text-gray-600">{log.entityType}</span>
                            {log.entityId && (
                              <span className="text-gray-400 ml-1">#{log.entityId}</span>
                            )}
                          </td>
                          <td className="px-4 py-3 font-mono text-xs">{log.ipAddress || "-"}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}

              {/* Pagination */}
              {logs && totalPages > 1 && (
                <div className="flex items-center justify-between pt-4 border-t">
                  <p className="text-sm text-gray-500">
                    Showing {(page - 1) * pageSize + 1} to {Math.min(page * pageSize, logs.totalCount)} of{" "}
                    {logs.totalCount}
                  </p>
                  <div className="flex items-center gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={page === 1 || loading}
                      onClick={() => setPage(page - 1)}
                      className="flex items-center gap-1"
                    >
                      <ChevronLeft className="h-4 w-4" />
                      Previous
                    </Button>
                    <span className="text-sm text-gray-600">
                      Page {page} of {totalPages}
                    </span>
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={page >= totalPages || loading}
                      onClick={() => setPage(page + 1)}
                      className="flex items-center gap-1"
                    >
                      Next
                      <ChevronRight className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </main>
      </div>
    </AuthGuard>
  );
}
