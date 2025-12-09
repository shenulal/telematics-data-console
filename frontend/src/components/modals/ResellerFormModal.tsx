"use client";

import { useState, useEffect } from "react";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Alert } from "@/components/ui/alert";
import { resellerApi } from "@/lib/api";

// Status enum: 0=INACTIVE, 1=ACTIVE, 2=SUSPENDED
const STATUS_OPTIONS = [
  { value: "1", label: "ACTIVE" },
  { value: "0", label: "INACTIVE" },
  { value: "2", label: "SUSPENDED" },
];

const STATUS_LABELS: Record<string, string> = {
  "0": "INACTIVE",
  "1": "ACTIVE",
  "2": "SUSPENDED",
};

interface Reseller {
  resellerId: number;
  companyName: string;
  displayName?: string;
  contactPerson?: string;
  email?: string;
  mobile?: string;
  phone?: string;
  addressLine1?: string;
  city?: string;
  state?: string;
  country?: string;
  status: number;
}

interface Props {
  open: boolean;
  reseller: Reseller | null;
  onClose: (refresh?: boolean) => void;
}

interface FormErrors {
  companyName?: string;
  email?: string;
  mobile?: string;
  phone?: string;
}

interface StatusUpdateResult {
  resellerId: number;
  companyName: string;
  newStatus: number;
  statusText: string;
  usersUpdated: number;
  techniciansUpdated: number;
  tagsUpdated: number;
  rolesUpdated: number;
  message: string;
}

export function ResellerFormModal({ open, reseller, onClose }: Props) {
  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState<FormErrors>({});
  const [showStatusConfirm, setShowStatusConfirm] = useState(false);
  const [pendingStatus, setPendingStatus] = useState<string | null>(null);
  const [statusUpdateResult, setStatusUpdateResult] = useState<StatusUpdateResult | null>(null);
  const [formData, setFormData] = useState({
    companyName: "",
    displayName: "",
    contactPerson: "",
    email: "",
    mobile: "",
    phone: "",
    addressLine1: "",
    city: "",
    state: "",
    country: "",
    status: "1",
  });

  useEffect(() => {
    if (open) {
      setErrors({});
      setShowStatusConfirm(false);
      setPendingStatus(null);
      setStatusUpdateResult(null);
      if (reseller) {
        setFormData({
          companyName: reseller.companyName,
          displayName: reseller.displayName || "",
          contactPerson: reseller.contactPerson || "",
          email: reseller.email || "",
          mobile: reseller.mobile || "",
          phone: reseller.phone || "",
          addressLine1: reseller.addressLine1 || "",
          city: reseller.city || "",
          state: reseller.state || "",
          country: reseller.country || "",
          status: reseller.status?.toString() || "1",
        });
      } else {
        setFormData({ companyName: "", displayName: "", contactPerson: "", email: "", mobile: "", phone: "", addressLine1: "", city: "", state: "", country: "", status: "1" });
      }
    }
  }, [open, reseller]);

  // Handle status change - show confirmation for existing resellers
  const handleStatusChange = (newStatus: string) => {
    if (reseller && newStatus !== reseller.status.toString()) {
      setPendingStatus(newStatus);
      setShowStatusConfirm(true);
    } else {
      setFormData({ ...formData, status: newStatus });
    }
  };

  // Confirm status change with cascade
  const confirmStatusChange = async () => {
    if (!reseller || !pendingStatus) return;

    setLoading(true);
    try {
      const response = await resellerApi.updateStatusWithCascade(reseller.resellerId, parseInt(pendingStatus));
      setStatusUpdateResult(response.data);
      setFormData({ ...formData, status: pendingStatus });
      setShowStatusConfirm(false);
      setPendingStatus(null);
    } catch (error) {
      console.error("Failed to update status:", error);
    } finally {
      setLoading(false);
    }
  };

  // Cancel status change
  const cancelStatusChange = () => {
    setShowStatusConfirm(false);
    setPendingStatus(null);
  };

  const validateForm = (): boolean => {
    const newErrors: FormErrors = {};

    // Company name validation (required, min 2 chars, max 100 chars)
    if (!formData.companyName.trim()) {
      newErrors.companyName = "Company name is required";
    } else if (formData.companyName.trim().length < 2) {
      newErrors.companyName = "Company name must be at least 2 characters";
    } else if (formData.companyName.trim().length > 100) {
      newErrors.companyName = "Company name must not exceed 100 characters";
    }

    // Email validation (optional, but must be valid if provided)
    if (formData.email) {
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRegex.test(formData.email)) {
        newErrors.email = "Please enter a valid email address";
      }
    }

    // Mobile validation (optional, but must be valid if provided)
    if (formData.mobile) {
      const phoneRegex = /^[\d\s\-+()]{7,20}$/;
      if (!phoneRegex.test(formData.mobile)) {
        newErrors.mobile = "Please enter a valid mobile number";
      }
    }

    // Phone validation (optional, but must be valid if provided)
    if (formData.phone) {
      const phoneRegex = /^[\d\s\-+()]{7,20}$/;
      if (!phoneRegex.test(formData.phone)) {
        newErrors.phone = "Please enter a valid phone number";
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    setLoading(true);
    try {
      const payload = {
        ...formData,
        status: parseInt(formData.status),
      };
      if (reseller) {
        await resellerApi.update(reseller.resellerId, payload);
      } else {
        await resellerApi.create(payload);
      }
      onClose(true);
    } catch (error) {
      console.error("Failed to save reseller:", error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={() => onClose()}>
      <DialogContent className="max-w-2xl" onClose={() => onClose()}>
        <DialogHeader>
          <DialogTitle>{reseller ? "Edit Reseller" : "Add Reseller"}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit}>
          <div className="px-6 py-4 space-y-4 max-h-[60vh] overflow-y-auto">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label htmlFor="companyName">Company Name *</Label>
                <Input
                  id="companyName"
                  value={formData.companyName}
                  onChange={(e) => setFormData({ ...formData, companyName: e.target.value })}
                  className={errors.companyName ? "border-red-500" : ""}
                  maxLength={100}
                />
                {errors.companyName && <p className="text-red-500 text-xs mt-1">{errors.companyName}</p>}
              </div>
              <div>
                <Label htmlFor="displayName">Display Name</Label>
                <Input
                  id="displayName"
                  value={formData.displayName}
                  onChange={(e) => setFormData({ ...formData, displayName: e.target.value })}
                  maxLength={100}
                />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label htmlFor="contactPerson">Contact Person</Label>
                <Input
                  id="contactPerson"
                  value={formData.contactPerson}
                  onChange={(e) => setFormData({ ...formData, contactPerson: e.target.value })}
                  maxLength={100}
                />
              </div>
              <div>
                <Label htmlFor="email">Email</Label>
                <Input
                  id="email"
                  type="email"
                  value={formData.email}
                  onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                  className={errors.email ? "border-red-500" : ""}
                  maxLength={100}
                />
                {errors.email && <p className="text-red-500 text-xs mt-1">{errors.email}</p>}
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
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
              <div>
                <Label htmlFor="phone">Phone</Label>
                <Input
                  id="phone"
                  value={formData.phone}
                  onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                  className={errors.phone ? "border-red-500" : ""}
                  placeholder="+1 234 567 8900"
                  maxLength={20}
                />
                {errors.phone && <p className="text-red-500 text-xs mt-1">{errors.phone}</p>}
              </div>
            </div>
            <div>
              <Label htmlFor="addressLine1">Address</Label>
              <Input
                id="addressLine1"
                value={formData.addressLine1}
                onChange={(e) => setFormData({ ...formData, addressLine1: e.target.value })}
                maxLength={200}
              />
            </div>
            <div className="grid grid-cols-3 gap-4">
              <div>
                <Label htmlFor="city">City</Label>
                <Input
                  id="city"
                  value={formData.city}
                  onChange={(e) => setFormData({ ...formData, city: e.target.value })}
                  maxLength={50}
                />
              </div>
              <div>
                <Label htmlFor="state">State</Label>
                <Input
                  id="state"
                  value={formData.state}
                  onChange={(e) => setFormData({ ...formData, state: e.target.value })}
                  maxLength={50}
                />
              </div>
              <div>
                <Label htmlFor="country">Country</Label>
                <Input
                  id="country"
                  value={formData.country}
                  onChange={(e) => setFormData({ ...formData, country: e.target.value })}
                  maxLength={50}
                />
              </div>
            </div>
            {/* Status - only shown when editing */}
            {reseller && (
              <div>
                <Label htmlFor="status">Status *</Label>
                <Select
                  id="status"
                  value={formData.status}
                  onChange={(e) => handleStatusChange(e.target.value)}
                >
                  {STATUS_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>{option.label}</option>
                  ))}
                </Select>
                <p className="text-gray-500 text-xs mt-1">
                  Changing status will affect all users, technicians, tags, and custom roles under this reseller.
                </p>
              </div>
            )}

            {/* Status Update Result */}
            {statusUpdateResult && (
              <Alert variant="success" className="bg-green-50 border-green-200">
                <div className="text-green-800">
                  <p className="font-medium">Status Updated Successfully!</p>
                  <p className="text-sm mt-1">{statusUpdateResult.message}</p>
                  <ul className="text-sm mt-2 list-disc list-inside">
                    <li>Users updated: {statusUpdateResult.usersUpdated}</li>
                    <li>Technicians updated: {statusUpdateResult.techniciansUpdated}</li>
                    <li>Tags updated: {statusUpdateResult.tagsUpdated}</li>
                    <li>Custom Roles updated: {statusUpdateResult.rolesUpdated}</li>
                  </ul>
                </div>
              </Alert>
            )}

            {/* Status Change Confirmation */}
            {showStatusConfirm && pendingStatus && (
              <Alert variant="warning" className="bg-amber-50 border-amber-200">
                <div className="text-amber-800">
                  <p className="font-medium">⚠️ Confirm Status Change</p>
                  <p className="text-sm mt-1">
                    You are about to change the status from <strong>{STATUS_LABELS[reseller?.status.toString() || "1"]}</strong> to <strong>{STATUS_LABELS[pendingStatus]}</strong>.
                  </p>
                  <p className="text-sm mt-1">
                    This will also update the status of all related:
                  </p>
                  <ul className="text-sm mt-1 list-disc list-inside">
                    <li>Users under this reseller</li>
                    <li>Technicians under this reseller</li>
                    <li>Tags under this reseller</li>
                    <li>Custom roles associated with this reseller&apos;s users</li>
                  </ul>
                  <div className="flex gap-2 mt-3">
                    <Button
                      type="button"
                      size="sm"
                      onClick={confirmStatusChange}
                      disabled={loading}
                    >
                      {loading ? "Updating..." : "Confirm Change"}
                    </Button>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={cancelStatusChange}
                      disabled={loading}
                    >
                      Cancel
                    </Button>
                  </div>
                </div>
              </Alert>
            )}
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onClose()}>Cancel</Button>
            <Button type="submit" disabled={loading || showStatusConfirm}>{loading ? "Saving..." : "Save"}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

