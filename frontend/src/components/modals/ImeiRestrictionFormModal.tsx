"use client";

import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useState, useEffect, useCallback, useRef } from "react";
import { restrictionApi, ExternalDevice } from "@/lib/api";
import { Search, X, Smartphone, Tag, Shield, ShieldCheck, ShieldX, Calendar, FileText, Info, Plus, Layers, Loader2 } from "lucide-react";

interface Restriction {
  restrictionId: number;
  technicianId: number;
  deviceId?: number;
  deviceImei?: string;
  tagId?: number;
  tagName?: string;
  accessType?: number;
  priority?: number;
  reason?: string;
  isPermanent?: boolean;
  validFrom?: string;
  validUntil?: string;
  notes?: string;
  status: number;
}

interface TagItem {
  tagId: number;
  tagName: string;
}

interface Props {
  open: boolean;
  restriction: Restriction | null;
  technicianId: number;
  tags: TagItem[];
  onClose: (refresh?: boolean) => void;
}

export function ImeiRestrictionFormModal({ open, restriction, technicianId, tags, onClose }: Props) {
  const [loading, setLoading] = useState(false);
  const [restrictionType, setRestrictionType] = useState<"single" | "multi" | "tag">("single");
  const [deviceSearch, setDeviceSearch] = useState("");
  const [searchResults, setSearchResults] = useState<ExternalDevice[]>([]);
  const [searching, setSearching] = useState(false);
  const [selectedDevice, setSelectedDevice] = useState<ExternalDevice | null>(null);
  const [selectedDevices, setSelectedDevices] = useState<ExternalDevice[]>([]);
  const [formData, setFormData] = useState({
    tagId: "",
    accessType: "1",
    priority: "0",
    reason: "",
    isPermanent: true,
    validFrom: "",
    validUntil: "",
    notes: "",
    status: "1",
  });

  useEffect(() => {
    if (restriction) {
      // For edit mode, determine type based on existing data
      setRestrictionType(restriction.tagId ? "tag" : "single");
      setFormData({
        tagId: restriction.tagId?.toString() || "",
        accessType: restriction.accessType?.toString() || "1",
        priority: restriction.priority?.toString() || "0",
        reason: restriction.reason || "",
        isPermanent: restriction.isPermanent ?? true,
        validFrom: restriction.validFrom?.split("T")[0] || "",
        validUntil: restriction.validUntil?.split("T")[0] || "",
        notes: restriction.notes || "",
        status: restriction.status?.toString() || "1",
      });
      if (restriction.deviceId && restriction.deviceImei) {
        setSelectedDevice({ deviceId: restriction.deviceId, imei: restriction.deviceImei } as ExternalDevice);
      }
    } else {
      setRestrictionType("single");
      setFormData({
        tagId: "",
        accessType: "1",
        priority: "0",
        reason: "",
        isPermanent: true,
        validFrom: "",
        validUntil: "",
        notes: "",
        status: "1",
      });
      setSelectedDevice(null);
      setSelectedDevices([]);
    }
    setDeviceSearch("");
    setSearchResults([]);
  }, [restriction, open]);

  const handleDeviceSearch = useCallback(async (searchValue?: string) => {
    const searchQuery = searchValue !== undefined ? searchValue : deviceSearch;
    if (!searchQuery.trim()) {
      setSearchResults([]);
      return;
    }
    // Only auto-search if at least 3 characters (for partial search)
    if (searchQuery.length < 3) {
      setSearchResults([]);
      return;
    }
    setSearching(true);
    try {
      const response = await restrictionApi.searchDevices({ search: searchQuery, pageSize: 20 });
      setSearchResults(response.data.items || []);
    } catch (error) {
      console.error("Failed to search devices:", error);
    } finally {
      setSearching(false);
    }
  }, [deviceSearch]);

  // Debounced auto-search as user types
  const searchTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  useEffect(() => {
    // Clear previous timeout
    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current);
    }

    // Don't search if less than 3 characters
    if (deviceSearch.length < 3) {
      setSearchResults([]);
      return;
    }

    // Debounce the search
    searchTimeoutRef.current = setTimeout(() => {
      handleDeviceSearch(deviceSearch);
    }, 400);

    return () => {
      if (searchTimeoutRef.current) {
        clearTimeout(searchTimeoutRef.current);
      }
    };
  }, [deviceSearch]);

  const handleSelectDevice = (device: ExternalDevice) => {
    if (restrictionType === "single") {
      setSelectedDevice(device);
    } else if (restrictionType === "multi") {
      // Add to multi-select list if not already added
      if (!selectedDevices.find(d => d.deviceId === device.deviceId)) {
        setSelectedDevices([...selectedDevices, device]);
      }
    }
    setSearchResults([]);
    setDeviceSearch("");
  };

  const handleRemoveDevice = (deviceId: number) => {
    setSelectedDevices(selectedDevices.filter(d => d.deviceId !== deviceId));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const basePayload = {
        technicianId,
        accessType: parseInt(formData.accessType),
        priority: parseInt(formData.priority),
        reason: formData.reason || undefined,
        isPermanent: formData.isPermanent,
        validFrom: !formData.isPermanent && formData.validFrom ? formData.validFrom : undefined,
        validUntil: !formData.isPermanent && formData.validUntil ? formData.validUntil : undefined,
        notes: formData.notes || undefined,
        status: parseInt(formData.status),
      };

      if (restriction) {
        // Edit mode - single update
        const payload = {
          ...basePayload,
          deviceId: restrictionType === "single" ? selectedDevice?.deviceId : undefined,
          tagId: restrictionType === "tag" ? parseInt(formData.tagId) : undefined,
        };
        await restrictionApi.update(restriction.restrictionId, payload);
      } else {
        // Create mode
        if (restrictionType === "multi" && selectedDevices.length > 0) {
          // Create multiple restrictions for each device
          for (const device of selectedDevices) {
            await restrictionApi.create({
              ...basePayload,
              deviceId: device.deviceId,
            });
          }
        } else {
          const payload = {
            ...basePayload,
            deviceId: restrictionType === "single" ? selectedDevice?.deviceId : undefined,
            tagId: restrictionType === "tag" ? parseInt(formData.tagId) : undefined,
          };
          await restrictionApi.create(payload);
        }
      }
      onClose(true);
    } catch (error) {
      console.error("Failed to save restriction:", error);
    } finally {
      setLoading(false);
    }
  };

  const isFormValid = () => {
    if (restrictionType === "single") return !!selectedDevice;
    if (restrictionType === "multi") return selectedDevices.length > 0;
    if (restrictionType === "tag") return !!formData.tagId;
    return false;
  };

  return (
    <Dialog open={open} onOpenChange={() => onClose()}>
      <DialogContent className="max-w-xl max-h-[90vh] overflow-y-auto p-0">
        {/* Header */}
        <DialogHeader className="px-6 py-4 bg-gradient-to-r from-purple-600 to-indigo-600 text-white rounded-t-lg">
          <DialogTitle className="flex items-center gap-2 text-white text-lg">
            <Shield className="h-5 w-5" />
            {restriction ? "Edit Restriction" : "Add New Restriction"}
          </DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="p-6 space-y-6">
          {/* Restriction Type Card - Show only for new restrictions */}
          {!restriction && (
            <div className="bg-gray-50 rounded-lg p-4">
              <Label className="text-sm font-semibold text-gray-700 mb-3 block">Select Restriction Type</Label>
              <div className="grid grid-cols-3 gap-3">
                <button
                  type="button"
                  onClick={() => { setRestrictionType("single"); setSelectedDevices([]); }}
                  className={`flex flex-col items-center gap-2 p-4 rounded-lg border-2 transition-all ${
                    restrictionType === "single"
                      ? "border-purple-500 bg-purple-50 text-purple-700"
                      : "border-gray-200 bg-white hover:border-gray-300"
                  }`}
                >
                  <Smartphone className={`h-6 w-6 ${restrictionType === "single" ? "text-purple-600" : "text-gray-400"}`} />
                  <span className="font-medium text-xs">Single Device</span>
                </button>
                <button
                  type="button"
                  onClick={() => { setRestrictionType("multi"); setSelectedDevice(null); }}
                  className={`flex flex-col items-center gap-2 p-4 rounded-lg border-2 transition-all ${
                    restrictionType === "multi"
                      ? "border-purple-500 bg-purple-50 text-purple-700"
                      : "border-gray-200 bg-white hover:border-gray-300"
                  }`}
                >
                  <Layers className={`h-6 w-6 ${restrictionType === "multi" ? "text-purple-600" : "text-gray-400"}`} />
                  <span className="font-medium text-xs">Multi Device</span>
                </button>
                <button
                  type="button"
                  onClick={() => { setRestrictionType("tag"); setSelectedDevice(null); setSelectedDevices([]); }}
                  className={`flex flex-col items-center gap-2 p-4 rounded-lg border-2 transition-all ${
                    restrictionType === "tag"
                      ? "border-purple-500 bg-purple-50 text-purple-700"
                      : "border-gray-200 bg-white hover:border-gray-300"
                  }`}
                >
                  <Tag className={`h-6 w-6 ${restrictionType === "tag" ? "text-purple-600" : "text-gray-400"}`} />
                  <span className="font-medium text-xs">Tag Level</span>
                </button>
              </div>
            </div>
          )}

          {/* Single Device Selection */}
          {restrictionType === "single" && (
            <div className="space-y-3">
              <Label className="text-sm font-semibold text-gray-700 flex items-center gap-2">
                <Smartphone className="h-4 w-4 text-gray-500" />
                Select Device
              </Label>
              {selectedDevice ? (
                <div className="flex items-center justify-between p-4 bg-green-50 border border-green-200 rounded-lg">
                  <div className="flex items-center gap-3">
                    <div className="h-10 w-10 bg-green-100 rounded-full flex items-center justify-center">
                      <Smartphone className="h-5 w-5 text-green-600" />
                    </div>
                    <div>
                      <p className="font-semibold text-gray-800">{selectedDevice.imei}</p>
                      <p className="text-xs text-gray-500">
                        Server: {selectedDevice.server || "N/A"} • Country: {selectedDevice.countryCode || "N/A"}
                      </p>
                    </div>
                  </div>
                  <Button type="button" variant="ghost" size="icon" onClick={() => setSelectedDevice(null)} className="text-gray-400 hover:text-red-500">
                    <X className="h-4 w-4" />
                  </Button>
                </div>
              ) : (
                <div className="space-y-3">
                  <div className="relative">
                    {searching ? (
                      <Loader2 className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-purple-500 animate-spin" />
                    ) : (
                      <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
                    )}
                    <Input
                      placeholder="Type at least 3 digits to search IMEI..."
                      value={deviceSearch}
                      onChange={(e) => setDeviceSearch(e.target.value)}
                      onKeyDown={(e) => e.key === "Enter" && (e.preventDefault(), handleDeviceSearch())}
                      className="pl-10 pr-4"
                    />
                  </div>
                  <p className="text-xs text-gray-500 flex items-center gap-1">
                    <Info className="h-3 w-3" />
                    Partial search supported - enter any part of the IMEI (min 3 digits)
                  </p>
                  {deviceSearch.length >= 3 && (
                    searching ? (
                      <div className="border rounded-lg p-4 bg-gray-50 flex items-center justify-center gap-2">
                        <Loader2 className="h-4 w-4 animate-spin text-purple-500" />
                        <span className="text-sm text-gray-500">Searching...</span>
                      </div>
                    ) : searchResults.length > 0 ? (
                      <div className="border rounded-lg overflow-hidden shadow-sm">
                        <div className="bg-purple-50 px-3 py-2 border-b flex items-center justify-between">
                          <span className="text-xs font-medium text-purple-700">{searchResults.length} device(s) found</span>
                          <span className="text-xs text-purple-500">Click to select</span>
                        </div>
                        <div className="max-h-48 overflow-y-auto">
                          {searchResults.map((device) => (
                            <div
                              key={device.deviceId}
                              className="flex items-center gap-3 p-3 hover:bg-purple-50 cursor-pointer border-b last:border-b-0 transition-colors"
                              onClick={() => handleSelectDevice(device)}
                            >
                              <div className="h-8 w-8 bg-gray-100 rounded-full flex items-center justify-center flex-shrink-0">
                                <Smartphone className="h-4 w-4 text-gray-500" />
                              </div>
                              <div className="flex-1 min-w-0">
                                <p className="font-medium text-gray-800 truncate font-mono">{device.imei}</p>
                                <p className="text-xs text-gray-500">
                                  {device.server || "Unknown Server"} • {device.countryCode || "N/A"}
                                </p>
                              </div>
                            </div>
                          ))}
                        </div>
                      </div>
                    ) : (
                      <div className="border rounded-lg p-4 bg-gray-50 text-center">
                        <p className="text-sm text-gray-500">No devices found matching &quot;{deviceSearch}&quot;</p>
                      </div>
                    )
                  )}
                </div>
              )}
            </div>
          )}

          {/* Multi Device Selection */}
          {restrictionType === "multi" && (
            <div className="space-y-3">
              <Label className="text-sm font-semibold text-gray-700 flex items-center gap-2">
                <Layers className="h-4 w-4 text-gray-500" />
                Select Multiple Devices
                {selectedDevices.length > 0 && (
                  <span className="ml-auto text-xs bg-purple-100 text-purple-700 px-2 py-0.5 rounded-full">
                    {selectedDevices.length} selected
                  </span>
                )}
              </Label>

              {/* Search Input */}
              <div className="space-y-3">
                <div className="relative">
                  {searching ? (
                    <Loader2 className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-purple-500 animate-spin" />
                  ) : (
                    <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
                  )}
                  <Input
                    placeholder="Type at least 3 digits to search IMEI..."
                    value={deviceSearch}
                    onChange={(e) => setDeviceSearch(e.target.value)}
                    onKeyDown={(e) => e.key === "Enter" && (e.preventDefault(), handleDeviceSearch())}
                    className="pl-10 pr-4"
                  />
                </div>
                <p className="text-xs text-gray-500 flex items-center gap-1">
                  <Info className="h-3 w-3" />
                  Partial search supported - enter any part of the IMEI (min 3 digits)
                </p>

                {/* Search Results */}
                {deviceSearch.length >= 3 && (
                  searching ? (
                    <div className="border rounded-lg p-4 bg-gray-50 flex items-center justify-center gap-2">
                      <Loader2 className="h-4 w-4 animate-spin text-purple-500" />
                      <span className="text-sm text-gray-500">Searching...</span>
                    </div>
                  ) : searchResults.length > 0 ? (
                    <div className="border rounded-lg overflow-hidden shadow-sm">
                      <div className="bg-purple-50 px-3 py-2 border-b flex items-center justify-between">
                        <span className="text-xs font-medium text-purple-700">{searchResults.length} device(s) found</span>
                        <span className="text-xs text-purple-500">Click to add</span>
                      </div>
                      <div className="max-h-40 overflow-y-auto">
                        {searchResults.map((device) => {
                          const isAlreadySelected = selectedDevices.find(d => d.deviceId === device.deviceId);
                          return (
                            <div
                              key={device.deviceId}
                              className={`flex items-center gap-3 p-3 border-b last:border-b-0 transition-colors ${
                                isAlreadySelected ? "bg-gray-100 opacity-50" : "hover:bg-purple-50 cursor-pointer"
                              }`}
                              onClick={() => !isAlreadySelected && handleSelectDevice(device)}
                            >
                              <div className="h-8 w-8 bg-gray-100 rounded-full flex items-center justify-center flex-shrink-0">
                                {isAlreadySelected ? (
                                  <ShieldCheck className="h-4 w-4 text-green-500" />
                                ) : (
                                  <Plus className="h-4 w-4 text-gray-500" />
                                )}
                              </div>
                              <div className="flex-1 min-w-0">
                                <p className="font-medium text-gray-800 truncate font-mono">{device.imei}</p>
                                <p className="text-xs text-gray-500">
                                  {device.server || "Unknown Server"} • {device.countryCode || "N/A"}
                                </p>
                              </div>
                              {isAlreadySelected && (
                                <span className="text-xs text-green-600">Added</span>
                              )}
                            </div>
                          );
                        })}
                      </div>
                    </div>
                  ) : (
                    <div className="border rounded-lg p-4 bg-gray-50 text-center">
                      <p className="text-sm text-gray-500">No devices found matching &quot;{deviceSearch}&quot;</p>
                    </div>
                  )
                )}
              </div>

              {/* Selected Devices List */}
              {selectedDevices.length > 0 && (
                <div className="border rounded-lg overflow-hidden">
                  <div className="bg-purple-50 px-3 py-2 border-b flex items-center justify-between">
                    <span className="text-xs font-medium text-purple-700">Selected Devices ({selectedDevices.length})</span>
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      onClick={() => setSelectedDevices([])}
                      className="text-xs text-red-600 hover:text-red-700 h-6 px-2"
                    >
                      Clear All
                    </Button>
                  </div>
                  <div className="max-h-40 overflow-y-auto">
                    {selectedDevices.map((device) => (
                      <div
                        key={device.deviceId}
                        className="flex items-center gap-3 p-3 bg-green-50 border-b last:border-b-0"
                      >
                        <div className="h-8 w-8 bg-green-100 rounded-full flex items-center justify-center flex-shrink-0">
                          <Smartphone className="h-4 w-4 text-green-600" />
                        </div>
                        <div className="flex-1 min-w-0">
                          <p className="font-medium text-gray-800 truncate">{device.imei}</p>
                          <p className="text-xs text-gray-500">
                            {device.server || "Unknown Server"} • {device.countryCode || "N/A"}
                          </p>
                        </div>
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon"
                          onClick={() => handleRemoveDevice(device.deviceId)}
                          className="text-gray-400 hover:text-red-500 h-8 w-8"
                        >
                          <X className="h-4 w-4" />
                        </Button>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}

          {/* Tag Selection Section */}
          {restrictionType === "tag" && (
            <div className="space-y-3">
              <Label className="text-sm font-semibold text-gray-700 flex items-center gap-2">
                <Tag className="h-4 w-4 text-gray-500" />
                Select Tag
              </Label>
              <select
                id="tagId"
                className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-purple-500"
                value={formData.tagId}
                onChange={(e) => setFormData({ ...formData, tagId: e.target.value })}
                required
              >
                <option value="">Choose a tag...</option>
                {tags.map((tag) => (
                  <option key={tag.tagId} value={tag.tagId}>{tag.tagName}</option>
                ))}
              </select>
              <p className="text-xs text-gray-500 flex items-center gap-1">
                <Info className="h-3 w-3" />
                Apply restriction to all devices in this tag
              </p>
            </div>
          )}

          {/* Divider */}
          <div className="border-t border-gray-200"></div>

          {/* Access Type Card */}
          <div className="space-y-3">
            <Label className="text-sm font-semibold text-gray-700 flex items-center gap-2">
              <Shield className="h-4 w-4 text-gray-500" />
              Access Type
            </Label>
            <div className="grid grid-cols-2 gap-3">
              <button
                type="button"
                onClick={() => setFormData({ ...formData, accessType: "1" })}
                className={`flex items-center gap-3 p-4 rounded-lg border-2 transition-all ${
                  formData.accessType === "1"
                    ? "border-green-500 bg-green-50"
                    : "border-gray-200 bg-white hover:border-gray-300"
                }`}
              >
                <ShieldCheck className={`h-5 w-5 ${formData.accessType === "1" ? "text-green-600" : "text-gray-400"}`} />
                <div className="text-left">
                  <span className={`font-medium text-sm ${formData.accessType === "1" ? "text-green-700" : "text-gray-700"}`}>Allow</span>
                  <p className="text-xs text-gray-500">Permit access</p>
                </div>
              </button>
              <button
                type="button"
                onClick={() => setFormData({ ...formData, accessType: "2" })}
                className={`flex items-center gap-3 p-4 rounded-lg border-2 transition-all ${
                  formData.accessType === "2"
                    ? "border-red-500 bg-red-50"
                    : "border-gray-200 bg-white hover:border-gray-300"
                }`}
              >
                <ShieldX className={`h-5 w-5 ${formData.accessType === "2" ? "text-red-600" : "text-gray-400"}`} />
                <div className="text-left">
                  <span className={`font-medium text-sm ${formData.accessType === "2" ? "text-red-700" : "text-gray-700"}`}>Deny</span>
                  <p className="text-xs text-gray-500">Block access</p>
                </div>
              </button>
            </div>
          </div>

          {/* Reason Field */}
          <div className="space-y-2">
            <Label htmlFor="reason" className="text-sm font-semibold text-gray-700 flex items-center gap-2">
              <FileText className="h-4 w-4 text-gray-500" />
              Reason <span className="text-gray-400 font-normal">(Optional)</span>
            </Label>
            <Input
              id="reason"
              value={formData.reason}
              onChange={(e) => setFormData({ ...formData, reason: e.target.value })}
              placeholder="Brief reason for this restriction..."
              className="border-gray-300"
            />
          </div>

          {/* Validity Section */}
          <div className="space-y-3">
            <Label className="text-sm font-semibold text-gray-700 flex items-center gap-2">
              <Calendar className="h-4 w-4 text-gray-500" />
              Validity Period
            </Label>
            <div className="flex items-center gap-3">
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={formData.isPermanent}
                  onChange={(e) => setFormData({ ...formData, isPermanent: e.target.checked })}
                  className="h-4 w-4 text-purple-600 rounded focus:ring-purple-500"
                />
                <span className="text-sm text-gray-700">Permanent (No expiry)</span>
              </label>
            </div>
            {!formData.isPermanent && (
              <div className="grid grid-cols-2 gap-4 p-4 bg-gray-50 rounded-lg">
                <div>
                  <Label htmlFor="validFrom" className="text-xs text-gray-600">Start Date</Label>
                  <Input
                    id="validFrom"
                    type="date"
                    value={formData.validFrom}
                    onChange={(e) => setFormData({ ...formData, validFrom: e.target.value })}
                    className="mt-1"
                  />
                </div>
                <div>
                  <Label htmlFor="validUntil" className="text-xs text-gray-600">End Date</Label>
                  <Input
                    id="validUntil"
                    type="date"
                    value={formData.validUntil}
                    onChange={(e) => setFormData({ ...formData, validUntil: e.target.value })}
                    className="mt-1"
                  />
                </div>
              </div>
            )}
          </div>

          {/* Notes Field (Collapsible) */}
          <div className="space-y-2">
            <Label htmlFor="notes" className="text-sm font-semibold text-gray-700">
              Additional Notes <span className="text-gray-400 font-normal">(Optional)</span>
            </Label>
            <textarea
              id="notes"
              className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-purple-500 resize-none"
              rows={2}
              value={formData.notes}
              onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
              placeholder="Any additional notes or comments..."
            />
          </div>

          {/* Status (for edit only) */}
          {restriction && (
            <div className="space-y-2">
              <Label htmlFor="status" className="text-sm font-semibold text-gray-700">Status</Label>
              <select
                id="status"
                className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500"
                value={formData.status}
                onChange={(e) => setFormData({ ...formData, status: e.target.value })}
              >
                <option value="1">Active</option>
                <option value="0">Inactive</option>
              </select>
            </div>
          )}

          {/* Actions */}
          <div className="flex justify-end gap-3 pt-4 border-t border-gray-200">
            <Button type="button" variant="outline" onClick={() => onClose()} className="px-6">
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={loading || !isFormValid()}
              className="px-6 bg-purple-600 hover:bg-purple-700"
            >
              {loading ? "Saving..." : restriction ? "Update Restriction" : restrictionType === "multi" ? `Create ${selectedDevices.length} Restriction(s)` : "Create Restriction"}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}

