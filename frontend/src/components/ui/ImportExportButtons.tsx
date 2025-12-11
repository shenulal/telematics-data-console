"use client";

import { useState, useRef } from "react";
import { Button } from "@/components/ui/button";
import { Download, Upload, Loader2, ChevronDown, FileJson, FileSpreadsheet, FileDown } from "lucide-react";
import { importExportApi } from "@/lib/api";

type EntityType = "tags" | "technicians" | "resellers" | "users" | "roles";
type FileFormat = "json" | "xlsx";

interface ImportExportButtonsProps {
  entityType: EntityType;
  onImportComplete?: () => void;
  includeItems?: boolean; // For tags - include tag items
}

interface ImportResult {
  totalRows: number;
  successCount: number;
  failedCount: number;
  errors: { rowNumber: number; identifier?: string; errorMessage: string }[];
}

export function ImportExportButtons({
  entityType,
  onImportComplete,
  includeItems = true,
}: ImportExportButtonsProps) {
  const [exporting, setExporting] = useState(false);
  const [importing, setImporting] = useState(false);
  const [downloadingTemplate, setDownloadingTemplate] = useState(false);
  const [importResult, setImportResult] = useState<ImportResult | null>(null);
  const [showExportMenu, setShowExportMenu] = useState(false);
  const [showImportMenu, setShowImportMenu] = useState(false);
  const [importFormat, setImportFormat] = useState<FileFormat>("json");
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleExport = async (format: FileFormat) => {
    setShowExportMenu(false);
    setExporting(true);
    try {
      if (format === "json") {
        let response;
        switch (entityType) {
          case "tags":
            response = await importExportApi.exportTags(includeItems);
            break;
          case "technicians":
            response = await importExportApi.exportTechnicians();
            break;
          case "resellers":
            response = await importExportApi.exportResellers();
            break;
          case "users":
            response = await importExportApi.exportUsers();
            break;
          case "roles":
            response = await importExportApi.exportRoles();
            break;
        }

        // Download as JSON file
        const blob = new Blob([JSON.stringify(response.data, null, 2)], {
          type: "application/json",
        });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = `${entityType}_export_${new Date().toISOString().split("T")[0]}.json`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
      } else {
        // Excel export
        let response;
        switch (entityType) {
          case "tags":
            response = await importExportApi.exportTagsExcel(includeItems);
            break;
          case "technicians":
            response = await importExportApi.exportTechniciansExcel();
            break;
          case "resellers":
            response = await importExportApi.exportResellersExcel();
            break;
          case "users":
            response = await importExportApi.exportUsersExcel();
            break;
          case "roles":
            response = await importExportApi.exportRolesExcel();
            break;
        }

        // Download as Excel file
        const blob = new Blob([response.data], {
          type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = `${entityType}_export_${new Date().toISOString().split("T")[0]}.xlsx`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
      }
    } catch (error) {
      console.error("Export failed:", error);
      alert("Export failed. Please try again.");
    } finally {
      setExporting(false);
    }
  };

  const handleImportClick = (format: FileFormat) => {
    setShowImportMenu(false);
    setImportFormat(format);
    fileInputRef.current?.click();
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setImporting(true);
    setImportResult(null);

    try {
      const updateExisting = confirm(
        "Do you want to update existing records if they already exist?\n\nClick OK to update, Cancel to skip existing."
      );

      let response;

      if (importFormat === "json") {
        const text = await file.text();
        const data = JSON.parse(text);

        if (!Array.isArray(data)) {
          throw new Error("Invalid file format. Expected an array of items.");
        }

        switch (entityType) {
          case "tags":
            response = await importExportApi.importTags(data, updateExisting);
            break;
          case "technicians":
            response = await importExportApi.importTechnicians(data, updateExisting);
            break;
          case "resellers":
            response = await importExportApi.importResellers(data, updateExisting);
            break;
          case "users":
            response = await importExportApi.importUsers(data, updateExisting);
            break;
          case "roles":
            response = await importExportApi.importRoles(data, updateExisting);
            break;
        }
      } else {
        // Excel import
        switch (entityType) {
          case "tags":
            response = await importExportApi.importTagsExcel(file, updateExisting);
            break;
          case "technicians":
            response = await importExportApi.importTechniciansExcel(file, updateExisting);
            break;
          case "resellers":
            response = await importExportApi.importResellersExcel(file, updateExisting);
            break;
          case "users":
            response = await importExportApi.importUsersExcel(file, updateExisting);
            break;
          case "roles":
            response = await importExportApi.importRolesExcel(file, updateExisting);
            break;
        }
      }

      setImportResult(response.data);
      onImportComplete?.();
    } catch (error) {
      console.error("Import failed:", error);
      alert(`Import failed: ${error instanceof Error ? error.message : "Unknown error"}`);
    } finally {
      setImporting(false);
      if (fileInputRef.current) {
        fileInputRef.current.value = "";
      }
    }
  };

  const handleDownloadTemplate = async () => {
    setShowImportMenu(false);
    setDownloadingTemplate(true);
    try {
      let response;
      switch (entityType) {
        case "tags":
          response = await importExportApi.downloadTagsTemplate();
          break;
        case "technicians":
          response = await importExportApi.downloadTechniciansTemplate();
          break;
        case "resellers":
          response = await importExportApi.downloadResellersTemplate();
          break;
        case "users":
          response = await importExportApi.downloadUsersTemplate();
          break;
        case "roles":
          response = await importExportApi.downloadRolesTemplate();
          break;
      }

      const blob = new Blob([response.data], {
        type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
      });
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `${entityType}_import_template.xlsx`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch (error) {
      console.error("Template download failed:", error);
      alert("Failed to download template. Please try again.");
    } finally {
      setDownloadingTemplate(false);
    }
  };

  return (
    <div className="flex items-center gap-2">
      {/* Export Dropdown */}
      <div className="relative">
        <Button
          variant="outline"
          size="sm"
          onClick={() => setShowExportMenu(!showExportMenu)}
          disabled={exporting}
          className="flex items-center gap-1"
        >
          {exporting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
          Export
          <ChevronDown className="h-3 w-3" />
        </Button>
        {showExportMenu && (
          <div className="absolute top-full left-0 mt-1 bg-white border rounded-md shadow-lg z-50 min-w-[140px]">
            <button
              className="flex items-center gap-2 w-full px-3 py-2 text-sm hover:bg-gray-100 text-left"
              onClick={() => handleExport("json")}
            >
              <FileJson className="h-4 w-4 text-blue-600" />
              Export JSON
            </button>
            <button
              className="flex items-center gap-2 w-full px-3 py-2 text-sm hover:bg-gray-100 text-left"
              onClick={() => handleExport("xlsx")}
            >
              <FileSpreadsheet className="h-4 w-4 text-green-600" />
              Export Excel
            </button>
          </div>
        )}
      </div>

      {/* Import Dropdown */}
      <div className="relative">
        <Button
          variant="outline"
          size="sm"
          onClick={() => setShowImportMenu(!showImportMenu)}
          disabled={importing}
          className="flex items-center gap-1"
        >
          {importing ? <Loader2 className="h-4 w-4 animate-spin" /> : <Upload className="h-4 w-4" />}
          Import
          <ChevronDown className="h-3 w-3" />
        </Button>
        {showImportMenu && (
          <div className="absolute top-full left-0 mt-1 bg-white border rounded-md shadow-lg z-50 min-w-[160px]">
            <button
              className="flex items-center gap-2 w-full px-3 py-2 text-sm hover:bg-gray-100 text-left"
              onClick={() => handleImportClick("json")}
            >
              <FileJson className="h-4 w-4 text-blue-600" />
              Import JSON
            </button>
            <button
              className="flex items-center gap-2 w-full px-3 py-2 text-sm hover:bg-gray-100 text-left"
              onClick={() => handleImportClick("xlsx")}
            >
              <FileSpreadsheet className="h-4 w-4 text-green-600" />
              Import Excel
            </button>
            <div className="border-t my-1"></div>
            <button
              className="flex items-center gap-2 w-full px-3 py-2 text-sm hover:bg-gray-100 text-left"
              onClick={handleDownloadTemplate}
              disabled={downloadingTemplate}
            >
              {downloadingTemplate ? (
                <Loader2 className="h-4 w-4 text-purple-600 animate-spin" />
              ) : (
                <FileDown className="h-4 w-4 text-purple-600" />
              )}
              Download Template
            </button>
          </div>
        )}
      </div>

      <input
        ref={fileInputRef}
        type="file"
        accept={importFormat === "json" ? ".json" : ".xlsx,.xls"}
        onChange={handleFileChange}
        className="hidden"
      />

      {importResult && (
        <span className={`text-sm ${importResult.failedCount > 0 ? "text-yellow-600" : "text-green-600"}`}>
          {importResult.successCount}/{importResult.totalRows} imported
          {importResult.failedCount > 0 && ` (${importResult.failedCount} failed)`}
        </span>
      )}

      {/* Click outside to close menus */}
      {(showExportMenu || showImportMenu) && (
        <div
          className="fixed inset-0 z-40"
          onClick={() => { setShowExportMenu(false); setShowImportMenu(false); }}
        />
      )}
    </div>
  );
}

