"use client";

import { useState, useEffect } from "react";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { roleApi, permissionApi } from "@/lib/api";
import { useAuthStore } from "@/lib/store";

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
}

interface Props {
  open: boolean;
  role: Role | null;
  onClose: (refresh?: boolean) => void;
}

interface FormErrors {
  roleName?: string;
  description?: string;
}

export function RoleFormModal({ open, role, onClose }: Props) {
  const [loading, setLoading] = useState(false);
  const [permissions, setPermissions] = useState<Permission[]>([]);
  const [userPermissionIds, setUserPermissionIds] = useState<Set<number>>(new Set());
  const [errors, setErrors] = useState<FormErrors>({});
  const { hasRole } = useAuthStore();
  const isSuperAdmin = hasRole("SUPERADMIN");
  const [formData, setFormData] = useState({
    roleName: "",
    description: "",
    permissionIds: [] as number[],
  });

  useEffect(() => {
    if (open) {
      setErrors({});
      fetchPermissions();
      if (role) {
        setFormData({
          roleName: role.roleName,
          description: role.description || "",
          permissionIds: role.permissions?.map(p => p.permissionId) || [],
        });
      } else {
        setFormData({ roleName: "", description: "", permissionIds: [] });
      }
    }
  }, [open, role]);

  const fetchPermissions = async () => {
    try {
      if (isSuperAdmin) {
        // SuperAdmin: fetch all permissions
        const response = await permissionApi.getAll();
        const allPermissions = response.data.value || response.data || [];
        setPermissions(allPermissions);
        setUserPermissionIds(new Set(allPermissions.map((p: Permission) => p.permissionId)));
      } else {
        // Non-super admins: fetch only their own permissions
        const myPermsResponse = await roleApi.getMyPermissions();
        const myPerms = myPermsResponse.data.value || myPermsResponse.data || [];
        const myPermIds = new Set<number>(myPerms.map((p: Permission) => p.permissionId));
        setUserPermissionIds(myPermIds);
        setPermissions(myPerms);
      }
    } catch (error) {
      console.error("Failed to fetch permissions:", error);
    }
  };

  const validateForm = (): boolean => {
    const newErrors: FormErrors = {};

    // Role name validation
    if (!formData.roleName.trim()) {
      newErrors.roleName = "Role name is required";
    } else if (formData.roleName.length < 2) {
      newErrors.roleName = "Role name must be at least 2 characters";
    } else if (formData.roleName.length > 50) {
      newErrors.roleName = "Role name must not exceed 50 characters";
    } else if (!/^[a-zA-Z0-9\s_-]+$/.test(formData.roleName)) {
      newErrors.roleName = "Role name can only contain letters, numbers, spaces, hyphens and underscores";
    }

    // Description validation
    if (formData.description && formData.description.length > 200) {
      newErrors.description = "Description must not exceed 200 characters";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) return;

    setLoading(true);
    try {
      if (role) {
        await roleApi.update(role.roleId, { roleName: formData.roleName, description: formData.description });
        await roleApi.assignPermissions(role.roleId, formData.permissionIds);
      } else {
        const response = await roleApi.create({ roleName: formData.roleName, description: formData.description });
        const newRoleId = response.data.roleId;
        if (formData.permissionIds.length > 0) {
          await roleApi.assignPermissions(newRoleId, formData.permissionIds);
        }
      }
      onClose(true);
    } catch (error) {
      console.error("Failed to save role:", error);
    } finally {
      setLoading(false);
    }
  };

  const handlePermissionChange = (permId: number, checked: boolean) => {
    setFormData(prev => ({
      ...prev,
      permissionIds: checked ? [...prev.permissionIds, permId] : prev.permissionIds.filter(id => id !== permId),
    }));
  };

  const groupedPermissions = permissions.reduce((acc, perm) => {
    const module = perm.module || "General";
    if (!acc[module]) acc[module] = [];
    acc[module].push(perm);
    return acc;
  }, {} as Record<string, Permission[]>);

  return (
    <Dialog open={open} onOpenChange={() => onClose()}>
      <DialogContent className="max-w-2xl" onClose={() => onClose()}>
        <DialogHeader>
          <DialogTitle>{role ? "Edit Role" : "Add Role"}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit}>
          <div className="px-6 py-4 space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label htmlFor="roleName">Role Name *</Label>
                <Input
                  id="roleName"
                  value={formData.roleName}
                  onChange={(e) => setFormData({ ...formData, roleName: e.target.value })}
                  className={errors.roleName ? "border-red-500" : ""}
                  maxLength={50}
                />
                {errors.roleName && <p className="text-red-500 text-xs mt-1">{errors.roleName}</p>}
              </div>
              <div>
                <Label htmlFor="description">Description</Label>
                <Input
                  id="description"
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  className={errors.description ? "border-red-500" : ""}
                  maxLength={200}
                />
                {errors.description && <p className="text-red-500 text-xs mt-1">{errors.description}</p>}
              </div>
            </div>
            <div>
              <Label>Permissions</Label>
              <div className="mt-2 max-h-60 overflow-y-auto border rounded-md p-3 space-y-4">
                {Object.entries(groupedPermissions).map(([module, perms]) => (
                  <div key={module}>
                    <h4 className="font-medium text-sm text-gray-700 mb-2">{module}</h4>
                    <div className="grid grid-cols-2 gap-2">
                      {perms.map((perm) => (
                        <label key={perm.permissionId} className="flex items-center gap-2 text-sm">
                          <input type="checkbox" checked={formData.permissionIds.includes(perm.permissionId)} onChange={(e) => handlePermissionChange(perm.permissionId, e.target.checked)} />
                          {perm.description}
                        </label>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onClose()}>Cancel</Button>
            <Button type="submit" disabled={loading}>{loading ? "Saving..." : "Save"}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

