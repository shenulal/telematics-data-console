"use client";

import { useState, useEffect } from "react";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { tagApi } from "@/lib/api";
import { useAuthStore } from "@/lib/store";
import { USER_ROLES } from "@/lib/utils";

interface TagItem {
  tagId: number;
  tagName: string;
  description?: string;
  scope: number;
  color?: string;
  status: number;
}

interface Props {
  open: boolean;
  tag: TagItem | null;
  onClose: (refresh?: boolean) => void;
}

interface FormErrors {
  tagName?: string;
  description?: string;
  color?: string;
}

export function TagFormModal({ open, tag, onClose }: Props) {
  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState<FormErrors>({});
  const { user, hasRole } = useAuthStore();

  // Determine available scopes based on user role
  const isSuperAdmin = hasRole(USER_ROLES.SUPERADMIN);
  const isResellerAdmin = hasRole(USER_ROLES.RESELLER_ADMIN);

  // Get default scope based on role
  const getDefaultScope = () => {
    if (isSuperAdmin) return "0"; // Global
    if (isResellerAdmin) return "1"; // Reseller
    return "2"; // User
  };

  const [formData, setFormData] = useState({
    tagName: "",
    description: "",
    scope: getDefaultScope(),
    color: "#3B82F6",
  });

  useEffect(() => {
    if (open) {
      setErrors({});
      if (tag) {
        setFormData({
          tagName: tag.tagName,
          description: tag.description || "",
          scope: tag.scope.toString(),
          color: tag.color || "#3B82F6",
        });
      } else {
        setFormData({ tagName: "", description: "", scope: getDefaultScope(), color: "#3B82F6" });
      }
    }
  }, [open, tag]);

  const validateForm = (): boolean => {
    const newErrors: FormErrors = {};

    // Tag name validation
    if (!formData.tagName.trim()) {
      newErrors.tagName = "Tag name is required";
    } else if (formData.tagName.length < 2) {
      newErrors.tagName = "Tag name must be at least 2 characters";
    } else if (formData.tagName.length > 50) {
      newErrors.tagName = "Tag name must not exceed 50 characters";
    }

    // Description validation
    if (formData.description && formData.description.length > 200) {
      newErrors.description = "Description must not exceed 200 characters";
    }

    // Color validation
    if (formData.color && !/^#[0-9A-Fa-f]{6}$/.test(formData.color)) {
      newErrors.color = "Please enter a valid hex color (e.g., #3B82F6)";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) return;

    setLoading(true);
    try {
      const data = {
        tagName: formData.tagName,
        description: formData.description,
        scope: parseInt(formData.scope),
        color: formData.color,
      };
      if (tag) {
        await tagApi.update(tag.tagId, data);
      } else {
        await tagApi.create(data);
      }
      onClose(true);
    } catch (error) {
      console.error("Failed to save tag:", error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={() => onClose()}>
      <DialogContent onClose={() => onClose()}>
        <DialogHeader>
          <DialogTitle>{tag ? "Edit Tag" : "Add Tag"}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit}>
          <div className="px-6 py-4 space-y-4">
            <div>
              <Label htmlFor="tagName">Tag Name *</Label>
              <Input
                id="tagName"
                value={formData.tagName}
                onChange={(e) => setFormData({ ...formData, tagName: e.target.value })}
                className={errors.tagName ? "border-red-500" : ""}
                maxLength={50}
              />
              {errors.tagName && <p className="text-red-500 text-xs mt-1">{errors.tagName}</p>}
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
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label htmlFor="scope">Scope *</Label>
                <Select
                  id="scope"
                  value={formData.scope}
                  onChange={(e) => setFormData({ ...formData, scope: e.target.value })}
                  disabled={!isSuperAdmin && !isResellerAdmin}
                >
                  {isSuperAdmin && <option value="0">Global</option>}
                  {(isSuperAdmin || isResellerAdmin) && <option value="1">Reseller</option>}
                  <option value="2">User</option>
                </Select>
                {!isSuperAdmin && !isResellerAdmin && (
                  <p className="text-xs text-gray-500 mt-1">Tags are limited to User scope</p>
                )}
                {isResellerAdmin && !isSuperAdmin && (
                  <p className="text-xs text-gray-500 mt-1">Reseller Admin: Reseller or User scope only</p>
                )}
              </div>
              <div>
                <Label htmlFor="color">Color</Label>
                <div className="flex gap-2">
                  <Input
                    id="color"
                    type="color"
                    value={formData.color}
                    onChange={(e) => setFormData({ ...formData, color: e.target.value })}
                    className="w-12 h-10 p-1"
                  />
                  <Input
                    value={formData.color}
                    onChange={(e) => setFormData({ ...formData, color: e.target.value })}
                    className={`flex-1 ${errors.color ? "border-red-500" : ""}`}
                    maxLength={7}
                  />
                </div>
                {errors.color && <p className="text-red-500 text-xs mt-1">{errors.color}</p>}
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

