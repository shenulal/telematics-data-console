"use client";

import { Header } from "@/components/layout/Header";
import { AuthGuard } from "@/components/layout/AuthGuard";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { restrictionApi, technicianApi, tagApi } from "@/lib/api";
import { getStatusColor, getStatusText } from "@/lib/utils";
import { Shield, Plus, ArrowLeft, Edit, Trash2, Ban, CheckCircle } from "lucide-react";
import { ImeiRestrictionFormModal } from "@/components/modals/ImeiRestrictionFormModal";

interface Restriction {
  restrictionId: number;
  technicianId: number;
  technicianName?: string;
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
  createdAt?: string;
}

interface Technician {
  technicianId: number;
  fullName?: string;
  username: string;
  imeiRestrictionMode: number;
}

interface PagedResult {
  items: Restriction[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export default function TechnicianRestrictionsPage() {
  const params = useParams();
  const router = useRouter();
  const technicianId = parseInt(params.id as string);

  const [technician, setTechnician] = useState<Technician | null>(null);
  const [restrictions, setRestrictions] = useState<PagedResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [modalOpen, setModalOpen] = useState(false);
  const [editingRestriction, setEditingRestriction] = useState<Restriction | null>(null);
  const [tags, setTags] = useState<{ tagId: number; tagName: string }[]>([]);

  const fetchTechnician = async () => {
    try {
      const response = await technicianApi.getById(technicianId);
      setTechnician(response.data);
    } catch (error) {
      console.error("Failed to fetch technician:", error);
    }
  };

  const fetchRestrictions = async () => {
    setLoading(true);
    try {
      const response = await restrictionApi.getByTechnician(technicianId, page, 10);
      setRestrictions(response.data);
    } catch (error) {
      console.error("Failed to fetch restrictions:", error);
    } finally {
      setLoading(false);
    }
  };

  const fetchTags = async () => {
    try {
      const response = await tagApi.getAll({ status: 1, pageSize: 100 });
      setTags(response.data.items || []);
    } catch (error) {
      console.error("Failed to fetch tags:", error);
    }
  };

  useEffect(() => {
    fetchTechnician();
    fetchTags();
  }, [technicianId]);

  useEffect(() => {
    fetchRestrictions();
  }, [page, technicianId]);

  const handleAdd = () => {
    setEditingRestriction(null);
    setModalOpen(true);
  };

  const handleEdit = (restriction: Restriction) => {
    setEditingRestriction(restriction);
    setModalOpen(true);
  };

  const handleDelete = async (id: number) => {
    if (!confirm("Are you sure you want to delete this restriction?")) return;
    try {
      await restrictionApi.delete(id);
      fetchRestrictions();
    } catch (error) {
      console.error("Failed to delete restriction:", error);
    }
  };

  const handleModalClose = (refresh?: boolean) => {
    setModalOpen(false);
    setEditingRestriction(null);
    if (refresh) fetchRestrictions();
  };

  const getAccessTypeLabel = (type?: number) => {
    switch (type) {
      case 1: return { label: "Allow", color: "text-green-600", icon: CheckCircle };
      case 2: return { label: "Deny", color: "text-red-600", icon: Ban };
      default: return { label: "Unknown", color: "text-gray-600", icon: Shield };
    }
  };

  const getRestrictionModeLabel = (mode: number) => {
    switch (mode) {
      case 0: return "No Restriction";
      case 1: return "Allow List";
      case 2: return "Deny List";
      default: return "Unknown";
    }
  };

  return (
    <AuthGuard requiredRoles={["SUPERADMIN", "RESELLER ADMIN"]}>
      <div className="min-h-screen bg-gray-50">
        <Header />
        <main className="max-w-7xl mx-auto px-4 py-6 sm:px-6 lg:px-8">
          <div className="mb-4">
            <Button variant="ghost" size="sm" onClick={() => router.push("/admin/technicians")}>
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Technicians
            </Button>
          </div>
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <div>
                <CardTitle className="flex items-center gap-2">
                  <Shield className="h-5 w-5" />
                  IMEI Restrictions
                </CardTitle>
                {technician && (
                  <p className="text-sm text-gray-500 mt-1">
                    {technician.fullName || technician.username} - Mode: {getRestrictionModeLabel(technician.imeiRestrictionMode)}
                  </p>
                )}
              </div>
              <Button size="sm" onClick={handleAdd}>
                <Plus className="h-4 w-4 mr-2" />
                Add Restriction
              </Button>
            </CardHeader>
            <CardContent>
              {loading ? (
                <div className="flex justify-center py-8">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                </div>
              ) : restrictions?.items.length === 0 ? (
                <div className="text-center py-8 text-gray-500">
                  No restrictions configured for this technician.
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Type</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Device/Tag</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Access</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Validity</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Status</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Actions</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y">
                      {restrictions?.items.map((r) => {
                        const accessInfo = getAccessTypeLabel(r.accessType);
                        const AccessIcon = accessInfo.icon;
                        return (
                          <tr key={r.restrictionId} className="hover:bg-gray-50">
                            <td className="px-4 py-3">
                              {r.tagId ? (
                                <span className="px-2 py-1 bg-purple-100 text-purple-700 rounded text-xs">Tag</span>
                              ) : (
                                <span className="px-2 py-1 bg-blue-100 text-blue-700 rounded text-xs">Device</span>
                              )}
                            </td>
                            <td className="px-4 py-3">
                              <div>
                                <p className="font-medium">{r.tagName || r.deviceImei || `Device #${r.deviceId}`}</p>
                                {r.reason && <p className="text-xs text-gray-500">{r.reason}</p>}
                              </div>
                            </td>
                            <td className="px-4 py-3">
                              <span className={`flex items-center gap-1 ${accessInfo.color}`}>
                                <AccessIcon className="h-4 w-4" />
                                {accessInfo.label}
                              </span>
                            </td>
                            <td className="px-4 py-3">
                              {r.isPermanent ? (
                                <span className="text-gray-600">Permanent</span>
                              ) : (
                                <div className="text-xs">
                                  {r.validFrom && <p>From: {new Date(r.validFrom).toLocaleDateString()}</p>}
                                  {r.validUntil && <p>Until: {new Date(r.validUntil).toLocaleDateString()}</p>}
                                </div>
                              )}
                            </td>
                            <td className="px-4 py-3">
                              <span className={`px-2 py-1 rounded text-xs font-medium ${getStatusColor(r.status)}`}>
                                {getStatusText(r.status)}
                              </span>
                            </td>
                            <td className="px-4 py-3">
                              <div className="flex gap-2">
                                <Button variant="ghost" size="icon" onClick={() => handleEdit(r)}>
                                  <Edit className="h-4 w-4" />
                                </Button>
                                <Button variant="ghost" size="icon" onClick={() => handleDelete(r.restrictionId)}>
                                  <Trash2 className="h-4 w-4 text-red-600" />
                                </Button>
                              </div>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              )}

              {restrictions && restrictions.totalCount > 10 && (
                <div className="flex justify-between items-center mt-4">
                  <p className="text-sm text-gray-500">
                    Showing {(page - 1) * 10 + 1} to {Math.min(page * 10, restrictions.totalCount)} of{" "}
                    {restrictions.totalCount}
                  </p>
                  <div className="flex gap-2">
                    <Button variant="outline" size="sm" disabled={page === 1} onClick={() => setPage(page - 1)}>
                      Previous
                    </Button>
                    <Button variant="outline" size="sm" disabled={page * 10 >= restrictions.totalCount} onClick={() => setPage(page + 1)}>
                      Next
                    </Button>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </main>
      </div>
      <ImeiRestrictionFormModal
        open={modalOpen}
        restriction={editingRestriction}
        technicianId={technicianId}
        tags={tags}
        onClose={handleModalClose}
      />
    </AuthGuard>
  );
}

