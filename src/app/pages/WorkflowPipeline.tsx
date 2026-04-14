import { useEffect, useState } from "react";
import { toast } from "sonner";
import { Card, CardContent, CardHeader, CardTitle } from "../components/ui/card";
import { Badge } from "../components/ui/badge";
import { Button } from "../components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "../components/ui/select";
import { Skeleton } from "../components/ui/skeleton";
import { CheckCircle2, Circle, Clock, XCircle, ChevronRight, AlertCircle } from "lucide-react";
import { useApi } from "../hooks/useApi";
import { workflowService } from "../services/workflow.service";
import { caseService } from "../services/case.service";
import type { WorkflowDto } from "../types/api.types";
import type { CaseStatus } from "../types";

export function WorkflowPipeline() {
  const [selectedWorkflowId, setSelectedWorkflowId] = useState<string>("");
  const [selectedStep, setSelectedStep] = useState<string | null>(null);

  const {
    data,
    loading,
    error,
    refetch,
  } = useApi(() => workflowService.getWorkflows(), { immediate: false });

  useEffect(() => {
    refetch().catch(() => undefined);
  }, []);

  const workflows = data?.data || [];
  const selectedWorkflow = workflows.find((workflow) => workflow.id === selectedWorkflowId) || workflows[0];

  useEffect(() => {
    if (!selectedWorkflowId && workflows.length > 0) {
      setSelectedWorkflowId(workflows[0].id);
    }
  }, [selectedWorkflowId, workflows]);

  useEffect(() => {
    if (selectedWorkflow?.steps.length && !selectedStep) {
      setSelectedStep(selectedWorkflow.steps[0].id);
    }
  }, [selectedWorkflow?.id]);

  const selectedStepData = selectedWorkflow?.steps.find((step) => step.id === selectedStep);

  const getStatusIcon = (status: CaseStatus) => {
    switch (status) {
      case "completed":
      case "approved":
        return <CheckCircle2 className="w-6 h-6 text-green-600" />;
      case "in-progress":
        return <Clock className="w-6 h-6 text-blue-600 animate-pulse" />;
      case "rejected":
        return <XCircle className="w-6 h-6 text-red-600" />;
      default:
        return <Circle className="w-6 h-6 text-gray-400" />;
    }
  };

  const getStepColor = (status: CaseStatus) => {
    switch (status) {
      case "completed":
      case "approved":
        return "border-green-600 bg-green-50";
      case "in-progress":
        return "border-blue-600 bg-blue-50";
      case "rejected":
        return "border-red-600 bg-red-50";
      default:
        return "border-gray-300 bg-white";
    }
  };

  const runAction = async (action: () => Promise<unknown>, successMessage: string) => {
    try {
      await action();
      toast.success(successMessage);
      await refetch();
    } catch (actionError) {
      const message = actionError instanceof Error ? actionError.message : "Action failed";
      toast.error(message);
    }
  };

  const handleAdvanceWorkflow = async () => {
    if (!selectedWorkflow) {
      return;
    }

    const notes = window.prompt("Workflow note", "Advanced to the next workflow step.") || undefined;
    await runAction(() => workflowService.advanceWorkflow(selectedWorkflow.id, notes), "Workflow advanced.");
  };

  const handleReassign = async () => {
    if (!selectedWorkflow) {
      return;
    }

    const assignTo = window.prompt("Assign this workflow to");
    if (!assignTo) {
      return;
    }

    await runAction(() => caseService.assignCase(selectedWorkflow.caseId, assignTo), "Case reassigned.");
  };

  const handleRequestInfo = async () => {
    if (!selectedWorkflow) {
      return;
    }

    const message = window.prompt("Request additional information", "Please upload the missing documentation.");
    if (!message) {
      return;
    }

    await runAction(() => caseService.requestInfo(selectedWorkflow.caseId, message), "Information request sent.");
  };

  const handleReject = async () => {
    if (!selectedWorkflow) {
      return;
    }

    const reason = window.prompt("Reason for rejection");
    if (!reason) {
      return;
    }

    await runAction(() => caseService.rejectCase(selectedWorkflow.caseId, reason), "Case rejected.");
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        <div>
          <h1 className="text-3xl font-semibold text-gray-900">
            Workflow Pipeline
          </h1>
          <p className="text-gray-500 mt-1">
            Visual representation of service workflow stages
          </p>
        </div>
        <div className="w-full md:w-[340px]">
          <Select
            value={selectedWorkflow?.id || ""}
            onValueChange={(value) => {
              setSelectedWorkflowId(value);
              setSelectedStep(null);
            }}
          >
            <SelectTrigger>
              <SelectValue placeholder="Select a workflow" />
            </SelectTrigger>
            <SelectContent>
              {workflows.map((workflow) => (
                <SelectItem key={workflow.id} value={workflow.id}>
                  {workflow.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      {error ? (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <div className="flex items-center gap-2 text-red-800">
            <AlertCircle className="w-5 h-5" />
            <p className="font-medium">Error loading workflows</p>
          </div>
          <p className="text-sm text-red-600 mt-1">{error}</p>
          <Button onClick={() => refetch().catch(() => undefined)} variant="outline" size="sm" className="mt-3">
            Retry
          </Button>
        </div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2">
            <Card className="border-gray-200 shadow-sm">
              <CardHeader>
                {loading ? (
                  <>
                    <Skeleton className="h-6 w-72" />
                    <Skeleton className="h-4 w-56 mt-2" />
                  </>
                ) : selectedWorkflow ? (
                  <>
                    <CardTitle>{selectedWorkflow.name}</CardTitle>
                    <p className="text-sm text-gray-500">
                      Case: {selectedWorkflow.caseId} • Updated{" "}
                      {new Date(selectedWorkflow.updatedAt).toLocaleString()}
                    </p>
                  </>
                ) : (
                  <>
                    <CardTitle>No workflows available</CardTitle>
                    <p className="text-sm text-gray-500">
                      Create a case to generate the first workflow.
                    </p>
                  </>
                )}
              </CardHeader>
              <CardContent>
                {loading ? (
                  <div className="space-y-4">
                    {[...Array(4)].map((_, index) => (
                      <Skeleton key={index} className="h-24 w-full" />
                    ))}
                  </div>
                ) : selectedWorkflow ? (
                  <>
                    <div className="space-y-4">
                      {selectedWorkflow.steps.map((step, index) => (
                        <div key={step.id}>
                          <div
                            className={`relative flex items-center gap-4 p-4 rounded-lg border-2 cursor-pointer transition-all hover:shadow-md ${getStepColor(
                              step.status as CaseStatus
                            )} ${selectedStep === step.id ? "ring-2 ring-blue-600 ring-offset-2" : ""}`}
                            onClick={() => setSelectedStep(step.id)}
                          >
                            <div className="flex-shrink-0">
                              {getStatusIcon(step.status as CaseStatus)}
                            </div>

                            <div className="flex-1">
                              <div className="flex items-center justify-between">
                                <h3 className="font-semibold text-gray-900">
                                  {step.name}
                                </h3>
                                <Badge
                                  variant="outline"
                                  className={`capitalize ${
                                    step.status === "completed" || step.status === "approved"
                                      ? "bg-green-100 text-green-800 border-green-300"
                                      : step.status === "in-progress"
                                        ? "bg-blue-100 text-blue-800 border-blue-300"
                                        : step.status === "rejected"
                                          ? "bg-red-100 text-red-800 border-red-300"
                                          : "bg-gray-100 text-gray-800 border-gray-300"
                                  }`}
                                >
                                  {step.status.replace("-", " ")}
                                </Badge>
                              </div>
                              <p className="text-sm text-gray-600 mt-1">
                                {step.description}
                              </p>
                              {step.assignedTo && (
                                <p className="text-sm text-gray-500 mt-2">
                                  <span className="font-medium">Assigned to:</span>{" "}
                                  {step.assignedTo}
                                </p>
                              )}
                              {step.completedAt && (
                                <p className="text-sm text-gray-500">
                                  <span className="font-medium">Completed:</span>{" "}
                                  {new Date(step.completedAt).toLocaleString()}
                                </p>
                              )}
                            </div>

                            <ChevronRight className="w-5 h-5 text-gray-400" />
                          </div>

                          {index < selectedWorkflow.steps.length - 1 && (
                            <div className="flex justify-center py-2">
                              <div
                                className={`w-0.5 h-8 ${
                                  step.status === "completed" || step.status === "approved"
                                    ? "bg-green-600"
                                    : "bg-gray-300"
                                }`}
                              ></div>
                            </div>
                          )}
                        </div>
                      ))}
                    </div>

                    <div className="mt-6 pt-6 border-t border-gray-200 flex flex-wrap gap-3">
                      <Button className="bg-blue-600 hover:bg-blue-700" onClick={handleAdvanceWorkflow}>
                        Advance Stage
                      </Button>
                      <Button variant="outline" onClick={handleReassign}>
                        Reassign
                      </Button>
                      <Button variant="outline" className="text-amber-600 border-amber-300" onClick={handleRequestInfo}>
                        Request Info
                      </Button>
                      <Button variant="outline" className="text-red-600 border-red-300" onClick={handleReject}>
                        Reject
                      </Button>
                    </div>
                  </>
                ) : (
                  <div className="text-center py-8 text-gray-500">
                    No workflow data is available yet.
                  </div>
                )}
              </CardContent>
            </Card>
          </div>

          <div className="lg:col-span-1">
            <Card className="border-gray-200 shadow-sm sticky top-6">
              <CardHeader>
                <CardTitle>Step Details</CardTitle>
              </CardHeader>
              <CardContent>
                {selectedStepData ? (
                  <div className="space-y-4">
                    <div>
                      <label className="text-sm font-medium text-gray-600">
                        Step Name
                      </label>
                      <p className="text-gray-900 mt-1">
                        {selectedStepData.name}
                      </p>
                    </div>

                    <div>
                      <label className="text-sm font-medium text-gray-600">
                        Status
                      </label>
                      <div className="mt-1">
                        <Badge
                          variant="outline"
                          className={`capitalize ${
                            selectedStepData.status === "completed" || selectedStepData.status === "approved"
                              ? "bg-green-100 text-green-800 border-green-300"
                              : selectedStepData.status === "in-progress"
                                ? "bg-blue-100 text-blue-800 border-blue-300"
                                : selectedStepData.status === "rejected"
                                  ? "bg-red-100 text-red-800 border-red-300"
                                  : "bg-gray-100 text-gray-800 border-gray-300"
                          }`}
                        >
                          {selectedStepData.status.replace("-", " ")}
                        </Badge>
                      </div>
                    </div>

                    <div>
                      <label className="text-sm font-medium text-gray-600">
                        Description
                      </label>
                      <p className="text-gray-900 mt-1">
                        {selectedStepData.description}
                      </p>
                    </div>

                    {selectedStepData.assignedTo && (
                      <div>
                        <label className="text-sm font-medium text-gray-600">
                          Assigned To
                        </label>
                        <p className="text-gray-900 mt-1">
                          {selectedStepData.assignedTo}
                        </p>
                      </div>
                    )}

                    {selectedStepData.completedAt && (
                      <div>
                        <label className="text-sm font-medium text-gray-600">
                          Completed At
                        </label>
                        <p className="text-gray-900 mt-1">
                          {new Date(selectedStepData.completedAt).toLocaleString()}
                        </p>
                      </div>
                    )}

                    <div className="pt-4 border-t border-gray-200">
                      <h4 className="text-sm font-semibold text-gray-900 mb-2">
                        Available Actions
                      </h4>
                      <div className="space-y-2">
                        <Button size="sm" variant="outline" className="w-full" onClick={handleAdvanceWorkflow}>
                          Advance Workflow
                        </Button>
                        <Button size="sm" variant="outline" className="w-full" onClick={handleReassign}>
                          Reassign Owner
                        </Button>
                        <Button size="sm" variant="outline" className="w-full" onClick={handleRequestInfo}>
                          Request Information
                        </Button>
                      </div>
                    </div>
                  </div>
                ) : (
                  <div className="text-center py-8 text-gray-500">
                    <Circle className="w-12 h-12 mx-auto mb-3 text-gray-300" />
                    <p className="text-sm">
                      Select a workflow step to view details
                    </p>
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        </div>
      )}
    </div>
  );
}
