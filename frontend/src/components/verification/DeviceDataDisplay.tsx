"use client";

import {
  useVerificationStore,
  useAuthStore,
  LiveDeviceData,
  VerificationSnapshot,
  COMMON_COMMENTS,
  getVerificationSnapshots,
  addVerificationSnapshot,
  clearVerificationSnapshots
} from "@/lib/store";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Alert } from "@/components/ui/alert";
import {
  MapPin,
  Wifi,
  WifiOff,
  Gauge,
  CheckCircle,
  ArrowLeft,
  Clock,
  Navigation,
  Power,
  Activity,
  RefreshCw,
  ChevronDown,
  ChevronUp,
  Trash2,
} from "lucide-react";
import { formatDate } from "@/lib/utils";
import { useState, useEffect, useCallback } from "react";
import { useRouter } from "next/navigation";
import { imeiApi } from "@/lib/api";

// Status badge component
function StatusBadge({ status }: { status: string | unknown }) {
  const statusConfig: Record<string, { bg: string; text: string; icon: React.ReactNode }> = {
    STOP: { bg: "bg-red-100", text: "text-red-700", icon: <div className="w-2 h-2 rounded-full bg-red-500 animate-pulse" /> },
    TRAVEL: { bg: "bg-green-100", text: "text-green-700", icon: <div className="w-2 h-2 rounded-full bg-green-500 animate-pulse" /> },
    MOVING: { bg: "bg-green-100", text: "text-green-700", icon: <div className="w-2 h-2 rounded-full bg-green-500 animate-pulse" /> },
    IDLE: { bg: "bg-blue-100", text: "text-blue-700", icon: <div className="w-2 h-2 rounded-full bg-blue-500 animate-pulse" /> },
    OFFLINE: { bg: "bg-gray-100", text: "text-gray-700", icon: <div className="w-2 h-2 rounded-full bg-gray-500" /> },
  };

  const statusStr = typeof status === 'string' ? status : 'OFFLINE';
  const config = statusConfig[statusStr.toUpperCase()] || statusConfig.OFFLINE;

  return (
    <span className={`inline-flex items-center gap-2 px-3 py-1 rounded-full text-sm font-medium ${config.bg} ${config.text}`}>
      {config.icon}
      {statusStr}
    </span>
  );
}

export function DeviceDataDisplay() {
  const { deviceData, liveDeviceData, currentImei, reset, setLiveDeviceData, setLoading } = useVerificationStore();
  const { user } = useAuthStore();
  const [snapshots, setSnapshots] = useState<VerificationSnapshot[]>([]);
  const [refreshing, setRefreshing] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [completed, setCompleted] = useState(false);
  const [showCompleteDialog, setShowCompleteDialog] = useState(false);
  const [selectedComment, setSelectedComment] = useState("");
  const [customComment, setCustomComment] = useState("");
  const [expandedSnapshots, setExpandedSnapshots] = useState<Set<string>>(new Set());
  const [noNewDataMessage, setNoNewDataMessage] = useState("");
  const router = useRouter();

  // Check if user is a technician (not admin roles)
  const isTechnician = user?.roles?.includes("TECHNICIAN") && user?.technicianId;
  const isSuperAdmin = user?.roles?.includes("SUPERADMIN");
  const isResellerAdmin = user?.roles?.includes("RESELLER ADMIN");
  const isSupervisor = user?.roles?.includes("SUPERVISOR");
  const isAdminUser = isSuperAdmin || isResellerAdmin || isSupervisor;

  // Only technicians can complete verification, not admin users
  const canCompleteVerification = isTechnician && !isAdminUser;

  // Load snapshots from localStorage on mount
  useEffect(() => {
    if (currentImei) {
      const stored = getVerificationSnapshots(currentImei);
      setSnapshots(stored);
      // Auto-expand the latest snapshot
      if (stored.length > 0) {
        setExpandedSnapshots(new Set([stored[stored.length - 1].id]));
      }
    }
  }, [currentImei]);

  // Helper to check if data is valid (has meaningful trackTime and data)
  const isValidDeviceData = (data: LiveDeviceData | null | undefined): boolean => {
    if (!data) return false;
    // Check if trackTime is valid (not null, undefined, or invalid date string)
    if (!data.trackTime) return false;
    const trackDate = new Date(data.trackTime);
    if (isNaN(trackDate.getTime())) return false;
    // Data should have at least some location or IO data
    return true;
  };

  // Add current live data as snapshot when it loads
  useEffect(() => {
    if (liveDeviceData && currentImei && isValidDeviceData(liveDeviceData)) {
      const existing = getVerificationSnapshots(currentImei);
      // Only add if this is new data (not already captured)
      const isDuplicate = existing.some(s => s.data.trackTime === liveDeviceData.trackTime);
      if (!isDuplicate) {
        const newSnapshot = addVerificationSnapshot(currentImei, liveDeviceData);
        setSnapshots([...existing, newSnapshot]);
        setExpandedSnapshots(new Set([newSnapshot.id]));
      }
    }
  }, [liveDeviceData, currentImei]);

  const handleRefresh = useCallback(async () => {
    if (!currentImei) return;
    setRefreshing(true);
    setNoNewDataMessage(""); // Clear any previous message
    try {
      const response = await imeiApi.getLiveDeviceData(currentImei);
      const newData = response.data as LiveDeviceData;

      // Check if the new data is valid
      if (!isValidDeviceData(newData)) {
        setNoNewDataMessage("No new data available from the device. The latest data is already displayed.");
        return;
      }

      // Check if this data is already in snapshots (same trackTime)
      const existing = getVerificationSnapshots(currentImei);
      const isDuplicate = existing.some(s => s.data.trackTime === newData.trackTime);

      if (isDuplicate) {
        setNoNewDataMessage("No new data available from the device. The latest data is already displayed.");
      } else {
        setLiveDeviceData(newData);
        setNoNewDataMessage("");
      }
    } catch (error) {
      console.error("Failed to refresh data:", error);
      setNoNewDataMessage("Failed to fetch data. Please try again.");
    } finally {
      setRefreshing(false);
    }
  }, [currentImei, setLiveDeviceData]);

  const handleCompleteVerification = async () => {
    const finalComment = selectedComment === "custom" ? customComment : selectedComment;
    if (!finalComment.trim()) {
      return;
    }

    setSubmitting(true);
    try {
      const latestSnapshot = snapshots[snapshots.length - 1];
      await imeiApi.submitVerification({
        imei: currentImei,
        verificationStatus: "Verified",
        gpsData: latestSnapshot?.data
          ? {
              latitude: latestSnapshot.data.latitude,
              longitude: latestSnapshot.data.longitude,
              gpsTime: latestSnapshot.data.trackTime,
            }
          : undefined,
        notes: `${finalComment}\n\nTotal data captures: ${snapshots.length}`,
      });
      // Clear snapshots from localStorage
      clearVerificationSnapshots(currentImei);
      setCompleted(true);
    } catch (error) {
      console.error("Verification submission failed:", error);
    } finally {
      setSubmitting(false);
    }
  };

  const handleNewVerification = () => {
    if (currentImei) {
      clearVerificationSnapshots(currentImei);
    }
    reset();
    router.push("/verify");
  };

  const toggleSnapshotExpanded = (id: string) => {
    setExpandedSnapshots(prev => {
      const newSet = new Set(prev);
      if (newSet.has(id)) {
        newSet.delete(id);
      } else {
        newSet.add(id);
      }
      return newSet;
    });
  };

  const handleDeleteSnapshot = (id: string) => {
    const updated = snapshots.filter(s => s.id !== id);
    setSnapshots(updated);
    localStorage.setItem(`verification_snapshots_${currentImei}`, JSON.stringify(updated));
  };

  if (!liveDeviceData && !deviceData && snapshots.length === 0) {
    return (
      <Alert variant="warning" title="No Data">
        No device data available. Please search for an IMEI first.
      </Alert>
    );
  }

  if (completed) {
    return (
      <Card className="max-w-md mx-auto">
        <CardContent className="pt-6 text-center">
          <CheckCircle className="h-16 w-16 text-green-500 mx-auto mb-4" />
          <h2 className="text-xl font-bold mb-2">Verification Complete</h2>
          <p className="text-gray-600 mb-6">
            Device {currentImei} verification has been submitted successfully.
          </p>
          <Button onClick={handleNewVerification} className="w-full">
            Verify Another Device
          </Button>
        </CardContent>
      </Card>
    );
  }

  // Format value for display
  const formatValue = (value: unknown): string => {
    if (value === null || value === undefined) return "";
    if (typeof value === "boolean") return value ? "True" : "False";
    return String(value);
  };

  // Check if coordinates are valid (not null, undefined, or 0)
  const hasValidCoordinates = (lat: number | null | undefined, lng: number | null | undefined): boolean => {
    return lat !== null && lat !== undefined && lat !== 0 && lng !== null && lng !== undefined && lng !== 0;
  };

  // Get status color for header
  const getStatusBgColor = (status?: string | unknown) => {
    const s = typeof status === 'string' ? status.toUpperCase() : '';
    if (s === 'TRAVEL' || s === 'MOVING') return 'bg-gradient-to-r from-green-500 to-green-600';
    if (s === 'IDLE') return 'bg-gradient-to-r from-blue-500 to-blue-600';
    if (s === 'STOP') return 'bg-gradient-to-r from-red-500 to-red-600';
    return 'bg-gradient-to-r from-gray-500 to-gray-600';
  };

  // Render a single snapshot's data
  const renderSnapshotData = (data: LiveDeviceData, snapshotId: string, timestamp: string, isExpanded: boolean) => (
    <Card key={snapshotId} className="overflow-hidden">
      {/* Snapshot Header - Clickable */}
      <div
        className={`p-3 cursor-pointer ${getStatusBgColor(data.status)}`}
        onClick={() => toggleSnapshotExpanded(snapshotId)}
      >
        <div className="flex items-center justify-between text-white">
          <div className="flex items-center gap-3">
            {data.isOnline ? <Wifi className="h-5 w-5" /> : <WifiOff className="h-5 w-5" />}
            <StatusBadge status={data.status || "OFFLINE"} />
          </div>
          <div className="flex items-center gap-2">
            <span className="text-xs opacity-90">{formatDate(timestamp)}</span>
            {isExpanded ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
          </div>
        </div>
      </div>

      {/* Always Visible Summary */}
      <CardContent className="py-3 border-b border-gray-100">
        {/* Track Time and Speed Row */}
        <div className="flex flex-wrap gap-6 mb-3">
          <div className="flex items-center gap-2">
            <Clock className="h-4 w-4 text-gray-400 flex-shrink-0" />
            <div>
              <p className="text-xs text-gray-500">Track Time</p>
              <p className="text-sm font-semibold whitespace-nowrap">{formatDate(data.trackTime)}</p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <Gauge className="h-4 w-4 text-gray-400 flex-shrink-0" />
            <div>
              <p className="text-xs text-gray-500">Speed</p>
              <p className="text-sm font-semibold whitespace-nowrap">{data.speed?.toFixed(1) || 0} km/h</p>
            </div>
          </div>
        </div>
        {/* Location Section */}
        <div className="bg-gray-50 rounded-lg p-3">
          <div className="flex items-start gap-2">
            <MapPin className={`h-4 w-4 mt-0.5 flex-shrink-0 ${hasValidCoordinates(data.latitude, data.longitude) ? 'text-blue-600' : 'text-gray-400'}`} />
            <div className="flex-1 min-w-0">
              <p className="text-xs text-gray-500 mb-1">Location</p>
              {hasValidCoordinates(data.latitude, data.longitude) ? (
                <>
                  {data.locationName && (
                    <p className="text-sm font-medium text-gray-800 mb-1">{data.locationName}</p>
                  )}
                  <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs">
                    <span className="font-mono text-gray-600">
                      {data.latitude?.toFixed(6)}, {data.longitude?.toFixed(6)}
                    </span>
                    {data.locationProximity !== undefined && data.locationProximity !== null && (
                      <span className="text-gray-500">
                        Proximity: {(data.locationProximity * 1000).toFixed(0)}m
                      </span>
                    )}
                    <a
                      href={`https://www.google.com/maps?q=${data.latitude},${data.longitude}`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="inline-flex items-center gap-1 text-blue-600 hover:text-blue-800 hover:underline"
                      onClick={(e) => e.stopPropagation()}
                    >
                      <Navigation className="h-3 w-3" />
                      View on Maps
                    </a>
                  </div>
                </>
              ) : (
                <p className="text-sm text-gray-500 italic">No coordinates available</p>
              )}
            </div>
          </div>
        </div>
      </CardContent>

      {/* Expanded Content - Device Parameters */}
      {isExpanded && (
        <>
          <CardContent className="pt-3">
            <h4 className="text-sm font-medium text-gray-700 mb-2 flex items-center gap-2">
              <Activity className="h-4 w-4" />
              Device Parameters ({data.ioData?.length || 0})
            </h4>
            {data.ioData && data.ioData.length > 0 && (
              <div className="overflow-x-auto border rounded-lg">
                <table className="w-full text-xs">
                  <thead>
                    <tr className="border-b border-gray-200 bg-gray-50">
                      <th className="text-left py-2 px-2 font-medium text-gray-500">Universal IO ID</th>
                      <th className="text-left py-2 px-2 font-semibold text-blue-700 bg-blue-50">Universal IO Name</th>
                      <th className="text-left py-2 px-2 font-medium text-gray-500">IO Code</th>
                      <th className="text-left py-2 px-2 font-medium text-gray-500">IO Name</th>
                      <th className="text-left py-2 px-2 font-semibold text-green-700 bg-green-50">Value</th>
                      <th className="text-left py-2 px-2 font-medium text-gray-500">Raw Value</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.ioData.map((io, index) => (
                      <tr key={index} className="border-b border-gray-100 hover:bg-gray-50">
                        <td className="py-2 px-2 font-mono text-gray-600">{io.universalIOID ?? ""}</td>
                        <td className="py-2 px-2 font-medium text-blue-700 bg-blue-50/50">{io.universalIOName || ""}</td>
                        <td className="py-2 px-2 font-mono text-gray-600">{io.ioCode || ""}</td>
                        <td className="py-2 px-2 text-gray-600">{io.ioName || ""}</td>
                        <td className="py-2 px-2 font-mono font-medium text-green-700 bg-green-50/50">{formatValue(io.value)}</td>
                        <td className="py-2 px-2 font-mono text-gray-600">{io.rawValue ?? ""}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </CardContent>

          {/* Delete Button */}
          {snapshots.length > 1 && (
            <div className="px-4 pb-3 flex justify-end">
              <Button
                variant="ghost"
                size="sm"
                className="text-red-600 hover:text-red-700 hover:bg-red-50"
                onClick={(e) => { e.stopPropagation(); handleDeleteSnapshot(snapshotId); }}
              >
                <Trash2 className="h-4 w-4 mr-1" /> Remove
              </Button>
            </div>
          )}
        </>
      )}
    </Card>
  );

  // Sort snapshots by trackTime descending (most recent first)
  const sortedSnapshots = [...snapshots].sort((a, b) => {
    const timeA = new Date(a.data.trackTime || a.timestamp).getTime();
    const timeB = new Date(b.data.trackTime || b.timestamp).getTime();
    return timeB - timeA;
  });

  return (
    <div className="space-y-4 max-w-3xl mx-auto px-4 sm:px-0">
      {/* Top Action Bar */}
      <div className="flex flex-col sm:flex-row gap-2 sm:items-center sm:justify-between">
        {/* Back to Search - Only for Super Admin and Reseller Admin */}
        {(isSuperAdmin || isResellerAdmin) ? (
          <Button variant="ghost" onClick={handleNewVerification} size="sm">
            <ArrowLeft className="mr-2 h-4 w-4" /> Back to Search
          </Button>
        ) : (
          <div /> /* Empty div to maintain layout */
        )}
        <div className="flex gap-2">
          <Button
            onClick={handleRefresh}
            disabled={refreshing}
            variant="outline"
            size="sm"
          >
            <RefreshCw className={`mr-2 h-4 w-4 ${refreshing ? 'animate-spin' : ''}`} />
            {refreshing ? 'Refreshing...' : 'Refresh'}
          </Button>
          {canCompleteVerification && snapshots.length > 0 && !showCompleteDialog && (
            <Button
              onClick={() => setShowCompleteDialog(true)}
              size="sm"
            >
              <CheckCircle className="mr-2 h-4 w-4" />
              Complete
            </Button>
          )}
        </div>
      </div>

      {/* Complete Verification Dialog - Shown at top when active (only for technicians) */}
      {canCompleteVerification && showCompleteDialog && (
        <Card className="border-2 border-blue-200 bg-blue-50/30">
          <CardHeader className="pb-2">
            <CardTitle className="text-base flex items-center gap-2">
              <CheckCircle className="h-5 w-5 text-blue-600" />
              Complete Verification
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-sm text-gray-600">
              You have captured <strong>{snapshots.length}</strong> data snapshot{snapshots.length > 1 ? 's' : ''} for this device.
              Select or enter a comment to complete the verification.
            </p>

            {/* Common Comments */}
            <div className="space-y-2">
              <label className="block text-sm font-medium text-gray-700">Select Comment</label>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                {COMMON_COMMENTS.map((comment) => (
                  <label
                    key={comment}
                    className={`flex items-center gap-2 p-2 rounded-lg border cursor-pointer transition-colors ${
                      selectedComment === comment
                        ? 'border-blue-500 bg-blue-50'
                        : 'border-gray-200 hover:bg-gray-50'
                    }`}
                  >
                    <input
                      type="radio"
                      name="comment"
                      value={comment}
                      checked={selectedComment === comment}
                      onChange={(e) => setSelectedComment(e.target.value)}
                      className="text-blue-600"
                    />
                    <span className="text-sm">{comment}</span>
                  </label>
                ))}
                <label
                  className={`flex items-center gap-2 p-2 rounded-lg border cursor-pointer transition-colors ${
                    selectedComment === 'custom'
                      ? 'border-blue-500 bg-blue-50'
                      : 'border-gray-200 hover:bg-gray-50'
                  }`}
                >
                  <input
                    type="radio"
                    name="comment"
                    value="custom"
                    checked={selectedComment === 'custom'}
                    onChange={(e) => setSelectedComment(e.target.value)}
                    className="text-blue-600"
                  />
                  <span className="text-sm">Custom comment...</span>
                </label>
              </div>
            </div>

            {/* Custom Comment Input */}
            {selectedComment === 'custom' && (
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Custom Comment</label>
                <textarea
                  className="w-full border rounded-lg p-2 text-sm"
                  rows={3}
                  placeholder="Enter your custom verification comment..."
                  value={customComment}
                  onChange={(e) => setCustomComment(e.target.value)}
                />
              </div>
            )}

            {/* Action Buttons */}
            <div className="flex gap-3 pt-2">
              <Button
                variant="outline"
                onClick={() => setShowCompleteDialog(false)}
                className="flex-1"
              >
                Cancel
              </Button>
              <Button
                onClick={handleCompleteVerification}
                disabled={!selectedComment || (selectedComment === 'custom' && !customComment.trim()) || submitting}
                className="flex-1"
              >
                {submitting ? (
                  <>
                    <RefreshCw className="mr-2 h-4 w-4 animate-spin" />
                    Submitting...
                  </>
                ) : (
                  <>
                    <CheckCircle className="mr-2 h-4 w-4" />
                    Submit Verification
                  </>
                )}
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* No New Data Message */}
      {noNewDataMessage && (
        <div className="bg-amber-50 border border-amber-200 rounded-lg p-4 flex items-center gap-3">
          <div className="flex-shrink-0">
            <Clock className="h-5 w-5 text-amber-600" />
          </div>
          <div>
            <p className="text-sm font-medium text-amber-800">No New Data Available</p>
            <p className="text-sm text-amber-700">{noNewDataMessage}</p>
          </div>
          <Button
            variant="ghost"
            size="sm"
            className="ml-auto text-amber-600 hover:text-amber-800"
            onClick={() => setNoNewDataMessage("")}
          >
            Dismiss
          </Button>
        </div>
      )}

      {/* IMEI Info Header */}
      <Card className="bg-gradient-to-r from-slate-700 to-slate-800 text-white">
        <CardContent className="py-4">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2">
            <div>
              <p className="text-xs opacity-70">Verifying IMEI</p>
              <p className="font-mono font-bold text-xl">{currentImei}</p>
            </div>
            <div className="flex items-center gap-4 text-sm">
              <div className="text-center">
                <p className="text-2xl font-bold">{snapshots.length}</p>
                <p className="text-xs opacity-70">Data Captures</p>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Verification Snapshots */}
      <div className="space-y-3">
        <h3 className="text-sm font-medium text-gray-700 flex items-center gap-2">
          <Activity className="h-4 w-4" />
          Data Captures ({snapshots.length})
        </h3>
        {sortedSnapshots.length === 0 ? (
          <Card>
            <CardContent className="py-8 text-center text-gray-500">
              <RefreshCw className="h-8 w-8 mx-auto mb-2 opacity-50" />
              <p>No data captured yet. Click "Refresh" to fetch device data.</p>
            </CardContent>
          </Card>
        ) : (
          sortedSnapshots.map((snapshot) =>
            renderSnapshotData(
              snapshot.data,
              snapshot.id,
              snapshot.timestamp,
              expandedSnapshots.has(snapshot.id)
            )
          )
        )}
      </div>
    </div>
  );
}

