"use client";

import { useState, useEffect } from "react";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { technicianApi, resellerApi } from "@/lib/api";

// Status options
const STATUS_OPTIONS = [
  { value: "0", label: "INACTIVE" },
  { value: "1", label: "ACTIVE" },
  { value: "2", label: "SUSPENDED" },
];

interface Technician {
  technicianId: number;
  userId: number;
  username: string;
  email: string;
  fullName?: string;
  resellerId?: number;
  employeeCode?: string;
  skillset?: string;
  certification?: string;
  workRegion?: string;
  dailyLimit?: number;
  status: number;
}

interface Reseller {
  resellerId: number;
  companyName: string;
}

interface Props {
  open: boolean;
  technician: Technician | null;
  onClose: (refresh?: boolean) => void;
}

interface FormErrors {
  username?: string;
  email?: string;
  password?: string;
  fullName?: string;
  resellerId?: string;
  dailyLimit?: string;
}

export function TechnicianFormModal({ open, technician, onClose }: Props) {
  const [loading, setLoading] = useState(false);
  const [resellers, setResellers] = useState<Reseller[]>([]);
  const [errors, setErrors] = useState<FormErrors>({});
  const [formData, setFormData] = useState({
    username: "",
    email: "",
    password: "",
    fullName: "",
    resellerId: "",
    employeeCode: "",
    skillset: "",
    certification: "",
    workRegion: "",
    dailyLimit: "50",
    status: "1",
  });

  useEffect(() => {
    if (open) {
      setErrors({});
      fetchResellers();
      if (technician) {
        setFormData({
          username: technician.username,
          email: technician.email,
          password: "",
          fullName: technician.fullName || "",
          resellerId: technician.resellerId?.toString() || "",
          employeeCode: technician.employeeCode || "",
          skillset: technician.skillset || "",
          certification: technician.certification || "",
          workRegion: technician.workRegion || "",
          dailyLimit: technician.dailyLimit?.toString() || "50",
          status: technician.status?.toString() || "1",
        });
      } else {
        setFormData({
          username: "", email: "", password: "", fullName: "", resellerId: "",
          employeeCode: "", skillset: "", certification: "", workRegion: "",
          dailyLimit: "50", status: "1"
        });
      }
    }
  }, [open, technician]);

  const fetchResellers = async () => {
    try {
      const response = await resellerApi.getAll({ pageSize: 100 });
      setResellers(response.data.items || response.data || []);
    } catch (error) {
      console.error("Failed to fetch resellers:", error);
    }
  };

  const validateForm = (): boolean => {
    const newErrors: FormErrors = {};

    // Username validation (only for new technicians)
    if (!technician) {
      if (!formData.username.trim()) {
        newErrors.username = "Username is required";
      } else if (formData.username.length < 3) {
        newErrors.username = "Username must be at least 3 characters";
      } else if (formData.username.length > 50) {
        newErrors.username = "Username must not exceed 50 characters";
      } else if (!/^[a-zA-Z0-9_]+$/.test(formData.username)) {
        newErrors.username = "Username can only contain letters, numbers, and underscores";
      }
    }

    // Email validation (only for new technicians)
    if (!technician) {
      if (!formData.email.trim()) {
        newErrors.email = "Email is required";
      } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
        newErrors.email = "Please enter a valid email address";
      }
    }

    // Password validation (only for new technicians)
    if (!technician) {
      if (!formData.password) {
        newErrors.password = "Password is required";
      } else if (formData.password.length < 8) {
        newErrors.password = "Password must be at least 8 characters";
      } else if (!/(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/.test(formData.password)) {
        newErrors.password = "Password must contain uppercase, lowercase, and number";
      }
    }

    // Full name validation
    if (formData.fullName && formData.fullName.length > 150) {
      newErrors.fullName = "Full name must not exceed 150 characters";
    }

    // Reseller validation
    if (!formData.resellerId) {
      newErrors.resellerId = "Reseller is required";
    }

    // Daily limit validation
    if (formData.dailyLimit) {
      const limit = parseInt(formData.dailyLimit);
      if (isNaN(limit) || limit < 0 || limit > 1000) {
        newErrors.dailyLimit = "Daily limit must be between 0 and 1000";
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
      const data = {
        ...formData,
        resellerId: formData.resellerId ? parseInt(formData.resellerId) : undefined,
        dailyLimit: formData.dailyLimit ? parseInt(formData.dailyLimit) : 50,
        status: parseInt(formData.status),
      };
      if (technician) {
        await technicianApi.update(technician.technicianId, data);
      } else {
        await technicianApi.create(data);
      }
      onClose(true);
    } catch (error) {
      console.error("Failed to save technician:", error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={() => onClose()}>
      <DialogContent className="max-w-2xl" onClose={() => onClose()}>
        <DialogHeader>
          <DialogTitle>{technician ? "Edit Technician" : "Add Technician"}</DialogTitle>
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
                  disabled={!!technician}
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
                  disabled={!!technician}
                  maxLength={100}
                />
                {errors.email && <p className="text-red-500 text-xs mt-1">{errors.email}</p>}
              </div>
            </div>

            {/* Password - only for new technicians */}
            {!technician && (
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
                <p className="text-gray-500 text-xs mt-1">Must contain uppercase, lowercase, and number</p>
              </div>
            )}

            {/* Full Name & Employee Code */}
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label htmlFor="fullName">Full Name</Label>
                <Input
                  id="fullName"
                  value={formData.fullName}
                  onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
                  className={errors.fullName ? "border-red-500" : ""}
                  maxLength={150}
                />
                {errors.fullName && <p className="text-red-500 text-xs mt-1">{errors.fullName}</p>}
              </div>
              <div>
                <Label htmlFor="employeeCode">Employee Code</Label>
                <Input id="employeeCode" value={formData.employeeCode} onChange={(e) => setFormData({ ...formData, employeeCode: e.target.value })} maxLength={50} placeholder="e.g., TECH-001" />
              </div>
            </div>

            {/* Reseller & Work Region */}
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label htmlFor="resellerId">Reseller *</Label>
                <Select
                  id="resellerId"
                  value={formData.resellerId}
                  onChange={(e) => setFormData({ ...formData, resellerId: e.target.value })}
                  className={errors.resellerId ? "border-red-500" : ""}
                >
                  <option value="">Select Reseller</option>
                  {resellers.map((r) => (<option key={r.resellerId} value={r.resellerId}>{r.companyName}</option>))}
                </Select>
                {errors.resellerId && <p className="text-red-500 text-xs mt-1">{errors.resellerId}</p>}
              </div>
              <div>
                <Label htmlFor="workRegion">Work Region</Label>
                <Input id="workRegion" value={formData.workRegion} onChange={(e) => setFormData({ ...formData, workRegion: e.target.value })} maxLength={100} placeholder="e.g., Dubai, Abu Dhabi" />
              </div>
            </div>

            {/* Skillset */}
            <div>
              <Label htmlFor="skillset">Skillset</Label>
              <Textarea
                id="skillset"
                value={formData.skillset}
                onChange={(e) => setFormData({ ...formData, skillset: e.target.value })}
                placeholder="e.g., GPS Installation, Vehicle Tracking, Fleet Management"
                rows={2}
                maxLength={255}
              />
            </div>

            {/* Certification */}
            <div>
              <Label htmlFor="certification">Certification</Label>
              <Textarea
                id="certification"
                value={formData.certification}
                onChange={(e) => setFormData({ ...formData, certification: e.target.value })}
                placeholder="e.g., Certified GPS Installer, Vehicle Tracking Specialist"
                rows={2}
                maxLength={255}
              />
            </div>

            {/* Daily Limit */}
            <div>
              <Label htmlFor="dailyLimit">Daily Verification Limit</Label>
              <Input
                id="dailyLimit"
                type="number"
                value={formData.dailyLimit}
                onChange={(e) => setFormData({ ...formData, dailyLimit: e.target.value })}
                className={errors.dailyLimit ? "border-red-500" : ""}
                min={0}
                max={1000}
                placeholder="50"
              />
              {errors.dailyLimit && <p className="text-red-500 text-xs mt-1">{errors.dailyLimit}</p>}
              <p className="text-gray-500 text-xs mt-1">Maximum verifications per day (0-1000). IMEI access is controlled via IMEI Restrictions.</p>
            </div>

            {/* Status - only shown for editing */}
            {technician && (
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label htmlFor="status">Status</Label>
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

