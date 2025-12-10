"use client";

import { useState, useEffect, useRef, useCallback } from "react";
import { useRouter } from "next/navigation";
import { Search, Scan, Camera, X, QrCode } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Alert } from "@/components/ui/alert";
import { imeiApi } from "@/lib/api";
import { useVerificationStore } from "@/lib/store";

// Error type for proper styling
type ErrorType = "error" | "warning" | "info";

interface ApiError {
  message: string;
  type: ErrorType;
}

export function ImeiInput() {
  const [imei, setImei] = useState("");
  const [error, setError] = useState<ApiError | null>(null);
  const [showScanner, setShowScanner] = useState(false);
  const [scannerError, setScannerError] = useState<string | null>(null);
  const { setImei: storeImei, setDeviceData, setLiveDeviceData, setLoading, isLoading } =
    useVerificationStore();
  const router = useRouter();
  const scannerRef = useRef<HTMLDivElement>(null);
  const html5QrCodeRef = useRef<unknown>(null);

  // Parse API error and return appropriate message and type
  const parseApiError = (err: unknown): ApiError => {
    const error = err as { response?: { status: number; data?: { message?: string } } };
    const status = error.response?.status;
    const message = error.response?.data?.message || "";

    switch (status) {
      case 400:
        // Bad Request - e.g., IMEI cannot be empty
        return {
          message: message || "Invalid request. Please check the IMEI number.",
          type: "warning"
        };
      case 401:
        // Unauthorized - e.g., Invalid Token / Authentication Failed
        return {
          message: message || "Authentication failed. Please try again later.",
          type: "error"
        };
      case 403:
        // Forbidden - Access denied
        return {
          message: message || "You are not authorized to access this IMEI.",
          type: "error"
        };
      case 404:
        // Not Found - Device not found or No data for device
        return {
          message: message || "Device not found. Please check the IMEI number.",
          type: "info"
        };
      case 500:
        // Server Error - No Server Defined or other exceptions
        if (message.toLowerCase().includes("no server defined")) {
          return {
            message: message || "No server defined for this device.",
            type: "warning"
          };
        }
        return {
          message: message || "An unexpected error occurred. Please try again.",
          type: "error"
        };
      default:
        return {
          message: message || "An error occurred. Please try again.",
          type: "error"
        };
    }
  };

  // Get alert variant based on error type
  const getAlertVariant = (type: ErrorType): "destructive" | "warning" | "info" => {
    switch (type) {
      case "error":
        return "destructive";
      case "warning":
        return "warning";
      case "info":
        return "info";
    }
  };

  // Get alert title based on error type
  const getAlertTitle = (type: ErrorType): string => {
    switch (type) {
      case "error":
        return "Error";
      case "warning":
        return "Warning";
      case "info":
        return "Information";
    }
  };

  const validateImei = (value: string): boolean => {
    // IMEI should be 15 digits
    const cleanImei = value.replace(/\D/g, "");
    return cleanImei.length === 15;
  };

  // Stop scanner function
  const stopScanner = useCallback(async () => {
    if (html5QrCodeRef.current) {
      try {
        const scanner = html5QrCodeRef.current as { stop: () => Promise<void>; clear: () => void };
        await scanner.stop();
        scanner.clear();
      } catch (err) {
        console.error("Error stopping scanner:", err);
      }
      html5QrCodeRef.current = null;
    }
  }, []);

  // Handle scanned code
  const handleScanSuccess = useCallback((decodedText: string) => {
    // Strip any AIM code prefix (e.g., ]C0, ]C1, ]C2 for Code 128, ]d1 for DataMatrix, etc.)
    // These are symbology identifiers that some scanners add on Android
    let cleanText = decodedText;
    if (cleanText.startsWith("]")) {
      // AIM code format: ]Xn where X is letter, n is digit
      cleanText = cleanText.replace(/^\][A-Za-z]\d/, "");
    }

    // Extract digits from the scanned text (IMEI should be 15 digits)
    const cleanImei = cleanText.replace(/\D/g, "");

    if (cleanImei.length === 15) {
      // Exact 15 digits - perfect IMEI
      setImei(cleanImei);
      setShowScanner(false);
      setScannerError(null);
    } else if (cleanImei.length === 16 && cleanImei.startsWith("1")) {
      // Android barcode scanner sometimes adds a leading "1" (symbology identifier remnant)
      // Check if removing it gives a valid 15-digit IMEI
      const imeiWithoutPrefix = cleanImei.substring(1);
      setImei(imeiWithoutPrefix);
      setShowScanner(false);
      setScannerError(null);
    } else if (cleanImei.length > 15) {
      // More than 15 digits - take last 15 (IMEI is typically at the end)
      setImei(cleanImei.substring(cleanImei.length - 15));
      setShowScanner(false);
      setScannerError(null);
    } else if (cleanImei.length > 0) {
      // Partial IMEI scanned
      setImei(cleanImei);
      setScannerError(`Scanned ${cleanImei.length} digits. IMEI should be 15 digits.`);
    }
  }, []);

  // Initialize scanner when modal opens
  useEffect(() => {
    if (!showScanner) {
      stopScanner();
      return;
    }

    const initScanner = async () => {
      try {
        // Dynamic import to avoid SSR issues
        const { Html5Qrcode } = await import("html5-qrcode");

        if (!scannerRef.current) return;

        const scannerId = "imei-scanner";

        // Create scanner element if it doesn't exist
        let scannerElement = document.getElementById(scannerId);
        if (!scannerElement && scannerRef.current) {
          scannerElement = document.createElement("div");
          scannerElement.id = scannerId;
          scannerRef.current.appendChild(scannerElement);
        }

        const html5QrCode = new Html5Qrcode(scannerId);
        html5QrCodeRef.current = html5QrCode;

        await html5QrCode.start(
          { facingMode: "environment" }, // Use back camera
          {
            fps: 10,
            qrbox: { width: 250, height: 150 },
            aspectRatio: 1.777778,
          },
          handleScanSuccess,
          () => {} // Ignore scan failures (happens frequently while scanning)
        );

        setScannerError(null);
      } catch (err) {
        console.error("Scanner error:", err);
        if (err instanceof Error) {
          if (err.message.includes("Permission")) {
            setScannerError("Camera permission denied. Please allow camera access.");
          } else if (err.message.includes("NotFoundError")) {
            setScannerError("No camera found on this device.");
          } else {
            setScannerError("Failed to start camera. Please try again or enter IMEI manually.");
          }
        }
      }
    };

    initScanner();

    return () => {
      stopScanner();
    };
  }, [showScanner, handleScanSuccess, stopScanner]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    const cleanImei = imei.replace(/\D/g, "");

    if (!validateImei(cleanImei)) {
      setError({ message: "Please enter a valid 15-digit IMEI number", type: "warning" });
      return;
    }

    setLoading(true);
    storeImei(cleanImei);

    try {
      // First check access
      const accessResponse = await imeiApi.checkAccess(cleanImei);

      if (!accessResponse.data.hasAccess) {
        setError({ message: "You are not authorized to access this IMEI", type: "error" });
        setLoading(false);
        return;
      }

      // Fetch live device data from Vzone API
      const liveDataResponse = await imeiApi.getLiveDeviceData(cleanImei);
      setLiveDeviceData(liveDataResponse.data);

      // Also get device data for verification submission
      const dataResponse = await imeiApi.getDeviceData(cleanImei);
      setDeviceData(dataResponse.data);

      router.push("/verify/result");
    } catch (err: unknown) {
      // Use the parseApiError function to get appropriate message and type
      const apiError = parseApiError(err);
      setError(apiError);
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <Card className="w-full max-w-md mx-auto">
        <CardHeader className="text-center">
          <CardTitle className="flex items-center justify-center gap-2">
            <Scan className="h-6 w-6 text-blue-600" />
            IMEI Verification
          </CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            {/* IMEI Input with Scan Button */}
            <div className="flex gap-2">
              <Input
                type="text"
                placeholder="Enter 15-digit IMEI"
                value={imei}
                onChange={(e) => setImei(e.target.value)}
                maxLength={17}
                className="text-center text-lg tracking-wider flex-1"
                disabled={isLoading}
              />
              <Button
                type="button"
                variant="outline"
                size="lg"
                onClick={() => setShowScanner(true)}
                disabled={isLoading}
                className="px-3"
                title="Scan IMEI Barcode/QR Code"
              >
                <Camera className="h-5 w-5" />
              </Button>
            </div>

            {error && (
              <Alert variant={getAlertVariant(error.type)} title={getAlertTitle(error.type)}>
                {error.message}
              </Alert>
            )}

            <Button
              type="submit"
              className="w-full"
              size="lg"
              isLoading={isLoading}
            >
              <Search className="mr-2 h-5 w-5" />
              Verify Device
            </Button>
          </form>

          <div className="mt-6 text-center text-sm text-gray-500">
            <p>Enter or scan the IMEI from the GPS tracking device</p>
            <p className="mt-1 flex items-center justify-center gap-1">
              <QrCode className="h-4 w-4" />
              Supports barcode and QR code scanning
            </p>
          </div>
        </CardContent>
      </Card>

      {/* Scanner Modal */}
      {showScanner && (
        <div className="fixed inset-0 z-50 bg-black/80 flex items-center justify-center p-4">
          <div className="bg-white rounded-lg w-full max-w-md overflow-hidden">
            {/* Modal Header */}
            <div className="flex items-center justify-between p-4 border-b">
              <h3 className="font-semibold flex items-center gap-2">
                <Camera className="h-5 w-5 text-blue-600" />
                Scan IMEI Barcode / QR Code
              </h3>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setShowScanner(false)}
              >
                <X className="h-5 w-5" />
              </Button>
            </div>

            {/* Scanner Area */}
            <div className="p-4">
              <div
                ref={scannerRef}
                className="w-full bg-gray-900 rounded-lg overflow-hidden min-h-[280px]"
              />

              {scannerError && (
                <Alert variant="destructive" className="mt-4">
                  {scannerError}
                </Alert>
              )}

              <div className="mt-4 text-center text-sm text-gray-500">
                <p>Point your camera at the IMEI barcode or QR code</p>
                <p className="mt-1">The barcode is usually on the device label</p>
              </div>
            </div>

            {/* Modal Footer */}
            <div className="p-4 border-t bg-gray-50">
              <Button
                variant="outline"
                className="w-full"
                onClick={() => setShowScanner(false)}
              >
                Cancel
              </Button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

