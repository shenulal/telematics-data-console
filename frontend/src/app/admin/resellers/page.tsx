"use client";

import { Header } from "@/components/layout/Header";
import { AuthGuard } from "@/components/layout/AuthGuard";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useEffect, useState } from "react";
import { resellerApi } from "@/lib/api";
import { getStatusColor, getStatusText } from "@/lib/utils";
import { Building2, Plus, Search, Edit, Users } from "lucide-react";
import { ResellerFormModal } from "@/components/modals/ResellerFormModal";
import { ImportExportButtons } from "@/components/ui/ImportExportButtons";

interface Reseller {
  resellerId: number;
  companyName: string;
  displayName?: string;
  contactPerson?: string;
  email?: string;
  city?: string;
  country?: string;
  status: number;
  technicianCount: number;
  createdAt: string;
}

interface PagedResult {
  items: Reseller[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export default function ResellersPage() {
  const [resellers, setResellers] = useState<PagedResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [page, setPage] = useState(1);
  const [modalOpen, setModalOpen] = useState(false);
  const [editingReseller, setEditingReseller] = useState<Reseller | null>(null);

  const fetchResellers = async () => {
    setLoading(true);
    try {
      const response = await resellerApi.getAll({
        searchTerm,
        page,
        pageSize: 10,
      });
      setResellers(response.data);
    } catch (error) {
      console.error("Failed to fetch resellers:", error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchResellers();
  }, [page, searchTerm]);

  const handleAdd = () => {
    setEditingReseller(null);
    setModalOpen(true);
  };

  const handleEdit = (reseller: Reseller) => {
    setEditingReseller(reseller);
    setModalOpen(true);
  };

  const handleModalClose = (refresh?: boolean) => {
    setModalOpen(false);
    setEditingReseller(null);
    if (refresh) fetchResellers();
  };

  return (
    <AuthGuard requiredRoles={["SUPERADMIN"]}>
      <div className="min-h-screen bg-gray-50">
        <Header />
        <main className="max-w-7xl mx-auto px-4 py-6 sm:px-6 lg:px-8">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle className="flex items-center gap-2">
                <Building2 className="h-5 w-5" />
                Resellers
              </CardTitle>
              <div className="flex items-center gap-2">
                <ImportExportButtons entityType="resellers" onImportComplete={fetchResellers} />
                <Button size="sm" onClick={handleAdd}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add Reseller
                </Button>
              </div>
            </CardHeader>
            <CardContent>
              {/* Search */}
              <div className="mb-4 relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
                <Input
                  placeholder="Search resellers..."
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
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Company</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Contact</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Location</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Technicians</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Status</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Actions</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y">
                      {resellers?.items.map((reseller) => (
                        <tr key={reseller.resellerId} className="hover:bg-gray-50">
                          <td className="px-4 py-3">
                            <div>
                              <p className="font-medium">{reseller.companyName}</p>
                              <p className="text-xs text-gray-500">{reseller.displayName}</p>
                            </div>
                          </td>
                          <td className="px-4 py-3">
                            <div>
                              <p>{reseller.contactPerson || "-"}</p>
                              <p className="text-xs text-gray-500">{reseller.email}</p>
                            </div>
                          </td>
                          <td className="px-4 py-3">
                            {reseller.city && reseller.country
                              ? `${reseller.city}, ${reseller.country}`
                              : "-"}
                          </td>
                          <td className="px-4 py-3">
                            <span className="flex items-center gap-1">
                              <Users className="h-4 w-4 text-gray-400" />
                              {reseller.technicianCount}
                            </span>
                          </td>
                          <td className="px-4 py-3">
                            <span className={`px-2 py-1 rounded text-xs font-medium ${getStatusColor(reseller.status)}`}>
                              {getStatusText(reseller.status)}
                            </span>
                          </td>
                          <td className="px-4 py-3">
                            <Button variant="ghost" size="icon" onClick={() => handleEdit(reseller)}>
                              <Edit className="h-4 w-4" />
                            </Button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}

              {/* Pagination */}
              {resellers && resellers.totalCount > 10 && (
                <div className="flex justify-between items-center mt-4">
                  <p className="text-sm text-gray-500">
                    Showing {(page - 1) * 10 + 1} to {Math.min(page * 10, resellers.totalCount)} of{" "}
                    {resellers.totalCount}
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
                      disabled={page * 10 >= resellers.totalCount}
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
      <ResellerFormModal open={modalOpen} reseller={editingReseller} onClose={handleModalClose} />
    </AuthGuard>
  );
}

