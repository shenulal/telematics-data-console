"use client";

import { Header } from "@/components/layout/Header";
import { AuthGuard } from "@/components/layout/AuthGuard";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useEffect, useState } from "react";
import { imeiApi } from "@/lib/api";
import { formatDate, getStatusColor } from "@/lib/utils";
import { History, MapPin, CheckCircle, XCircle } from "lucide-react";

interface VerificationHistory {
  verificationId: number;
  imei: string;
  verificationStatus: string;
  vehicleInfo?: string;
  latitude?: number;
  longitude?: number;
  notes?: string;
  verifiedAt: string;
}

export default function HistoryPage() {
  const [history, setHistory] = useState<VerificationHistory[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchHistory = async () => {
      try {
        const response = await imeiApi.getHistory(30);
        setHistory(response.data);
      } catch (error) {
        console.error("Failed to fetch history:", error);
      } finally {
        setLoading(false);
      }
    };
    fetchHistory();
  }, []);

  return (
    <AuthGuard requiredRoles={["TECHNICIAN", "SUPERADMIN", "RESELLER ADMIN"]}>
      <div className="min-h-screen bg-gray-50">
        <Header />
        <main className="max-w-7xl mx-auto px-4 py-6 sm:px-6 lg:px-8">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <History className="h-5 w-5" />
                Verification History (Last 30 Days)
              </CardTitle>
            </CardHeader>
            <CardContent>
              {loading ? (
                <div className="flex justify-center py-8">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                </div>
              ) : history.length === 0 ? (
                <div className="text-center py-8 text-gray-500">
                  <History className="h-12 w-12 mx-auto mb-4 opacity-50" />
                  <p>No verification history found</p>
                </div>
              ) : (
                <div className="space-y-4">
                  {history.map((item) => (
                    <div
                      key={item.verificationId}
                      className="border rounded-lg p-4 hover:bg-gray-50 transition-colors"
                    >
                      <div className="flex items-start justify-between">
                        <div className="flex items-center gap-3">
                          {item.verificationStatus === "Verified" ? (
                            <CheckCircle className="h-5 w-5 text-green-600" />
                          ) : (
                            <XCircle className="h-5 w-5 text-red-600" />
                          )}
                          <div>
                            <p className="font-mono font-medium">{item.imei}</p>
                            <p className="text-sm text-gray-500">
                              {formatDate(item.verifiedAt)}
                            </p>
                          </div>
                        </div>
                        <span
                          className={`px-2 py-1 rounded text-xs font-medium ${
                            item.verificationStatus === "Verified"
                              ? "bg-green-100 text-green-800"
                              : "bg-red-100 text-red-800"
                          }`}
                        >
                          {item.verificationStatus}
                        </span>
                      </div>
                      {item.latitude && item.longitude && (
                        <div className="mt-2 flex items-center gap-1 text-sm text-gray-500">
                          <MapPin className="h-4 w-4" />
                          <span>
                            {item.latitude.toFixed(4)}, {item.longitude.toFixed(4)}
                          </span>
                        </div>
                      )}
                      {item.notes && (
                        <p className="mt-2 text-sm text-gray-600 italic">
                          &quot;{item.notes}&quot;
                        </p>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </main>
      </div>
    </AuthGuard>
  );
}

