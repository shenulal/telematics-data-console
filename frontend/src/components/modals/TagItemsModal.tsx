"use client";

import { useState, useEffect, useCallback, useRef } from "react";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { tagApi, restrictionApi, EntityType, TagItemDto, technicianApi, resellerApi, userApi } from "@/lib/api";
import { Search, Plus, Trash2, Loader2, Info, Smartphone, User, Building2, Users } from "lucide-react";

interface TagInfo {
  tagId: number;
  tagName: string;
  color?: string;
}

interface Props {
  open: boolean;
  tag: TagInfo | null;
  onClose: (refresh?: boolean) => void;
}

interface ExternalDevice {
  deviceId: number;
  imei: string;
  timeZone?: string;
  sim?: string;
  countryCode?: string;
  typeId?: number;
  server?: string;
}

interface SearchableEntity {
  id: number;
  name: string;
  subText?: string;
}

// Extended tag item with device details
interface EnrichedTagItem extends TagItemDto {
  deviceDetails?: ExternalDevice;
}

export function TagItemsModal({ open, tag, onClose }: Props) {
  const [items, setItems] = useState<EnrichedTagItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [entityType, setEntityType] = useState<number>(EntityType.Device);
  const [searchTerm, setSearchTerm] = useState("");
  const [searchResults, setSearchResults] = useState<ExternalDevice[]>([]);
  const [entitySearchResults, setEntitySearchResults] = useState<SearchableEntity[]>([]);
  const [searching, setSearching] = useState(false);

  const fetchItems = async () => {
    if (!tag) return;
    setLoading(true);
    try {
      const response = await tagApi.getItems(tag.tagId, entityType);
      const tagItems: TagItemDto[] = response.data || [];

      // If devices, enrich with device details
      if (entityType === EntityType.Device && tagItems.length > 0) {
        const enrichedItems: EnrichedTagItem[] = await Promise.all(
          tagItems.map(async (item) => {
            try {
              const deviceResponse = await restrictionApi.getDevice(item.entityId);
              return { ...item, deviceDetails: deviceResponse.data };
            } catch {
              return { ...item, deviceDetails: undefined };
            }
          })
        );
        setItems(enrichedItems);
      } else {
        setItems(tagItems);
      }
    } catch (error) {
      console.error("Failed to fetch tag items:", error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (open && tag) {
      fetchItems();
      setSearchTerm("");
      setSearchResults([]);
      setEntitySearchResults([]);
    }
  }, [open, tag, entityType]);

  // Search for devices
  const handleDeviceSearch = useCallback(async (searchValue?: string) => {
    const searchQuery = searchValue !== undefined ? searchValue : searchTerm;
    if (!searchQuery.trim()) {
      setSearchResults([]);
      return;
    }
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
  }, [searchTerm]);

  // Search for technicians, resellers, users
  const handleEntitySearch = useCallback(async (searchValue?: string) => {
    const searchQuery = searchValue !== undefined ? searchValue : searchTerm;
    if (!searchQuery.trim() || searchQuery.length < 2) {
      setEntitySearchResults([]);
      return;
    }
    setSearching(true);
    try {
      let results: SearchableEntity[] = [];

      if (entityType === EntityType.Technician) {
        const response = await technicianApi.getAll({ searchTerm: searchQuery, pageSize: 20 });
        const technicians = response.data.items || response.data || [];
        results = technicians.map((t: { technicianId: number; fullName?: string; username?: string; employeeCode?: string }) => ({
          id: t.technicianId,
          name: t.fullName || t.username || `Technician ${t.technicianId}`,
          subText: t.employeeCode || undefined,
        }));
      } else if (entityType === EntityType.Reseller) {
        const response = await resellerApi.getAll({ searchTerm: searchQuery, pageSize: 20 });
        const resellers = response.data.items || response.data || [];
        results = resellers.map((r: { resellerId: number; companyName: string; displayName?: string }) => ({
          id: r.resellerId,
          name: r.companyName,
          subText: r.displayName || undefined,
        }));
      } else if (entityType === EntityType.User) {
        const response = await userApi.getAll({ search: searchQuery, pageSize: 20 });
        const users = response.data.items || response.data || [];
        results = users.map((u: { userId: number; fullName?: string; username: string; email?: string }) => ({
          id: u.userId,
          name: u.fullName || u.username,
          subText: u.email || undefined,
        }));
      }

      setEntitySearchResults(results);
    } catch (error) {
      console.error("Failed to search entities:", error);
    } finally {
      setSearching(false);
    }
  }, [searchTerm, entityType]);

  // Debounced auto-search as user types
  const searchTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  useEffect(() => {
    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current);
    }

    if (searchTerm.length < 2) {
      setSearchResults([]);
      setEntitySearchResults([]);
      return;
    }

    searchTimeoutRef.current = setTimeout(() => {
      if (entityType === EntityType.Device) {
        handleDeviceSearch(searchTerm);
      } else {
        handleEntitySearch(searchTerm);
      }
    }, 400);

    return () => {
      if (searchTimeoutRef.current) {
        clearTimeout(searchTimeoutRef.current);
      }
    };
  }, [searchTerm, entityType]);

  const handleAddDevice = async (device: ExternalDevice) => {
    if (!tag) return;
    try {
      await tagApi.addItem(tag.tagId, {
        entityType: EntityType.Device,
        entityId: device.deviceId,
        entityIdentifier: device.imei,
      });
      fetchItems();
      setSearchResults(searchResults.filter(d => d.deviceId !== device.deviceId));
    } catch (error) {
      console.error("Failed to add device to tag:", error);
    }
  };

  const handleAddEntity = async (entity: SearchableEntity) => {
    if (!tag) return;
    try {
      await tagApi.addItem(tag.tagId, {
        entityType: entityType,
        entityId: entity.id,
        entityIdentifier: entity.name,
      });
      fetchItems();
      setEntitySearchResults(entitySearchResults.filter(e => e.id !== entity.id));
    } catch (error) {
      console.error("Failed to add entity to tag:", error);
    }
  };

  const handleRemoveItem = async (item: TagItemDto) => {
    if (!tag) return;
    if (!confirm(`Remove ${item.entityIdentifier || item.entityId} from this tag?`)) return;
    try {
      await tagApi.removeItem(item.tagItemId);
      fetchItems();
    } catch (error) {
      console.error("Failed to remove item:", error);
    }
  };

  const getEntityTypeName = (type: number) => {
    switch (type) {
      case EntityType.Device: return "Device";
      case EntityType.Technician: return "Technician";
      case EntityType.Reseller: return "Reseller";
      case EntityType.User: return "User";
      default: return "Unknown";
    }
  };

  const getEntityIcon = (type: number) => {
    switch (type) {
      case EntityType.Device: return <Smartphone className="h-4 w-4 text-gray-500" />;
      case EntityType.Technician: return <User className="h-4 w-4 text-blue-500" />;
      case EntityType.Reseller: return <Building2 className="h-4 w-4 text-purple-500" />;
      case EntityType.User: return <Users className="h-4 w-4 text-green-500" />;
      default: return <User className="h-4 w-4 text-gray-500" />;
    }
  };

  return (
    <Dialog open={open} onOpenChange={() => onClose()}>
      <DialogContent className="max-w-3xl" onClose={() => onClose()}>
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <span className="w-3 h-3 rounded-full" style={{ backgroundColor: tag?.color || '#3B82F6' }}></span>
            Manage Items: {tag?.tagName}
          </DialogTitle>
        </DialogHeader>
        
        <div className="px-6 py-4 space-y-4">
          {/* Entity Type Filter */}
          <div className="flex gap-4 items-end">
            <div className="flex-1">
              <Label>Entity Type</Label>
              <Select value={entityType.toString()} onChange={(e) => setEntityType(parseInt(e.target.value))}>
                <option value={EntityType.Device}>Devices</option>
                <option value={EntityType.Technician}>Technicians</option>
                <option value={EntityType.Reseller}>Resellers</option>
                <option value={EntityType.User}>Users</option>
              </Select>
            </div>
          </div>

          {/* Search Section */}
          <div className="border rounded-lg p-4 bg-gray-50 space-y-3">
            <Label className="block">Search {getEntityTypeName(entityType)}s to Add</Label>
            <div className="relative">
              {searching ? (
                <Loader2 className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-blue-500 animate-spin" />
              ) : (
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
              )}
              <Input
                placeholder={entityType === EntityType.Device
                  ? "Type at least 3 digits to search IMEI..."
                  : `Search ${getEntityTypeName(entityType).toLowerCase()}s by name...`}
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10"
              />
            </div>
            <p className="text-xs text-gray-500 flex items-center gap-1">
              <Info className="h-3 w-3" />
              {entityType === EntityType.Device
                ? "Partial search supported - enter any part of the IMEI (min 3 digits)"
                : `Enter at least 2 characters to search ${getEntityTypeName(entityType).toLowerCase()}s`}
            </p>

            {/* Device Search Results */}
            {entityType === EntityType.Device && searchTerm.length >= 3 && (
              searching ? (
                <div className="border rounded-lg p-4 bg-white flex items-center justify-center gap-2">
                  <Loader2 className="h-4 w-4 animate-spin text-blue-500" />
                  <span className="text-sm text-gray-500">Searching...</span>
                </div>
              ) : searchResults.length > 0 ? (
                <div className="border rounded-lg overflow-hidden bg-white shadow-sm">
                  <div className="bg-blue-50 px-3 py-2 border-b flex items-center justify-between">
                    <span className="text-xs font-medium text-blue-700">{searchResults.length} device(s) found</span>
                    <span className="text-xs text-blue-500">Click + to add</span>
                  </div>
                  <div className="max-h-48 overflow-y-auto">
                    {searchResults.map((device) => {
                      const isAlreadyAdded = items.some(item => item.entityId === device.deviceId);
                      return (
                        <div
                          key={device.deviceId}
                          className={`flex items-center gap-3 px-3 py-2 border-b last:border-b-0 ${
                            isAlreadyAdded ? "bg-gray-100 opacity-50" : "hover:bg-blue-50"
                          }`}
                        >
                          <div className="h-8 w-8 bg-gray-100 rounded-full flex items-center justify-center flex-shrink-0">
                            <Smartphone className="h-4 w-4 text-gray-500" />
                          </div>
                          <div className="flex-1 min-w-0">
                            <span className="font-mono text-sm block">{device.imei}</span>
                            <span className="text-xs text-gray-500">Type: {device.typeId || "N/A"} • {device.server || "Unknown Server"}</span>
                          </div>
                          {isAlreadyAdded ? (
                            <span className="text-xs text-gray-500">Already added</span>
                          ) : (
                            <Button size="sm" variant="ghost" onClick={() => handleAddDevice(device)} className="text-blue-600 hover:text-blue-700 hover:bg-blue-100">
                              <Plus className="h-4 w-4" />
                            </Button>
                          )}
                        </div>
                      );
                    })}
                  </div>
                </div>
              ) : (
                <div className="border rounded-lg p-4 bg-white text-center">
                  <p className="text-sm text-gray-500">No devices found matching &quot;{searchTerm}&quot;</p>
                </div>
              )
            )}

            {/* Entity Search Results (Technicians, Resellers, Users) */}
            {entityType !== EntityType.Device && searchTerm.length >= 2 && (
              searching ? (
                <div className="border rounded-lg p-4 bg-white flex items-center justify-center gap-2">
                  <Loader2 className="h-4 w-4 animate-spin text-blue-500" />
                  <span className="text-sm text-gray-500">Searching...</span>
                </div>
              ) : entitySearchResults.length > 0 ? (
                <div className="border rounded-lg overflow-hidden bg-white shadow-sm">
                  <div className="bg-blue-50 px-3 py-2 border-b flex items-center justify-between">
                    <span className="text-xs font-medium text-blue-700">{entitySearchResults.length} {getEntityTypeName(entityType).toLowerCase()}(s) found</span>
                    <span className="text-xs text-blue-500">Click + to add</span>
                  </div>
                  <div className="max-h-48 overflow-y-auto">
                    {entitySearchResults.map((entity) => {
                      const isAlreadyAdded = items.some(item => item.entityId === entity.id);
                      return (
                        <div
                          key={entity.id}
                          className={`flex items-center gap-3 px-3 py-2 border-b last:border-b-0 ${
                            isAlreadyAdded ? "bg-gray-100 opacity-50" : "hover:bg-blue-50"
                          }`}
                        >
                          <div className="h-8 w-8 bg-gray-100 rounded-full flex items-center justify-center flex-shrink-0">
                            {getEntityIcon(entityType)}
                          </div>
                          <div className="flex-1 min-w-0">
                            <span className="text-sm font-medium block">{entity.name}</span>
                            {entity.subText && <span className="text-xs text-gray-500">{entity.subText}</span>}
                          </div>
                          {isAlreadyAdded ? (
                            <span className="text-xs text-gray-500">Already added</span>
                          ) : (
                            <Button size="sm" variant="ghost" onClick={() => handleAddEntity(entity)} className="text-blue-600 hover:text-blue-700 hover:bg-blue-100">
                              <Plus className="h-4 w-4" />
                            </Button>
                          )}
                        </div>
                      );
                    })}
                  </div>
                </div>
              ) : (
                <div className="border rounded-lg p-4 bg-white text-center">
                  <p className="text-sm text-gray-500">No {getEntityTypeName(entityType).toLowerCase()}s found matching &quot;{searchTerm}&quot;</p>
                </div>
              )
            )}
          </div>

          {/* Current Items */}
          <div>
            <Label className="mb-2 block">Tagged {getEntityTypeName(entityType)}s ({items.length})</Label>
            {loading ? (
              <div className="flex justify-center py-4">
                <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600"></div>
              </div>
            ) : items.length === 0 ? (
              <p className="text-gray-500 text-sm py-4 text-center">No items tagged yet</p>
            ) : (
              <div className="border rounded-lg max-h-60 overflow-y-auto">
                <table className="w-full text-sm">
                  <thead className="bg-gray-50 sticky top-0">
                    {entityType === EntityType.Device ? (
                      <tr>
                        <th className="px-3 py-2 text-left">IMEI Number</th>
                        <th className="px-3 py-2 text-left">Device Model</th>
                        <th className="px-3 py-2 text-left">Server</th>
                        <th className="px-3 py-2 text-right">Action</th>
                      </tr>
                    ) : (
                      <tr>
                        <th className="px-3 py-2 text-left">Name</th>
                        <th className="px-3 py-2 text-left">ID</th>
                        <th className="px-3 py-2 text-left">Added</th>
                        <th className="px-3 py-2 text-right">Action</th>
                      </tr>
                    )}
                  </thead>
                  <tbody className="divide-y">
                    {items.map((item) => (
                      <tr key={item.tagItemId} className="hover:bg-gray-50">
                        {entityType === EntityType.Device ? (
                          <>
                            <td className="px-3 py-2 font-mono">{item.entityIdentifier || "-"}</td>
                            <td className="px-3 py-2">{item.deviceDetails?.typeId || "-"}</td>
                            <td className="px-3 py-2">{item.deviceDetails?.server || "-"}</td>
                          </>
                        ) : (
                          <>
                            <td className="px-3 py-2">{item.entityIdentifier || "-"}</td>
                            <td className="px-3 py-2">{item.entityId}</td>
                            <td className="px-3 py-2 text-gray-500">{new Date(item.createdAt).toLocaleDateString()}</td>
                          </>
                        )}
                        <td className="px-3 py-2 text-right">
                          <Button size="sm" variant="ghost" onClick={() => handleRemoveItem(item)}>
                            <Trash2 className="h-4 w-4 text-red-600" />
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onClose(true)}>Close</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

