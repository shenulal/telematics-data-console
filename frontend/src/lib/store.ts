import { create } from "zustand";
import { persist } from "zustand/middleware";

export interface User {
  userId: number;
  username: string;
  email: string;
  fullName?: string;
  technicianId?: number;
  resellerId?: number;
  roles: string[];
  permissions: string[];
}

interface AuthState {
  user: User | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  login: (user: User, accessToken: string) => void;
  logout: () => void;
  hasRole: (role: string) => boolean;
  hasPermission: (permission: string) => boolean;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,
      accessToken: null,
      isAuthenticated: false,

      login: (user: User, accessToken: string) => {
        localStorage.setItem("accessToken", accessToken);
        set({ user, accessToken, isAuthenticated: true });
      },

      logout: () => {
        localStorage.removeItem("accessToken");
        set({ user: null, accessToken: null, isAuthenticated: false });
      },

      hasRole: (role: string) => {
        const { user } = get();
        // Check for role variations (case-insensitive and with/without spaces)
        const normalizedRole = role.toUpperCase().replace(/\s+/g, '');
        return user?.roles?.some(r => r.toUpperCase().replace(/\s+/g, '') === normalizedRole) ?? false;
      },

      hasPermission: (permission: string) => {
        const { user } = get();
        // Check for Super Admin role variations
        const isSuperAdmin = user?.roles?.some(r => {
          const normalized = r.toUpperCase().replace(/\s+/g, '');
          return normalized === 'SUPERADMIN' || normalized === 'ADMIN';
        });
        if (isSuperAdmin) return true;
        return user?.permissions?.includes(permission) ?? false;
      },
    }),
    {
      name: "auth-storage",
      partialize: (state) => ({
        user: state.user,
        accessToken: state.accessToken,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
);

// Device verification state
interface VerificationState {
  currentImei: string;
  deviceData: DeviceData | null;
  liveDeviceData: LiveDeviceData | null;
  isLoading: boolean;
  error: string | null;
  setImei: (imei: string) => void;
  setDeviceData: (data: DeviceData | null) => void;
  setLiveDeviceData: (data: LiveDeviceData | null) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  reset: () => void;
}

export interface DeviceData {
  deviceId: number;
  imei: string;
  serialNumber?: string;
  deviceModel?: string;
  firmwareVersion?: string;
  isOnline: boolean;
  lastGpsData?: GpsData;
  vehicleInfo?: VehicleInfo;
}

export interface GpsData {
  latitude?: number;
  longitude?: number;
  altitude?: number;
  speed?: number;
  heading?: number;
  satellites?: number;
  signalStrength?: number;
  ignitionOn?: boolean;
  batteryVoltage?: number;
  externalVoltage?: number;
  gpsTime?: string;
  serverTime?: string;
}

export interface VehicleInfo {
  vehicleId?: number;
  plateNumber?: string;
  vehicleName?: string;
  make?: string;
  model?: string;
  year?: number;
  vin?: string;
  ownerName?: string;
}

// Live device data from Vzone API
export interface LiveDeviceData {
  imei: string;
  trackTime: string;
  status: string;
  speed: number;
  isOnline: boolean;
  latitude?: number;
  longitude?: number;
  locationName?: string;
  locationProximity?: number;
  ioData: IoDataItem[];
}

// IO data item - includes all VZone API fields
export interface IoDataItem {
  universalIOID?: number | null;
  universalIOName?: string | null;
  ioCode?: string | null;
  ioName?: string | null;
  value?: unknown;
  rawValue?: string | null;
}

// Verification snapshot - stores a single data capture
export interface VerificationSnapshot {
  id: string;
  timestamp: string;
  data: LiveDeviceData;
}

// Common verification comments
export const COMMON_COMMENTS = [
  "Device working properly",
  "GPS signal confirmed",
  "All parameters within normal range",
  "Device installed and tested",
  "Firmware updated successfully",
  "Connection verified",
  "Needs follow-up",
  "Issue identified - requires attention",
];

export const useVerificationStore = create<VerificationState>((set) => ({
  currentImei: "",
  deviceData: null,
  liveDeviceData: null,
  isLoading: false,
  error: null,

  setImei: (imei: string) => set({ currentImei: imei }),
  setDeviceData: (data: DeviceData | null) => set({ deviceData: data }),
  setLiveDeviceData: (data: LiveDeviceData | null) => set({ liveDeviceData: data }),
  setLoading: (loading: boolean) => set({ isLoading: loading }),
  setError: (error: string | null) => set({ error }),
  reset: () =>
    set({
      currentImei: "",
      deviceData: null,
      liveDeviceData: null,
      isLoading: false,
      error: null,
    }),
}));

// localStorage helper functions for verification snapshots
export const getVerificationSnapshots = (imei: string): VerificationSnapshot[] => {
  if (typeof window === 'undefined') return [];
  const key = `verification_snapshots_${imei}`;
  const stored = localStorage.getItem(key);
  return stored ? JSON.parse(stored) : [];
};

export const addVerificationSnapshot = (imei: string, data: LiveDeviceData): VerificationSnapshot => {
  const snapshots = getVerificationSnapshots(imei);
  const newSnapshot: VerificationSnapshot = {
    id: `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
    timestamp: new Date().toISOString(),
    data,
  };
  snapshots.push(newSnapshot);
  localStorage.setItem(`verification_snapshots_${imei}`, JSON.stringify(snapshots));
  return newSnapshot;
};

export const clearVerificationSnapshots = (imei: string): void => {
  localStorage.removeItem(`verification_snapshots_${imei}`);
};

