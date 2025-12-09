"use client";

import { Header } from "@/components/layout/Header";
import { AuthGuard } from "@/components/layout/AuthGuard";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useEffect, useState } from "react";
import { auditApi } from "@/lib/api";
import { formatDate } from "@/lib/utils";
import { FileText, Search, Filter } from "lucide-react";

interface AuditLog {
  auditId: number;
  userId?: number;
  username?: string;
  action: string;
  entityType: string;
  entityId?: string;
  ipAddress?: string;
  createdAt: string;
}

interface PagedResult {
  items: AuditLog[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export default function AuditPage() {
  const [logs, setLogs] = useState<PagedResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [actionFilter, setActionFilter] = useState("");

  const fetchLogs = async () => {
    setLoading(true);
    try {
      const response = await auditApi.getLogs({
        action: actionFilter || undefined,
        page,
        pageSize: 20,
      });
      setLogs(response.data);
    } catch (error) {
      console.error("Failed to fetch audit logs:", error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchLogs();
  }, [page, actionFilter]);

  const getActionColor = (action: string) => {
    if (action.includes("Login")) return "bg-blue-100 text-blue-800";
    if (action.includes("Create")) return "bg-green-100 text-green-800";
    if (action.includes("Update")) return "bg-yellow-100 text-yellow-800";
    if (action.includes("Delete")) return "bg-red-100 text-red-800";
    if (action.includes("Denied")) return "bg-red-100 text-red-800";
    return "bg-gray-100 text-gray-800";
  };

  return (
    <AuthGuard requiredRoles={["SUPERADMIN"]}>
      <div className="min-h-screen bg-gray-50">
        <Header />
        <main className="max-w-7xl mx-auto px-4 py-6 sm:px-6 lg:px-8">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <FileText className="h-5 w-5" />
                Audit Logs
              </CardTitle>
            </CardHeader>
            <CardContent>
              {/* Filters */}
              <div className="mb-4 flex gap-4">
                <select
                  className="border rounded-md px-3 py-2 text-sm"
                  value={actionFilter}
                  onChange={(e) => setActionFilter(e.target.value)}
                >
                  <option value="">All Actions</option>
                  <option value="Login">Login</option>
                  <option value="LoginFailed">Login Failed</option>
                  <option value="ImeiAccess">IMEI Access</option>
                  <option value="ImeiAccessDenied">IMEI Access Denied</option>
                  <option value="ImeiVerification">IMEI Verification</option>
                  <option value="Create">Create</option>
                  <option value="Update">Update</option>
                </select>
              </div>

              {/* Table */}
              {loading ? (
                <div className="flex justify-center py-8">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
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
                          <td className="px-4 py-3 text-xs">{formatDate(log.createdAt)}</td>
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
              {logs && logs.totalCount > 20 && (
                <div className="flex justify-between items-center mt-4">
                  <p className="text-sm text-gray-500">
                    Showing {(page - 1) * 20 + 1} to {Math.min(page * 20, logs.totalCount)} of{" "}
                    {logs.totalCount}
                  </p>
                  <div className="flex gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={page === 1}
                      onClick={() => setPage(page - 1)}
                    >
                      Previous
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={page * 20 >= logs.totalCount}
                      onClick={() => setPage(page + 1)}
                    >
                      Next
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

