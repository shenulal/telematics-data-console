"use client";

import * as React from "react";
import { X } from "lucide-react";
import { cn } from "@/lib/utils";

interface DialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  children: React.ReactNode;
}

export function Dialog({ open, onOpenChange, children }: DialogProps) {
  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50">
      <div className="fixed inset-0 bg-black/50" onClick={() => onOpenChange(false)} />
      <div className="fixed inset-0 flex items-center justify-center p-4">
        {children}
      </div>
    </div>
  );
}

interface DialogContentProps {
  children: React.ReactNode;
  className?: string;
  onClose?: () => void;
}

export function DialogContent({ children, className, onClose }: DialogContentProps) {
  return (
    <div
      className={cn(
        "relative rounded-lg shadow-lg max-h-[90vh] overflow-y-auto w-full max-w-lg",
        className
      )}
      style={{ backgroundColor: 'var(--card-background)' }}
      role="dialog"
    >
      {onClose && (
        <button
          onClick={onClose}
          className="absolute right-4 top-4 p-1 rounded-sm opacity-70 hover:opacity-100"
          style={{ color: 'var(--muted)' }}
        >
          <X className="h-4 w-4" />
        </button>
      )}
      {children}
    </div>
  );
}

export function DialogHeader({ children, className }: { children: React.ReactNode; className?: string }) {
  return (
    <div className={cn("px-6 pt-6 pb-2", className)}>
      {children}
    </div>
  );
}

export function DialogTitle({ children, className }: { children: React.ReactNode; className?: string }) {
  return (
    <h2 className={cn("text-lg font-semibold", className)} style={{ color: 'var(--foreground)' }}>
      {children}
    </h2>
  );
}

export function DialogDescription({ children, className }: { children: React.ReactNode; className?: string }) {
  return (
    <p className={cn("text-sm mt-1", className)} style={{ color: 'var(--muted)' }}>
      {children}
    </p>
  );
}

export function DialogFooter({ children, className }: { children: React.ReactNode; className?: string }) {
  return (
    <div
      className={cn("px-6 py-4 flex justify-end gap-2 rounded-b-lg", className)}
      style={{ backgroundColor: 'var(--accent)' }}
    >
      {children}
    </div>
  );
}

