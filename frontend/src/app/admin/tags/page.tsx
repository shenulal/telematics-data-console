"use client";

import { Header } from "@/components/layout/Header";
import { AuthGuard } from "@/components/layout/AuthGuard";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useEffect, useState } from "react";
import { tagApi } from "@/lib/api";
import { getStatusColor, getStatusText, USER_ROLES } from "@/lib/utils";
import { Tag, Plus, Search, Edit, Trash2, List } from "lucide-react";
import { TagFormModal } from "@/components/modals/TagFormModal";
import { TagItemsModal } from "@/components/modals/TagItemsModal";
import { ImportExportButtons } from "@/components/ui/ImportExportButtons";
import { useAuthStore } from "@/lib/store";

interface TagItem {
  tagId: number;
  tagName: string;
  description?: string;
  scope: number;
  scopeText?: string;
  color?: string;
  status: number;
  itemCount: number;
  createdAt: string;
}

interface PagedResult {
  items: TagItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export default function TagsPage() {
  const [tags, setTags] = useState<PagedResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [page, setPage] = useState(1);
  const [modalOpen, setModalOpen] = useState(false);
  const [editingTag, setEditingTag] = useState<TagItem | null>(null);
  const [itemsModalOpen, setItemsModalOpen] = useState(false);
  const [selectedTagForItems, setSelectedTagForItems] = useState<TagItem | null>(null);
  const { hasRole } = useAuthStore();
  const isSuperAdmin = hasRole(USER_ROLES.SUPERADMIN);

  // Check if user can edit/delete a tag (only SuperAdmin can edit/delete Global tags)
  const canEditTag = (tag: TagItem) => {
    if (isSuperAdmin) return true;
    return tag.scope !== 0; // 0 = Global scope
  };

  const fetchTags = async () => {
    setLoading(true);
    try {
      const response = await tagApi.getAll({ search: searchTerm, page, pageSize: 10 });
      setTags(response.data);
    } catch (error) {
      console.error("Failed to fetch tags:", error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTags();
  }, [page, searchTerm]);

  const handleAdd = () => {
    setEditingTag(null);
    setModalOpen(true);
  };

  const handleEdit = (tag: TagItem) => {
    setEditingTag(tag);
    setModalOpen(true);
  };

  const handleDelete = async (id: number) => {
    if (!confirm("Are you sure you want to delete this tag?")) return;
    try {
      await tagApi.delete(id);
      fetchTags();
    } catch (error) {
      console.error("Failed to delete tag:", error);
    }
  };

  const handleModalClose = (refresh?: boolean) => {
    setModalOpen(false);
    setEditingTag(null);
    if (refresh) fetchTags();
  };

  const handleManageItems = (tag: TagItem) => {
    setSelectedTagForItems(tag);
    setItemsModalOpen(true);
  };

  const handleItemsModalClose = (refresh?: boolean) => {
    setItemsModalOpen(false);
    setSelectedTagForItems(null);
    if (refresh) fetchTags();
  };

  return (
    <AuthGuard requiredRoles={["SUPERADMIN", "RESELLER ADMIN"]}>
      <div className="min-h-screen bg-gray-50">
        <Header />
        <main className="max-w-7xl mx-auto px-4 py-6 sm:px-6 lg:px-8">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle className="flex items-center gap-2">
                <Tag className="h-5 w-5" />
                Tags
              </CardTitle>
              <div className="flex items-center gap-2">
                <ImportExportButtons entityType="tags" onImportComplete={fetchTags} includeItems={true} />
                <Button size="sm" onClick={handleAdd}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add Tag
                </Button>
              </div>
            </CardHeader>
            <CardContent>
              <div className="mb-4 relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
                <Input
                  placeholder="Search tags..."
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
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Tag</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Description</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Scope</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Items</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Status</th>
                        <th className="px-4 py-3 text-left font-medium text-gray-500">Actions</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y">
                      {tags?.items.map((tag) => (
                        <tr key={tag.tagId} className="hover:bg-gray-50">
                          <td className="px-4 py-3">
                            <div className="flex items-center gap-2">
                              <span className="w-3 h-3 rounded-full" style={{ backgroundColor: tag.color || '#3B82F6' }}></span>
                              <span className="font-medium">{tag.tagName}</span>
                            </div>
                          </td>
                          <td className="px-4 py-3 text-gray-600">{tag.description || "-"}</td>
                          <td className="px-4 py-3">{tag.scopeText || "Global"}</td>
                          <td className="px-4 py-3">{tag.itemCount}</td>
                          <td className="px-4 py-3">
                            <span className={`px-2 py-1 rounded text-xs font-medium ${getStatusColor(tag.status)}`}>
                              {getStatusText(tag.status)}
                            </span>
                          </td>
                          <td className="px-4 py-3">
                            <div className="flex gap-2">
                              <Button variant="ghost" size="icon" onClick={() => handleManageItems(tag)} title="Manage Items">
                                <List className="h-4 w-4 text-blue-600" />
                              </Button>
                              {canEditTag(tag) ? (
                                <>
                                  <Button variant="ghost" size="icon" onClick={() => handleEdit(tag)} title="Edit Tag">
                                    <Edit className="h-4 w-4" />
                                  </Button>
                                  <Button variant="ghost" size="icon" onClick={() => handleDelete(tag.tagId)} title="Delete Tag">
                                    <Trash2 className="h-4 w-4 text-red-600" />
                                  </Button>
                                </>
                              ) : (
                                <span className="text-xs text-gray-400">Read only</span>
                              )}
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </CardContent>
          </Card>
        </main>
      </div>
      <TagFormModal open={modalOpen} tag={editingTag} onClose={handleModalClose} />
      <TagItemsModal open={itemsModalOpen} tag={selectedTagForItems} onClose={handleItemsModalClose} />
    </AuthGuard>
  );
}

