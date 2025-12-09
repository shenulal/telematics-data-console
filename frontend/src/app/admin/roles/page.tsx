"use client";

import { Header } from "@/components/layout/Header";
import { AuthGuard } from "@/components/layout/AuthGuard";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { useEffect, useState } from "react";
import { roleApi } from "@/lib/api";
import { Shield, Plus, Edit, Trash2 } from "lucide-react";
import { RoleFormModal } from "@/components/modals/RoleFormModal";
import { ImportExportButtons } from "@/components/ui/ImportExportButtons";

interface Permission {
  permissionId: number;
  permissionName: string;
  description?: string;
  module?: string;
}

interface Role {
  roleId: number;
  roleName: string;
  description?: string;
  isSystemRole: boolean;
  permissions: Permission[];
  createdAt: string;
}

export default function RolesPage() {
  const [roles, setRoles] = useState<Role[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editingRole, setEditingRole] = useState<Role | null>(null);

  const fetchRoles = async () => {
    setLoading(true);
    try {
      const response = await roleApi.getAll();
      setRoles(response.data.value || response.data);
    } catch (error) {
      console.error("Failed to fetch roles:", error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRoles();
  }, []);

  const handleAdd = () => {
    setEditingRole(null);
    setModalOpen(true);
  };

  const handleEdit = (role: Role) => {
    setEditingRole(role);
    setModalOpen(true);
  };

  const handleDelete = async (id: number) => {
    if (!confirm("Are you sure you want to delete this role?")) return;
    try {
      await roleApi.delete(id);
      fetchRoles();
    } catch (error) {
      console.error("Failed to delete role:", error);
    }
  };

  const handleModalClose = (refresh?: boolean) => {
    setModalOpen(false);
    setEditingRole(null);
    if (refresh) fetchRoles();
  };

  return (
    <AuthGuard requiredRoles={["SUPERADMIN", "RESELLER ADMIN"]}>
      <div className="min-h-screen bg-gray-50">
        <Header />
        <main className="max-w-7xl mx-auto px-4 py-6 sm:px-6 lg:px-8">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle className="flex items-center gap-2">
                <Shield className="h-5 w-5" />
                Roles & Permissions
              </CardTitle>
              <div className="flex items-center gap-2">
                <ImportExportButtons entityType="roles" onImportComplete={fetchRoles} />
                <Button size="sm" onClick={handleAdd}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add Role
                </Button>
              </div>
            </CardHeader>
            <CardContent>
              {loading ? (
                <div className="flex justify-center py-8">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                </div>
              ) : (
                <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                  {roles.map((role) => (
                    <Card key={role.roleId} className="border">
                      <CardHeader className="pb-2">
                        <div className="flex justify-between items-start">
                          <div>
                            <CardTitle className="text-lg">{role.roleName}</CardTitle>
                            {role.isSystemRole && (
                              <span className="text-xs text-gray-500">System Role</span>
                            )}
                          </div>
                          {!role.isSystemRole && (
                            <div className="flex gap-1">
                              <Button variant="ghost" size="icon" onClick={() => handleEdit(role)}>
                                <Edit className="h-4 w-4" />
                              </Button>
                              <Button variant="ghost" size="icon" onClick={() => handleDelete(role.roleId)}>
                                <Trash2 className="h-4 w-4 text-red-600" />
                              </Button>
                            </div>
                          )}
                        </div>
                      </CardHeader>
                      <CardContent>
                        <p className="text-sm text-gray-600 mb-3">{role.description || "No description"}</p>
                        <div className="flex flex-wrap gap-1">
                          {role.permissions?.slice(0, 5).map((perm) => (
                            <span key={perm.permissionId} className="text-xs bg-green-100 text-green-800 px-2 py-0.5 rounded">
                              {perm.description || perm.permissionName}
                            </span>
                          ))}
                          {role.permissions?.length > 5 && (
                            <span className="text-xs text-gray-500">+{role.permissions.length - 5} more</span>
                          )}
                        </div>
                      </CardContent>
                    </Card>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </main>
      </div>
      <RoleFormModal open={modalOpen} role={editingRole} onClose={handleModalClose} />
    </AuthGuard>
  );
}

