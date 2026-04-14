import { Link, useLocation } from "react-router";
import {
  LayoutDashboard,
  GitBranch,
  FolderKanban,
  Settings,
  FileText,
  Workflow,
} from "lucide-react";

const navItems = [
  { icon: LayoutDashboard, label: "Dashboard", path: "/" },
  { icon: GitBranch, label: "Workflows", path: "/workflows" },
  { icon: FolderKanban, label: "Cases", path: "/cases" },
  { icon: Workflow, label: "Rules Engine", path: "/rules" },
  { icon: FileText, label: "Reports", path: "/reports" },
  { icon: Settings, label: "Settings", path: "/settings" },
];

export function Sidebar() {
  const location = useLocation();

  return (
    <aside className="w-64 bg-[#1e3a5f] text-white flex flex-col">
      <div className="p-6 border-b border-white/10">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 bg-blue-500 rounded-lg flex items-center justify-center">
            <Workflow className="w-6 h-6" />
          </div>
          <div>
            <h1 className="font-semibold text-lg">GovFlow</h1>
            <p className="text-xs text-blue-200">Services Orchestrator</p>
          </div>
        </div>
      </div>

      <nav className="flex-1 p-4 space-y-1">
        {navItems.map((item) => {
          const Icon = item.icon;
          const isActive = location.pathname === item.path;

          return (
            <Link
              key={item.path}
              to={item.path}
              className={`flex items-center gap-3 px-4 py-3 rounded-lg transition-colors ${
                isActive
                  ? "bg-blue-600 text-white"
                  : "text-blue-100 hover:bg-white/10"
              }`}
            >
              <Icon className="w-5 h-5" />
              <span className="font-medium">{item.label}</span>
            </Link>
          );
        })}
      </nav>

      <div className="p-4 border-t border-white/10">
        <div className="bg-blue-900/50 rounded-lg p-4">
          <p className="text-sm text-blue-200 mb-2">System Status</p>
          <div className="flex items-center gap-2">
            <div className="w-2 h-2 bg-green-400 rounded-full animate-pulse"></div>
            <span className="text-sm">All Systems Operational</span>
          </div>
        </div>
      </div>
    </aside>
  );
}
