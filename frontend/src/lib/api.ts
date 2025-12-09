import axios, { AxiosError, InternalAxiosRequestConfig } from "axios";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000/api";

export const api = axios.create({
  baseURL: API_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

// Request interceptor to add auth token
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem("accessToken");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor for error handling
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const requestUrl = error.config?.url || "";

    // Don't redirect on 401 for login/auth endpoints - let the page handle the error
    const isAuthEndpoint = requestUrl.includes("/auth/login") || requestUrl.includes("/auth/refresh");

    if (error.response?.status === 401 && !isAuthEndpoint) {
      // Token expired or invalid - redirect to login
      localStorage.removeItem("accessToken");
      localStorage.removeItem("user");
      window.location.href = "/login";
    }
    return Promise.reject(error);
  }
);

// Auth API
export const authApi = {
  login: (usernameOrEmail: string, password: string) =>
    api.post("/auth/login", { usernameOrEmail, password }),
  logout: () => api.post("/auth/logout"),
  me: () => api.get("/auth/me"),
  changePassword: (currentPassword: string, newPassword: string) =>
    api.post("/auth/change-password", { currentPassword, newPassword }),
};

// IMEI API
export const imeiApi = {
  checkAccess: (imei: string) => api.get(`/imei/${imei}/check-access`),
  getDeviceData: (imei: string) => api.get(`/imei/${imei}/verify`),
  getLiveDeviceData: (imei: string) => api.get(`/imei/${imei}/live`),
  submitVerification: (data: VerificationRequest) =>
    api.post("/imei/verification", data),
  getHistory: (days?: number) =>
    api.get("/imei/history", { params: { days } }),
};

// Technician API
export const technicianApi = {
  getAll: (params?: TechnicianFilter) =>
    api.get("/technicians", { params }),
  getById: (id: number) => api.get(`/technicians/${id}`),
  create: (data: CreateTechnicianDto) => api.post("/technicians", data),
  update: (id: number, data: UpdateTechnicianDto) =>
    api.put(`/technicians/${id}`, data),
  deactivate: (id: number) => api.post(`/technicians/${id}/deactivate`),
  activate: (id: number) => api.post(`/technicians/${id}/activate`),
  getStats: (id: number) => api.get(`/technicians/${id}/stats`),
};

// Reseller API
export const resellerApi = {
  getAll: (params?: ResellerFilter) => api.get("/resellers", { params }),
  getById: (id: number) => api.get(`/resellers/${id}`),
  create: (data: CreateResellerDto) => api.post("/resellers", data),
  update: (id: number, data: UpdateResellerDto) =>
    api.put(`/resellers/${id}`, data),
  updateStatusWithCascade: (id: number, status: number) =>
    api.put(`/resellers/${id}/status`, { status }),
  deactivate: (id: number) => api.post(`/resellers/${id}/deactivate`),
  activate: (id: number) => api.post(`/resellers/${id}/activate`),
  getStats: (id: number) => api.get(`/resellers/${id}/stats`),
  getTechnicians: (id: number) => api.get(`/resellers/${id}/technicians`),
};

// IMEI Restriction API
export const restrictionApi = {
  getByTechnician: (technicianId: number, page?: number, pageSize?: number) =>
    api.get(`/imei/restrictions/technician/${technicianId}`, {
      params: { page, pageSize },
    }),
  getById: (id: number) => api.get(`/imei/restrictions/${id}`),
  create: (data: CreateRestrictionDto) => api.post("/imei/restrictions", data),
  update: (id: number, data: UpdateRestrictionDto) =>
    api.put(`/imei/restrictions/${id}`, data),
  delete: (id: number) => api.delete(`/imei/restrictions/${id}`),
  searchDevices: (params?: DeviceSearchFilter) =>
    api.get("/imei/restrictions/devices/search", { params }),
  getDevice: (deviceId: number) =>
    api.get(`/imei/restrictions/devices/${deviceId}`),
  getDeviceByImei: (imei: string) =>
    api.get(`/imei/restrictions/devices/imei/${imei}`),
};

// Audit API
export const auditApi = {
  getLogs: (params?: AuditFilter) => api.get("/audit/logs", { params }),
  getUserActivity: (userId: number, from?: string, to?: string) =>
    api.get(`/audit/user/${userId}/activity`, { params: { from, to } }),
};

// Users API
export const userApi = {
  getAll: (params?: UserFilter) => api.get("/users", { params }),
  getById: (id: number) => api.get(`/users/${id}`),
  create: (data: CreateUserDto) => api.post("/users", data),
  update: (id: number, data: UpdateUserDto) => api.put(`/users/${id}`, data),
  delete: (id: number) => api.delete(`/users/${id}`),
  resetPassword: (id: number, newPassword: string) =>
    api.post(`/users/${id}/reset-password`, { newPassword }),
};

// Roles API
export const roleApi = {
  getAll: () => api.get("/roles"),
  getById: (id: number) => api.get(`/roles/${id}`),
  create: (data: CreateRoleDto) => api.post("/roles", data),
  update: (id: number, data: UpdateRoleDto) => api.put(`/roles/${id}`, data),
  delete: (id: number) => api.delete(`/roles/${id}`),
  assignPermissions: (id: number, permissionIds: number[]) =>
    api.post(`/roles/${id}/permissions`, { permissionIds }),
  getMyPermissions: () => api.get("/roles/my-permissions"),
};

// Permissions API
export const permissionApi = {
  getAll: () => api.get("/permissions"),
  getModules: () => api.get("/permissions/modules"),
};

// Tags API
export const tagApi = {
  getAll: (params?: TagFilter) => api.get("/tags", { params }),
  getById: (id: number) => api.get(`/tags/${id}`),
  create: (data: CreateTagDto) => api.post("/tags", data),
  update: (id: number, data: UpdateTagDto) => api.put(`/tags/${id}`, data),
  delete: (id: number) => api.delete(`/tags/${id}`),
  getItems: (tagId: number, entityType?: number) =>
    api.get(`/tags/${tagId}/items`, { params: { entityType } }),
  addItem: (tagId: number, data: CreateTagItemDto) =>
    api.post(`/tags/${tagId}/items`, data),
  bulkAddItems: (tagId: number, data: BulkAddTagItemsDto) =>
    api.post(`/tags/${tagId}/items/bulk`, data),
  removeItem: (tagItemId: number) => api.delete(`/tags/items/${tagItemId}`),
  removeItemByEntity: (tagId: number, entityType: number, entityId: number) =>
    api.delete(`/tags/${tagId}/items/${entityType}/${entityId}`),
  getByEntity: (entityType: number, entityId: number) =>
    api.get(`/tags/entity/${entityType}/${entityId}`),
};

// Import/Export API
export const importExportApi = {
  // Tags - JSON
  exportTags: (includeItems: boolean = true) =>
    api.get("/importexport/tags/export", { params: { includeItems } }),
  importTags: (data: unknown[], updateExisting: boolean = false) =>
    api.post("/importexport/tags/import", data, { params: { updateExisting } }),
  // Tags - Excel
  exportTagsExcel: (includeItems: boolean = true) =>
    api.get("/importexport/tags/export/excel", { params: { includeItems }, responseType: "blob" }),
  importTagsExcel: (file: File, updateExisting: boolean = false) => {
    const formData = new FormData();
    formData.append("file", file);
    return api.post("/importexport/tags/import/excel", formData, {
      params: { updateExisting },
      headers: { "Content-Type": "multipart/form-data" },
    });
  },

  // Technicians - JSON
  exportTechnicians: () => api.get("/importexport/technicians/export"),
  importTechnicians: (data: unknown[], updateExisting: boolean = false) =>
    api.post("/importexport/technicians/import", data, { params: { updateExisting } }),
  // Technicians - Excel
  exportTechniciansExcel: () =>
    api.get("/importexport/technicians/export/excel", { responseType: "blob" }),
  importTechniciansExcel: (file: File, updateExisting: boolean = false) => {
    const formData = new FormData();
    formData.append("file", file);
    return api.post("/importexport/technicians/import/excel", formData, {
      params: { updateExisting },
      headers: { "Content-Type": "multipart/form-data" },
    });
  },

  // Resellers - JSON
  exportResellers: () => api.get("/importexport/resellers/export"),
  importResellers: (data: unknown[], updateExisting: boolean = false) =>
    api.post("/importexport/resellers/import", data, { params: { updateExisting } }),
  // Resellers - Excel
  exportResellersExcel: () =>
    api.get("/importexport/resellers/export/excel", { responseType: "blob" }),
  importResellersExcel: (file: File, updateExisting: boolean = false) => {
    const formData = new FormData();
    formData.append("file", file);
    return api.post("/importexport/resellers/import/excel", formData, {
      params: { updateExisting },
      headers: { "Content-Type": "multipart/form-data" },
    });
  },

  // Users - JSON
  exportUsers: () => api.get("/importexport/users/export"),
  importUsers: (data: unknown[], updateExisting: boolean = false) =>
    api.post("/importexport/users/import", data, { params: { updateExisting } }),
  // Users - Excel
  exportUsersExcel: () =>
    api.get("/importexport/users/export/excel", { responseType: "blob" }),
  importUsersExcel: (file: File, updateExisting: boolean = false) => {
    const formData = new FormData();
    formData.append("file", file);
    return api.post("/importexport/users/import/excel", formData, {
      params: { updateExisting },
      headers: { "Content-Type": "multipart/form-data" },
    });
  },

  // Roles - JSON
  exportRoles: () => api.get("/importexport/roles/export"),
  importRoles: (data: unknown[], updateExisting: boolean = false) =>
    api.post("/importexport/roles/import", data, { params: { updateExisting } }),
  // Roles - Excel
  exportRolesExcel: () =>
    api.get("/importexport/roles/export/excel", { responseType: "blob" }),
  importRolesExcel: (file: File, updateExisting: boolean = false) => {
    const formData = new FormData();
    formData.append("file", file);
    return api.post("/importexport/roles/import/excel", formData, {
      params: { updateExisting },
      headers: { "Content-Type": "multipart/form-data" },
    });
  },
};

// Dashboard API
export const dashboardApi = {
  // Get dashboard based on current user's role
  getDashboard: () => api.get("/dashboard"),
  // Role-specific dashboards
  getSuperAdminDashboard: () => api.get("/dashboard/superadmin"),
  getResellerAdminDashboard: (resellerId?: number) =>
    api.get("/dashboard/reselleradmin", { params: { resellerId } }),
  getSupervisorDashboard: (resellerId?: number) =>
    api.get("/dashboard/supervisor", { params: { resellerId } }),
  getTechnicianDashboard: (technicianId?: number) =>
    api.get("/dashboard/technician", { params: { technicianId } }),
};

// Types
export interface VerificationRequest {
  imei: string;
  verificationStatus: string;
  gpsData?: GpsData;
  notes?: string;
}

export interface GpsData {
  latitude?: number;
  longitude?: number;
  signalStrength?: number;
  gpsTime?: string;
}

export interface TechnicianFilter {
  resellerId?: number;
  status?: number;
  workRegion?: string;
  searchTerm?: string;
  page?: number;
  pageSize?: number;
}

export interface CreateTechnicianDto {
  username: string;
  email: string;
  password: string;
  fullName?: string;
  resellerId?: number;
  employeeCode?: string;
  skillset?: string;
  certification?: string;
  workRegion?: string;
  imeiRestrictionMode?: number;
  dailyLimit?: number;
}

export interface UpdateTechnicianDto {
  fullName?: string;
  resellerId?: number;
  employeeCode?: string;
  skillset?: string;
  certification?: string;
  workRegion?: string;
  imeiRestrictionMode?: number;
  dailyLimit?: number;
  status?: number;
}

export interface ResellerFilter {
  status?: number;
  country?: string;
  searchTerm?: string;
  page?: number;
  pageSize?: number;
}

export interface CreateResellerDto {
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
}

export interface UpdateResellerDto extends CreateResellerDto {
  status?: number;
}

export interface CreateRestrictionDto {
  technicianId: number;
  deviceId?: number;
  tagId?: number;
  accessType: number;
  priority?: number;
  reason?: string;
  isPermanent?: boolean;
  validFrom?: string;
  validUntil?: string;
  notes?: string;
}

export interface UpdateRestrictionDto {
  deviceId?: number;
  tagId?: number;
  accessType?: number;
  priority?: number;
  reason?: string;
  isPermanent?: boolean;
  validFrom?: string;
  validUntil?: string;
  notes?: string;
  status?: number;
}

export interface AuditFilter {
  userId?: number;
  action?: string;
  entityType?: string;
  fromDate?: string;
  toDate?: string;
  page?: number;
  pageSize?: number;
}

export interface UserFilter {
  resellerId?: number;
  roleId?: number;
  status?: number;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface CreateUserDto {
  username: string;
  email: string;
  password: string;
  fullName?: string;
  mobile?: string;
  resellerId?: number;
  roleIds?: number[];
  roles?: string[]; // Role names: SUPERADMIN, RESELLER ADMIN, TECHNICIAN, SUPERVISOR
  status?: number;
  lockoutUntil?: string;
}

export interface UpdateUserDto {
  fullName?: string;
  mobile?: string;
  resellerId?: number;
  status?: number;
  lockoutUntil?: string;
  roleIds?: number[];
  roles?: string[]; // Role names: SUPERADMIN, RESELLER ADMIN, TECHNICIAN, SUPERVISOR
}

export interface CreateRoleDto {
  roleName: string;
  description?: string;
}

export interface UpdateRoleDto {
  roleName?: string;
  description?: string;
}

export interface TagFilter {
  scope?: number;
  status?: number;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface CreateTagDto {
  tagName: string;
  description?: string;
  scope?: number;
  resellerId?: number;
  userId?: number;
  color?: string;
}

export interface UpdateTagDto {
  tagName?: string;
  description?: string;
  color?: string;
  status?: number;
}

export interface CreateTagItemDto {
  tagId?: number;
  entityType: number;
  entityId: number;
  entityIdentifier?: string;
}

export interface BulkAddTagItemsDto {
  tagId?: number;
  entityType: number;
  items: Array<{ entityId: number; entityIdentifier?: string }>;
}

export interface TagItemDto {
  tagItemId: number;
  tagId: number;
  tagName?: string;
  entityType: number;
  entityTypeName: string;
  entityId: number;
  entityIdentifier?: string;
  createdAt: string;
}

// Entity type constants
export const EntityType = {
  Device: 1,
  Technician: 2,
  Reseller: 3,
  User: 4,
} as const;

export interface DeviceSearchFilter {
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface ExternalDevice {
  deviceId: number;
  imei: string;
  timeZone?: string;
  sim?: string;
  countryCode?: string;
  typeId?: number;
  server?: string;
}
