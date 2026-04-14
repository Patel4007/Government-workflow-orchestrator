import { useEffect, useState } from "react";
import { Link } from "react-router";
import { toast } from "sonner";
import { Card, CardContent, CardHeader } from "../components/ui/card";
import { Button } from "../components/ui/button";
import { Input } from "../components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "../components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "../components/ui/table";
import { StatusBadge } from "../components/StatusBadge";
import { Badge } from "../components/ui/badge";
import { Skeleton } from "../components/ui/skeleton";
import { Search, Filter, Download, ChevronRight, AlertCircle } from "lucide-react";
import { useApi } from "../hooks/useApi";
import { caseService } from "../services/case.service";
import { getPriorityColor, getServiceTypeLabel, formatDate } from "../utils/helpers";
import type { CaseStatus, ServiceType } from "../types";

const PAGE_SIZE = 10;

export function CaseManagement() {
  const [statusFilter, setStatusFilter] = useState<string>("all");
  const [serviceTypeFilter, setServiceTypeFilter] = useState<string>("all");
  const [draftSearchQuery, setDraftSearchQuery] = useState("");
  const [searchQuery, setSearchQuery] = useState("");
  const [page, setPage] = useState(1);

  const {
    data: casesData,
    loading,
    error,
    refetch,
  } = useApi(
    () =>
      caseService.getCases({
        status: statusFilter !== "all" ? statusFilter : undefined,
        serviceType: serviceTypeFilter !== "all" ? serviceTypeFilter : undefined,
        searchQuery: searchQuery || undefined,
        pageNumber: page,
        pageSize: PAGE_SIZE,
      }),
    { immediate: false }
  );

  useEffect(() => {
    refetch().catch(() => undefined);
  }, [statusFilter, serviceTypeFilter, searchQuery, page]);

  const cases = casesData?.items || [];
  const totalPages = casesData?.totalPages || 1;

  const handleCreateCase = async () => {
    const applicantName = window.prompt("Applicant name");
    if (!applicantName) {
      return;
    }

    const serviceType =
      window.prompt(
        "Service type: tax-filing, benefit-approval, document-verification, or license-renewal",
        "document-verification"
      ) || "";

    const priority =
      window.prompt("Priority: low, medium, high, or critical", "medium") || "";

    try {
      await caseService.createCase({
        applicantName,
        serviceType,
        priority,
      });
      toast.success("Case created.");
      setPage(1);
      refetch().catch(() => undefined);
    } catch (createError) {
      const message = createError instanceof Error ? createError.message : "Unable to create case";
      toast.error(message);
    }
  };

  const handleExportCases = () => {
    if (cases.length === 0) {
      toast.error("There are no cases to export.");
      return;
    }

    const header = [
      "Case ID",
      "Applicant",
      "Service Type",
      "Status",
      "Priority",
      "Submitted Date",
      "Assigned To",
    ];
    const rows = cases.map((caseItem) => [
      caseItem.id,
      caseItem.applicantName,
      caseItem.serviceType,
      caseItem.status,
      caseItem.priority,
      caseItem.submittedDate,
      caseItem.assignedTo || "",
    ]);

    const csv = [header, ...rows]
      .map((row) => row.map((value) => `"${String(value).replaceAll('"', '""')}"`).join(","))
      .join("\n");

    const blob = new Blob([csv], { type: "text/csv;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = "government-services-cases.csv";
    link.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-semibold text-gray-900">
            Case Management
          </h1>
          <p className="text-gray-500 mt-1">
            Manage and track all service requests
          </p>
        </div>
        <Button className="bg-blue-600 hover:bg-blue-700" onClick={handleCreateCase}>
          + New Case
        </Button>
      </div>

      <Card className="border-gray-200 shadow-sm">
        <CardHeader>
          <div className="flex flex-col lg:flex-row lg:items-center gap-4">
            <div className="flex-1 flex gap-2">
              <div className="flex-1 relative">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                <Input
                  placeholder="Search by case ID or applicant name..."
                  value={draftSearchQuery}
                  onChange={(event) => setDraftSearchQuery(event.target.value)}
                  onKeyDown={(event) => {
                    if (event.key === "Enter") {
                      setPage(1);
                      setSearchQuery(draftSearchQuery.trim());
                    }
                  }}
                  className="pl-10"
                />
              </div>
              <Button
                variant="outline"
                onClick={() => {
                  setPage(1);
                  setSearchQuery(draftSearchQuery.trim());
                }}
              >
                Search
              </Button>
            </div>

            <div className="flex gap-3">
              <Select
                value={statusFilter}
                onValueChange={(value) => {
                  setStatusFilter(value);
                  setPage(1);
                }}
              >
                <SelectTrigger className="w-[180px]">
                  <Filter className="w-4 h-4 mr-2" />
                  <SelectValue placeholder="Filter by status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Statuses</SelectItem>
                  <SelectItem value="pending">Pending</SelectItem>
                  <SelectItem value="in-progress">In Progress</SelectItem>
                  <SelectItem value="approved">Approved</SelectItem>
                  <SelectItem value="rejected">Rejected</SelectItem>
                  <SelectItem value="completed">Completed</SelectItem>
                </SelectContent>
              </Select>

              <Select
                value={serviceTypeFilter}
                onValueChange={(value) => {
                  setServiceTypeFilter(value);
                  setPage(1);
                }}
              >
                <SelectTrigger className="w-[200px]">
                  <Filter className="w-4 h-4 mr-2" />
                  <SelectValue placeholder="Filter by service" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Services</SelectItem>
                  <SelectItem value="tax-filing">Tax Filing</SelectItem>
                  <SelectItem value="benefit-approval">Benefit Approval</SelectItem>
                  <SelectItem value="document-verification">Document Verification</SelectItem>
                  <SelectItem value="license-renewal">License Renewal</SelectItem>
                </SelectContent>
              </Select>

              <Button variant="outline" size="icon" onClick={handleExportCases}>
                <Download className="w-4 h-4" />
              </Button>
            </div>
          </div>
        </CardHeader>

        <CardContent>
          {error ? (
            <div className="bg-red-50 border border-red-200 rounded-lg p-4">
              <div className="flex items-center gap-2 text-red-800">
                <AlertCircle className="w-5 h-5" />
                <p className="font-medium">Error loading cases</p>
              </div>
              <p className="text-sm text-red-600 mt-1">{error}</p>
              <Button onClick={() => refetch().catch(() => undefined)} variant="outline" size="sm" className="mt-3">
                Retry
              </Button>
            </div>
          ) : (
            <>
              <div className="rounded-lg border border-gray-200 overflow-hidden">
                <Table>
                  <TableHeader>
                    <TableRow className="bg-gray-50">
                      <TableHead className="font-semibold">Case ID</TableHead>
                      <TableHead className="font-semibold">Applicant</TableHead>
                      <TableHead className="font-semibold">Service Type</TableHead>
                      <TableHead className="font-semibold">Status</TableHead>
                      <TableHead className="font-semibold">Priority</TableHead>
                      <TableHead className="font-semibold">Submitted</TableHead>
                      <TableHead className="font-semibold">Assigned To</TableHead>
                      <TableHead className="text-right font-semibold">
                        Actions
                      </TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {loading ? (
                      [...Array(5)].map((_, index) => (
                        <TableRow key={index}>
                          <TableCell><Skeleton className="h-4 w-28" /></TableCell>
                          <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                          <TableCell><Skeleton className="h-4 w-36" /></TableCell>
                          <TableCell><Skeleton className="h-6 w-20" /></TableCell>
                          <TableCell><Skeleton className="h-6 w-16" /></TableCell>
                          <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                          <TableCell><Skeleton className="h-4 w-28" /></TableCell>
                          <TableCell><Skeleton className="h-8 w-16 ml-auto" /></TableCell>
                        </TableRow>
                      ))
                    ) : cases.length === 0 ? (
                      <TableRow>
                        <TableCell colSpan={8} className="text-center py-8 text-gray-500">
                          No cases found matching your filters
                        </TableCell>
                      </TableRow>
                    ) : (
                      cases.map((caseItem) => (
                        <TableRow key={caseItem.id} className="hover:bg-gray-50 transition-colors">
                          <TableCell className="font-mono text-sm font-medium">
                            {caseItem.id}
                          </TableCell>
                          <TableCell className="font-medium">
                            {caseItem.applicantName}
                          </TableCell>
                          <TableCell>
                            {getServiceTypeLabel(caseItem.serviceType as ServiceType)}
                          </TableCell>
                          <TableCell>
                            <StatusBadge status={caseItem.status as CaseStatus} />
                          </TableCell>
                          <TableCell>
                            <Badge
                              variant="outline"
                              className={`${getPriorityColor(caseItem.priority as any)} capitalize`}
                            >
                              {caseItem.priority}
                            </Badge>
                          </TableCell>
                          <TableCell className="text-gray-600">
                            {formatDate(caseItem.submittedDate)}
                          </TableCell>
                          <TableCell className="text-gray-600">
                            {caseItem.assignedTo || (
                              <span className="text-gray-400">Unassigned</span>
                            )}
                          </TableCell>
                          <TableCell className="text-right">
                            <Link to={`/cases/${caseItem.id}`}>
                              <Button variant="ghost" size="sm">
                                View
                                <ChevronRight className="w-4 h-4 ml-1" />
                              </Button>
                            </Link>
                          </TableCell>
                        </TableRow>
                      ))
                    )}
                  </TableBody>
                </Table>
              </div>

              <div className="mt-4 flex items-center justify-between text-sm text-gray-600">
                <p>
                  Showing {cases.length} {casesData?.totalCount ? `of ${casesData.totalCount}` : ""} cases
                </p>
                <div className="flex items-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={page <= 1}
                    onClick={() => setPage((currentPage) => Math.max(currentPage - 1, 1))}
                  >
                    Previous
                  </Button>
                  <span>
                    Page {page} of {totalPages}
                  </span>
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={page >= totalPages}
                    onClick={() => setPage((currentPage) => Math.min(currentPage + 1, totalPages))}
                  >
                    Next
                  </Button>
                </div>
              </div>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
