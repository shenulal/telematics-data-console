"use client";

import { Header } from "@/components/layout/Header";
import { AuthGuard } from "@/components/layout/AuthGuard";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useEffect, useState, useCallback } from "react";
import { imeiApi } from "@/lib/api";
import { formatDate } from "@/lib/utils";
import {
  History,
  MapPin,
  CheckCircle,
  XCircle,
  Download,
  Search,
  ChevronLeft,
  ChevronRight,
  Navigation,
  Clock,
  FileText,
} from "lucide-react";

interface VerificationHistory {
  verificationId: number;
  deviceId: number;
  imei: string;
  verificationStatus: string;
  notes?: string;
  latitude?: number;
  longitude?: number;
  gpsTime?: string;
  verifiedAt: string;
}

interface PagedResult {
  items: VerificationHistory[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// Helper to format date for input
const formatDateForInput = (date: Date): string => {
  return date.toISOString().split("T")[0];
};

// Helper to check if coordinates are valid
const hasValidCoordinates = (lat?: number, lng?: number): boolean => {
  return lat !== undefined && lat !== null && lat !== 0 && lng !== undefined && lng !== null && lng !== 0;
};

export default function HistoryPage() {
  const [result, setResult] = useState<PagedResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [exporting, setExporting] = useState(false);

  // Date filter states - default to current date
  const today = new Date();
  const [fromDate, setFromDate] = useState(formatDateForInput(today));
  const [toDate, setToDate] = useState(formatDateForInput(today));
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const fetchHistory = useCallback(async () => {
    setLoading(true);
    try {
      const response = await imeiApi.getHistory({
        fromDate: fromDate ? `${fromDate}T00:00:00Z` : undefined,
        toDate: toDate ? `${toDate}T23:59:59Z` : undefined,
        page,
        pageSize,
      });
      setResult(response.data);
    } catch (error) {
      console.error("Failed to fetch history:", error);
      setResult(null);
    } finally {
      setLoading(false);
    }
  }, [fromDate, toDate, page, pageSize]);

  useEffect(() => {
    fetchHistory();
  }, [fetchHistory]);

  const handleSearch = () => {
    setPage(1);
    fetchHistory();
  };

  const handleExport = async () => {
    setExporting(true);
    try {
      // Fetch all data for export (without pagination)
      const response = await imeiApi.getHistory({
        fromDate: fromDate ? `${fromDate}T00:00:00Z` : undefined,
        toDate: toDate ? `${toDate}T23:59:59Z` : undefined,
        page: 1,
        pageSize: 10000, // Get all records
      });

      const data = response.data.items || [];

      // Create CSV content
      const headers = ["IMEI", "Status", "Verified At", "GPS Time", "Latitude", "Longitude", "Notes"];
      const csvRows = [headers.join(",")];

      data.forEach((item: VerificationHistory) => {
        const row = [
          item.imei || "",
          item.verificationStatus || "",
          item.verifiedAt ? new Date(item.verifiedAt).toLocaleString() : "",
          item.gpsTime ? new Date(item.gpsTime).toLocaleString() : "",
          hasValidCoordinates(item.latitude, item.longitude) ? item.latitude?.toFixed(6) : "",
          hasValidCoordinates(item.latitude, item.longitude) ? item.longitude?.toFixed(6) : "",
          `"${(item.notes || "").replace(/"/g, '""')}"`,
        ];
        csvRows.push(row.join(","));
      });

      // Create and download file
      const csvContent = csvRows.join("\n");
      const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
      const link = document.createElement("a");
      const url = URL.createObjectURL(blob);
      link.setAttribute("href", url);
      link.setAttribute("download", `verification_history_${fromDate}_to_${toDate}.csv`);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    } catch (error) {
      console.error("Export failed:", error);
    } finally {
      setExporting(false);
    }
  };

  const history = result?.items || [];
  const totalPages = result?.totalPages || 1;
  const totalCount = result?.totalCount || 0;

  return (
    <AuthGuard requiredRoles={["TECHNICIAN", "SUPERADMIN", "RESELLER ADMIN", "SUPERVISOR"]}>
      <div className="min-h-screen bg-gray-50">
        <Header />
        <main className="max-w-7xl mx-auto px-4 py-6 sm:px-6 lg:px-8">
          <Card>
            <CardHeader>
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
                <CardTitle className="flex items-center gap-2">
                  <History className="h-5 w-5" />
                  Verification History
                </CardTitle>
                <Button
                  variant="outline"
                  onClick={handleExport}
                  disabled={exporting || history.length === 0}
                  className="flex items-center gap-2"
                >
                  <Download className="h-4 w-4" />
                  {exporting ? "Exporting..." : "Export to CSV"}
                </Button>
              </div>
            </CardHeader>
            <CardContent className="space-y-6">
              {/* Date Filter Section */}
              <div className="bg-gray-50 rounded-lg p-4">
                <div className="flex flex-col sm:flex-row gap-4 items-end">
                  <div className="flex-1">
                    <Label htmlFor="fromDate" className="text-gray-700">From Date</Label>
                    <Input
                      id="fromDate"
                      type="date"
                      value={fromDate}
                      onChange={(e) => setFromDate(e.target.value)}
                      className="mt-1"
                    />
                  </div>
                  <div className="flex-1">
                    <Label htmlFor="toDate" className="text-gray-700">To Date</Label>
                    <Input
                      id="toDate"
                      type="date"
                      value={toDate}
                      onChange={(e) => setToDate(e.target.value)}
                      className="mt-1"
                    />
                  </div>
                  <Button onClick={handleSearch} disabled={loading} className="flex items-center gap-2">
                    <Search className="h-4 w-4" />
                    Search
                  </Button>
                </div>
              </div>

              {/* Results Summary */}
              {!loading && (
                <div className="text-sm text-gray-600">
                  Showing {history.length} of {totalCount} verification{totalCount !== 1 ? "s" : ""}
                  {fromDate && toDate && ` from ${fromDate} to ${toDate}`}
                </div>
              )}

              {/* Content */}
              {loading ? (
                <div className="flex justify-center py-8">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                </div>
              ) : history.length === 0 ? (
                <div className="text-center py-8 text-gray-500">
                  <History className="h-12 w-12 mx-auto mb-4 opacity-50" />
                  <p>No verification history found for the selected date range</p>
                </div>
              ) : (
                <div className="space-y-3">
                  {history.map((item) => (
                    <div
                      key={item.verificationId}
                      className="border rounded-lg p-4 bg-white hover:shadow-md transition-shadow"
                    >
                      {/* Header Row */}
                      <div className="flex items-start justify-between mb-3">
                        <div className="flex items-center gap-3">
                          {item.verificationStatus === "Verified" ? (
                            <CheckCircle className="h-6 w-6 text-green-600 flex-shrink-0" />
                          ) : (
                            <XCircle className="h-6 w-6 text-red-600 flex-shrink-0" />
                          )}
                          <div>
                            <p className="font-mono font-semibold text-lg text-gray-900">
                              {item.imei || "N/A"}
                            </p>
                            <div className="flex items-center gap-1 text-sm text-gray-500">
                              <Clock className="h-3 w-3" />
                              <span>Verified: {formatDate(item.verifiedAt)}</span>
                            </div>
                          </div>
                        </div>
                        <span
                          className={`px-3 py-1 rounded-full text-xs font-medium ${
                            item.verificationStatus === "Verified"
                              ? "bg-green-100 text-green-800"
                              : "bg-red-100 text-red-800"
                          }`}
                        >
                          {item.verificationStatus}
                        </span>
                      </div>

                      {/* Details Grid */}
                      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-sm">
                        {/* GPS Time */}
                        {item.gpsTime && (
                          <div className="flex items-center gap-2 text-gray-600">
                            <Clock className="h-4 w-4 text-blue-500" />
                            <span className="font-medium">GPS Time:</span>
                            <span>{formatDate(item.gpsTime)}</span>
                          </div>
                        )}

                        {/* Location */}
                        <div className="flex items-center gap-2 text-gray-600">
                          <MapPin className={`h-4 w-4 ${hasValidCoordinates(item.latitude, item.longitude) ? "text-blue-500" : "text-gray-400"}`} />
                          <span className="font-medium">Location:</span>
                          {hasValidCoordinates(item.latitude, item.longitude) ? (
                            <div className="flex items-center gap-2">
                              <span className="font-mono text-xs">
                                {item.latitude?.toFixed(6)}, {item.longitude?.toFixed(6)}
                              </span>
                              <a
                                href={`https://www.google.com/maps?q=${item.latitude},${item.longitude}`}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="inline-flex items-center gap-1 text-blue-600 hover:text-blue-800 hover:underline"
                              >
                                <Navigation className="h-3 w-3" />
                                Map
                              </a>
                            </div>
                          ) : (
                            <span className="text-gray-400 italic">No coordinates available</span>
                          )}
                        </div>
                      </div>

                      {/* Notes */}
                      {item.notes && (
                        <div className="mt-3 pt-3 border-t">
                          <div className="flex items-start gap-2 text-sm">
                            <FileText className="h-4 w-4 text-gray-400 mt-0.5 flex-shrink-0" />
                            <div>
                              <span className="font-medium text-gray-700">Notes:</span>
                              <p className="text-gray-600 mt-1 whitespace-pre-wrap">{item.notes}</p>
                            </div>
                          </div>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              )}

              {/* Pagination */}
              {totalPages > 1 && (
                <div className="flex items-center justify-between pt-4 border-t">
                  <Button
                    variant="outline"
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                    disabled={page === 1 || loading}
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
                    onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                    disabled={page === totalPages || loading}
                    className="flex items-center gap-1"
                  >
                    Next
                    <ChevronRight className="h-4 w-4" />
                  </Button>
                </div>
              )}
            </CardContent>
          </Card>
        </main>
      </div>
    </AuthGuard>
  );
}
