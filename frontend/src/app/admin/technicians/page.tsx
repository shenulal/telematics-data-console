"use client";

import { Header } from "@/components/layout/Header";
import { AuthGuard } from "@/components/layout/AuthGuard";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useEffect, useState } from "react";
import { technicianApi } from "@/lib/api";
import { formatDate, getStatusColor, getStatusText } from "@/lib/utils";
import { Users, Plus, Search, Edit, UserX, UserCheck, Shield } from "lucide-react";
import { useRouter } from "next/navigation";
import { TechnicianFormModal } from "@/components/modals/TechnicianFormModal";
import { ImportExportButtons } from "@/components/ui/ImportExportButtons";

interface Technician {
  technicianId: number;
  userId: number;
  username: string;
  email: string;
  fullName?: string;
  resellerName?: string;
  employeeCode?: string;
  workRegion?: string;
  status: number;
  createdAt: string;
  imeiRestrictionCount?: number;
  imeiRestrictionMode?: number;
}

interface PagedResult {
  items: Technician[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export default function TechniciansPage() {
  const router = useRouter();
  const [technicians, setTechnicians] = useState<PagedResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [page, setPage] = useState(1);
  const [modalOpen, setModalOpen] = useState(false);
  const [editingTechnician, setEditingTechnician] = useState<Technician | null>(null);

  const fetchTechnicians = async () => {
    setLoading(true);
    try {
      const response = await technicianApi.getAll({
        searchTerm,
        page,
        pageSize: 10,
      });
      setTechnicians(response.data);
    } catch (error) {
      console.error("Failed to fetch technicians:", error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTechnicians();
  }, [page, searchTerm]);

  const handleStatusToggle = async (id: number, currentStatus: number) => {
    try {
      if (currentStatus === 1) {
        await technicianApi.deactivate(id);
      } else {
        await technicianApi.activate(id);
      }
      fetchTechnicians();
    } catch (error) {
      console.error("Failed to update status:", error);
    }
  };

  const handleAdd = () => {
    setEditingTechnician(null);
    setModalOpen(true);
  };

  const handleEdit = (technician: Technician) => {
    setEditingTechnician(technician);
    setModalOpen(true);
  };

  const handleModalClose = (refresh?: boolean) => {
    setModalOpen(false);
    setEditingTechnician(null);
    if (refresh) fetchTechnicians();
  };

  return (
    <AuthGuard requiredRoles={["SUPERADMIN", "RESELLER ADMIN"]}>
      <div className="min-h-screen bg-gray-50">
        <Header />
        <main className="max-w-7xl mx-auto px-4 py-6 sm:px-6 lg:px-8">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle className="flex items-center gap-2">
                <Users className="h-5 w-5" />
                Technicians
              </CardTitle>
              <div className="flex items-center gap-2">
                <ImportExportButtons entityType="technicians" onImportComplete={fetchTechnicians} />
                <Button size="sm" onClick={handleAdd}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add Technician
                </Button>
              </div>
            </CardHeader>
            <CardContent>
              {/* Search */}
              <div className="mb-4 relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
                <Input
                  placeholder="Search technicians..."
                  className="pl-10"
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                />
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
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Name</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Email</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Reseller</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Region</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">IMEI Restrictions</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Status</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Actions</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y">
                      {technicians?.items.map((tech) => (
                        <tr key={tech.technicianId} className="hover:bg-gray-50">
                          <td className="px-4 py-3">
                            <div>
                              <p className="font-medium">{tech.fullName || tech.username}</p>
                              <p className="text-xs text-gray-500">{tech.employeeCode}</p>
                            </div>
                          </td>
                          <td className="px-4 py-3">{tech.email}</td>
                          <td className="px-4 py-3">{tech.resellerName || "-"}</td>
                          <td className="px-4 py-3">{tech.workRegion || "-"}</td>
                          <td className="px-4 py-3">
                            {tech.imeiRestrictionCount ? (
                              <span
                                className={`px-2 py-1 rounded text-xs font-medium cursor-pointer ${
                                  tech.imeiRestrictionMode === 1
                                    ? "bg-green-100 text-green-700"
                                    : "bg-red-100 text-red-700"
                                }`}
                                onClick={() => router.push(`/admin/technicians/${tech.technicianId}/restrictions`)}
                                title={tech.imeiRestrictionMode === 1 ? "Allow List" : "Deny List"}
                              >
                                {tech.imeiRestrictionCount} {tech.imeiRestrictionMode === 1 ? "Allow" : "Deny"}
                              </span>
                            ) : (
                              <span className="text-gray-400">-</span>
                            )}
                          </td>
                          <td className="px-4 py-3">
                            <span className={`px-2 py-1 rounded text-xs font-medium ${getStatusColor(tech.status)}`}>
                              {getStatusText(tech.status)}
                            </span>
                          </td>
                          <td className="px-4 py-3">
                            <div className="flex gap-2">
                              <Button variant="ghost" size="icon" onClick={() => handleEdit(tech)} title="Edit">
                                <Edit className="h-4 w-4" />
                              </Button>
                              <Button
                                variant="ghost"
                                size="icon"
                                onClick={() => router.push(`/admin/technicians/${tech.technicianId}/restrictions`)}
                                title="IMEI Restrictions"
                              >
                                <Shield className="h-4 w-4 text-purple-600" />
                              </Button>
                              <Button
                                variant="ghost"
                                size="icon"
                                onClick={() => handleStatusToggle(tech.technicianId, tech.status)}
                                title={tech.status === 1 ? "Deactivate" : "Activate"}
                              >
                                {tech.status === 1 ? (
                                  <UserX className="h-4 w-4 text-red-600" />
                                ) : (
                                  <UserCheck className="h-4 w-4 text-green-600" />
                                )}
                              </Button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}

              {/* Pagination */}
              {technicians && technicians.totalCount > 10 && (
                <div className="flex justify-between items-center mt-4">
                  <p className="text-sm text-gray-500">
                    Showing {(page - 1) * 10 + 1} to {Math.min(page * 10, technicians.totalCount)} of{" "}
                    {technicians.totalCount}
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
                      disabled={page * 10 >= technicians.totalCount}
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
      <TechnicianFormModal open={modalOpen} technician={editingTechnician} onClose={handleModalClose} />
    </AuthGuard>
  );
}

