"use client";

import { Header } from "@/components/layout/Header";
import { AuthGuard } from "@/components/layout/AuthGuard";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useEffect, useState, useCallback } from "react";
import { verificationLogApi, VerificationLogDto, technicianApi } from "@/lib/api";
import { formatDate } from "@/lib/utils";
import { ClipboardCheck, Search, ChevronLeft, ChevronRight, Download, RefreshCw, MessageSquare, MapPin } from "lucide-react";

interface PagedResult {
  items: VerificationLogDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export default function VerificationLogsPage() {
  const [logs, setLogs] = useState<PagedResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const pageSize = 25;

  // Filter states
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [technicianName, setTechnicianName] = useState("");
  const [imeiSearch, setImeiSearch] = useState("");
  const [expandedNotes, setExpandedNotes] = useState<Set<number>>(new Set());

  const fetchLogs = useCallback(async () => {
    setLoading(true);
    try {
      const response = await verificationLogApi.getAll({
        technicianName: technicianName || undefined,
        imei: imeiSearch || undefined,
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
        page,
        pageSize,
      });
      setLogs(response.data);
    } catch (error) {
      console.error("Failed to fetch verification logs:", error);
    } finally {
      setLoading(false);
    }
  }, [technicianName, imeiSearch, fromDate, toDate, page, pageSize]);

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
    setTechnicianName("");
    setImeiSearch("");
    setPage(1);
  };

  const toggleNotes = (id: number) => {
    setExpandedNotes(prev => {
      const newSet = new Set(prev);
      if (newSet.has(id)) {
        newSet.delete(id);
      } else {
        newSet.add(id);
      }
      return newSet;
    });
  };

  const handleExport = async () => {
    try {
      const response = await verificationLogApi.getAll({
        technicianName: technicianName || undefined,
        imei: imeiSearch || undefined,
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
        page: 1,
        pageSize: 10000,
      });

      const data = response.data.items || [];
      const headers = ["Verified At", "Technician", "Employee Code", "Reseller", "IMEI", "Status", "Notes", "GPS Location"];
      const csvRows = [headers.join(",")];

      data.forEach((log: VerificationLogDto) => {
        const gpsLocation = log.latitude && log.longitude ? `${log.latitude},${log.longitude}` : "";
        const row = [
          log.verifiedAt ? new Date(log.verifiedAt).toLocaleString() : "",
          log.technicianName || "",
          log.technicianEmployeeCode || "",
          log.resellerName || "",
          log.imei || "",
          log.verificationStatus || "",
          (log.notes || "").replace(/"/g, '""'),
          gpsLocation,
        ];
        csvRows.push(row.map((v) => `"${v}"`).join(","));
      });

      const csvContent = csvRows.join("\n");
      const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
      const link = document.createElement("a");
      link.setAttribute("href", URL.createObjectURL(blob));
      link.setAttribute("download", `verification_logs_${new Date().toISOString().split("T")[0]}.csv`);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    } catch (error) {
      console.error("Export failed:", error);
    }
  };

  const getStatusColor = (status?: string) => {
    if (!status) return "bg-gray-100 text-gray-800";
    const s = status.toLowerCase();
    if (s.includes("success") || s.includes("verified") || s.includes("complete")) return "bg-green-100 text-green-800";
    if (s.includes("fail") || s.includes("error") || s.includes("denied")) return "bg-red-100 text-red-800";
    if (s.includes("pending") || s.includes("progress")) return "bg-yellow-100 text-yellow-800";
    return "bg-blue-100 text-blue-800";
  };

  const totalPages = logs ? Math.ceil(logs.totalCount / pageSize) : 1;

  return (
    <AuthGuard requiredRoles={["SUPERADMIN", "RESELLER ADMIN"]}>
      <div className="min-h-screen bg-gray-50">
        <Header />
        <main className="max-w-7xl mx-auto px-4 py-6 sm:px-6 lg:px-8">
          <Card>
            <CardHeader>
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
                <CardTitle className="flex items-center gap-2">
                  <ClipboardCheck className="h-5 w-5" />
                  Technician Verification Logs
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

                  {/* Technician Name Search */}
                  <div>
                    <Label htmlFor="technicianName" className="text-gray-700">Technician Name</Label>
                    <Input
                      id="technicianName"
                      type="text"
                      placeholder="Search by name or code..."
                      value={technicianName}
                      onChange={(e) => setTechnicianName(e.target.value)}
                      className="mt-1"
                    />
                  </div>

                  {/* IMEI Search */}
                  <div>
                    <Label htmlFor="imeiSearch" className="text-gray-700">IMEI</Label>
                    <Input
                      id="imeiSearch"
                      type="text"
                      placeholder="Search by IMEI..."
                      value={imeiSearch}
                      onChange={(e) => setImeiSearch(e.target.value)}
                      className="mt-1"
                    />
                  </div>
                </div>

                {/* Search Buttons */}
                <div className="flex gap-2 justify-end">
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

              {/* Results Summary */}
              {!loading && logs && (
                <div className="text-sm text-gray-600">
                  Found {logs.totalCount} verification log{logs.totalCount !== 1 ? "s" : ""}
                </div>
              )}

              {/* Table */}
              {loading ? (
                <div className="flex justify-center py-8">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                </div>
              ) : logs?.items.length === 0 ? (
                <div className="text-center py-8 text-gray-500">
                  <ClipboardCheck className="h-12 w-12 mx-auto mb-4 opacity-50" />
                  <p>No verification logs found for the selected filters</p>
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Verified At</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Technician</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Reseller</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">IMEI</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Status</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Notes</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Location</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y">
                      {logs?.items.map((log) => (
                        <tr key={log.verificationId} className="hover:bg-gray-50">
                          <td className="px-4 py-3 text-xs whitespace-nowrap">{formatDate(log.verifiedAt)}</td>
                          <td className="px-4 py-3">
                            <div className="font-medium">{log.technicianName || "-"}</div>
                            {log.technicianEmployeeCode && (
                              <div className="text-xs text-gray-500">{log.technicianEmployeeCode}</div>
                            )}
                          </td>
                          <td className="px-4 py-3 text-gray-600">{log.resellerName || "-"}</td>
                          <td className="px-4 py-3 font-mono text-xs">{log.imei || "-"}</td>
                          <td className="px-4 py-3">
                            {log.verificationStatus ? (
                              <span className={`px-2 py-1 rounded text-xs font-medium ${getStatusColor(log.verificationStatus)}`}>
                                {log.verificationStatus}
                              </span>
                            ) : (
                              <span className="text-gray-400">-</span>
                            )}
                          </td>
                          <td className="px-4 py-3 max-w-xs">
                            {log.notes ? (
                              <div>
                                <button
                                  onClick={() => toggleNotes(log.verificationId)}
                                  className="flex items-center gap-1 text-blue-600 hover:text-blue-800"
                                >
                                  <MessageSquare className="h-4 w-4" />
                                  <span className="text-xs">View</span>
                                </button>
                                {expandedNotes.has(log.verificationId) && (
                                  <div className="mt-2 p-2 bg-gray-100 rounded text-xs text-gray-700 whitespace-pre-wrap">
                                    {log.notes}
                                  </div>
                                )}
                              </div>
                            ) : (
                              <span className="text-gray-400">-</span>
                            )}
                          </td>
                          <td className="px-4 py-3">
                            {log.latitude && log.longitude ? (
                              <a
                                href={`https://www.google.com/maps?q=${log.latitude},${log.longitude}`}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="flex items-center gap-1 text-blue-600 hover:text-blue-800"
                              >
                                <MapPin className="h-4 w-4" />
                                <span className="text-xs">View</span>
                              </a>
                            ) : (
                              <span className="text-gray-400">-</span>
                            )}
                          </td>
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

