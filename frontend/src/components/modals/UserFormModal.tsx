"use client";

import { useState, useEffect } from "react";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { userApi, resellerApi } from "@/lib/api";
import { useAuthStore } from "@/lib/store";
import { USER_ROLES, USER_STATUS } from "@/lib/utils";

// Role options with ENUM values
const ROLE_OPTIONS = [
  { value: USER_ROLES.SUPERADMIN, label: "SUPERADMIN" },
  { value: USER_ROLES.RESELLER_ADMIN, label: "RESELLER ADMIN" },
  { value: USER_ROLES.TECHNICIAN, label: "TECHNICIAN" },
  { value: USER_ROLES.SUPERVISOR, label: "SUPERVISOR" },
];

// Status options with ENUM values
const STATUS_OPTIONS = [
  { value: USER_STATUS.ACTIVE.toString(), label: "ACTIVE" },
  { value: USER_STATUS.INACTIVE.toString(), label: "INACTIVE" },
  { value: USER_STATUS.SUSPENDED.toString(), label: "SUSPENDED" },
  { value: USER_STATUS.LOCKED.toString(), label: "LOCKED" },
];

interface User {
  userId: number;
  username: string;
  email: string;
  fullName?: string;
  mobile?: string;
  resellerId?: number;
  status: number;
  lockoutUntil?: string;
  roles: string[];
}

interface Reseller {
  resellerId: number;
  companyName: string;
}

interface Props {
  open: boolean;
  user: User | null;
  onClose: (refresh?: boolean) => void;
}

interface FormErrors {
  username?: string;
  email?: string;
  password?: string;
  confirmPassword?: string;
  fullName?: string;
  mobile?: string;
  roles?: string;
  resellerId?: string;
  lockoutUntil?: string;
}

export function UserFormModal({ open, user, onClose }: Props) {
  const { user: currentUser } = useAuthStore();
  const isSuperAdmin = currentUser?.roles?.includes(USER_ROLES.SUPERADMIN);
  const isResellerAdmin = currentUser?.roles?.includes(USER_ROLES.RESELLER_ADMIN);

  const [loading, setLoading] = useState(false);
  const [resellers, setResellers] = useState<Reseller[]>([]);
  const [errors, setErrors] = useState<FormErrors>({});
  const [formData, setFormData] = useState({
    username: "",
    email: "",
    password: "",
    confirmPassword: "",
    fullName: "",
    mobile: "",
    resellerId: "",
    roles: [] as string[],
    status: USER_STATUS.ACTIVE.toString(),
    lockoutUntil: "",
  });

  useEffect(() => {
    if (open) {
      setErrors({});
      fetchResellers();
      if (user) {
        setFormData({
          username: user.username,
          email: user.email,
          password: "",
          confirmPassword: "",
          fullName: user.fullName || "",
          mobile: user.mobile || "",
          resellerId: user.resellerId?.toString() || "",
          roles: user.roles || [],
          status: user.status?.toString() || USER_STATUS.ACTIVE.toString(),
          lockoutUntil: user.lockoutUntil ? user.lockoutUntil.split("T")[0] : "",
        });
      } else {
        setFormData({
          username: "", email: "", password: "", confirmPassword: "", fullName: "", mobile: "",
          resellerId: isResellerAdmin ? currentUser?.resellerId?.toString() || "" : "",
          roles: [], status: USER_STATUS.ACTIVE.toString(), lockoutUntil: "",
        });
      }
    }
  }, [open, user, isResellerAdmin, currentUser]);

  const fetchResellers = async () => {
    try {
      const response = await resellerApi.getAll({ pageSize: 100 });
      setResellers(response.data.items || response.data || []);
    } catch (error) {
      console.error("Failed to fetch resellers:", error);
    }
  };

  // Get available roles based on current user's role
  const getAvailableRoles = () => {
    if (isSuperAdmin) {
      return ROLE_OPTIONS;
    } else if (isResellerAdmin) {
      // Reseller Admin can only create Technician and Supervisor
      return ROLE_OPTIONS.filter(r => r.value === USER_ROLES.TECHNICIAN || r.value === USER_ROLES.SUPERVISOR);
    }
    return [];
  };

  const validateForm = (): boolean => {
    const newErrors: FormErrors = {};

    // Username validation
    if (!formData.username.trim()) {
      newErrors.username = "Username is required";
    } else if (formData.username.length < 3) {
      newErrors.username = "Username must be at least 3 characters";
    } else if (formData.username.length > 50) {
      newErrors.username = "Username must not exceed 50 characters";
    } else if (!/^[a-zA-Z0-9_]+$/.test(formData.username)) {
      newErrors.username = "Username can only contain letters, numbers, and underscores";
    }

    // Email validation
    if (!formData.email.trim()) {
      newErrors.email = "Email is required";
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      newErrors.email = "Please enter a valid email address";
    }

    // Password validation (only for new users)
    if (!user) {
      if (!formData.password) {
        newErrors.password = "Password is required";
      } else if (formData.password.length < 8) {
        newErrors.password = "Password must be at least 8 characters";
      } else if (!/(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/.test(formData.password)) {
        newErrors.password = "Password must contain uppercase, lowercase, and number";
      }
      if (formData.password !== formData.confirmPassword) {
        newErrors.confirmPassword = "Passwords do not match";
      }
    }

    // Full name validation
    if (formData.fullName && formData.fullName.length > 100) {
      newErrors.fullName = "Full name must not exceed 100 characters";
    }

    // Mobile validation
    if (formData.mobile) {
      if (!/^[\d\s\-+()]{7,20}$/.test(formData.mobile)) {
        newErrors.mobile = "Please enter a valid mobile number";
      }
    }

    // Role validation
    if (formData.roles.length === 0) {
      newErrors.roles = "At least one role must be selected";
    }

    // Reseller validation - required for Reseller Admin, Technician, or Supervisor roles
    // But not required if only SuperAdmin role is selected
    const requiresResellerForValidation = formData.roles.some(r =>
      r === USER_ROLES.RESELLER_ADMIN || r === USER_ROLES.TECHNICIAN || r === USER_ROLES.SUPERVISOR
    );
    const hasSuperAdminOnlyForValidation = formData.roles.length === 1 && formData.roles.includes(USER_ROLES.SUPERADMIN);

    if (requiresResellerForValidation && !hasSuperAdminOnlyForValidation && !formData.resellerId) {
      newErrors.resellerId = "Reseller is required for Reseller Admin, Technician, or Supervisor roles";
    }

    // Lockout Until validation when status is LOCKED
    if (formData.status === USER_STATUS.LOCKED.toString()) {
      if (!formData.lockoutUntil) {
        newErrors.lockoutUntil = "Lockout Until date is required when status is LOCKED";
      } else {
        const lockoutDate = new Date(formData.lockoutUntil);
        if (lockoutDate <= new Date()) {
          newErrors.lockoutUntil = "Lockout Until date must be in the future";
        }
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) return;

    setLoading(true);
    try {
      const payload = {
        username: formData.username,
        email: formData.email,
        password: formData.password || undefined,
        fullName: formData.fullName || undefined,
        mobile: formData.mobile || undefined,
        resellerId: formData.resellerId ? parseInt(formData.resellerId) : undefined,
        roles: formData.roles,
        status: parseInt(formData.status),
        lockoutUntil: formData.status === USER_STATUS.LOCKED.toString() ? formData.lockoutUntil : undefined,
      };

      if (user) {
        await userApi.update(user.userId, payload);
      } else {
        await userApi.create(payload);
      }
      onClose(true);
    } catch (error) {
      console.error("Failed to save user:", error);
    } finally {
      setLoading(false);
    }
  };

  const handleRoleChange = (role: string, checked: boolean) => {
    setFormData(prev => ({
      ...prev,
      roles: checked ? [...prev.roles, role] : prev.roles.filter(r => r !== role),
    }));
  };

  const availableRoles = getAvailableRoles();
  // Show Reseller field only when Reseller Admin, Supervisor, or Technician roles are selected (not for SuperAdmin only)
  const requiresReseller = formData.roles.some(r =>
    r === USER_ROLES.RESELLER_ADMIN || r === USER_ROLES.TECHNICIAN || r === USER_ROLES.SUPERVISOR
  );
  // SuperAdmin alone doesn't need reseller, but if combined with other roles, it might
  const hasSuperAdminOnly = formData.roles.length === 1 && formData.roles.includes(USER_ROLES.SUPERADMIN);
  const showResellerField = requiresReseller && !hasSuperAdminOnly;

  return (
    <Dialog open={open} onOpenChange={() => onClose()}>
      <DialogContent className="max-w-2xl" onClose={() => onClose()}>
        <DialogHeader>
          <DialogTitle>{user ? "Edit User" : "Add User"}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit}>
          <div className="px-6 py-4 space-y-4 max-h-[65vh] overflow-y-auto">
            {/* Username & Email */}
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label htmlFor="username">Username *</Label>
                <Input
                  id="username"
                  value={formData.username}
                  onChange={(e) => setFormData({ ...formData, username: e.target.value })}
                  className={errors.username ? "border-red-500" : ""}
                  disabled={!!user}
                  maxLength={50}
                />
                {errors.username && <p className="text-red-500 text-xs mt-1">{errors.username}</p>}
              </div>
              <div>
                <Label htmlFor="email">Email *</Label>
                <Input
                  id="email"
                  type="email"
                  value={formData.email}
                  onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                  className={errors.email ? "border-red-500" : ""}
                  disabled={!!user}
                  maxLength={100}
                />
                {errors.email && <p className="text-red-500 text-xs mt-1">{errors.email}</p>}
              </div>
            </div>

            {/* Password fields - only for new users */}
            {!user && (
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label htmlFor="password">Password *</Label>
                  <Input
                    id="password"
                    type="password"
                    value={formData.password}
                    onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                    className={errors.password ? "border-red-500" : ""}
                  />
                  {errors.password && <p className="text-red-500 text-xs mt-1">{errors.password}</p>}
                </div>
                <div>
                  <Label htmlFor="confirmPassword">Confirm Password *</Label>
                  <Input
                    id="confirmPassword"
                    type="password"
                    value={formData.confirmPassword}
                    onChange={(e) => setFormData({ ...formData, confirmPassword: e.target.value })}
                    className={errors.confirmPassword ? "border-red-500" : ""}
                  />
                  {errors.confirmPassword && <p className="text-red-500 text-xs mt-1">{errors.confirmPassword}</p>}
                </div>
              </div>
            )}

            {/* Full Name & Mobile */}
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label htmlFor="fullName">Full Name</Label>
                <Input
                  id="fullName"
                  value={formData.fullName}
                  onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
                  className={errors.fullName ? "border-red-500" : ""}
                  maxLength={100}
                />
                {errors.fullName && <p className="text-red-500 text-xs mt-1">{errors.fullName}</p>}
              </div>
              <div>
                <Label htmlFor="mobile">Mobile</Label>
                <Input
                  id="mobile"
                  value={formData.mobile}
                  onChange={(e) => setFormData({ ...formData, mobile: e.target.value })}
                  className={errors.mobile ? "border-red-500" : ""}
                  placeholder="+1 234 567 8900"
                  maxLength={20}
                />
                {errors.mobile && <p className="text-red-500 text-xs mt-1">{errors.mobile}</p>}
              </div>
            </div>

            {/* Roles selection */}
            <div>
              <Label>Role(s) *</Label>
              <div className="flex flex-wrap gap-3 mt-2 p-3 bg-gray-50 rounded-md">
                {availableRoles.map((role) => (
                  <label key={role.value} className="flex items-center gap-2 text-sm cursor-pointer">
                    <input
                      type="checkbox"
                      checked={formData.roles.includes(role.value)}
                      onChange={(e) => handleRoleChange(role.value, e.target.checked)}
                      className="rounded border-gray-300"
                    />
                    <span className="font-medium">{role.label}</span>
                  </label>
                ))}
              </div>
              {errors.roles && <p className="text-red-500 text-xs mt-1">{errors.roles}</p>}
              {formData.roles.includes(USER_ROLES.SUPERADMIN) && (
                <p className="text-green-600 text-xs mt-1">
                  ✓ Super Administrator has full access to all features and permissions
                </p>
              )}
              {formData.roles.includes(USER_ROLES.TECHNICIAN) && !formData.roles.includes(USER_ROLES.SUPERADMIN) && (
                <p className="text-blue-600 text-xs mt-1">
                  ℹ️ Users with TECHNICIAN role will appear in the Technicians menu with additional details
                </p>
              )}
            </div>

            {/* Reseller selection - only shown for Reseller Admin, Supervisor, or Technician roles */}
            {showResellerField && (
              <div>
                <Label htmlFor="resellerId">Reseller *</Label>
                <Select
                  id="resellerId"
                  value={formData.resellerId}
                  onChange={(e) => setFormData({ ...formData, resellerId: e.target.value })}
                  className={errors.resellerId ? "border-red-500" : ""}
                  disabled={isResellerAdmin}
                >
                  <option value="">Select Reseller</option>
                  {resellers.map((r) => (
                    <option key={r.resellerId} value={r.resellerId}>{r.companyName}</option>
                  ))}
                </Select>
                {errors.resellerId && <p className="text-red-500 text-xs mt-1">{errors.resellerId}</p>}
              </div>
            )}

            {/* Status & Lockout Until - only shown when editing */}
            {user && (
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label htmlFor="status">Status *</Label>
                  <Select
                    id="status"
                    value={formData.status}
                    onChange={(e) => setFormData({ ...formData, status: e.target.value })}
                  >
                    {STATUS_OPTIONS.map((opt) => (
                      <option key={opt.value} value={opt.value}>{opt.label}</option>
                    ))}
                  </Select>
                </div>
                {formData.status === USER_STATUS.LOCKED.toString() && (
                  <div>
                    <Label htmlFor="lockoutUntil">Lockout Until *</Label>
                    <Input
                      id="lockoutUntil"
                      type="date"
                      value={formData.lockoutUntil}
                      onChange={(e) => setFormData({ ...formData, lockoutUntil: e.target.value })}
                      className={errors.lockoutUntil ? "border-red-500" : ""}
                      min={new Date().toISOString().split("T")[0]}
                    />
                    {errors.lockoutUntil && <p className="text-red-500 text-xs mt-1">{errors.lockoutUntil}</p>}
                    <p className="text-gray-500 text-xs mt-1">User will be automatically unlocked after this date</p>
                  </div>
                )}
              </div>
            )}
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

