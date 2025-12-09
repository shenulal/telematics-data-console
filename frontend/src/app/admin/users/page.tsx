"use client";

import { Header } from "@/components/layout/Header";
import { AuthGuard } from "@/components/layout/AuthGuard";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useEffect, useState } from "react";
import { userApi } from "@/lib/api";
import { getStatusColor, getStatusText, formatDate, USER_STATUS } from "@/lib/utils";
import { Users, Plus, Search, Edit, Trash2, Lock, Clock } from "lucide-react";
import { UserFormModal } from "@/components/modals/UserFormModal";
import { ImportExportButtons } from "@/components/ui/ImportExportButtons";

interface User {
  userId: number;
  username: string;
  email: string;
  fullName?: string;
  resellerName?: string;
  status: number;
  lockoutUntil?: string;
  roles: string[];
  createdAt: string;
}

interface PagedResult {
  items: User[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export default function UsersPage() {
  const [users, setUsers] = useState<PagedResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [page, setPage] = useState(1);
  const [modalOpen, setModalOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<User | null>(null);

  const fetchUsers = async () => {
    setLoading(true);
    try {
      const response = await userApi.getAll({ search: searchTerm, page, pageSize: 10 });
      setUsers(response.data);
    } catch (error) {
      console.error("Failed to fetch users:", error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchUsers();
  }, [page, searchTerm]);

  const handleAdd = () => {
    setEditingUser(null);
    setModalOpen(true);
  };

  const handleEdit = (user: User) => {
    setEditingUser(user);
    setModalOpen(true);
  };

  const handleDelete = async (id: number) => {
    if (!confirm("Are you sure you want to delete this user?")) return;
    try {
      await userApi.delete(id);
      fetchUsers();
    } catch (error) {
      console.error("Failed to delete user:", error);
    }
  };

  const handleModalClose = (refresh?: boolean) => {
    setModalOpen(false);
    setEditingUser(null);
    if (refresh) fetchUsers();
  };

  // Get role badge color
  const getRoleBadgeColor = (role: string) => {
    switch (role) {
      case "SUPERADMIN":
        return "bg-purple-100 text-purple-800";
      case "RESELLER ADMIN":
        return "bg-indigo-100 text-indigo-800";
      case "TECHNICIAN":
        return "bg-blue-100 text-blue-800";
      case "SUPERVISOR":
        return "bg-teal-100 text-teal-800";
      default:
        return "bg-gray-100 text-gray-800";
    }
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
                Users
              </CardTitle>
              <div className="flex items-center gap-2">
                <ImportExportButtons entityType="users" onImportComplete={fetchUsers} />
                <Button size="sm" onClick={handleAdd}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add User
                </Button>
              </div>
            </CardHeader>
            <CardContent>
              <div className="mb-4 relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
                <Input
                  placeholder="Search users..."
                  className="pl-10"
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                />
              </div>

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
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Roles</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Status</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Actions</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y">
                      {users?.items.map((user) => (
                        <tr key={user.userId} className="hover:bg-gray-50">
                          <td className="px-4 py-3">
                            <div>
                              <p className="font-medium">{user.fullName || user.username}</p>
                              <p className="text-xs text-gray-500">@{user.username}</p>
                            </div>
                          </td>
                          <td className="px-4 py-3">{user.email}</td>
                          <td className="px-4 py-3">{user.resellerName || "-"}</td>
                          <td className="px-4 py-3">
                            <div className="flex flex-wrap gap-1">
                              {user.roles?.map((role) => (
                                <span key={role} className={`text-xs px-2 py-0.5 rounded ${getRoleBadgeColor(role)}`}>
                                  {role}
                                </span>
                              ))}
                            </div>
                          </td>
                          <td className="px-4 py-3">
                            <div className="flex flex-col gap-1">
                              <span className={`px-2 py-1 rounded text-xs font-medium inline-flex items-center gap-1 w-fit ${getStatusColor(user.status)}`}>
                                {user.status === USER_STATUS.LOCKED && <Lock className="h-3 w-3" />}
                                {getStatusText(user.status)}
                              </span>
                              {user.status === USER_STATUS.LOCKED && user.lockoutUntil && (
                                <span className="text-xs text-gray-500 flex items-center gap-1">
                                  <Clock className="h-3 w-3" />
                                  Until: {new Date(user.lockoutUntil).toLocaleDateString()}
                                </span>
                              )}
                            </div>
                          </td>
                          <td className="px-4 py-3">
                            <div className="flex gap-2">
                              <Button variant="ghost" size="icon" onClick={() => handleEdit(user)}>
                                <Edit className="h-4 w-4" />
                              </Button>
                              <Button variant="ghost" size="icon" onClick={() => handleDelete(user.userId)}>
                                <Trash2 className="h-4 w-4 text-red-600" />
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
              {users && users.totalCount > 10 && (
                <div className="flex justify-between items-center mt-4">
                  <p className="text-sm text-gray-500">
                    Showing {(page - 1) * 10 + 1} to {Math.min(page * 10, users.totalCount)} of{" "}
                    {users.totalCount}
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
                      disabled={page * 10 >= users.totalCount}
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
      <UserFormModal open={modalOpen} user={editingUser} onClose={handleModalClose} />
    </AuthGuard>
  );
}

