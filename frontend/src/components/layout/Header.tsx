"use client";

import { useAuthStore } from "@/lib/store";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { LogOut, Menu, User, MapPin, ChevronDown, Lock, Settings } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useState, useRef, useEffect } from "react";
import { isSuperAdmin, isResellerAdmin, isSupervisor, isTechnician } from "@/lib/utils";
import { ChangePasswordModal } from "@/components/modals/ChangePasswordModal";

export function Header() {
  const { user, logout } = useAuthStore();
  const router = useRouter();
  const [menuOpen, setMenuOpen] = useState(false);
  const [adminDropdown, setAdminDropdown] = useState(false);
  const [userDropdown, setUserDropdown] = useState(false);
  const [changePasswordOpen, setChangePasswordOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const userDropdownRef = useRef<HTMLDivElement>(null);

  const handleLogout = () => {
    logout();
    router.push("/login");
  };

  // Close dropdowns when clicking outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setAdminDropdown(false);
      }
      if (userDropdownRef.current && !userDropdownRef.current.contains(event.target as Node)) {
        setUserDropdown(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const adminMenuItems = [
    { href: "/admin/resellers", label: "Resellers" },
    { href: "/admin/users", label: "Users" },
    { href: "/admin/technicians", label: "Technicians" },
    { href: "/admin/roles", label: "Roles" },
    { href: "/admin/tags", label: "Tags" },
    { href: "/admin/audit", label: "Audit Logs" },
  ];

  return (
    <header className="bg-slate-900 text-white shadow-lg sticky top-0 z-50">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-16">
          {/* Logo */}
          <div className="flex items-center gap-2">
            <MapPin className="h-8 w-8 text-blue-400" />
            <div>
              <h1 className="text-lg font-bold">TDC</h1>
              <p className="text-xs text-gray-400 hidden sm:block">
                Telematics Data Console
              </p>
            </div>
          </div>

          {/* Desktop Navigation */}
          <nav className="hidden md:flex items-center gap-1">
            {/* Dashboard link for all users */}
            <Link href="/dashboard" className="text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md text-sm font-medium">
              Dashboard
            </Link>
            {isSuperAdmin(user?.roles) && (
              <>
                <Link href="/verify" className="text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md text-sm font-medium">
                  Verify IMEI
                </Link>
                <Link href="/admin/resellers" className="text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md text-sm">
                  Resellers
                </Link>
                <Link href="/admin/users" className="text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md text-sm">
                  Users
                </Link>
                <Link href="/admin/technicians" className="text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md text-sm">
                  Technicians
                </Link>
                <Link href="/admin/roles" className="text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md text-sm">
                  Roles
                </Link>
                <Link href="/admin/tags" className="text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md text-sm">
                  Tags
                </Link>
                <Link href="/admin/audit" className="text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md text-sm">
                  Audit Logs
                </Link>
              </>
            )}
            {isResellerAdmin(user?.roles) && !isSuperAdmin(user?.roles) && (
              <>
                <Link href="/verify" className="text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md text-sm font-medium">
                  Verify IMEI
                </Link>
                <Link href="/admin/users" className="text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md text-sm">
                  Users
                </Link>
                <Link href="/admin/technicians" className="text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md text-sm">
                  Technicians
                </Link>
                <Link href="/admin/roles" className="text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md text-sm">
                  Roles
                </Link>
                <Link href="/admin/tags" className="text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md text-sm">
                  Tags
                </Link>
              </>
            )}
            {isSupervisor(user?.roles) && !isSuperAdmin(user?.roles) && !isResellerAdmin(user?.roles) && (
              <>
                <Link href="/verify" className="text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md text-sm font-medium">
                  Verify IMEI
                </Link>
                <Link href="/admin/technicians" className="text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md text-sm">
                  Technicians
                </Link>
              </>
            )}
            {isTechnician(user?.roles) && (
              <>
                <Link href="/verify" className="text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md text-sm">
                  Verify IMEI
                </Link>
                <Link href="/history" className="text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md text-sm">
                  History
                </Link>
              </>
            )}
          </nav>

          {/* User Menu */}
          <div className="flex items-center gap-4">
            {/* User Dropdown */}
            <div className="relative" ref={userDropdownRef}>
              <button
                onClick={() => setUserDropdown(!userDropdown)}
                className="hidden sm:flex items-center gap-2 text-sm text-gray-300 hover:text-white px-3 py-2 rounded-md hover:bg-slate-800"
              >
                <User className="h-4 w-4" />
                <span>{user?.fullName || user?.username}</span>
                <span className="text-xs bg-blue-600 px-2 py-0.5 rounded">
                  {user?.roles?.[0]}
                </span>
                <ChevronDown className="h-4 w-4" />
              </button>

              {userDropdown && (
                <div className="absolute right-0 mt-2 w-48 bg-slate-800 rounded-md shadow-lg py-1 z-50">
                  <button
                    onClick={() => {
                      setChangePasswordOpen(true);
                      setUserDropdown(false);
                    }}
                    className="w-full text-left px-4 py-2 text-sm text-gray-300 hover:bg-slate-700 hover:text-white flex items-center gap-2"
                  >
                    <Lock className="h-4 w-4" />
                    Change Password
                  </button>
                  <hr className="border-slate-700 my-1" />
                  <button
                    onClick={handleLogout}
                    className="w-full text-left px-4 py-2 text-sm text-gray-300 hover:bg-slate-700 hover:text-white flex items-center gap-2"
                  >
                    <LogOut className="h-4 w-4" />
                    Logout
                  </button>
                </div>
              )}
            </div>

            {/* Logout button (shown on mobile) */}
            <Button
              variant="ghost"
              size="icon"
              onClick={handleLogout}
              className="sm:hidden text-gray-300 hover:text-white hover:bg-slate-800"
            >
              <LogOut className="h-5 w-5" />
            </Button>

            {/* Mobile menu button */}
            <Button
              variant="ghost"
              size="icon"
              className="md:hidden text-gray-300 hover:text-white"
              onClick={() => setMenuOpen(!menuOpen)}
            >
              <Menu className="h-5 w-5" />
            </Button>
          </div>
        </div>

        {/* Mobile Navigation */}
        {menuOpen && (
          <nav className="md:hidden pb-4 space-y-1">
            {/* Dashboard link for all users */}
            <Link href="/dashboard" className="block text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md font-medium" onClick={() => setMenuOpen(false)}>
              Dashboard
            </Link>
            {isSuperAdmin(user?.roles) && (
              <>
                <Link href="/verify" className="block text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md font-medium" onClick={() => setMenuOpen(false)}>
                  Verify IMEI
                </Link>
                {adminMenuItems.map((item) => (
                  <Link
                    key={item.href}
                    href={item.href}
                    className="block text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md"
                    onClick={() => setMenuOpen(false)}
                  >
                    {item.label}
                  </Link>
                ))}
              </>
            )}
            {isResellerAdmin(user?.roles) && !isSuperAdmin(user?.roles) && (
              <>
                <Link href="/verify" className="block text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md font-medium" onClick={() => setMenuOpen(false)}>
                  Verify IMEI
                </Link>
                <Link href="/admin/users" className="block text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md" onClick={() => setMenuOpen(false)}>
                  Users
                </Link>
                <Link href="/admin/technicians" className="block text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md" onClick={() => setMenuOpen(false)}>
                  Technicians
                </Link>
                <Link href="/admin/roles" className="block text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md" onClick={() => setMenuOpen(false)}>
                  Roles
                </Link>
                <Link href="/admin/tags" className="block text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md" onClick={() => setMenuOpen(false)}>
                  Tags
                </Link>
              </>
            )}
            {isSupervisor(user?.roles) && !isSuperAdmin(user?.roles) && !isResellerAdmin(user?.roles) && (
              <>
                <Link href="/verify" className="block text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md font-medium" onClick={() => setMenuOpen(false)}>
                  Verify IMEI
                </Link>
                <Link href="/admin/technicians" className="block text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md" onClick={() => setMenuOpen(false)}>
                  Technicians
                </Link>
              </>
            )}
            {isTechnician(user?.roles) && (
              <>
                <Link href="/verify" className="block text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md" onClick={() => setMenuOpen(false)}>
                  Verify IMEI
                </Link>
                <Link href="/history" className="block text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md" onClick={() => setMenuOpen(false)}>
                  History
                </Link>
              </>
            )}
            {/* Mobile Change Password */}
            <hr className="border-slate-700 my-2" />
            <button
              onClick={() => {
                setChangePasswordOpen(true);
                setMenuOpen(false);
              }}
              className="w-full text-left text-gray-300 hover:text-white hover:bg-slate-800 px-3 py-2 rounded-md flex items-center gap-2"
            >
              <Lock className="h-4 w-4" />
              Change Password
            </button>
          </nav>
        )}
      </div>

      {/* Change Password Modal */}
      <ChangePasswordModal
        open={changePasswordOpen}
        onClose={() => setChangePasswordOpen(false)}
      />
    </header>
  );
}

