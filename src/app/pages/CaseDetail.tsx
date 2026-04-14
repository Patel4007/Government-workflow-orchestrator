import { useEffect } from "react";
import { useParams, Link } from "react-router";
import { toast } from "sonner";
import { Card, CardContent, CardHeader, CardTitle } from "../components/ui/card";
import { Button } from "../components/ui/button";
import { Separator } from "../components/ui/separator";
import { StatusBadge } from "../components/StatusBadge";
import { Badge } from "../components/ui/badge";
import { Skeleton } from "../components/ui/skeleton";
import {
  ArrowLeft,
  CheckCircle2,
  XCircle,
  AlertCircle,
  FileText,
  Clock,
  User,
} from "lucide-react";
import { useApi } from "../hooks/useApi";
import { caseService } from "../services/case.service";
import {
  getPriorityColor,
  getServiceTypeLabel,
  formatDateTime,
} from "../utils/helpers";
import type { CaseStatus, ServiceType } from "../types";

export function CaseDetail() {
  const { id = "" } = useParams();
  const {
    data,
    loading,
    error,
    refetch,
  } = useApi(() => caseService.getCaseById(id), { immediate: false });

  useEffect(() => {
    if (id) {
      refetch().catch(() => undefined);
    }
  }, [id]);

  const caseData = data?.data;

  const withRefresh = async (action: () => Promise<unknown>, successMessage: string) => {
    try {
      await action();
      toast.success(successMessage);
      await refetch();
    } catch (actionError) {
      const message = actionError instanceof Error ? actionError.message : "Action failed";
      toast.error(message);
    }
  };

  const handleApprove = async () => {
    if (!id) {
      return;
    }

    const notes = window.prompt("Approval notes", "Approved after verification review.") || undefined;
    await withRefresh(() => caseService.approveCase(id, notes), "Case approved.");
  };

  const handleReject = async () => {
    if (!id) {
      return;
    }

    const reason = window.prompt("Reason for rejection");
    if (!reason) {
      return;
    }

    await withRefresh(() => caseService.rejectCase(id, reason), "Case rejected.");
  };

  const handleRequestInfo = async () => {
    if (!id) {
      return;
    }

    const message = window.prompt("What information do you need?", "Please upload the missing supporting documents.");
    if (!message) {
      return;
    }

    await withRefresh(() => caseService.requestInfo(id, message), "Information request sent.");
  };

  const handleReassign = async () => {
    if (!id) {
      return;
    }

    const assignTo = window.prompt("Assign this case to");
    if (!assignTo) {
      return;
    }

    await withRefresh(() => caseService.assignCase(id, assignTo), "Case reassigned.");
  };

  const handleAddNote = async () => {
    if (!id) {
      return;
    }

    const notes = window.prompt("Add a case note");
    if (!notes) {
      return;
    }

    await withRefresh(() => caseService.updateCase(id, { notes }), "Note added.");
  };

  const handleFlagForReview = async () => {
    if (!id) {
      return;
    }

    await withRefresh(
      () => caseService.updateCase(id, { priority: "high", notes: "Flagged for expedited review." }),
      "Case flagged for review."
    );
  };

  if (loading) {
    return (
      <div className="p-6 space-y-6">
        <Skeleton className="h-12 w-80" />
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2 space-y-6">
            <Skeleton className="h-64 w-full" />
            <Skeleton className="h-72 w-full" />
            <Skeleton className="h-72 w-full" />
          </div>
          <div className="space-y-6">
            <Skeleton className="h-64 w-full" />
            <Skeleton className="h-48 w-full" />
            <Skeleton className="h-44 w-full" />
          </div>
        </div>
      </div>
    );
  }

  if (error || !caseData) {
    return (
      <div className="p-6">
        <div className="max-w-2xl mx-auto text-center py-12">
          <AlertCircle className="w-16 h-16 text-gray-400 mx-auto mb-4" />
          <h2 className="text-2xl font-semibold text-gray-900 mb-2">
            Case Not Found
          </h2>
          <p className="text-gray-500 mb-6">
            {error || "The case you're looking for doesn't exist or has been removed."}
          </p>
          <Link to="/cases">
            <Button variant="outline">
              <ArrowLeft className="w-4 h-4 mr-2" />
              Back to Cases
            </Button>
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center gap-4">
        <Link to="/cases">
          <Button variant="ghost" size="icon">
            <ArrowLeft className="w-5 h-5" />
          </Button>
        </Link>
        <div className="flex-1">
          <div className="flex items-center gap-3">
            <h1 className="text-3xl font-semibold text-gray-900">
              {caseData.id}
            </h1>
            <StatusBadge status={caseData.status as CaseStatus} />
            <Badge
              variant="outline"
              className={`${getPriorityColor(caseData.priority as any)} capitalize`}
            >
              {caseData.priority} priority
            </Badge>
          </div>
          <p className="text-gray-500 mt-1">
            {getServiceTypeLabel(caseData.serviceType as ServiceType)} •{" "}
            {caseData.applicantName}
          </p>
        </div>
        <div className="flex gap-2">
          <Button className="bg-green-600 hover:bg-green-700" onClick={handleApprove}>
            <CheckCircle2 className="w-4 h-4 mr-2" />
            Approve
          </Button>
          <Button variant="outline" className="border-red-300 text-red-600 hover:bg-red-50" onClick={handleReject}>
            <XCircle className="w-4 h-4 mr-2" />
            Reject
          </Button>
          <Button variant="outline" onClick={handleRequestInfo}>
            <AlertCircle className="w-4 h-4 mr-2" />
            Request Info
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 space-y-6">
          <Card className="border-gray-200 shadow-sm">
            <CardHeader>
              <CardTitle>Case Information</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm font-medium text-gray-600">Case ID</label>
                  <p className="text-gray-900 font-mono mt-1">{caseData.id}</p>
                </div>
                <div>
                  <label className="text-sm font-medium text-gray-600">Applicant Name</label>
                  <p className="text-gray-900 mt-1">{caseData.applicantName}</p>
                </div>
                <div>
                  <label className="text-sm font-medium text-gray-600">Service Type</label>
                  <p className="text-gray-900 mt-1">
                    {getServiceTypeLabel(caseData.serviceType as ServiceType)}
                  </p>
                </div>
                <div>
                  <label className="text-sm font-medium text-gray-600">Current Stage</label>
                  <p className="text-gray-900 mt-1 capitalize">
                    {caseData.currentStage}
                  </p>
                </div>
                <div>
                  <label className="text-sm font-medium text-gray-600">Submitted Date</label>
                  <p className="text-gray-900 mt-1">
                    {formatDateTime(caseData.submittedDate)}
                  </p>
                </div>
                <div>
                  <label className="text-sm font-medium text-gray-600">Last Updated</label>
                  <p className="text-gray-900 mt-1">
                    {formatDateTime(caseData.lastUpdated)}
                  </p>
                </div>
              </div>

              {caseData.assignedTo && (
                <div className="pt-4 border-t border-gray-200">
                  <label className="text-sm font-medium text-gray-600">Assigned To</label>
                  <div className="flex items-center gap-2 mt-2">
                    <div className="w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center">
                      <User className="w-4 h-4 text-blue-600" />
                    </div>
                    <span className="text-gray-900">{caseData.assignedTo}</span>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>

          <Card className="border-gray-200 shadow-sm">
            <CardHeader>
              <CardTitle>Rule Engine Analysis</CardTitle>
              <p className="text-sm text-gray-500">
                Automated verification and routing decisions for this case
              </p>
            </CardHeader>
            <CardContent className="space-y-4">
              {caseData.ruleEngineOutput.length === 0 ? (
                <div className="bg-gray-50 border border-gray-200 rounded-lg p-4 text-sm text-gray-600">
                  No automation output is available yet for this case.
                </div>
              ) : (
                caseData.ruleEngineOutput.map((output) => {
                  const toneClasses = output.ruleId === "verification-bot"
                    ? "bg-blue-50 border-blue-200 text-blue-900"
                    : output.matched
                      ? "bg-green-50 border-green-200 text-green-900"
                      : "bg-amber-50 border-amber-200 text-amber-900";

                  return (
                    <div key={`${output.ruleId}-${output.timestamp}`} className={`border rounded-lg p-4 ${toneClasses}`}>
                      <div className="flex items-start gap-3">
                        {output.matched ? (
                          <CheckCircle2 className="w-5 h-5 mt-0.5" />
                        ) : (
                          <AlertCircle className="w-5 h-5 mt-0.5" />
                        )}
                        <div className="space-y-2">
                          <div>
                            <p className="font-medium">{output.ruleName}</p>
                            <p className="text-xs opacity-70 mt-1">
                              {formatDateTime(output.timestamp)}
                            </p>
                          </div>
                          {output.executedActions.map((action, index) => (
                            <p key={index} className="text-sm opacity-90">
                              {action}
                            </p>
                          ))}
                        </div>
                      </div>
                    </div>
                  );
                })
              )}
            </CardContent>
          </Card>

          <Card className="border-gray-200 shadow-sm">
            <CardHeader>
              <CardTitle>Activity Timeline</CardTitle>
              <p className="text-sm text-gray-500">
                Complete history of case events
              </p>
            </CardHeader>
            <CardContent>
              <div className="space-y-6">
                {caseData.timeline.map((event, index) => (
                  <div key={event.id} className="flex gap-4">
                    <div className="flex flex-col items-center">
                      <div className="w-10 h-10 bg-blue-100 rounded-full flex items-center justify-center">
                        <Clock className="w-5 h-5 text-blue-600" />
                      </div>
                      {index < caseData.timeline.length - 1 && (
                        <div className="w-0.5 flex-1 bg-gray-200 my-2"></div>
                      )}
                    </div>
                    <div className="flex-1 pb-6">
                      <div className="flex items-center justify-between mb-1">
                        <h4 className="font-semibold text-gray-900">{event.action}</h4>
                        <span className="text-sm text-gray-500">
                          {formatDateTime(event.timestamp)}
                        </span>
                      </div>
                      <p className="text-sm text-gray-600 mb-1">{event.details}</p>
                      <p className="text-xs text-gray-500">By {event.user}</p>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>

        <div className="lg:col-span-1 space-y-6">
          <Card className="border-gray-200 shadow-sm">
            <CardHeader>
              <CardTitle>Documents</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {caseData.documents.map((document) => (
                  <div
                    key={document.id}
                    className="flex items-start gap-3 p-3 bg-gray-50 rounded-lg hover:bg-gray-100 transition-colors"
                  >
                    <FileText className="w-5 h-5 text-gray-600 mt-0.5" />
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-gray-900 truncate">
                        {document.name}
                      </p>
                      <p className="text-xs text-gray-500 mt-1">
                        {formatDateTime(document.uploadedAt)}
                      </p>
                    </div>
                    <Badge
                      variant="outline"
                      className={`text-xs ${
                        document.status === "verified"
                          ? "bg-green-100 text-green-800 border-green-300"
                          : document.status === "pending"
                            ? "bg-amber-100 text-amber-800 border-amber-300"
                            : "bg-red-100 text-red-800 border-red-300"
                      }`}
                    >
                      {document.status}
                    </Badge>
                  </div>
                ))}
              </div>
              <Separator className="my-4" />
              <Button
                variant="outline"
                className="w-full"
                onClick={() => toast.message("Document upload UI is not wired yet, but the backend is ready for future integration.")}
              >
                <FileText className="w-4 h-4 mr-2" />
                Upload Document
              </Button>
            </CardContent>
          </Card>

          <Card className="border-gray-200 shadow-sm">
            <CardHeader>
              <CardTitle>Quick Actions</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              <Button variant="outline" className="w-full justify-start" onClick={handleReassign}>
                <User className="w-4 h-4 mr-2" />
                Reassign Case
              </Button>
              <Button
                variant="outline"
                className="w-full justify-start"
                onClick={() => toast.message("Deadline extensions can be added next if you want a full scheduling workflow.")}
              >
                <Clock className="w-4 h-4 mr-2" />
                Extend Deadline
              </Button>
              <Button variant="outline" className="w-full justify-start" onClick={handleAddNote}>
                <FileText className="w-4 h-4 mr-2" />
                Add Note
              </Button>
              <Button variant="outline" className="w-full justify-start" onClick={handleFlagForReview}>
                <AlertCircle className="w-4 h-4 mr-2" />
                Flag for Review
              </Button>
            </CardContent>
          </Card>

          <Card className="border-gray-200 shadow-sm">
            <CardHeader>
              <CardTitle>Case Metrics</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label className="text-sm text-gray-600">Time Elapsed</label>
                <p className="text-2xl font-semibold text-gray-900 mt-1">
                  {caseData.metrics.timeElapsed} days
                </p>
              </div>
              <Separator />
              <div>
                <label className="text-sm text-gray-600">SLA Status</label>
                <div className="flex items-center gap-2 mt-1">
                  <CheckCircle2 className={`w-4 h-4 ${caseData.metrics.slaStatus === "Within SLA" ? "text-green-600" : "text-red-600"}`} />
                  <span className={`text-sm font-medium ${caseData.metrics.slaStatus === "Within SLA" ? "text-green-600" : "text-red-600"}`}>
                    {caseData.metrics.slaStatus}
                    {typeof caseData.metrics.daysRemaining === "number" ? ` (${caseData.metrics.daysRemaining} days remaining)` : ""}
                  </span>
                </div>
              </div>
              <Separator />
              <div>
                <label className="text-sm text-gray-600">Automation Score</label>
                <p className="text-2xl font-semibold text-gray-900 mt-1">
                  {caseData.metrics.automationScore}%
                </p>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
