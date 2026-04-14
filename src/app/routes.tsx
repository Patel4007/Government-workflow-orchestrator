import { createBrowserRouter } from "react-router";
import { Layout } from "./components/Layout";
import { Dashboard } from "./pages/Dashboard";
import { WorkflowPipeline } from "./pages/WorkflowPipeline";
import { CaseManagement } from "./pages/CaseManagement";
import { CaseDetail } from "./pages/CaseDetail";
import { RuleEngine } from "./pages/RuleEngine";
import { Reports } from "./pages/Reports";
import { Settings } from "./pages/Settings";

export const router = createBrowserRouter([
  {
    path: "/",
    Component: Layout,
    children: [
      { index: true, Component: Dashboard },
      { path: "workflows", Component: WorkflowPipeline },
      { path: "cases", Component: CaseManagement },
      { path: "cases/:id", Component: CaseDetail },
      { path: "rules", Component: RuleEngine },
      { path: "reports", Component: Reports },
      { path: "settings", Component: Settings },
    ],
  },
]);
